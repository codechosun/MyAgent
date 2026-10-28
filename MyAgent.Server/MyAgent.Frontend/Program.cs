// Program.cs

using Microsoft.Extensions.DependencyInjection;
using MyAgent.Backend.Auth;
using MyAgent.Backend.Chat;
using MyAgent.Backend.Provider.Claude;
using MyAgent.Backend.Agent;
using MyAgent.Backend.Conversation;
using MyAgent.Backend.Provider;

ServiceCollection Services = new ServiceCollection();
Services.AddSingleton<AuthConfig>();
Services.AddSingleton<AgentSession>();
Services.AddSingleton<IModelProvider, ClaudeProvider>();

ServiceProvider ServiceProvider = Services.BuildServiceProvider();

AuthConfig Auth = ServiceProvider.GetRequiredService<AuthConfig>();
AgentSession AgentSession = ServiceProvider.GetRequiredService<AgentSession>();
IModelProvider Provider = ServiceProvider.GetRequiredService<IModelProvider>();

Auth.Load();

bool bHasKey = Auth.HasKey(Provider.KeyName);
if (!bHasKey)
{
	Console.Write($"{Provider.KeyLabel}를 입력하세요: ");
	string? Key = Console.ReadLine();

	bool bIsKeyEmpty = string.IsNullOrWhiteSpace(Key);
	if (bIsKeyEmpty)
	{
		Console.WriteLine($"{Provider.KeyLabel}가 입력되지 않았습니다.");
		return;
	}

	Auth.SetKey(Provider.KeyName, Key!);
	Console.WriteLine($"{Provider.KeyLabel} 저장 완료!");
}

while (true)
{
	Console.Write("\n> ");
	string? Input = Console.ReadLine();

	bool bIsInputEmpty = string.IsNullOrWhiteSpace(Input);
	if (bIsInputEmpty)
	{
		continue;
	}

	bool bIsExit = Input!.Equals("exit", StringComparison.OrdinalIgnoreCase);
	if (bIsExit)
	{
		break;
	}
	
	MessageSpan CurrentMessageSpan = AgentSession.Conversation.AddMessageSpan(Input);
	
	ModelRequest Request = new ModelRequest
	{
		Model = "claude-opus-5-5",
		MaxTokens = 1024,
		Messages = AgentSession.Conversation.Spans
	};
	
	ModelTurn Turn = new ModelTurn();
	IAsyncEnumerable<ChatEvent> Events = Provider.StreamAsync(Request, Turn);
	await foreach (ChatEvent Event in Events)
	{
		switch (Event)
		{
			case ChatEvent.Thinking Think:
			{
				Console.Write(Think.Content);
				break;
			}

			case ChatEvent.Assistant Reply:
			{
				Console.Write(Reply.Content);
				break;
			}
		}
	}

	switch (Turn.Result)
	{
		case ModelTurnResult.EndSpan EndResult:
		{
			CurrentMessageSpan.AssistantSpans.Add(EndResult.CompletedSpan);
			break;
		}
	}

	Console.WriteLine();
}
