// ModelTurn.cs

namespace MyAgent.Backend.Provider;

public sealed class ModelTurn
{
	// Model 호출 한 번의 결과를 담는 빈 그릇.
	// Streaming이 끝나면 채워져 있음.
	// IAsyncEnumerable을 돌려주는 메서드는 값을 따로 돌려줄 수 없으므로,
	// 부르는 쪽이 빈 그릇을 넘기고 다 받은 뒤에 그릇을 볼 예정.

	public ModelTurnResult? Result { get; set; }
		// 다음 행동과 완성된 AssistantSpan.
}