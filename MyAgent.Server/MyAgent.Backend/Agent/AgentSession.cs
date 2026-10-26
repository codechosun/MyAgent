// AgentSession.cs

namespace MyAgent.Backend.Agent;

public sealed class AgentSession
{
	// Agentic Harness 하나가 유지하는 상태(지금은 대화 히스토리)를 보관합니다.

	public Conversation.Conversation Conversation { get; } = new Conversation.Conversation();
		// 이 Session의 대화 히스토리.
}