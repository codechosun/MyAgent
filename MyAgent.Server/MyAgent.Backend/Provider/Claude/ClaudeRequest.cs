// ClaudeRequest.cs

using Anthropic.Models.Messages;
using MyAgent.Backend.Conversation;
using Block = MyAgent.Backend.Core.Block;
using UserInput = MyAgent.Backend.Conversation.UserInput;

namespace MyAgent.Backend.Provider.Claude;

public static class ClaudeRequest
{
	public static MessageCreateParams Build(ModelRequest Request)
	{
		// 우리 요청을 Claude의 요청으로 바꾼다.

		return new MessageCreateParams
		{
			Model = Request.Model,
			MaxTokens = Request.MaxTokens,
			Messages = ToMessages(Request.Messages),
			Thinking = new ThinkingConfigAdaptive { Display = Display.Summarized },
			OutputConfig = new OutputConfig { Effort = Effort.High }
		};
	}
	
	public static List<MessageParam> ToMessages(IReadOnlyList<MessageSpan> Spans)
	{
		List<MessageParam> Messages = new List<MessageParam>();

		foreach (MessageSpan MessageSpan in Spans)
		{
			if (MessageSpan.UserInput != null)
				Messages.Add(ToUserMessage(MessageSpan.UserInput));

			foreach (AssistantSpan Span in MessageSpan.AssistantSpans)
			{
				Messages.Add(ToAssistantMessage(Span.AssistantBlocks));
			}
		}

		return Messages;
	}

	private static MessageParam ToUserMessage(UserInput Input)
	{
		// UserInput을 Claude의 Messages.Content로 변환.

		List<ContentBlockParam> Blocks = new List<ContentBlockParam>();

		bool bIsTextEmpty = string.IsNullOrWhiteSpace(Input.Text);
		if (!bIsTextEmpty)
			Blocks.Add(new TextBlockParam { Text = Input.Text });

		return new MessageParam { Role = Role.User, Content = Blocks };
	}

	private static MessageParam ToAssistantMessage(IReadOnlyList<Block> Blocks)
	{
		// MessageSpan.AssistantSpans 속에 AssistantBlock(우리 Block 타입)을 넘겨받아
		// Claude의 ContentBlock으로 변환

		List<ContentBlockParam> ContentBlocks = new List<ContentBlockParam>();
			// 응답을 받을 때 블록으로 나뉘어 왔던 것처럼, 보낼 때도 ContentBlockParam 목록으로 보냄.

		foreach (Block Block in Blocks)
		{
			switch (Block)
			{
				case Block.Text TextBlock:
				{
					ContentBlocks.Add(new TextBlockParam { Text = TextBlock.Content });
					break;
				}

				case Block.Thinking ThinkingBlock:
				{
					// Claude가 만든 생각만 서명과 함께 되돌려 보냄.
					// 서명 없는 Thinking Block을 보내면 Claude API가 요청을 거부함.
					// 다른 회사의 생각은 보내지 않음.

					if (ThinkingBlock.Payload == null || ThinkingBlock.Payload.Vendor != ModelVendor.Claude)
						// Payload가 없거나 Claude의 것이 아니면 break로 건너뜀.
						break;

					ContentBlocks.Add(new ThinkingBlockParam { Thinking = ThinkingBlock.Content, Signature = ThinkingBlock.Payload.Data });
					break;
				}
			}
		}

		return new MessageParam { Role = Role.Assistant, Content = ContentBlocks };
	}
}