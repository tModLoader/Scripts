using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using YamlDotNet.Serialization;
using Path = System.IO.Path;

static class Consts
{
	public const string LogosDir = "./logos";
	public const string InputsDir = "./inputs";
	public const string OutputsDir = "./outputs";
	public const string OptionsFile = "./Screenshots.yaml";
}
struct Options()
{
	public Dictionary<string, ModOptions> Mods = [];
}
struct ModOptions()
{
	public string? Id = null;
	public string? Text = null;
	public string Logo = $"{Consts.LogosDir}/{{id}}.png";
	//public string Images = $"{Consts.InputsDir}/{{id}}*.png";
}

static class Program
{
	static readonly IDeserializer YamlDeserializer = new DeserializerBuilder().WithCaseInsensitivePropertyMatching().Build();

	static void Main()
	{
		Directory.CreateDirectory(Consts.LogosDir);
		Directory.CreateDirectory(Consts.InputsDir);
		Directory.CreateDirectory(Consts.OutputsDir);

		// Andy Bold is expected to be installed in the system.
		var font = SystemFonts.CreateFont("Andy", 64);

		var modIdRegex = new Regex(@"([\w]+?)([\d]+)\.png", RegexOptions.Compiled);
		var options = new Options();
		if (File.Exists(Consts.OptionsFile)) {
			string yaml = File.ReadAllText(Consts.OptionsFile);
			options = YamlDeserializer.Deserialize<Options>(yaml);
		}

		Parallel.ForEach(Directory.EnumerateFiles(Consts.InputsDir, "*.png"), screenshotPath => {
			var modOptions = new ModOptions();
			if (modIdRegex.Match(screenshotPath) is { Success: true } match) {
				var modId = match.Groups[1].Value;
				if (options.Mods.TryGetValue(modId, out var result)) {
					modOptions = result with { Id = modId };
				} else {
					modOptions.Id = modId;
				}
			}

			Console.WriteLine($"Processing '{Path.GetRelativePath(".", screenshotPath)}'...");

			// Load and resize screenshot.
			using var screenshot = Image.Load(screenshotPath);
			screenshot.Mutate(ctx => ctx.Resize(new ResizeOptions {
				Size = new Size(1920),
				Mode = ResizeMode.Max,
			}));

			// Find logo
			Image? logoTemp;
			if (modOptions.Id != null && modOptions.Logo.Replace("{id}", modOptions.Id) is { } logoPath && File.Exists(logoPath)) {
				logoTemp = Image.Load(logoPath);
			} else {
				logoTemp = new Image<Rgba32>(320, 180);
			}
			using var logo = logoTemp;

			// Prepare parameters
			int outlineOffset = (int)((3f / 1920f) * screenshot.Width);
			int outlineIterations = 2;
			var logoMargin = new Point(
				(int)(Math.Max(screenshot.Width, screenshot.Height) / 90f),
				(int)(Math.Max(screenshot.Width, screenshot.Height) / 90f)
			);
			var resizedFont = new Font(font, screenshot.Height / 30f);
			var logoBonusOffset = new Point(outlineOffset * 2, outlineOffset * 2);
			var logoText = !string.IsNullOrEmpty(modOptions.Text) ? modOptions.Text : null;
			if (logoText != null) {
				logoMargin.Y += (int)resizedFont.Size;
				logoBonusOffset.Y += (int)resizedFont.Size;
			}
			var logoBonusSize = new Size(logoBonusOffset.X * 2, logoBonusOffset.Y * 2);

			screenshot.Mutate(ctx => {
				var targetLogoDimensions = new Size(
					(int)(screenshot.Width * 0.25f),
					(int)(screenshot.Height * 0.25f)
				);
				using var scaledLogo = logo.Clone(ctx => ctx.Resize(new ResizeOptions {
					Size = targetLogoDimensions,
					Mode = ResizeMode.Max,
				}));
				using var prettyLogo = scaledLogo.Clone(ctx => {
					var bufferedSize = scaledLogo.Size + logoBonusSize;
					ctx.Resize(new ResizeOptions { Size = bufferedSize, Mode = ResizeMode.BoxPad });

					var fontOptions = new RichTextOptions(resizedFont) {
						Origin = new Vector2(bufferedSize.Width * 0.5f, bufferedSize.Height),
						HorizontalAlignment = HorizontalAlignment.Center,
						VerticalAlignment = VerticalAlignment.Bottom,
					};

					// Glow
					for (int i = 0; i < outlineIterations; i++) {
						ctx.DrawImage(scaledLogo, logoBonusOffset, 1f);
						ctx.Saturate(1.25f);
						ctx.Brightness(2.0f);
						//ctx.Hue(180f);
						if (logoText != null) ctx.DrawText(fontOptions, logoText, Color.Black);
						ctx.BoxBlur(radius: outlineOffset);
					}
					// Albedo
					ctx.DrawImage(scaledLogo, logoBonusOffset, 1f);
					if (logoText != null) ctx.DrawText(fontOptions, logoText, Color.White);
				});

				var logoLocation = new Point(
					screenshot.Width - prettyLogo.Width - logoMargin.X + logoBonusOffset.X,
					screenshot.Height - prettyLogo.Height - logoMargin.Y + logoBonusOffset.Y
				);
				ctx.DrawImage(prettyLogo, logoLocation, 1f);
			});

			string outputPath = Path.Combine(Consts.OutputsDir, Path.GetFileName(screenshotPath));
			screenshot.SaveAsPng(outputPath);
		});
	}
}