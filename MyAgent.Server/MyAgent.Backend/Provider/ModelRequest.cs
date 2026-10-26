// ModelRequest.cs

using MyAgent.Backend.Conversation;

namespace MyAgent.Backend.Provider;

public sealed record ModelRequest
{
	// 어느 회사에 보내든, Model 호출 하나에 필요한 재료들을 관리하는 클래스.
	// 어느 회사의 요청 모양으로 바꿀지는 Provider가 정한다.
	// 4장의 System Prompty, 5장의 Tool 정의, 6장의 Effort처럼 장마다 늘어날 예정.

	public required string Model { get; init; }
		// Model ID(예: "claude-opus-5-5").

	public required int MaxTokens { get; init; }
		// 이번 답에 쓸 수 있는 최대 Token 수.

	public required IReadOnlyList<MessageSpan> Messages { get; init; }
		// 지금까지의 대화 히스토리.
}