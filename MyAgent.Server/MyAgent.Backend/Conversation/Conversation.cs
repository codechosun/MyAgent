namespace MyAgent.Backend.Conversation;

public sealed class Conversation
{
	// 대화 기록을 관리하는 클래스.
	// MessageSpan 기반으로 사용자 입력과 Model 호출 결과를 구조화하여 관리.
	// 단순히 사용자 입력과 Model 호출 결과만 관리함.
	// 특정 회사에 맞춘 모양이 아님. 해당 작업은 Provider 클래스가 할 예정.

	private readonly List<MessageSpan> MessageSpans = new List<MessageSpan>();
		// 사용자의 입력 한 턴에 대한 MessageSpan 목록.

	public IReadOnlyList<MessageSpan> Spans => MessageSpans;
		// MessageSpan 목록을 읽기 전용으로 내놓는다.
		// Provider가 회사의 요청 모양으로 옮길 때 사용함.

	public MessageSpan AddMessageSpan(UserInput Input)
	{
		// 사용자가 입력하면 새 MessageSpan을 만들어 MessageSpans에 넣고 반환.
		// 반환하는 이유는 이번 턴에 대한 Model의 응답을 이 MessageSpan에 추가해야하기 때문.
		// 매개변수는 UserInput이지만,
		// implicit operator를 통해 Program.cs에서 전달한 string이 그대로 들어온다.

		MessageSpan MessageSpan = new MessageSpan { UserInput = Input };
		MessageSpans.Add(MessageSpan);

		return MessageSpan;
	}
}