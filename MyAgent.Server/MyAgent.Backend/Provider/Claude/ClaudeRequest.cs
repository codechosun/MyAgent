// ClaudeRequest.cs

using Anthropic.Models.Messages;
using MyAgent.Backend.Conversation;
using Block = MyAgent.Backend.Core.Block;
using UserInput = MyAgent.Backend.Conversation.UserInput;

namespace MyAgent.Backend.Provider.Claude;

public static class ClaudeRequest
{
	public static List<MessageParam> ToMessages(IReadOnlyList<MessageSpan> MessageSpans)
	{
		List<MessageParam> MessageParams = new List<MessageParam>();

		foreach (MessageSpan MessageSpan in MessageSpans)
		{
			if (MessageSpan.UserInput != null)
			{
				MessageParams.Add(ToUserMessage(MessageSpan.UserInput));
			}

			foreach (AssistantSpan AssistantSpan in MessageSpan.AssistantSpans)
			{
				MessageParams.Add(ToAssistantMessage(AssistantSpan.AssistantBlocks));
			}
		}

		return MessageParams;
	}

	private static MessageParam ToUserMessage(UserInput UserInput)
	{
		// UserInput을 Claude의 Messages.Content로 변환.

		List<ContentBlockParam> ContentBlockParams = new List<ContentBlockParam>();

		bool bIsTextEmpty = string.IsNullOrWhiteSpace(UserInput.Text);
		if (!bIsTextEmpty)
		{
			ContentBlockParams.Add(new TextBlockParam { Text = UserInput.Text });
		}

		MessageParam UserMessage = new MessageParam { Role = Role.User, Content = ContentBlockParams };
		return UserMessage;
	}

	private static MessageParam ToAssistantMessage(IReadOnlyList<Block> AssistantBlocks)
	{
		// MessageSpan.AssistantSpans 속에 AssistantBlocks(우리 Block 타입)를 넘겨받아
		// Claude의 ContentBlock으로 변환

		List<ContentBlockParam> ContentBlockParams = new List<ContentBlockParam>();
			// 응답을 받을 때 블록으로 나뉘어 왔던 것처럼, 보낼 때도 ContentBlockParam 목록으로 보냄.

		foreach (Block Block in AssistantBlocks)
		{
			switch (Block)
			{
				case Block.Text TextBlock:
				{
					ContentBlockParams.Add(new TextBlockParam { Text = TextBlock.Content });
					break;
				}

				case Block.Thinking ThinkingBlock:
				{
					// Claude가 만든 생각만 서명과 함께 되돌려 보냄.
					// 서명 없는 Thinking Block을 보내면 Claude API가 요청을 거부함.
					// 다른 회사의 생각은 보내지 않음.

					if (null == ThinkingBlock.VendorPayload || ThinkingBlock.VendorPayload.ModelVendor != ModelVendor.Claude)
					{
						// VendorPayload가 없거나 Claude의 것이 아니면 break로 건너뜀.

						break;
					}

					ContentBlockParams.Add(new ThinkingBlockParam { Thinking = ThinkingBlock.Content, Signature = ThinkingBlock.VendorPayload.Data });
					break;
				}
			}
		}

		MessageParam AssistantMessage = new MessageParam { Role = Role.Assistant, Content = ContentBlockParams };
		return AssistantMessage;
	}

	public static MessageCreateParams Build(ModelRequest ModelRequest)
	{
		// 우리 요청을 Claude의 요청으로 바꾼다.

		MessageCreateParams Parameters = new MessageCreateParams
		{
			Model = ModelRequest.Model,
			MaxTokens = ModelRequest.MaxTokens,
			Messages = ToMessages(ModelRequest.MessageSpans),
			Thinking = new ThinkingConfigAdaptive { Display = Display.Summarized },
			OutputConfig = new OutputConfig { Effort = Effort.High }
		};
		return Parameters;
	}
}