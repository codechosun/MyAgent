// Block.cs

using MyAgent.Backend.Provider;

namespace MyAgent.Backend.Core;

public abstract record Block
{
	// 우리만의 Content Block 타입.
	// Claude SDK에도 Block 타입이 있으나
	// 생각 내용과 서명 내용이 따로따로 오는 등 들고다니기 불편함.
	// 무엇보다 Claude SDK의 타입을 그대로 사용하면 Claude에 종속됨.
	// 그래서 필요한 정보만 담은 우리 타입으로 한 번 감싸고자 함.
	// 특히 Thinking 블록은 생각 내용과 되돌려 보낼 데이터(서명 내용이 담긴 Payload)를 한데 묶음.
	// 5장에서는 Tool Call(Tool Use) 블록을 추가할 예정.

	public sealed record Text(string Content) : Block;

	public sealed record Thinking(string Content, VendorPayload? VendorPayload) : Block;
	
}