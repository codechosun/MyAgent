// ModelRequest.cs

using MyAgent.Backend.Conversation;

namespace MyAgent.Backend.Provider;

public sealed record ModelRequest
{
	public required string Model { get; init; }

	public required int MaxTokens { get; init; }

	public required IReadOnlyList<MessageSpan> MessageSpans { get; init; }
		
	public string? SystemPrompt { get; init; }
		
}