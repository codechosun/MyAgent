// Program.cs

using Microsoft.Extensions.DependencyInjection;
using MyAgent.Backend.Auth;
using MyAgent.Backend.Chat;
using MyAgent.Backend.Provider.Claude;
using MyAgent.Backend.Agent;
using MyAgent.Backend.Conversation;
using MyAgent.Backend.Provider;
using MyAgent.Backend.Prompt;

ServiceCollection Services = new ServiceCollection();
Services.AddSingleton<AuthConfig>();
Services.AddSingleton<AgentSession>();
Services.AddSingleton<IModelProvider, ClaudeProvider>();
Services.AddSingleton<PromptBuilder>();

ServiceProvider ServiceProvider = Services.BuildServiceProvider();

AuthConfig AuthConfig = ServiceProvider.GetRequiredService<AuthConfig>();
AgentSession AgentSession = ServiceProvider.GetRequiredService<AgentSession>();
IModelProvider ModelProvider = ServiceProvider.GetRequiredService<IModelProvider>();
PromptBuilder PromptBuilder = ServiceProvider.GetRequiredService<PromptBuilder>();

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
	
	// ModelRequest ModelRequest = new ModelRequest
	// {
	// 	Model = "claude-opus-5-5",
	// 	MaxTokens = 1024,
	// 	MessageSpans = AgentSession.Conversation.MessageSpans
	// };
	ModelRequest ModelRequest = PromptBuilder.Build(AgentSession);
		// 요청 재료를 PromptBuilder가 만든다
		// Model API에 요청을 한 번 보낼 때 담는 재료(System Prompt와 대화 전체)
		// 이제 Program.cs는 "무엇을 보낼지"를 몰라도 됨. ClaudeRequest가 그걸 담당하게 됨.
	
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
