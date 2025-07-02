using EmojiGenerator;
using System.CommandLine;

internal class Program {
  private static int Main(string[] args) {
    string? outputPath = null;

    Option<string?> outputPathOption = new("--outputPath", ["-o"]) {
      Description = "The generated code output path."
    };

    RootCommand rootCommand = new() {
      Description = "A C# emoji known sequences code generator."
    };

    rootCommand.Options.Add(outputPathOption);

    rootCommand.SetAction(parseResult => {
      outputPath = parseResult.GetValue(outputPathOption);

      return new Generator(outputPath).Generate();
    });

    return rootCommand.Parse(args).Invoke();
  }
}