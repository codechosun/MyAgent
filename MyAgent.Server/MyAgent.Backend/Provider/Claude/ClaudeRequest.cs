// ClaudeRequest.cs

using Anthropic.Models.Messages;
using MyAgent.Backend.Conversation;
using Block = MyAgent.Backend.Core.Block;
using UserInput = MyAgent.Backend.Conversation.UserInput;
	// Claude SDK에도 UserInput이라는 타입이 있어서 우리 타입을 명시함.

namespace MyAgent.Backend.Provider.Claude;

public static class ClaudeRequest
{
	// 대화 기록을 Claude의 요청 모양으로 바꿉니다.

	public static List<MessageParam> ToMessages(IReadOnlyList<MessageSpan> Spans)
	{
		// ToMessage() 함수는 저장된 대화를 Claude API가 원하는 메세지 목록(List<MessageParam>)으로 변환해줌.
		// 각 MessageSpan을 순서대로 돌며 사용자 입력은 User 메세지로,
		// 응답 블록은 Assistant 메세지로 만듦. 이때 우리 타입을 Claude SDK의 타입으로 변환.
		// Claude API는 user ↔ assistant 교대를 요구함.
		// 예: [user] → [assistant: thinking+text] → [user] → [assistant: text] …

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

		if (!string.IsNullOrWhiteSpace(Input.Text))
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