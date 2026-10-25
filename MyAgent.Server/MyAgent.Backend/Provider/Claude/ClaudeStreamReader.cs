// ClaudeSteamReader.cs

using System.Text;
using Anthropic.Models.Messages;
using MyAgent.Backend.Chat;
using Block = MyAgent.Backend.Core.Block;

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

		if (RawEvent.TryPickContentBlockStop(out RawContentBlockStopEvent? BlockStopEvent))
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
		if (BlockStartEvent.ContentBlock.TryPickText(out _))
			CurrentBlock = new ActiveBlock.Text();

		else if (BlockStartEvent.ContentBlock.TryPickThinking(out _))
			CurrentBlock = new ActiveBlock.Thinking();

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
}