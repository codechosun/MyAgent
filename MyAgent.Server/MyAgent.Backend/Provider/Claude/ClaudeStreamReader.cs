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

	private ActiveBlock? _CurrentBlock;

	private readonly StringBuilder _TextBuffer = new StringBuilder();

	private readonly StringBuilder _ThinkingBuffer = new StringBuilder();

	private string? _ThinkingSignature;
	
	public StopReason? FinalStopReason;

	private readonly List<Block> _AssistantBlocks = new List<Block>();
	
	public IReadOnlyList<Block> AssistantBlocks => _AssistantBlocks;

	public ChatEvent? Process(RawMessageStreamEvent RawEvent)
	{
		bool bIsBlockStart = RawEvent.TryPickContentBlockStart(out RawContentBlockStartEvent? ContentBlockStartEvent);
		if (bIsBlockStart)
		{
			ChatEvent? ChatEvent = ProcessBlockStart(ContentBlockStartEvent!);
			return ChatEvent;
		}

		bool bIsBlockDelta = RawEvent.TryPickContentBlockDelta(out RawContentBlockDeltaEvent? ContentBlockDeltaEvent);
		if (bIsBlockDelta)
		{
			ChatEvent? ChatEvent = ProcessBlockDelta(ContentBlockDeltaEvent!);
			return ChatEvent;
		}

		bool bIsBlockStop = RawEvent.TryPickContentBlockStop(out RawContentBlockStopEvent? ContentBlockStopEvent);
		if (bIsBlockStop)
		{
			ChatEvent? ChatEvent = ProcessBlockStop();
			return ChatEvent;
		}

		bool bIsMessageDelta = RawEvent.TryPickDelta(out RawMessageDeltaEvent? MessageDeltaEvent);
		if (bIsMessageDelta)
		{
			ChatEvent? ChatEvent = ProcessMessageDelta(MessageDeltaEvent!);
			return ChatEvent;
		}

		return null;
	}

	private ChatEvent? ProcessBlockStart(RawContentBlockStartEvent ContentBlockStartEvent)
	{
		bool bIsTextBlock = ContentBlockStartEvent.ContentBlock.TryPickText(out _);
		if (bIsTextBlock)
		{
			_CurrentBlock = new ActiveBlock.Text();
		}
		else
		{
			bool bIsThinkingBlock = ContentBlockStartEvent.ContentBlock.TryPickThinking(out _);
			if (bIsThinkingBlock)
			{
				_CurrentBlock = new ActiveBlock.Thinking();
			}
		}

		return null;
	}

	private ChatEvent? ProcessBlockDelta(RawContentBlockDeltaEvent ContentBlockDeltaEvent)
	{
		switch (_CurrentBlock)
		{
			case ActiveBlock.Text:
			{
				bool bIsTextDelta = ContentBlockDeltaEvent.Delta.TryPickText(out TextDelta? TextDelta);
				if (bIsTextDelta)
				{
					_TextBuffer.Append(TextDelta!.Text);
					ChatEvent.Assistant AssistantEvent = new ChatEvent.Assistant(TextDelta.Text);
					return AssistantEvent;
				}
				return null;
			}

			case ActiveBlock.Thinking:
			{
				bool bIsThinkingDelta = ContentBlockDeltaEvent.Delta.TryPickThinking(out ThinkingDelta? ThinkingDelta);
				if (bIsThinkingDelta)
				{
					_ThinkingBuffer.Append(ThinkingDelta!.Thinking);
					ChatEvent.Thinking ThinkingEvent = new ChatEvent.Thinking(ThinkingDelta.Thinking);
					return ThinkingEvent;
				}
				else
				{
					bool bIsSignatureDelta = ContentBlockDeltaEvent.Delta.TryPickSignature(out SignatureDelta? SignatureDelta);
					if (bIsSignatureDelta)
					{
						_ThinkingSignature = SignatureDelta!.Signature;
					}
				}
				return null;
			}

			default:
			{
				return null;
			}
		}
	}

	private ChatEvent? ProcessBlockStop()
	{
		switch (_CurrentBlock)
		{
			case ActiveBlock.Text:
			{
				if (0 < _TextBuffer.Length)
				{
					_AssistantBlocks.Add(new Block.Text(_TextBuffer.ToString()));
					_TextBuffer.Clear();
				}
				break;
			}

			case ActiveBlock.Thinking:
			{
				if (0 < _ThinkingBuffer.Length || _ThinkingSignature != null)
				{
					VendorPayload? VendorPayload = _ThinkingSignature != null ? new VendorPayload(ModelVendor.Claude, _ThinkingSignature) : null;
					_AssistantBlocks.Add(new Block.Thinking(_ThinkingBuffer.ToString(), VendorPayload));
					_ThinkingBuffer.Clear();
					_ThinkingSignature = null;
				}
				break;
			}
		}

		_CurrentBlock = null;
		return null;
	}

	private ChatEvent? ProcessMessageDelta(RawMessageDeltaEvent MessageDeltaEvent)
	{
		if (MessageDeltaEvent.Delta.StopReason != null)
		{
			FinalStopReason = MessageDeltaEvent.Delta.StopReason;
		}

		return null;
	}
	
	public ModelTurnResult Complete()
	{
		// 모은 블록을 AssistantSpan으로 묶어 ModelTurnResult에 담아 반환함.
		// 이 반환값으로 다음 행동(이어서 호출, 종료)을 결정합니다.
		// ModelTurnResult에는 Continue와 EndSpan 두 가지가 있는데,
		// 지금은 Tool이 없으므로 항상 EndSpan을 돌려줌.
		// Continue는 5장에서 종료 이유(FinalStopReason)에 따라 돌려줌.

		AssistantSpan CompletedSpan = new AssistantSpan
		{
			AssistantBlocks = _AssistantBlocks.ToList(),
		};

		ModelTurnResult.EndSpan EndResult = new ModelTurnResult.EndSpan(CompletedSpan);
		return EndResult;
	}
	
}