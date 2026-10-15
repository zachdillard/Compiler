using Helpers;
using System.CommandLine;
using System.CommandLine.Help;
using Assembly = Syntax.Assembly;
using C = Syntax.C;

var compilerArgs = CreateCompilerArguments(args, out var exitCode);
if (compilerArgs is null)
  return exitCode;

try
{
  try
  {
    exitCode = Preprocessor.Run(compilerArgs.InputFile, compilerArgs.PreprocessedFile);
    if (exitCode != ExitCode.Success)
      return exitCode;

    List<Token> tokens = Lexer.Run(File.ReadAllText(compilerArgs.PreprocessedFile));
    if (!compilerArgs.LexOnly)
    {
      C.Program program = Parser.Run(tokens);
      Assembly.Program assembly = Generator.Run(program);
      string output = Emitter.Run(assembly);
      Writer.Run(output, compilerArgs.AssemblyFile);
    }
  }
  catch (InvalidOperationException exception)
  {
    Console.Error.WriteLine($"Error: {exception.Message}");
    return ExitCode.Failure;
  }
  catch (OverflowException)
  {
    Console.Error.WriteLine("Error: integer constant is outside the supported int range.");
    return ExitCode.Failure;
  }
  finally
  {
    File.Delete(compilerArgs.PreprocessedFile);
  }

  if (compilerArgs.LexOnly || compilerArgs.AssemblyOnly)
    return ExitCode.Success;

  exitCode = Assembler.Run(compilerArgs.AssemblyFile, compilerArgs.OutputFile, x86_64: true);
  if (exitCode != ExitCode.Success)
    Console.Error.WriteLine("Error: could not assemble and link the program.");

  return exitCode;
}
catch (IOException exception)
{
  Console.Error.WriteLine($"Error: could not access a compiler file: {exception.Message}");
  return ExitCode.Failure;
}
catch (UnauthorizedAccessException exception)
{
  Console.Error.WriteLine($"Error: could not access a compiler file: {exception.Message}");
  return ExitCode.Failure;
}

static CompilerArguments? CreateCompilerArguments(string[] args, out int exitCode)
{
  exitCode = ExitCode.Success;
  const string Usage = "Usage: Compiler [-S] [--lex] <source-file>";

  var commandLine = CreateCommandLine();

  var versionOption = commandLine.Command.Options.OfType<VersionOption>().Single();

  var result = commandLine.Command.Parse(args, new ParserConfiguration
  {
    EnablePosixBundling = false,
    ResponseFileTokenReplacer = null
  });

  if (result.GetResult(versionOption) is not null)
  {
    if (args is not ["--version"])
    {
      Console.Error.WriteLine(Usage);
      exitCode = ExitCode.Failure;
      return null;
    }

    var version = typeof(Lexer).Assembly.GetName().Version!;
    Console.WriteLine($"Compiler {version.ToString(3)}");
    return null;
  }

  if (result.Action is HelpAction)
  {
    exitCode = result.Invoke();
    return null;
  }

  if (result.Errors.Count != 0)
  {
    Console.Error.WriteLine(Usage);
    exitCode = ExitCode.Failure;
    return null;
  }

  var assemblyOnly = result.GetValue(commandLine.AssemblyOnlyOption);
  var lexOnly = result.GetValue(commandLine.LexOnlyOption);
  var inputFile = result.GetRequiredValue(commandLine.SourceFileArgument);

  var preprocessedFile = Path.ChangeExtension(inputFile, ".i");
  var assemblyFile = Path.ChangeExtension(inputFile, ".s");
  var outputFile = Path.ChangeExtension(inputFile, null);

  return new CompilerArguments(inputFile, preprocessedFile, assemblyFile, outputFile, lexOnly, assemblyOnly);
}

static CommandLine CreateCommandLine()
{
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

  foreach (var option in new[] { assemblyOnlyOption, lexOnlyOption })
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
    assemblyOnlyOption,
    lexOnlyOption
  };

  return new CommandLine(command, sourceFileArgument, assemblyOnlyOption, lexOnlyOption);
}

internal sealed record CommandLine
(
  RootCommand Command,
  Argument<string> SourceFileArgument,
  Option<bool> AssemblyOnlyOption,
  Option<bool> LexOnlyOption
);

internal sealed record CompilerArguments
(
  string InputFile,
  string PreprocessedFile,
  string AssemblyFile,
  string OutputFile,
  bool LexOnly,
  bool AssemblyOnly
);
