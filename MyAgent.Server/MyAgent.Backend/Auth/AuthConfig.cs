// AuthConfig.cs

using System.Text.Json;
using System.Text.Json.Nodes;

namespace MyAgent.Backend.Auth;

public sealed class AuthConfig
{
	private readonly string _ConfigPath = Path.Combine(
		Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
		".myagent", "AuthConfig.json");

	private static readonly JsonSerializerOptions _JsonOptions = new JsonSerializerOptions { WriteIndented = true };

	private readonly Dictionary<string, string> _Keys = new Dictionary<string, string>();
	public string? GetKey(string Name)
	{
		string? Key = _Keys.GetValueOrDefault(Name);
		return Key;
	}

	public bool HasKey(string Name)
	{
		bool bHasKey = !string.IsNullOrWhiteSpace(GetKey(Name));
		return bHasKey;
	}

	public void SetKey(string Name, string Key)
	{
		_Keys[Name] = Key;
		Save();
	}

	private void Save()
	{
		string Dir = Path.GetDirectoryName(_ConfigPath)!;
		bool bDirExists = Directory.Exists(Dir);
		if (!bDirExists)
		{
			Directory.CreateDirectory(Dir);
		}

		JsonObject Root = new JsonObject();
		foreach (KeyValuePair<string, string> Pair in _Keys)
		{
			Root[Pair.Key] = Pair.Value;
		}

		File.WriteAllText(_ConfigPath, Root.ToJsonString(_JsonOptions));
	}

	public void Load()
	{
		bool bConfigExists = File.Exists(_ConfigPath);
		if (!bConfigExists)
		{
			return;
		}

		string Json = File.ReadAllText(_ConfigPath);
		JsonNode? Parsed = JsonNode.Parse(Json);
		JsonObject? Root = null != Parsed ? Parsed.AsObject() : null;
		if (null == Root)
		{
			return;
		}

		foreach (KeyValuePair<string, JsonNode?> Field in Root)
		{
			string? Key = null != Field.Value ? Field.Value.GetValue<string>() : null;
			if (null != Key)
			{
				_Keys[Field.Key] = Key;
			}
		}
	}
}