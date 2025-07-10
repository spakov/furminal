using Spakov.EmojiGenerator;
using System.CommandLine;

internal class Program {
  private static int Main(string[] args) {
    Option<string?> outputPathOption = new("--outputPath", ["-o"]) {
      Description = "The generated code output path."
    };

    Option<string> namespaceOption = new("--namespace", ["-n"]) {
      Description = "The namespace to use for generated code.",
      Required = true
    };

    RootCommand rootCommand = new() {
      Description = "A C# emoji known sequences code generator."
    };

    rootCommand.Options.Add(outputPathOption);
    rootCommand.Options.Add(namespaceOption);

    rootCommand.SetAction(parseResult => {
      return new Generator(
        parseResult.GetValue(outputPathOption),
        parseResult.GetValue(namespaceOption)!
      ).Generate();
    });

    return rootCommand.Parse(args).Invoke();
  }
}