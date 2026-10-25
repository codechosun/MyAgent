// AuthConfig.cs

using System.Text.Json;
using System.Text.Json.Nodes;

namespace MyAgent.Backend.Auth;

public sealed class AuthConfig
{
	private readonly string ConfigPath = Path.Combine(
		Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
		".myagent", "AuthConfig.json");

	private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions { WriteIndented = true };

	private readonly Dictionary<string, string> Keys = new Dictionary<string, string>();
	public string? GetKey(string Name)
	{
		return Keys.GetValueOrDefault(Name);
	}

	public bool HasKey(string Name)
	{
		return !string.IsNullOrWhiteSpace(GetKey(Name));
	}

	public void SetKey(string Name, string Key)
	{
		Keys[Name] = Key;
		Save();
	}

	private void Save()
	{
		string Dir = Path.GetDirectoryName(ConfigPath)!;
		if (!Directory.Exists(Dir))
			Directory.CreateDirectory(Dir);

		JsonObject Root = new JsonObject();
		foreach (KeyValuePair<string, string> Pair in Keys)
			Root[Pair.Key] = Pair.Value;

		File.WriteAllText(ConfigPath, Root.ToJsonString(JsonOptions));
	}

	public void Load()
	{
		if (!File.Exists(ConfigPath))
			return;

		string Json = File.ReadAllText(ConfigPath);
		JsonNode? Parsed = JsonNode.Parse(Json);
		JsonObject? Root = Parsed != null ? Parsed.AsObject() : null;
		if (Root == null)
			return;

		foreach (KeyValuePair<string, JsonNode?> Field in Root)
		{
			string? Key = Field.Value != null ? Field.Value.GetValue<string>() : null;
			if (Key != null)
				Keys[Field.Key] = Key;
		}
	}
}