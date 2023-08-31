#r "nuget: Newtonsoft.Json, 13.0.3"
#r "nuget: Hjson, 3.0.0"
#load "PortingNotes.csx"

using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Net;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Hjson;

record class Writer(Action<Dictionary<ChangeType, List<PortingNote>>, StringBuilder> Function, string FileName);

var writers = new Writer[] {
	new(WriteNotesForSteam, "PortingNotesConverter_Steam.txt"),
	new(WriteNotesForDiscord, "PortingNotesConverter_Discord.md"),
};

var lineBreakOrStartRegex = new Regex(@"(^|\n)", RegexOptions.Compiled);

void WriteNotesForDiscord(Dictionary<ChangeType, List<PortingNote>> notesByType, StringBuilder sb)
{
	bool insertLineBreak = false;

	foreach (var note in notesByType.SelectMany(p => p.Value)) {
		if (insertLineBreak) {
			sb.AppendLine();
		} else {
			insertLineBreak = true;
		}

		WriteNoteForDiscord(note, sb);
	}
}

void WriteNoteForDiscord(PortingNote note, StringBuilder sb)
{
	string Enquote(string text)
		=> lineBreakOrStartRegex.Replace(text, @"$1> ");

	sb.AppendLine($"# `{note.Title}`");
	sb.AppendLine($"by **{note.Author}** has been merged.");
	sb.AppendLine($"**Pull Request**: <{note.Url}>");

	if (!string.IsNullOrWhiteSpace(note.ArrivesInPreview)) {
		sb.AppendLine($"**Arrives in Preview**: `{note.ArrivesInPreview}`");
	}

	if (!string.IsNullOrWhiteSpace(note.ArrivesInStable)) {
		sb.AppendLine($"**Arrives in Stable**: `{note.ArrivesInStable}`");
	}

	if (note.Breakage != Breakage.Unknown) {
		string color = note.Breakage switch {
			Breakage.None or Breakage.Minimal => "🟢",
			Breakage.Low => "🟡",
			Breakage.Medium => "🟠",
			Breakage.High => "🔴",
			_ => "💀",
		};

		sb.AppendLine($"**Breakage**: {color} - **{note.Breakage}**");
	}

	if (note.RuntimeBreakage != RuntimeBreakage.Unknown) {
		string color = note.RuntimeBreakage switch {
			RuntimeBreakage.None => "🟢",
			RuntimeBreakage.Unlikely => "🟡",
			RuntimeBreakage.Likely => "🟠",
			RuntimeBreakage.Guaranteed => "🔴",
			_ => "💀",
		};

		sb.AppendLine($"**Runtime Breakage**: {color} - **{note.RuntimeBreakage}**");
	}

	if (note.SourceCodeBreakage != SourceCodeBreakage.Unknown) {
		(string prefix, string text) = note.SourceCodeBreakage switch {
			SourceCodeBreakage.None => ("🟢", "None"),
			SourceCodeBreakage.RunTModPorter => ("🟢🤖", "Fully covered by tModPorter"),
			SourceCodeBreakage.LowEffortWithTModPorter => ("🟡🤖", "Light effort required; Partially covered by tModPorter"),
			SourceCodeBreakage.LowEffort => ("🟡", "Light effort required; Not covered by tModPorter"),
			SourceCodeBreakage.MediumEffortWithTModPorter => ("🟠🤖", "Medium effort required; Partially covered by tModPorter"),
			SourceCodeBreakage.MediumEffort => ("🟠", "Medium effort required; Not covered by tModPorter"),
			SourceCodeBreakage.HighEffortWithTModPorter => ("🔴🤖", "High effort required; Partially covered by tModPorter"),
			SourceCodeBreakage.HighEffort => ("💀", "High effort required; Not covered by tModPorter"),
			_ => ("❓", "Umm..."),
		};

		sb.AppendLine($"**Source-code Breakage**: {prefix} - **{text}**");
	}

	if (!string.IsNullOrWhiteSpace(note.Summary)) {
		sb.AppendLine();
		sb.AppendLine($"## Short Summary");
		sb.AppendLine($"{Enquote(note.Summary)}");
	}

	if (!string.IsNullOrWhiteSpace(note.PortingNotes)) {
		sb.AppendLine();
		sb.AppendLine($"## Porting Notes");
		sb.AppendLine($"{Enquote(note.PortingNotes)}");
	}
}

