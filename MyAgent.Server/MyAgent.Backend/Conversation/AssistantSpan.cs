// AssistantSpan.cs

using MyAgent.Backend.Core;

namespace MyAgent.Backend.Conversation;

public sealed class AssistantSpan
{
	// Model 호출 1회에 대한 Model의 응답 블록 목록을 관리하는 클래스.

	public required IReadOnlyList<Block> AssistantBlocks { get; init; }
		
}