// IModelProvider.cs

using MyAgent.Backend.Chat;

namespace MyAgent.Backend.Provider;

public interface IModelProvider
{
	ModelVendor ModelVendor { get; }

	string KeyName { get; }

	string KeyLabel { get; }

	string? CheckReady();

	IAsyncEnumerable<ChatEvent> StreamAsync(ModelRequest ModelRequest, ModelTurn ModelTurn, CancellationToken CancelToken = default);

}