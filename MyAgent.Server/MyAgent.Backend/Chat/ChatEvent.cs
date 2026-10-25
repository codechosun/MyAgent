// ChatEvent.cs

namespace MyAgent.Backend.Chat;

public abstract record ChatEvent
{
	public sealed record Assistant(string Content) : ChatEvent;

	public sealed record Thinking(string Content) : ChatEvent;

	public sealed record Done : ChatEvent;
	
}