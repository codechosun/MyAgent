// ModelTurnResult.cs

using MyAgent.Backend.Conversation;

namespace MyAgent.Backend.Provider;

public abstract record ModelTurnResult
{
	// Model 호출 한 번이 끝난 뒤 다음 행동의 종류.
	// 호출하는 쪽에서 Pattern Matching으로 루프를 제어할 예정.
	// 5장에서는 Tool을 실행한 뒤 다시 호출(ExecuteTools)이 추가될 예정.

	public sealed record Continue(AssistantSpan CompletedSpan) : ModelTurnResult;
		// 서버가 응답을 도중에 멈춘 경우. 이어서 한 번 더 호출해야 함.(Claude의 pause_turn).
		// Tool 실행 없이 다음 호출로 이어감.

	public sealed record EndSpan(AssistantSpan CompletedSpan) : ModelTurnResult;
		// 대화가 완료됨.
	
}