// UserInput.cs

namespace MyAgent.Backend.Conversation;

public sealed record UserInput(string Text)
{
	public static implicit operator UserInput(string Text)
	{
		UserInput Converted = new UserInput(Text);
		return Converted;
	}
	
}