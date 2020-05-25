//**************************************************************************************************
// Program.cs                                                                                      *
// Copyright (c) 2019-2020 Aurora Berta-Oldham                                                     *
// This code is made available under the MIT License.                                              *
//**************************************************************************************************

using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.IO;
using System.IO;
using System.Linq;
using System.Threading;
using SharpFNT;

namespace FNTTools
{
    public class Program
    {
        private static int Main(string[] args)
        {
            var program = new Program();
            return program.Run(args);
        }

        public int Run(string[] args)
        {
            var root = new RootCommand("This program is a tool for working with AngelCode bitmap fonts (.fnt).")
            {
                CreateConvertCommand(),
                CreateInspectCommand()
            };

            return root.Invoke(args);
        }

        private Command CreateConvertCommand()
        {
            var outputOption = new Option<string[]>(new[] { "--output", "-o" }, "The name and location of the output file(s).");
            var overwriteOption = new Option<bool>("--overwrite", "Allow existing files to be overwritten.");

            var formatArgument = new Argument<FormatHint>("format", "The format to convert to.");
            var sourceArgument = new Argument<string[]>("source", "The bitmap font(s) to convert.")
            {
                Arity = new ArgumentArity(1, int.MaxValue)
            };

            var convertCommand = new Command("convert", "Change the format used by a bitmap font into either binary, XML, or text.");

            convertCommand.AddArgument(formatArgument);
            convertCommand.AddArgument(sourceArgument);
            convertCommand.AddOption(outputOption);
            convertCommand.AddOption(overwriteOption);

            convertCommand.Handler = CommandHandler.Create<ConvertArgs>(Convert);

            return convertCommand;
        }

        public int Convert(ConvertArgs args)
        {
            var result = 0;

            for (var i = 0; i < args.Source.Length; i++)
            {
                var sourceFile = args.Source[i];

                try
                {
                    if (!File.Exists(sourceFile))
                    {
                        args.Console.Out.WriteLine($"Source file \"{sourceFile}\" does not exist. Skipping.");
                        result = 1;
                        continue;
                    }

                    var outputFile = args.Output?.ElementAtOrDefault(i) ?? Path.GetFileName(sourceFile);

                    if (!args.Overwrite && File.Exists(outputFile))
                    {
                        args.Console.Out.WriteLine($"File \"{outputFile}\" already exists. Use \"--overwrite\" to allow existing files to be overwritten. Skipping.");
                        result = 1;
                        continue;
                    }

                    BitmapFont.FromFile(sourceFile).Save(outputFile, args.Format);
                }
                catch (Exception exception)
                {
                    args.Console.Out.WriteLine($"Failed to convert \"{sourceFile}\" due to an unhandled exception.");
                    args.Console.Out.WriteLine("Please leave an issue at https://github.com/AuroraBertaOldham/FNTTools/issues.");
                    args.Console.Out.WriteLine("Writing exception and aborting.");
                    args.Console.Out.WriteLine(exception.ToString());
                    result = 1;
                    break;
                }
            }

            return result;
        }

        private Command CreateInspectCommand()
        {
            var sourceArgument = new Argument<string>("source", "The bitmap font to inspect.");

            var allOption = new Option<bool>("--all", "Display all blocks.");
            var infoOption = new Option<bool>("--info", "Display the info block.");
            var commonOption = new Option<bool>("--common", "Display the common block.");
            var pagesOption = new Option<bool>("--pages", "Display the pages block.");
            var pageOption = new Option<int[]>(new[] { "--page", "-p" }, "Display a specific page from the pages block.");
            var charactersOption = new Option<bool>("--characters", "Display the characters block.");
            var characterOption = new Option<int[]>(new []{ "--character", "-c" }, "Display a specific character from the characters block.");
            var kerningPairsOption = new Option<bool>("--kerningpairs", "Display the kerning pairs block.");
            var kerningPairOption = new Option<int[]>(new[] { "--kerningpair", "-k" }, "Display a specific kerning pair from the kerning pairs block.");

            var inspectCommand = new Command("inspect", "Inspects the properties of a bitmap font.");

            inspectCommand.AddArgument(sourceArgument);
            inspectCommand.AddOption(allOption);
            inspectCommand.AddOption(infoOption);
            inspectCommand.AddOption(commonOption);
            inspectCommand.AddOption(pagesOption);
            inspectCommand.AddOption(pageOption);
            inspectCommand.AddOption(charactersOption);
            inspectCommand.AddOption(characterOption);
            inspectCommand.AddOption(kerningPairsOption);
            inspectCommand.AddOption(kerningPairOption);

            inspectCommand.Handler = CommandHandler.Create<InspectArgs>(Inspect);

            return inspectCommand;
        }

