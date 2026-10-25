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

MessageCreateParams Parameters = new MessageCreateParams
{
	Model = "claude-opus-5-5",
	MaxTokens = 1024,
	Messages = new List<MessageParam>
	{
		new MessageParam
		{
			Role = Role.User,
			Content = "안녕하세요! 간단히 자기소개 해주세요."
		}
	}
};

Message Response = await Client.Messages.Create(Parameters);

foreach (ContentBlock Block in Response.Content)
{
	if (Block.TryPickText(out TextBlock? Text))
		Console.WriteLine(Text.Text);
}
