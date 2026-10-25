// Program.cs

// Console.WriteLine("Hello, World!");

using Microsoft.Extensions.DependencyInjection;
using MyAgent.Backend.Auth;

ServiceCollection Services = new ServiceCollection();
Services.AddSingleton<AuthConfig>();
	// ServiceCollition에 AuthConfig 객체를 Singlton으로 서비스 등록. 

ServiceProvider ServiceProvider = Services.BuildServiceProvider();
	// BuildServiceProvider() 함수를 통해 등록 마무리.

AuthConfig Auth = ServiceProvider.GetRequiredService<AuthConfig>();
	// GetRequiredService<>() 함수를 통해 서비스 꺼냄. 

Auth.Load();

const string ClaudeKeyName = "claude_api_key";

if (!Auth.HasKey(ClaudeKeyName))
{
	Console.Write("Claude API Key를 입력하세요: ");
	string? Key = Console.ReadLine();

	if (string.IsNullOrWhiteSpace(Key))
	{
		Console.WriteLine("Claude API Key가 입력되지 않았습니다.");
		return;
	}

	Auth.SetKey(ClaudeKeyName, Key);
	Console.WriteLine("Claude API Key 저장 완료!");
}
