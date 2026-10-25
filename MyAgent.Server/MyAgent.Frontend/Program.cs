// Program.cs

using Microsoft.Extensions.DependencyInjection;
using MyAgent.Backend.Auth;
using Anthropic;
	// AnthropicClient가 포함된 네임스페이스.
using Anthropic.Models.Messages;
	// 메세지 관련 타입(MessageCreateParams, Message, ContentBlock 등)이 포함된 네임스페이스.

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
	// Claude의 API Key로 Claude SDK의 클라이언트를 만든다.
	// 3장부터는 ClaudeProvider가 만들어 들고 있게 됨.

MessageCreateParams Parameters = new MessageCreateParams
{
	// 요청을 만든다: Model, 최대 출력 길이, 보낼 메시지
	// MessageCreateParams는 이름 그대로 "메세지를 만들 때(Client.Messages.Create() 함수) 넘기는 파라미터"의 뜻.
	// MessageCreateParams를 전달하고 만들어지는 메세지는 Claude의 답 메세지.

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
		// Messages라는 이름에서 알 수 있듯, 지금까지의 메세지 목록을 담는 변수임.
		// 나중에는 메세지가 많아질 것을 암시.
		// 지금은 사용자(Role.User)의 메세지 한 개 밖에 없는 상황.
};

Message Response = await Client.Messages.Create(Parameters);
	// 요청을 보내고 응답을 기다린다

foreach (ContentBlock Block in Response.Content)
{
	// 응답 Message의 Content에는 여러 블럭(Text, Thinking, ToolCall, ...)이 들어 있음.
	// 그 중에서 Text Block만 꺼내 출력한다

	if (Block.TryPickText(out TextBlock? Text))
		Console.WriteLine(Text.Text);
}
