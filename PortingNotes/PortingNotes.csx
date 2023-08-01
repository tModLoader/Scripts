#r "nuget: Newtonsoft.Json, 13.0.3"

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

public class PortingNote
{
	public ChangeType Type;
	public string Title;
	public string Url;
	public string Author;
	public string ArrivesInStable;
	public string ArrivesInPreview;
	[JsonConverter(typeof(StringEnumConverter))]
	public Breakage Breakage;
	public string Summary;
	public string PortingNotes;
	public string DiscordMessageUrl;
}

public enum Breakage
{
	Unknown,
	None,
	Minimal,
	Low,
	Medium,
	High,
}

public enum ChangeType
{
	Undefined,
	Fix,
	Performance,
}