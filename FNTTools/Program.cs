//**************************************************************************************************
// Program.cs                                                                                      *
// Copyright (c) 2019-2020 Aurora Berta-Oldham                                                     *
// This code is made available under the MIT License.                                              *
//**************************************************************************************************

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
            var root = new RootCommand("This program is a tool for working with Angel Code bitmap fonts (.fnt).")
            {
                CreateConvertCommand(),
                CreateInspectCommand()
            };

            return root.Invoke(args);
        }

        private Command CreateConvertCommand()
        {
            var outputOption = new Option<string[]>(new[] { "--output", "-o" }, "The name and location of the output file(s).");
            var forceOption = new Option<bool>(new[] { "--force", "-f" }, "Ignore most errors such as missing files.");
            var overwriteOption = new Option<bool>("--overwrite", "Allow existing files to be overwritten.");

            var formatArgument = new Argument<FormatHint>("format", "The format to convert to.");

            var sourceArgument = new Argument<string[]>("source", "The bitmap font(s) to convert.");

            var convertCommand = new Command("convert", "Change the format used by a bitmap font into either binary, XML, or text.");
            convertCommand.AddArgument(formatArgument);
            convertCommand.AddArgument(sourceArgument);
            convertCommand.AddOption(outputOption);
            convertCommand.AddOption(forceOption);
            convertCommand.AddOption(overwriteOption);

            convertCommand.Handler = CommandHandler.Create<ConvertArgs>(Convert);

            return convertCommand;
        }

        public int Convert(ConvertArgs args)
        {
            if (args.Source == null || args.Source.Length == 0)
            {
                args.Console.Out.WriteLine("No sources specified. Aborting.");
                return 1;
            }

            if (args.Output != null && args.Output.Length != 0 && args.Output.Length != args.Source.Length)
            {
                args.Console.Out.WriteLine($"{args.Output.Length} out of {args.Source.Length} outputs specified. Aborting.");
                return 1;
            }

            for (var i = 0; i < args.Source.Length; i++)
            {
                var sourceFile = args.Source[i];

                if (File.Exists(sourceFile))
                {
                    try
                    {
                        var bitmapFont = BitmapFont.FromFile(sourceFile);
                        var outputFile = args.Output?.ElementAtOrDefault(i) ?? Path.GetFileName(sourceFile);

                        if (File.Exists(outputFile) && !args.Overwrite)
                        {
                            if (args.Force)
                            {
                                args.Console.Out.WriteLine($"File \"{outputFile}\" already exists. Use \"--overwrite\" to allow existing files to be overwritten. Skipping.");
                                continue;
                            }

                            args.Console.Out.WriteLine($"File \"{outputFile}\" already exists. Use \"--overwrite\" to allow existing files to be overwritten. Aborting.");
                            return 1;
                        }

                        bitmapFont.Save(outputFile, args.Format);
                    }
                    catch
                    {
                        args.Console.Out.WriteLine($"Failed to convert bitmap font \"{sourceFile}\".");

                        if (!args.Force)
                        {
                            return 1;
                        }
                    }
                }
                else if (!args.Force)
                {
                    args.Console.Out.WriteLine($"Source file \"{sourceFile}\" was not found. Aborting.");
                    return 1;
                }
            }

            return 0;
        }

        private Command CreateInspectCommand()
        {
            var sourceArgument = new Argument<string>("source", "The bitmap font to inspect.");

            var allOption = new Option<bool>("--all", "Displays all blocks. Not recommended for large fonts.");
            var infoOption = new Option<bool>("--info", "Display the info block.");
            var commonOption = new Option<bool>("--common", "Display the common block.");
            var pagesOption = new Option<bool>("--pages", "Display the pages block.");
            var pageOption = new Option<int[]>(new[] { "--page", "-p" }, "Display a specific page from the pages block.");
            var charactersOption = new Option<bool>("--characters", "Display the characters block.");
            var characterOption = new Option<int[]>(new []{ "--character", "-c" }, "Display a specific character from the characters block.");
            var kerningPairsOption = new Option<bool>("--kerningpairs", "Display the kerning pairs block.");

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

            inspectCommand.Handler = CommandHandler.Create<InspectArgs>(Inspect);

            return inspectCommand;
        }

        public int Inspect(InspectArgs args)
        {
            if (!File.Exists(args.Source))
            {
                args.Console.Out.WriteLine($"Source file \"{args.Source}\" was not found. Aborting.\n");
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
                foreach (var (kerningPair, amount) in bitmapFont.KerningPairs)
                {
                    InspectObject(kerningPair, args.Console);
                    args.Console.Out.WriteLine($"Amount: {amount}");
                    args.Console.Out.WriteLine();
                }
                args.Console.Out.WriteLine();
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
            console.Out.WriteLine($"Character with the ID \"{id}\" does not exist. Skipping.");
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
            console.Out.WriteLine($"Page with the ID \"{id}\" does not exist. Skipping.");
            console.Out.WriteLine();
            return null;
        }

        private void InspectPage(int id, string file, IConsole console)
        {
            console.Out.WriteLine($"ID: {id}");
            console.Out.WriteLine($"File: {file}");
            console.Out.WriteLine();
        }
    }

    public class ConvertArgs
    {
        public FormatHint Format { get; set; }

        public string[] Source { get; set; }

        public string[] Output { get; set; }

        public bool Force { get; set; }

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

        public IConsole Console { get; set; }

        public CancellationToken CancellationToken { get; set; }
    }
}
