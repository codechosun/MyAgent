// Program.cs

using Microsoft.Extensions.DependencyInjection;
using MyAgent.Backend.Auth;
using Anthropic;
using Anthropic.Models.Messages;

ServiceCollection Services = new ServiceCollection();
Services.AddSingleton<AuthConfig>();

ServiceProvider ServiceProvider = Services.BuildServiceProvider();

AuthConfig Auth = ServiceProvider.GetRequiredService<AuthConfig>();

Auth.Load();

const string ClaudeKeyName = "claude_api_key";

if (!Auth.HasKey(ClaudeKeyName))
{
	Console.Write("Claude API Key를 입력하세요: ");
	string? Key = Console.ReadLine();

	if (string.IsNullOrWhiteSpace(Key))
	{
		Console.WriteLine("Claude API Key가 입력되지 않았습니다.");
		return;
	}

	Auth.SetKey(ClaudeKeyName, Key);
	Console.WriteLine("Claude API Key 저장 완료!");
}

AnthropicClient Client = new AnthropicClient { ApiKey = Auth.GetKey(ClaudeKeyName)! };

while (true)
{
	Console.Write("\n> ");
	string? Input = Console.ReadLine();

	if (string.IsNullOrWhiteSpace(Input))
		continue;

	if (Input.Equals("exit", StringComparison.OrdinalIgnoreCase))
		break;

	MessageCreateParams Parameters = new MessageCreateParams
	{
		Model = "claude-opus-5-5",
		MaxTokens = 1024,
		Messages = new List<MessageParam>
		{
			new MessageParam
			{
				Role = Role.User,
				Content = Input
			}
		}
	};

	await foreach (RawMessageStreamEvent RawEvent in Client.Messages.CreateStreaming(Parameters))
	{
		if (RawEvent.TryPickContentBlockDelta(out RawContentBlockDeltaEvent? Delta) &&
		    Delta.Delta.TryPickText(out TextDelta? Text))
		{
			Console.Write(Text.Text);
		}
	}

	Console.WriteLine();
}
