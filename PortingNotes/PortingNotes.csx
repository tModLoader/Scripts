#r "nuget: Newtonsoft.Json, 13.0.3"

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

public class PortingNote
{
	[JsonConverter(typeof(StringEnumConverter))]
	[JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
	public ChangeType Type;

	public string Title;
	public string Url;
	public string Author;
	public string ArrivesInStable;
	public string ArrivesInPreview;

	// Obsolete
	[JsonConverter(typeof(StringEnumConverter))]
	[JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
	public Breakage Breakage = Breakage.Unknown;

	[JsonConverter(typeof(StringEnumConverter))]
	[JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
	public RuntimeBreakage RuntimeBreakage = RuntimeBreakage.Unknown;

	[JsonConverter(typeof(StringEnumConverter))]
	[JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
	public SourceCodeBreakage SourceCodeBreakage = SourceCodeBreakage.Unknown;

	public string Summary;
	public string PortingNotes;
	public string DiscordMessageUrl;
}

public enum Breakage
{
	Unknown = 0,
	None,
	Minimal,
	Low,
	Medium,
	High,
}

public enum RuntimeBreakage
{
	Unknown = 0,
	None,
	Unlikely,
	Likely,
	Guaranteed,
}

[Flags]
public enum SourceCodeBreakage
{
	// Messy because Unknown has to be zero.
	Unknown = 0,
	RunTModPorter = 1,
	None = 2,
	LowEffort = 4,
	LowEffortWithTModPorter = LowEffort | RunTModPorter,
	MediumEffort = 8,
	MediumEffortWithTModPorter = MediumEffort | RunTModPorter,
	HighEffort = 16,
	HighEffortWithTModPorter = HighEffort | RunTModPorter,
}

public enum ChangeType
{
	Undefined,
	Fix,
	Performance,
}