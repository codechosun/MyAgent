// Program.cs

using Microsoft.Extensions.DependencyInjection;
using MyAgent.Backend.Auth;
using Anthropic;
using Anthropic.Models.Messages;
using MyAgent.Backend.Chat;
using MyAgent.Backend.Provider.Claude;
using MyAgent.Backend.Agent;
using MyAgent.Backend.Conversation;
using MyAgent.Backend.Provider;

ServiceCollection Services = new ServiceCollection();
Services.AddSingleton<AuthConfig>();
Services.AddSingleton<AgentSession>();

ServiceProvider ServiceProvider = Services.BuildServiceProvider();

AuthConfig Auth = ServiceProvider.GetRequiredService<AuthConfig>();
AgentSession AgentSession = ServiceProvider.GetRequiredService<AgentSession>();

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
	
	MessageSpan CurrentMessageSpan = AgentSession.Conversation.AddMessageSpan(Input);
	
	MessageCreateParams Parameters = new MessageCreateParams
	{
		Model = "claude-opus-5-5",
		MaxTokens = 1024,
		Messages = ClaudeRequest.ToMessages(AgentSession.Conversation.Spans),
		Thinking = new ThinkingConfigAdaptive { Display = Display.Summarized },
		OutputConfig = new OutputConfig { Effort = Effort.High }
	};

	ClaudeStreamReader Reader = new ClaudeStreamReader();
	
	await foreach (RawMessageStreamEvent RawEvent in Client.Messages.CreateStreaming(Parameters))
	{
		switch (Reader.Process(RawEvent))
		{
			case ChatEvent.Thinking Think:
				Console.Write(Think.Content);
				break;

			case ChatEvent.Assistant Reply:
				Console.Write(Reply.Content);
				break;
		}
	}
	
	switch (Reader.Complete())
	{
		case ModelTurnResult.EndSpan EndResult:
		{
			CurrentMessageSpan.AssistantSpans.Add(EndResult.CompletedSpan);
			break;
		}
	}

	Console.WriteLine();
}
