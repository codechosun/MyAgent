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

AuthConfig AuthConfig = ServiceProvider.GetRequiredService<AuthConfig>();
AgentSession AgentSession = ServiceProvider.GetRequiredService<AgentSession>();
IModelProvider ModelProvider = ServiceProvider.GetRequiredService<IModelProvider>();

AuthConfig.Load();

bool bHasKey = AuthConfig.HasKey(ModelProvider.KeyName);
if (!bHasKey)
{
	Console.Write($"{ModelProvider.KeyLabel}를 입력하세요: ");
	string? Key = Console.ReadLine();

	bool bIsKeyEmpty = string.IsNullOrWhiteSpace(Key);
	if (bIsKeyEmpty)
	{
		Console.WriteLine($"{ModelProvider.KeyLabel}가 입력되지 않았습니다.");
		return;
	}

	AuthConfig.SetKey(ModelProvider.KeyName, Key!);
	Console.WriteLine($"{ModelProvider.KeyLabel} 저장 완료!");
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
	
	ModelRequest ModelRequest = new ModelRequest
	{
		Model = "claude-opus-5-5",
		MaxTokens = 1024,
		MessageSpans = AgentSession.Conversation.MessageSpans
	};
	
	ModelTurn ModelTurn = new ModelTurn();
	IAsyncEnumerable<ChatEvent> ChatEvents = ModelProvider.StreamAsync(ModelRequest, ModelTurn);
	await foreach (ChatEvent ChatEvent in ChatEvents)
	{
		switch (ChatEvent)
		{
			case ChatEvent.Thinking ThinkingEvent:
			{
				Console.Write(ThinkingEvent.Content);
				break;
			}

			case ChatEvent.Assistant AssistantEvent:
			{
				Console.Write(AssistantEvent.Content);
				break;
			}
		}
	}

	switch (ModelTurn.ModelTurnResult)
	{
		case ModelTurnResult.EndSpan EndResult:
		{
			CurrentMessageSpan.AssistantSpans.Add(EndResult.CompletedSpan);
			break;
		}
	}

	Console.WriteLine();
}
