// ClaudeSteamReader.cs

using System.Text;
using Anthropic.Models.Messages;
using MyAgent.Backend.Chat;
using Block = MyAgent.Backend.Core.Block;
using MyAgent.Backend.Conversation;

namespace MyAgent.Backend.Provider.Claude;

public sealed class ClaudeStreamReader
{
	private abstract record ActiveBlock
	{
		public sealed record Text : ActiveBlock;

		public sealed record Thinking : ActiveBlock;
		
	}

	private ActiveBlock? CurrentBlock;

	private readonly StringBuilder TextBuffer = new StringBuilder();

	private readonly StringBuilder ThinkingBuffer = new StringBuilder();

	private string? ThinkingSignature;
	
	public StopReason? FinalStopReason;

	private readonly List<Block> AssistantBlocks = new List<Block>();
	
	public IReadOnlyList<Block> Blocks => AssistantBlocks;

	public ChatEvent? Process(RawMessageStreamEvent RawEvent)
	{
		if (RawEvent.TryPickStart(out RawMessageStartEvent? MessageStartEvent))
			return ProcessMessageStart(MessageStartEvent);

		if (RawEvent.TryPickContentBlockStart(out RawContentBlockStartEvent? BlockStartEvent))
			return ProcessBlockStart(BlockStartEvent);

		if (RawEvent.TryPickContentBlockDelta(out RawContentBlockDeltaEvent? BlockDeltaEvent))
			return ProcessBlockDelta(BlockDeltaEvent);

		bool bIsBlockStop = RawEvent.TryPickContentBlockStop(out RawContentBlockStopEvent? BlockStopEvent);
		if (bIsBlockStop)
			return ProcessBlockStop();

		if (RawEvent.TryPickDelta(out RawMessageDeltaEvent? MessageDeltaEvent))
			return ProcessMessageDelta(MessageDeltaEvent);

		return null;
	}

	private ChatEvent? ProcessMessageStart(RawMessageStartEvent MessageStartEvent)
	{
		return null;
	}

	private ChatEvent? ProcessBlockStart(RawContentBlockStartEvent BlockStartEvent)
	{
		bool bIsTextBlock = BlockStartEvent.ContentBlock.TryPickText(out _);
		if (bIsTextBlock)
			CurrentBlock = new ActiveBlock.Text();
		else
		{
			bool bIsThinkingBlock = BlockStartEvent.ContentBlock.TryPickThinking(out _);
			if (bIsThinkingBlock)
				CurrentBlock = new ActiveBlock.Thinking();
		}

		return null;
	}

	private ChatEvent? ProcessBlockDelta(RawContentBlockDeltaEvent BlockDeltaEvent)
	{
		switch (CurrentBlock)
		{
			case ActiveBlock.Text:
				if (BlockDeltaEvent.Delta.TryPickText(out TextDelta? TextDelta))
				{
					TextBuffer.Append(TextDelta.Text);
					return new ChatEvent.Assistant(TextDelta.Text);
				}
				return null;

			case ActiveBlock.Thinking:
				if (BlockDeltaEvent.Delta.TryPickThinking(out ThinkingDelta? ThinkingDelta))
				{
					ThinkingBuffer.Append(ThinkingDelta.Thinking);
					return new ChatEvent.Thinking(ThinkingDelta.Thinking);
				}
				else if (BlockDeltaEvent.Delta.TryPickSignature(out SignatureDelta? SignatureDelta))
				{
					ThinkingSignature = SignatureDelta.Signature;
				}
				return null;

			default:
				return null;
		}
	}

	private ChatEvent? ProcessBlockStop()
	{
		switch (CurrentBlock)
		{
			case ActiveBlock.Text:
			{
				if (TextBuffer.Length > 0)
				{
					AssistantBlocks.Add(new Block.Text(TextBuffer.ToString()));
					TextBuffer.Clear();
				}
				break;
			}

			case ActiveBlock.Thinking:
			{
				if (ThinkingBuffer.Length > 0 || ThinkingSignature != null)
				{
					VendorPayload? Payload = ThinkingSignature != null ? new VendorPayload(ModelVendor.Claude, ThinkingSignature) : null;
					AssistantBlocks.Add(new Block.Thinking(ThinkingBuffer.ToString(), Payload));
					ThinkingBuffer.Clear();
					ThinkingSignature = null;
				}
				break;
			}
		}

		CurrentBlock = null;
		return null;
	}

	private ChatEvent? ProcessMessageDelta(RawMessageDeltaEvent MessageDeltaEvent)
	{
		if (MessageDeltaEvent.Delta.StopReason != null)
			FinalStopReason = MessageDeltaEvent.Delta.StopReason;

		return null;
	}
	
	public ModelTurnResult Complete()
	{
		// 모은 블록을 AssistantSpan으로 묶어 ModelTurnResult에 담아 반환함.
		// 이 반환값으로 다음 행동(이어서 호출, 종료)을 결정합니다.
		// ModelTurnResult에는 Continue와 EndSpan 두 가지가 있는데,
		// 지금은 Tool이 없으므로 항상 EndTurn을 돌려줌.
		// Continue는 5장에서 종료 이유(FinalStopReason)에 따라 돌려줌. 

		AssistantSpan CompletedSpan = new AssistantSpan
		{
			AssistantBlocks = AssistantBlocks.ToList(),
		};

		return new ModelTurnResult.EndSpan(CompletedSpan);
	}
	
}