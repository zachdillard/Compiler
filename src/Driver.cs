using System.ComponentModel;
using System.CommandLine;
using System.CommandLine.Help;
using System.Diagnostics;

const string usage = "Usage: Compiler [-gcc] [-S] [--lex] <source-file>";

var sourceFileArgument = new Argument<string>("source-file")
{
  Description = "The C source file to compile.",
  Arity = ArgumentArity.ExactlyOne
};
sourceFileArgument.Validators.Add(result =>
{
  if (result.Tokens.Any(token => token.Value.StartsWith('-')))
    result.AddError("Expected a source file path, not an option.");
});

var gccOption = new Option<bool>("-gcc")
{
  Description = "Use the temporary GCC-backed compiler.",
  Arity = ArgumentArity.Zero
};

var assemblyOnlyOption = new Option<bool>("-S")
{
  Description = "Write assembly without linking an executable.",
  Arity = ArgumentArity.Zero
};

var lexOnlyOption = new Option<bool>("--lex")
{
  Description = "Check tokens without generating output files.",
  Arity = ArgumentArity.Zero
};

foreach (var option in new[] { gccOption, assemblyOnlyOption, lexOnlyOption })
{
  option.Validators.Add(result =>
  {
    if (result.IdentifierTokenCount > 1)
      result.AddError($"Option '{option.Name}' cannot be repeated.");
  });
}

var command = new RootCommand("A small C compiler following Writing a C Compiler.")
{
  sourceFileArgument,
  gccOption,
  assemblyOnlyOption,
  lexOnlyOption
};

var versionOption = command.Options.OfType<VersionOption>().Single();

var result = command.Parse(args, new ParserConfiguration
{
  EnablePosixBundling = false,
  ResponseFileTokenReplacer = null
});

if (result.GetResult(versionOption) is not null)
{
  if (args is not ["--version"])
  {
    Console.Error.WriteLine(usage);
    return 1;
  }

  var version = typeof(Lexer).Assembly.GetName().Version!;
  Console.WriteLine($"Compiler {version.ToString(3)}");
  return 0;
}

if (result.Action is HelpAction)
  return result.Invoke();

if (result.Errors.Count != 0)
{
  Console.Error.WriteLine(usage);
  return 1;
}

var assemblyOnly = result.GetValue(assemblyOnlyOption);
var lexOnly = result.GetValue(lexOnlyOption);
var useGcc = result.GetValue(gccOption);
var inputFile = result.GetRequiredValue(sourceFileArgument);

var preprocessedFile = Path.ChangeExtension(inputFile, ".i");
var assemblyFile = Path.ChangeExtension(inputFile, ".s");
var outputFile = Path.ChangeExtension(inputFile, null);

var preprocessingExitCode = Preprocessor.Run(inputFile, preprocessedFile);
if (preprocessingExitCode != 0)
  return preprocessingExitCode;

try
{
  try
  {
    if (useGcc && !lexOnly)
    {
      var startInfo = new ProcessStartInfo
      {
        FileName = "gcc",
        UseShellExecute = false,
        RedirectStandardError = true,
        CreateNoWindow = true,
        ArgumentList =
        {
          "-S",
          preprocessedFile,
          "-o",
          assemblyFile
        }
      };

      using var process = new Process { StartInfo = startInfo };
      process.Start();
      var diagnostics = process.StandardError.ReadToEnd();
      process.WaitForExit();

      Console.Error.Write(diagnostics);
      if (process.ExitCode != 0)
        return process.ExitCode;
    }
    else
    {
      List<Token> tokens = Lexer.Run(File.ReadAllText(preprocessedFile));
      if (!lexOnly)
      {
        C.Program program = Parser.Run(tokens);
        Assembly.Program assembly = Generator.Run(program);
        string output = Emitter.Run(assembly);
        Writer.Run(output, assemblyFile);
      }
    }
  }
  catch (Win32Exception) when (useGcc && !lexOnly)
  {
    Console.Error.WriteLine("Error: could not start gcc. Ensure gcc is installed and available on PATH.");
    return 1;
  }
  catch (InvalidOperationException exception)
  {
    Console.Error.WriteLine(useGcc && !lexOnly
        ? "Error: could not start gcc."
        : $"Error: {exception.Message}");
    return 1;
  }
  finally
  {
    File.Delete(preprocessedFile);
  }
}
catch (IOException)
{
  Console.Error.WriteLine("Error: could not create the assembly file.");
  return 1;
}
catch (UnauthorizedAccessException)
{
  Console.Error.WriteLine("Error: could not access a compiler file.");
  return 1;
}

if (lexOnly || assemblyOnly)
  return 0;

return Assembler.Run(assemblyFile, outputFile, x86_64: !useGcc);
