// AuthConfig.cs

using System.Text.Json;
using System.Text.Json.Nodes;

namespace MyAgent.Backend.Auth;

public sealed class AuthConfig
{
	// API Key 파일(AuthConfig.json)을 읽고 쓰는 클래스
	// AuthConfig 클래스는 자신이 저장한 API Key가 어떤 회사의 API Key인지는 모른다.
	// AuthConfig 클래스의 메서드나 멤버 변수를 사용하는 쪽에서 알아서 사용한다.

	private readonly string ConfigPath = Path.Combine(
		Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
		".myagent", "AuthConfig.json");
		// AuthConfig.json 파일 경로 문자열. 해당 파일이 프로젝트 폴더나 깃 레포에 포함되지 않도록 주의.

	private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions { WriteIndented = true };
		// JSON 직렬화 옵션. JSON을 저장할 때 들여쓰기를 넣는 옵션. 사람이 읽기 좋아짐.

	private readonly Dictionary<string, string> Keys = new Dictionary<string, string>();
		// 파일에 있던 키-벨류 쌍(claude_api_key: "sk_...")은 모두 담아 둠.
		// 파일 저장할 때 Keys에 있는 쌍들을 모두 작성.

	public string? GetKey(string Name)
	{
		// API Key Getter

		return Keys.GetValueOrDefault(Name);
	}

	public bool HasKey(string Name)
	{
		// API Key가 있는지 확인

		return !string.IsNullOrWhiteSpace(GetKey(Name));
	}

	public void SetKey(string Name, string Key)
	{
		// API Key 추가

		Keys[Name] = Key;
		Save();
	}

	private void Save()
	{
		// API Key들을 파일에 쓰기. 디렉토리가 없으면 생성.

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
		// AuthConfig.json 파일 읽기.

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