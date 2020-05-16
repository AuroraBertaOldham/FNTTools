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
            var root = new RootCommand("Copyright (c) 2019-2020 Aurora Berta-Oldham")
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

            var convertCommand = new Command("convert", "Change the format used by a .fnt bitmap font into either binary, XML, or text.");
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
            var charactersOption = new Option<bool>("--characters", "Display the characters block.");
            var kerningPairsOption = new Option<bool>("--kerningpairs", "Display the kerning pairs block.");

            var inspectCommand = new Command("inspect", "Inspects the properties of a .fnt bitmap font.");
            inspectCommand.AddArgument(sourceArgument);
            inspectCommand.AddOption(allOption);
            inspectCommand.AddOption(infoOption);
            inspectCommand.AddOption(commonOption);
            inspectCommand.AddOption(pagesOption);
            inspectCommand.AddOption(charactersOption);
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

            if (args.All || args.Info)
            {
                args.Console.Out.WriteLine("Info Block:");
                InspectObject(bitmapFont.Info, args.Console);
                args.Console.Out.WriteLine();
            }

            if (args.All || args.Common)
            {
                args.Console.Out.WriteLine("Common Block:");
                InspectObject(bitmapFont.Common, args.Console);
                args.Console.Out.WriteLine();
            }

            if (args.All || args.Pages)
            {
                args.Console.Out.WriteLine("Pages Block:");
                foreach (var (id, file) in bitmapFont.Pages)
                { 
                    args.Console.Out.WriteLine($"ID: {id}");
                    args.Console.Out.WriteLine($"File: {file}");
                    args.Console.Out.WriteLine();
                }
            }

            if (args.All || args.Characters)
            {
                args.Console.Out.WriteLine("Characters Block:");
                foreach (var (id, character) in bitmapFont.Characters)
                {
                    var characterID = (char)id;

                    args.Console.Out.WriteLine($"ID: {id}");

                    if (!char.IsControl(characterID) && !char.IsWhiteSpace(characterID))
                    {
                        args.Console.Out.WriteLine($"Character: {characterID}");
                    }

                    InspectObject(character, args.Console);
                    args.Console.Out.WriteLine();
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

        public bool Characters { get; set; }

        public bool KerningPairs { get; set; }

        public IConsole Console { get; set; }

        public CancellationToken CancellationToken { get; set; }
    }
}
