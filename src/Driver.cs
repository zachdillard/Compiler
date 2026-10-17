using Helpers;
using System.CommandLine;
using System.CommandLine.Help;
using Assembly = Syntax.Assembly;
using C = Syntax.C;

var compilerArgs = CreateCompilerArguments(args, out var exitCode);
if (compilerArgs is null)
  return exitCode;

CompilationTarget target;
if (OperatingSystem.IsMacOS())
  target = CompilationTarget.Mac;
else if (OperatingSystem.IsLinux())
  target = CompilationTarget.Linux;
else
{
  Console.Error.WriteLine("Error: only macOS and Linux compilation targets are supported.");
  return ExitCode.Failure;
}

try
{
  try
  {
    exitCode = Preprocessor.Run(compilerArgs.InputFile, compilerArgs.PreprocessedFile, x86_64: true);
    if (exitCode != ExitCode.Success)
      return exitCode;

    List<Token> tokens = Lexer.Run(File.ReadAllText(compilerArgs.PreprocessedFile));
    if (compilerArgs.LexOnly)
      return ExitCode.Success;

    C.Program program = Parser.Run(tokens);
    if (compilerArgs.ParseOnly)
      return ExitCode.Success;

    Assembly.Program assembly = Generator.Run(program);
    if (compilerArgs.CodegenOnly)
      return ExitCode.Success;

    string output = Emitter.Run(assembly, target);
    Writer.Run(output, compilerArgs.AssemblyFile);
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

  if (compilerArgs.AssemblyOnly)
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
  const string Usage = "Usage: Compiler [--lex | --parse | --codegen | -S] <source-file>";

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
  var parseOnly = result.GetValue(commandLine.ParseOnlyOption);
  var codegenOnly = result.GetValue(commandLine.CodegenOnlyOption);
  if (new[] { lexOnly, parseOnly, codegenOnly, assemblyOnly }.Count(selected => selected) > 1)
  {
    Console.Error.WriteLine(Usage);
    exitCode = ExitCode.Failure;
    return null;
  }

  var inputFile = result.GetRequiredValue(commandLine.SourceFileArgument);

  if (Path.GetExtension(inputFile) != ".c")
  {
    Console.Error.WriteLine("Error: source file must have a .c extension.");
    exitCode = ExitCode.Failure;
    return null;
  }

  var preprocessedFile = Path.ChangeExtension(inputFile, ".i");
  var assemblyFile = Path.ChangeExtension(inputFile, ".s");
  var outputFile = Path.ChangeExtension(inputFile, null);

  return new CompilerArguments
  (
    inputFile,
    preprocessedFile,
    assemblyFile,
    outputFile,
    lexOnly,
    parseOnly,
    codegenOnly,
    assemblyOnly
  );
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

  var parseOnlyOption = new Option<bool>("--parse")
  {
    Description = "Parse the C program without generating output files.",
    Arity = ArgumentArity.Zero
  };

  var codegenOnlyOption = new Option<bool>("--codegen")
  {
    Description = "Generate the assembly AST without emitting output files.",
    Arity = ArgumentArity.Zero
  };

  foreach (var option in new[] { assemblyOnlyOption, lexOnlyOption, parseOnlyOption, codegenOnlyOption })
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
    lexOnlyOption,
    parseOnlyOption,
    codegenOnlyOption
  };

  return new CommandLine
  (
    command,
    sourceFileArgument,
    assemblyOnlyOption,
    lexOnlyOption,
    parseOnlyOption,
    codegenOnlyOption
  );
}

internal sealed record CommandLine
(
  RootCommand Command,
  Argument<string> SourceFileArgument,
  Option<bool> AssemblyOnlyOption,
  Option<bool> LexOnlyOption,
  Option<bool> ParseOnlyOption,
  Option<bool> CodegenOnlyOption
);

internal sealed record CompilerArguments
(
  string InputFile,
  string PreprocessedFile,
  string AssemblyFile,
  string OutputFile,
  bool LexOnly,
  bool ParseOnly,
  bool CodegenOnly,
  bool AssemblyOnly
);
