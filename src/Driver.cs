using Helpers;
using System.CommandLine;
using System.CommandLine.Help;
using System.Reflection;
using System.Runtime.InteropServices;
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

// Linux uses the host's GCC toolchain, which must target x86-64.
if (target == CompilationTarget.Linux && RuntimeInformation.OSArchitecture != Architecture.X64)
{
  Console.Error.WriteLine("Error: Linux compilation requires an x86-64 host.");
  return ExitCode.Failure;
}

try
{
  // Intermediate files go in a private directory so files beside the source are never touched.
  var temporaryDirectory = Directory.CreateTempSubdirectory("compiler-").FullName;
  try
  {
    var name = Path.GetFileNameWithoutExtension(compilerArgs.InputFile);
    var preprocessedFile = Path.Combine(temporaryDirectory, $"{name}.i");
    var assemblyFile = compilerArgs.AssemblyOnly
      ? Path.ChangeExtension(compilerArgs.InputFile, ".s")
      : Path.Combine(temporaryDirectory, $"{name}.s");
    var outputFile = Path.ChangeExtension(compilerArgs.InputFile, null);

    exitCode = Preprocessor.Run(compilerArgs.InputFile, preprocessedFile);
    if (exitCode != ExitCode.Success)
      return exitCode;

    List<Token> tokens = Lexer.Run(File.ReadAllText(preprocessedFile));
    if (compilerArgs.LexOnly)
      return ExitCode.Success;

    C.Program program = Parser.Run(tokens);
    if (compilerArgs.ParseOnly)
      return ExitCode.Success;

    Assembly.Program assembly = Generator.Run(program);
    if (compilerArgs.CodegenOnly)
      return ExitCode.Success;

    string output = Emitter.Run(assembly, target);
    Writer.Run(output, assemblyFile);
    if (compilerArgs.AssemblyOnly)
      return ExitCode.Success;

    exitCode = Assembler.Run(assemblyFile, outputFile);
    if (exitCode != ExitCode.Success)
      Console.Error.WriteLine("Error: could not assemble and link the program.");

    return exitCode;
  }
  catch (CompileError exception)
  {
    Console.Error.WriteLine($"Error: {exception.Message}");
    return ExitCode.Failure;
  }
  finally
  {
    Directory.Delete(temporaryDirectory, recursive: true);
  }
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
      Console.Error.WriteLine("Error: --version option cannot be combined with other arguments.");
      Console.Error.WriteLine(Usage);
      exitCode = ExitCode.Failure;
      return null;
    }

    var version = typeof(Lexer).Assembly
      .GetCustomAttribute<AssemblyInformationalVersionAttribute>()!.InformationalVersion;
    Console.WriteLine($"Compiler {version}");
    return null;
  }

  if (result.Action is HelpAction)
  {
    exitCode = result.Invoke();
    return null;
  }

  if (result.Errors.Count != 0)
  {
    foreach (var error in result.Errors)
      Console.Error.WriteLine($"Error: {error.Message}");
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
    Console.Error.WriteLine("Error: Options '--lex', '--parse', '--codegen', and '-S' cannot be combined.");
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

  // GCC reads arguments such as @file.c as options, so give it absolute paths.
  inputFile = Path.GetFullPath(inputFile);

  return new CompilerArguments
  (
    inputFile,
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
  bool LexOnly,
  bool ParseOnly,
  bool CodegenOnly,
  bool AssemblyOnly
);
