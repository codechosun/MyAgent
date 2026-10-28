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

	public ModelVendor ModelVendor => ModelVendor.Claude;

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

	public async IAsyncEnumerable<ChatEvent> StreamAsync(ModelRequest ModelRequest, ModelTurn ModelTurn, [EnumeratorCancellation] CancellationToken CancelToken = default)
	{
		MessageCreateParams Parameters = ClaudeRequest.Build(ModelRequest);
		ClaudeStreamReader ClaudeStreamReader = new ClaudeStreamReader();

		IAsyncEnumerable<RawMessageStreamEvent> RawEvents = GetClient().Messages.CreateStreaming(Parameters, CancelToken);
		await foreach (RawMessageStreamEvent RawEvent in RawEvents)
		{
			ChatEvent? ChatEvent = ClaudeStreamReader.Process(RawEvent);
			if (ChatEvent != null)
			{
				yield return ChatEvent;
			}
		}

		ModelTurn.ModelTurnResult = ClaudeStreamReader.Complete();
	}
}