        public int Inspect(InspectArgs args)
        {
            if (!File.Exists(args.Source))
            {
                args.Console.Out.WriteLine($"Source file \"{args.Source}\" does not exist. Aborting.");
                return 1;
            }

            if (args.KerningPair != null && args.KerningPair.Length % 2 != 0)
            {
                args.Console.Out.WriteLine("Invalid number of arguments for -k/--kerningpair. A left and right value should be passed for each pair. Aborting.");
                return 1;
            }

            var bitmapFont = BitmapFont.FromFile(args.Source);

            // If info or common blocks are missing just return a new instance to print default values.

            if (args.All || args.Info)
            {
                args.Console.Out.WriteLine("Info Block:");
                InspectObject(bitmapFont.Info ?? new BitmapFontInfo(), args.Console);
                args.Console.Out.WriteLine();
            }

            if (args.All || args.Common)
            {
                args.Console.Out.WriteLine("Common Block:");
                InspectObject(bitmapFont.Common ?? new BitmapFontCommon(), args.Console);
                args.Console.Out.WriteLine();
            }

            if (args.All || args.Pages)
            {
                args.Console.Out.WriteLine("Pages Block:");
                if (bitmapFont.Pages != null)
                {
                    foreach (var (id, file) in bitmapFont.Pages)
                    {
                        InspectPage(id, file, args.Console);
                    }
                }

                if (args.Page != null)
                {
                    foreach (var id in args.Page)
                    {
                        GetPageFileWithErrorMessage(id, bitmapFont, args.Console);
                    }
                }

            }
            else if (args.Page != null && args.Page.Length > 0)
            {
                args.Console.Out.WriteLine("Selected Pages:");
                foreach (var id in args.Page)
                {
                    var file = GetPageFileWithErrorMessage(id, bitmapFont, args.Console);
                    if (file != null)
                    {
                        InspectPage(id, file, args.Console);
                    }
                }
            }

            if (args.All || args.Characters)
            {
                args.Console.Out.WriteLine("Characters Block:");

                if (bitmapFont.Characters != null)
                {
                    foreach (var (id, character) in bitmapFont.Characters)
                    {
                        InspectCharacter(id, character, args.Console);
                    }
                }

                if (args.Character != null)
                {
                    foreach (var id in args.Character)
                    {
                        GetCharacterWithErrorMessage(id, bitmapFont, args.Console);
                    }
                }
            }
            else if (args.Character != null && args.Character.Length > 0)
            {
                args.Console.Out.WriteLine("Selected Characters:");

                foreach (var id in args.Character)
                {
                    var character = GetCharacterWithErrorMessage(id, bitmapFont, args.Console);
                    if (character != null)
                    {
                        InspectCharacter(id, character, args.Console);
                    }
                }
            }

            if (args.All || args.KerningPairs)
            {
                args.Console.Out.WriteLine("Kerning Pairs Block:");
                if (bitmapFont.KerningPairs != null)
                {
                    foreach (var (kerningPair, amount) in bitmapFont.KerningPairs)
                    {
                        InspectKerningPair(kerningPair, amount, args.Console);
                    }
                }

                if (args.KerningPair != null)
                {
                    for (var i = 0; i < args.KerningPair.Length; i += 2)
                    {
                        var kerningPair = new KerningPair(args.KerningPair[i], args.KerningPair[i + 1]);
                        GetKerningPairAmountWithErrorMessage(kerningPair, bitmapFont, args.Console);
                    }
                }
            }
            else if (args.KerningPair != null && args.KerningPair.Length > 0)
            {
                args.Console.Out.WriteLine("Selected Kerning Pairs:");
                for (var i = 0; i < args.KerningPair.Length; i += 2)
                {
                    var kerningPair = new KerningPair(args.KerningPair[i], args.KerningPair[i + 1]);
                    var amount = GetKerningPairAmountWithErrorMessage(kerningPair, bitmapFont, args.Console);
                    if (amount != null)
                    {
                        InspectKerningPair(kerningPair, amount.Value, args.Console);
                    }
                }
            }

            return 0;
        }

