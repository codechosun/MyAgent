// ClaudeProvider.cs

using System.Runtime.CompilerServices;
using Anthropic;
using Anthropic.Models.Messages;
using MyAgent.Backend.Auth;
using MyAgent.Backend.Chat;

namespace MyAgent.Backend.Provider.Claude;

public sealed class ClaudeProvider : IModelProvider
{
	private readonly AuthConfig _Auth;

	public ClaudeProvider(AuthConfig InAuth)
	{
		_Auth = InAuth;
	}

	private AnthropicClient? _Client;

	private string? _ClientKey;

	public ModelVendor Vendor => ModelVendor.Claude;

	public string KeyName => "claude_api_key";

	public string KeyLabel => "Claude API Key";

	public string? CheckReady()
	{
		string? Problem = _Auth.HasKey(KeyName) ? null : $"{KeyLabel}가 설정되지 않았습니다.";
		return Problem;
	}
	
	private AnthropicClient GetClient()
	{
		string Key = _Auth.GetKey(KeyName)!;
		if (null == _Client || _ClientKey != Key)
		{
			_Client = new AnthropicClient { ApiKey = Key };
			_ClientKey = Key;
		}

		return _Client;
	}

	public async IAsyncEnumerable<ChatEvent> StreamAsync(ModelRequest Request, ModelTurn Turn, [EnumeratorCancellation] CancellationToken CancelToken = default)
	{
		MessageCreateParams Parameters = ClaudeRequest.Build(Request);
		ClaudeStreamReader Reader = new ClaudeStreamReader();

		IAsyncEnumerable<RawMessageStreamEvent> RawEvents = GetClient().Messages.CreateStreaming(Parameters, CancelToken);
		await foreach (RawMessageStreamEvent RawEvent in RawEvents)
		{
			ChatEvent? Event = Reader.Process(RawEvent);
			if (Event != null)
			{
				yield return Event;
			}
		}

		Turn.Result = Reader.Complete();
	}
}