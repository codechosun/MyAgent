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
		List<ContentBlockParam> ContentBlockParams = new List<ContentBlockParam>();

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
					if (null == ThinkingBlock.VendorPayload || ThinkingBlock.VendorPayload.ModelVendor != ModelVendor.Claude)
					{
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
		MessageCreateParams Parameters = new MessageCreateParams
		{
			Model = ModelRequest.Model,
			MaxTokens = ModelRequest.MaxTokens,
			Messages = ToMessages(ModelRequest.MessageSpans),
			Thinking = new ThinkingConfigAdaptive { Display = Display.Summarized },
			OutputConfig = new OutputConfig { Effort = Effort.High },
			System = ToSystem(ModelRequest.SystemPrompt),
			CacheControl = new CacheControlEphemeral()
				// 이 한 줄로 System Prompt와 지난 대화가 모두 캐시됨.
				// 요청의 앞부분(System Prompt, 앞선 대화)을 Claude 서버에 잠시 저장해 두고
				// 다음 요청에서 다시 씁니다(4.3절).
		};
		return Parameters;
	}
	
	private static MessageCreateParamsSystem? ToSystem(string? SystemPrompt)
	{
		// System Prompt를 TextBlockParam 하나에 담아 반환함.
		// 빈 글을 보내면 API가 요청을 거절하므로 null을 반환.

		bool bIsSystemPromptEmpty = string.IsNullOrEmpty(SystemPrompt);
		if (bIsSystemPromptEmpty)
		{
			return null;
		}

		List<TextBlockParam> SystemBlocks = new List<TextBlockParam> { new TextBlockParam { Text = SystemPrompt! } };
		return SystemBlocks;
	}
	
}