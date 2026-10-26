// ClaudeProvider.cs

using System.Runtime.CompilerServices;
using Anthropic;
using Anthropic.Models.Messages;
using MyAgent.Backend.Auth;
using MyAgent.Backend.Chat;

namespace MyAgent.Backend.Provider.Claude;

public sealed class ClaudeProvider : IModelProvider
{
	private readonly AuthConfig Auth;

	public ClaudeProvider(AuthConfig InAuth)
	{
		Auth = InAuth;
	}

	private AnthropicClient? Client;

	private string? ClientKey;

	public ModelVendor Vendor => ModelVendor.Claude;

	public string KeyName => "claude_api_key";

	public string KeyLabel => "Claude API Key";

	public string? CheckReady()
	{
		return Auth.HasKey(KeyName) ? null : $"{KeyLabel}가 설정되지 않았습니다.";
	}
	
	private AnthropicClient GetClient()
	{
		string Key = Auth.GetKey(KeyName)!;
		if (Client == null || ClientKey != Key)
		{
			Client = new AnthropicClient { ApiKey = Key };
			ClientKey = Key;
		}

		return Client;
	}

	public async IAsyncEnumerable<ChatEvent> StreamAsync(ModelRequest Request, ModelTurn Turn, [EnumeratorCancellation] CancellationToken CancelToken = default)
	{
		MessageCreateParams Parameters = ClaudeRequest.Build(Request);
		ClaudeStreamReader Reader = new ClaudeStreamReader();

		await foreach (RawMessageStreamEvent RawEvent in GetClient().Messages.CreateStreaming(Parameters, CancelToken))
		{
			ChatEvent? Event = Reader.Process(RawEvent);
			if (Event != null)
				yield return Event;
		}

		Turn.Result = Reader.Complete();
	}
}