var markdownCodeLineRegex = new Regex(@"`([\s\S]+?)`", RegexOptions.Compiled);
var markdownBoldRegex = new Regex(@"\*\*([\s\S]+?)\*\*", RegexOptions.Compiled);
var markdownItalicsRegex = new Regex(@"\*([\s\S]+?)\*", RegexOptions.Compiled);
var markdownBoldItalicsRegex = new Regex(@"\*\*\*([\s\S]+?)\*\*\*", RegexOptions.Compiled);
var markdownListRegex = new Regex(@"(?<=^|\n)- ", RegexOptions.Compiled);
var markdownEscapedLinkRegex = new Regex(@"<http[s]?:\/\/([^\r\n]+?)>", RegexOptions.Compiled);

void WriteNotesForSteam(Dictionary<ChangeType, List<PortingNote>> notesByType, StringBuilder sb)
{
	foreach (var pair in notesByType) {
		string heading = pair.Key switch {
			ChangeType.Undefined => "New Changes' Highlights",
			ChangeType.Performance => "Performance Improvements",
			ChangeType.Fix => "Bug Fixes",
		};

		sb.AppendLine($"[h1]{heading}[/h1]");
		sb.AppendLine();

		foreach (var note in pair.Value) {
			WriteNoteForSteam(note, sb);
		}

		sb.AppendLine();
	}
}

void WriteNoteForSteam(PortingNote note, StringBuilder sb)
{
	string ConvertFromMarkdown(string text)
	{
		text = markdownCodeLineRegex.Replace(text, @"[u]$1[/u]");
		text = markdownBoldItalicsRegex.Replace(text, @"[b][i]$1[/i][/b]");
		text = markdownBoldRegex.Replace(text, @"[b]$1[/b]");
		text = markdownItalicsRegex.Replace(text, @"[i]$1[/i]");
		text = markdownListRegex.Replace(text, @"• ");
		text = markdownEscapedLinkRegex.Replace(text, @"https://$1");

		return text;
	}

	/*
	[h3][b][url=https://github.com/tModLoader/tModLoader/pull/2645]Modernize PlayerLoader, support custom ModPlayer hooks[/url][/b] by [b]Mirsario[/b][/h3]
	[quote]• Allows mods to create custom interface hooks for [i][u]ModPlayer[/u][/i] instances, in line with support on [i][u]GlobalItem[/u][/i], [i][u]GlobalNPC[/u][/i], and [i][u]GlobalProjectile[/u][/i].
	• [i][u]PlayerLoader[/u][/i] backends modernized and streamlined. You may get whooping 0.01% more performance!
	• [i][u]Player.ModPlayers[/u][/i] getter property added. Don't go crazy.

	[b]Examples:[/b] [url=https://discord.com/channels/103110554649894912/928283223077830686/992428081656627240]#preview-update-log in Discord[/url][/quote]
	*/

	sb.AppendLine($"[h3][b][url={note.Url}]{note.Title}[/url][/b] by [b]{note.Author}[/b][/h3]");
	sb.Append("[quote]");
	sb.Append(ConvertFromMarkdown(note.Summary));

	if (!string.IsNullOrWhiteSpace(note.PortingNotes)) { // && !string.IsNullOrWhiteSpace(note.DiscordMessageUrl)) {
		sb.AppendLine();
		sb.AppendLine();
		sb.Append($"[b]Porting Notes:[/b] [url={note.DiscordMessageUrl}]#preview-update-log in Discord[/url]");
	}

	sb.Append("[/quote]");
}

string inputPath = Args.Count != 0 ? string.Join(' ', Args) : "PortingNotesParser_Output.hjson";

Console.WriteLine($"Loading '{inputPath}'.");

string inputText = File.ReadAllText(inputPath);
string jsonString = HjsonValue.Parse(inputText).ToString();

var json = JToken.Parse(jsonString);
var jsonObjects = json is JArray jsonArray ? jsonArray.Cast<JObject>() : new[] { (JObject)json };
var notes = jsonObjects.Select(o => o.ToObject<PortingNote>()).ToArray();
var notesByType = new Dictionary<ChangeType, List<PortingNote>>();

foreach (var note in notes) {
	if (!notesByType.TryGetValue(note.Type, out var list)) {
		notesByType[note.Type] = list = new();
	}

	list.Add(note);
}

foreach (var writer in writers) {
	var output = new StringBuilder();

	writer.Function(notesByType, output);

	File.WriteAllText(writer.FileName, output.ToString());
}