#r "nuget: Newtonsoft.Json, 13.0.3"

using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Net;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

// Console utilities

public readonly record struct ColoredText(ConsoleColor Color, string Value)
{
	public static implicit operator ColoredText(string text) => new(ConsoleColor.Gray, text);
	public static implicit operator ColoredText((ConsoleColor color, object obj) tuple) => new(tuple.color, tuple.obj.ToString());
}

static void PrintLine(params ColoredText[] coloredTexts)
{
	foreach (var text in coloredTexts) {
		Console.ForegroundColor = text.Color;
		Console.Write(text.Value);
	}

	Console.ForegroundColor = ConsoleColor.Gray;
	Console.Write("\r\n");
}

Console.OutputEncoding = System.Text.Encoding.UTF8;

// Parsing utils

private static Regex versionRegex = new Regex(@"(?:tModLoader v)?([\d.]+)", RegexOptions.Compiled);

static Version ParseVersion(string versionString)
{
	var match = versionRegex.Match(versionString);

	if (!match.Success || !Version.TryParse(match.Groups[1].Value, out var version)) {
		throw new InvalidOperationException($"Invalid version: '{versionString}'.");
	}

	return version;
}

// Mod data

// For some reason, "2023.4.0.0" is not equal to and is more than "2023.4".
// Do not include more than 2 numbers, even if they're zeroes.
private static Version minVersion = new Version(2023, 4);

public class ModInfo
{
	public JObject Json;
	public (Version ModVersion, Version TmlVersion)[] Versions;
	public Version OldestTmlVersion;
	public Version NewestTmlVersion;
	public string InternalName;
	public string DisplayName;
	public string AuthorName;
	public int SubscriberCount;

	public ModInfo(JObject json)
	{
		Json = json;
		InternalName = (string)json["internal_name"];
		DisplayName = (string)json["display_name"];
		AuthorName = (string)json["author"];
		SubscriberCount = (int)json["downloads_total"];
		
		Versions = json["versions"]
			.Select(pair => (ParseVersion((string)pair["mod_version"]), ParseVersion((string)pair["tmodloader_version"])))
			.ToArray();

		OldestTmlVersion = Versions.Select(v => v.TmlVersion).Min();
		NewestTmlVersion = Versions.Select(v => v.TmlVersion).Max();

		if (string.IsNullOrWhiteSpace(AuthorName)) {
			AuthorName = "Unknown";
		}
	}
}

// Handling input

string inputString = string.Join(' ', Args);

if (string.IsNullOrWhiteSpace(inputString)) {
	inputString = @"https://tmlapis.repl.co/1.4/list";
}

string inputJson;

if (Uri.TryCreate(inputString, UriKind.Absolute, out var url)) {
	Console.WriteLine($"Downloading json from URL '{url}'...");

	using var webClient = new WebClient();

	inputJson = webClient.DownloadString(url);
} else {
	Console.WriteLine($"Loading json from '{inputString}'...");

	inputJson = File.ReadAllText(inputString);
}

// 

var jArray = JArray.Parse(inputJson);

var mods = jArray.Children().Select(j => new ModInfo((JObject)j)).ToArray();

var portedMods = mods.Where(m => m.NewestTmlVersion >= minVersion).OrderByDescending(m => m.SubscriberCount).ToArray();
var outdatedMods = mods.Where(m => m.NewestTmlVersion < minVersion).OrderByDescending(m => m.SubscriberCount).ToArray();

int portedSubscriberCount = portedMods.Sum(m => m.SubscriberCount);
int outdatedSubscriberCount = outdatedMods.Sum(m => m.SubscriberCount);
int totalSubscriptionCount = portedSubscriberCount + outdatedSubscriberCount;

const string ColorStr = "\u001b[35m";
const string ColorRst = "\u001b[0m";

PrintLine((ConsoleColor.Cyan, "Total mod count: "), (ConsoleColor.Yellow, $"{mods.Length:n0}"));
PrintLine((ConsoleColor.Cyan, "Ported mod count: "), (ConsoleColor.Yellow, $"{portedMods.Length:n0}"));
PrintLine((ConsoleColor.Cyan, "Outdated mod count: "), (ConsoleColor.Yellow, $"{outdatedMods.Length:n0}"));
PrintLine((ConsoleColor.Cyan, "Percentage ported: "), (ConsoleColor.Yellow, $"{(portedMods.Length / (double)mods.Length) * 100.0:0.00}%"));
PrintLine();
PrintLine((ConsoleColor.Cyan, "Total mod subscriptions: "), (ConsoleColor.Yellow, $"{totalSubscriptionCount:n0}"));
PrintLine((ConsoleColor.Cyan, "Ported mods in subscribers: "), (ConsoleColor.Yellow, $"{portedSubscriberCount:n0}"));
PrintLine((ConsoleColor.Cyan, "Outdated mods in subscribers: "), (ConsoleColor.Yellow, $"{outdatedSubscriberCount:n0}"));
PrintLine((ConsoleColor.Cyan, "Percentage ported in subscribers: "), (ConsoleColor.Yellow, $"{(portedSubscriberCount / (double)totalSubscriptionCount) * 100.0:0.00}%"));
PrintLine();

const int TopListingCount = 50;

void ListTopMods(IEnumerable<ModInfo> mods)
{
	foreach (var mod in mods) {
double subsDividedByTotalSubCount = (mod.SubscriberCount / (double)totalSubscriptionCount) * 100.0;
double subsDividedByLifetimeTmlUsers = (mod.SubscriberCount / (double)6_490_835) * 100.0;

		PrintLine("- ", (ConsoleColor.Cyan, $"{mod.InternalName}: "), (ConsoleColor.Yellow, $"{subsDividedByTotalSubCount:0.00}%"), " or ", (ConsoleColor.Magenta, $"{subsDividedByLifetimeTmlUsers:0.00}%"), " - by ", mod.AuthorName);
	}
}

PrintLine("Top ", (ConsoleColor.Yellow, $"{TopListingCount}"), " ported mods by percentage of all subscriptions:");
PrintLine();
ListTopMods(portedMods.Take(TopListingCount));

PrintLine();
PrintLine();

PrintLine("Top ", (ConsoleColor.Yellow, $"{TopListingCount}"), " outdated mods by percentage of all subscriptions:");
PrintLine();
ListTopMods(outdatedMods.Take(TopListingCount));
