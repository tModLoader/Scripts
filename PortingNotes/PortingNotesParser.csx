#r "nuget: Newtonsoft.Json, 13.0.3"
#r "nuget: Hjson, 3.0.0"
#r "nuget: Markdig, 0.31.0"
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
using Markdig;

int numErrors = 0;

void LogWarning(string text)
{
	numErrors++;

	Console.WriteLine(text);
}

var parseMultipleRegex = new Regex(@"(?<=^|\s*)((?:#+[ ]+)?`([\s\S]+?)`\s*by\s*[\s\S]+?)\n(?=(?:#+[ ]+)?`[\s\S]+?`\s*by\s*|\s*$)", RegexOptions.Compiled);

List<PortingNote> ParseMultiple(string input)
{
	var matches = parseMultipleRegex.Matches(input);
	var notes = new List<PortingNote>();

	foreach (Match match in matches) {
		var note = ParseSingle(match.Groups[1].Value.Trim());

		notes.Add(note);
	}

	return notes;
}

var titleAndAuthorRegex = new Regex(
	@"^\s*(?:#+[ ]+)?((`?)[\s\S]+?\2)\s*by\s*((\**)[^\r\n]+\4)",
	RegexOptions.Compiled
);
var fieldsRegex = new Regex(
	@"(?<=[\r\n]|^)(?:(\*+)|## )([^\r\n]+?)(?:\1:|:\1|\1\n+)\s*([\s\S]+?)(?=[\r\n]+|$)",
	RegexOptions.Compiled | RegexOptions.ECMAScript | RegexOptions.Multiline | RegexOptions.IgnoreCase
);
var urlRegex = new Regex(
	@"(?:http[s]?:\/\/)?github\.com\/tModLoader\/tModLoader\/(pull|issues|commit)\/(\w\w?\w?\w?\w?\w?\w?)",
	RegexOptions.Compiled
);
var markdownQuoteRegex = new Regex(
	@"(?<=^|\n)> ",
	RegexOptions.Compiled
);

PortingNote ParseSingle(string input)
{
	var note = new PortingNote();
	var titleAndAuthorMatch = titleAndAuthorRegex.Match(input);
	var fieldsMatches = fieldsRegex.Matches(input);

	note.Title = StripMarkdown(titleAndAuthorMatch.Groups[1].Value);
	note.Author = StripMarkdown(titleAndAuthorMatch.Groups[3].Value);

	string ConvertSummary(string text)
	{
		text = markdownQuoteRegex.Replace(text.Trim(), "");
		text = text.Replace("• ", "- ");

		return text;
	}

	foreach (Match match in fieldsMatches) {
		string fieldName = match.Groups[2].Value.ToLower();
		string fieldValue = match.Groups[3].Value.Trim();
		string fieldValueLower = fieldValue.ToLower();

		switch (fieldName) {
			case "breakage":
				note.Breakage = fieldValueLower switch {
					_ when fieldValueLower.Contains("none") => Breakage.None,
					_ when fieldValueLower.Contains("minimal") => Breakage.Minimal,
					_ when fieldValueLower.Contains("low") => Breakage.Low,
					_ when fieldValueLower.Contains("medium") => Breakage.Medium,
					_ when fieldValueLower.Contains("high") => Breakage.High,
					_ => Breakage.Unknown,
				};

				if (note.Breakage == Breakage.Unknown) {
					LogWarning($"Unknown breakage level: '{fieldValue}'.");
				}

				break;
			case "runtime breakage":
				note.RuntimeBreakage = fieldValueLower switch {
					_ when fieldValueLower.Contains("none") => RuntimeBreakage.None,
					_ when fieldValueLower.Contains("unlikely") => RuntimeBreakage.Unlikely,
					_ when fieldValueLower.Contains("likely") => RuntimeBreakage.Likely,
					_ when fieldValueLower.Contains("guaranteed") => RuntimeBreakage.Guaranteed,
					_ => RuntimeBreakage.Unknown,
				};

				if (note.RuntimeBreakage == RuntimeBreakage.Unknown) {
					LogWarning($"Unknown breakage level: '{fieldValue}'.");
				}

				break;
			case "source breakage":
			case "source code breakage":
			case "source-code breakage":
				note.SourceCodeBreakage = fieldValueLower switch {
					_ when fieldValueLower.Contains("none") => SourceCodeBreakage.None,
					_ when fieldValueLower.Contains("fully covered") => SourceCodeBreakage.None,
					_ when fieldValueLower.Contains("minimal") => SourceCodeBreakage.LowEffort,
					_ when fieldValueLower.Contains("low") => SourceCodeBreakage.LowEffort,
					_ when fieldValueLower.Contains("light") => SourceCodeBreakage.LowEffort,
					_ when fieldValueLower.Contains("average") => SourceCodeBreakage.MediumEffort,
					_ when fieldValueLower.Contains("medium") => SourceCodeBreakage.MediumEffort,
					_ when fieldValueLower.Contains("high") => SourceCodeBreakage.HighEffort,
					_ when fieldValueLower.Contains("heavy") => SourceCodeBreakage.HighEffort,
					_ => SourceCodeBreakage.Unknown,
				};

				if (fieldValueLower.Contains("tmodporter")) {
					note.SourceCodeBreakage |= SourceCodeBreakage.RunTModPorter;
				}

				if (note.SourceCodeBreakage == SourceCodeBreakage.Unknown) {
					LogWarning($"Unknown breakage level: '{fieldValue}'.");
				}

				break;
			case "arrives in stable":
				note.ArrivesInStable = StripMarkdown(fieldValue);
				break;
			case "arrives in preview":
				note.ArrivesInPreview = StripMarkdown(fieldValue);
				break;
			case "summary":
			case "short summary":
				note.Summary = ConvertSummary(fieldValue);
				break;
			case "porting notes":
				note.PortingNotes = ConvertSummary(fieldValue);
				break;
			case "pr":
			case "pull request":
			case "commit":
				var urlMatch = urlRegex.Match(fieldValue);

				if (urlMatch.Success) {
					note.Url = @$"https://github.com/tModLoader/tModLoader/{urlMatch.Groups[1].Value}/{urlMatch.Groups[2].Value}";
				} else {
					LogWarning($"Unable to parse URL: '{fieldName}'.");
					note.Url = fieldValue;
				}

				break;
			default:
				LogWarning($"Unknown field: '{fieldName}'.");
				break;
		}
	}

	return note;
}

string StripMarkdown(string input)
{
	return Markdown.ToPlainText(input.Trim()).Trim();
}

var input = File.ReadAllText("PortingNotesParser_Input.md");
var notes = ParseMultiple(input);
var outputJsonArray = new JArray();

foreach (var note in notes) {
	var noteObject = JObject.FromObject(note);

	outputJsonArray.Add(noteObject);
}

string outputJsonText = outputJsonArray.ToString();
string outputHjsonText = JsonValue.Parse(outputJsonText).ToString(Stringify.Hjson);

outputHjsonText = outputHjsonText.Replace("  ", "\t");

File.WriteAllText("PortingNotesParser_Output.hjson", outputHjsonText);

if (numErrors != 0) {
	Console.WriteLine();
	Console.WriteLine($"{numErrors} errors occurred.");
	Console.ReadLine();
}