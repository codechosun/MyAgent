// MessageSpan.cs

namespace MyAgent.Backend.Conversation;

public sealed class MessageSpan
{
	// UserInput 1건과 그에 대한 Agentic Assistant 응답 전체를 포함하는 구간(Span).
	// Model이 Tool을 반복 호출하면 AssistantSpan이 여러 개 쌓임.

	public UserInput? UserInput { get; init; }
		// 사용자 입력.
		// UserInput이 null일 수 있게 해 둔 것은 13장의 요약(/compact) 때문.
		// 요약한 대화는 사용자 입력 없이 요약문만 담은 Turn이 됨.

	public List<AssistantSpan> AssistantSpans { get; } = new List<AssistantSpan>();
		// 이 메시지에서 수행된 Model 호출 결과 목록.
		
}