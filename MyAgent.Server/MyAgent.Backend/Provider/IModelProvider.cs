// IModelProvider.cs
 
 using MyAgent.Backend.Chat;
 
 namespace MyAgent.Backend.Provider;
 
 public interface IModelProvider
 { 
 	ModelVendor Vendor { get; }
 
 	string KeyName { get; }
 
 	string KeyLabel { get; }
 
 	string? CheckReady();
 
 	IAsyncEnumerable<ChatEvent> StreamAsync(ModelRequest Request, ModelTurn Turn, CancellationToken CancelToken = default);

 }