        private void InspectObject(object value, IConsole console)
        {
            var type = value.GetType();
            foreach (var propertyInfo in type.GetProperties())
            {
                console.Out.WriteLine($"{propertyInfo.Name}: {propertyInfo.GetValue(value)}");
            }
        }

        private Character GetCharacterWithErrorMessage(int id, BitmapFont bitmapFont, IConsole console)
        {
            if (bitmapFont.Characters != null && bitmapFont.Characters.TryGetValue(id, out var character)) return character;
            console.Out.WriteLine($"Character with the ID \"{id}\" does not exist.");
            console.Out.WriteLine();
            return null;
        }

        private void InspectCharacter(int id, Character character, IConsole console)
        {
            var characterID = (char)id;

            console.Out.WriteLine($"ID: {id}");

            if (char.IsControl(characterID))
            {
                console.Out.WriteLine("Character Type: Control");
            }
            else if (char.IsWhiteSpace(characterID))
            {
                console.Out.WriteLine("Character Type: Whitespace");
            }
            else
            {
                console.Out.WriteLine($"Character: {characterID}");
            }

            InspectObject(character, console);
            console.Out.WriteLine();
        }

        private string GetPageFileWithErrorMessage(int id, BitmapFont bitmapFont, IConsole console)
        {
            if (bitmapFont.Pages != null && bitmapFont.Pages.TryGetValue(id, out var file)) return file;
            console.Out.WriteLine($"Page with the ID \"{id}\" does not exist.");
            console.Out.WriteLine();
            return null;
        }

        private void InspectPage(int id, string file, IConsole console)
        {
            console.Out.WriteLine($"ID: {id}");
            console.Out.WriteLine($"File: {file}");
            console.Out.WriteLine();
        }

        private int? GetKerningPairAmountWithErrorMessage(KerningPair kerningPair, BitmapFont bitmapFont, IConsole console)
        {
            if (bitmapFont.KerningPairs != null && bitmapFont.KerningPairs.TryGetValue(kerningPair, out var amount)) return amount;
            console.Out.WriteLine($"Kerning Pair with the first \"{kerningPair.First}\" and the second \"{kerningPair.Second}\" does not exist.");
            console.Out.WriteLine();
            return null;
        }

        private void InspectKerningPair(KerningPair kerningPair, int amount, IConsole console)
        {
            InspectObject(kerningPair, console);
            console.Out.WriteLine($"Amount: {amount}");
            console.Out.WriteLine();
        }
    }

    public class ConvertArgs
    {
        public FormatHint Format { get; set; }

        public string[] Source { get; set; }

        public string[] Output { get; set; }

        public bool Overwrite { get; set; }

        public IConsole Console { get; set; }

        public CancellationToken CancellationToken { get; set; }
    }

    public class InspectArgs
    {
        public string Source { get; set; }

        public bool All { get; set; }

        public bool Info { get; set; }

        public bool Common { get; set; }

        public bool Pages { get; set; }

        public int[] Page { get; set; }

        public bool Characters { get; set; }

        public int[] Character { get; set; }

        public bool KerningPairs { get; set; }

        public int[] KerningPair { get; set; }

        public IConsole Console { get; set; }

        public CancellationToken CancellationToken { get; set; }
    }
}
