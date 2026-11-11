using Helpers;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Xunit;

namespace CompilerIntegrationTests;

[SupportedOSPlatform("macos")]
[SupportedOSPlatform("linux")]
public sealed class DriverTests
{
  public DriverTests()
  {
    Assert.SkipUnless(OperatingSystem.IsMacOS()
      || (OperatingSystem.IsLinux() && RuntimeInformation.OSArchitecture == Architecture.X64),
      "Compiler integration tests require macOS or Linux x86-64.");
  }

  private static readonly string[] StageOptions = ["--lex", "--parse", "--codegen", "-S"];
  private static readonly string[] CompilationOptions = ["", .. StageOptions];

  public static TheoryData<string, bool> StagePositions =>
    new MatrixTheoryData<string, bool>(StageOptions, [false, true]);

  public static TheoryData<string, string> OverflowingIntegers =>
    new MatrixTheoryData<string, string>(["2147483648", "999999999999999999999999"], CompilationOptions);

  public static TheoryData<string, string> TemporaryDirectoryCases =>
    new MatrixTheoryData<string, string>(CompilationOptions,
      ["int main(void) { return 7; }", "int main(void) { return 7 }", "int other(void) { return 7; }"]);

  [Fact(Timeout = 30_000)]
  public async Task VersionDoesNotRequireGcc()
  {
    var result = await RunCompilerWithoutGcc(TestContext.Current.CancellationToken, "--version");
    var version = typeof(Lexer).Assembly
      .GetCustomAttribute<AssemblyInformationalVersionAttribute>()!.InformationalVersion;

    Assert.Equal(ExitCode.Success, result.ExitCode);
    Assert.Equal($"Compiler {version}{Environment.NewLine}", result.StandardOutput);
    Assert.Empty(result.StandardError);
  }

  [Theory(Timeout = 30_000)]
  [InlineData("--version", "-S")]
  [InlineData("--version", "-gcc")]
  [InlineData("--version", "--lex")]
  [InlineData("--version", "--parse")]
  [InlineData("--version", "--codegen")]
  [InlineData("--version", "--help")]
  [InlineData("--version", "--version")]
  [InlineData("--version", "missing.c")]
  [InlineData("missing.c", "--version")]
  public async Task VersionRejectsOtherArguments(string first, string second)
  {
    var result = await RunCompilerWithoutGcc(TestContext.Current.CancellationToken, first, second);

    Assert.Equal(ExitCode.Failure, result.ExitCode);
    Assert.Empty(result.StandardOutput);
    Assert.Contains("Error: --version option cannot be combined with other arguments.", result.StandardError);
    Assert.Contains("Usage: Compiler", result.StandardError);
  }

  [Fact(Timeout = 30_000)]
  public async Task NoArgumentsPrintUsageAndFail()
  {
    var result = await RunCompiler(TestContext.Current.CancellationToken);

    Assert.Equal(ExitCode.Failure, result.ExitCode);
    Assert.Contains("Error: Required argument missing", result.StandardError);
    Assert.Contains("Usage: Compiler [--lex | --parse | --codegen | -S] <source-file>", result.StandardError);
  }

  [Theory(Timeout = 30_000)]
  [InlineData("--help")]
  [InlineData("-h")]
  public async Task HelpDescribesOptionsWithoutGcc(string option)
  {
    var result = await RunCompilerWithoutGcc(TestContext.Current.CancellationToken, option);

    Assert.Equal(ExitCode.Success, result.ExitCode);
    Assert.Empty(result.StandardError);
    Assert.Contains("<source-file>", result.StandardOutput);
    Assert.DoesNotContain("-gcc", result.StandardOutput);
    Assert.Contains("-S", result.StandardOutput);
    Assert.Contains("--lex", result.StandardOutput);
    Assert.Contains("--parse", result.StandardOutput);
    Assert.Contains("--codegen", result.StandardOutput);
    Assert.Contains("--version", result.StandardOutput);
  }

  [Theory(Timeout = 30_000)]
  [InlineData("-S", "Error: Required argument missing")]
  [InlineData("--lex", "Error: Required argument missing")]
  [InlineData("--parse", "Error: Required argument missing")]
  [InlineData("--codegen", "Error: Required argument missing")]
  [InlineData("--assembly", "Error: Expected a source file path, not an option.")]
  [InlineData("-unknown", "Error: Expected a source file path, not an option.")]
  [InlineData("--version=false", "Error: --version option cannot be combined with other arguments.")]
  public async Task MalformedArgumentsPrintErrorAndUsageAndFail(string argument, string error)
  {
    var result = await RunCompiler(TestContext.Current.CancellationToken, argument);

    Assert.Equal(ExitCode.Failure, result.ExitCode);
    Assert.Contains(error, result.StandardError);
    Assert.Contains("Usage: Compiler [--lex | --parse | --codegen | -S] <source-file>", result.StandardError);
  }

  [Theory(Timeout = 30_000)]
  [InlineData("program.i", "")]
  [InlineData("program.i", "--lex")]
  [InlineData("program.i", "--parse")]
  [InlineData("program.i", "--codegen")]
  [InlineData("program.i", "-S")]
  [InlineData("program.s", "")]
  [InlineData("program.S", "")]
  [InlineData("program", "")]
  [InlineData("program.C", "")]
  [InlineData("program.txt", "")]
  public async Task NonCSourceIsRejectedWithoutGccAndPreserved(string fileName, string option)
  {
    const string source = "int main(void) { return 7; }";
    using var fixture = new TestFixture(source, fileName);

    var result = option == string.Empty
      ? await RunCompilerWithoutGcc(TestContext.Current.CancellationToken, fixture.SourcePath)
      : await RunCompilerWithoutGcc(TestContext.Current.CancellationToken, option, fixture.SourcePath);

    Assert.Equal(ExitCode.Failure, result.ExitCode);
    Assert.Equal($"Error: source file must have a .c extension.{Environment.NewLine}", result.StandardError);
    Assert.Empty(result.StandardOutput);
    Assert.Equal(source, File.ReadAllText(fixture.SourcePath));
  }

  [Theory(Timeout = 30_000)]
  [InlineData("--lex")]
  [InlineData("--parse")]
  [InlineData("--codegen")]
  public async Task IntermediateStageProducesNoOutputAndRemovesPreprocessedFile(string option)
  {
    using var fixture = new TestFixture("int main(void) {\n\treturn 7;\n}\n");

    var result = await RunCompiler(TestContext.Current.CancellationToken, option, fixture.SourcePath);

    Assert.Equal(ExitCode.Success, result.ExitCode);
    Assert.Empty(result.StandardOutput);
    Assert.Empty(result.StandardError);
    Assert.False(File.Exists(fixture.PreprocessedPath));
    Assert.False(File.Exists(fixture.AssemblyPath));
    Assert.False(File.Exists(fixture.ExecutablePath));
  }

  [Theory(Timeout = 30_000)]
  [MemberData(nameof(StagePositions))]
  public async Task StageOptionCanAppearBeforeOrAfterSource(string option, bool sourceFirst)
  {
    using var fixture = new TestFixture("int main(void) { return 7; }");

    var result = sourceFirst
      ? await RunCompiler(TestContext.Current.CancellationToken, fixture.SourcePath, option)
      : await RunCompiler(TestContext.Current.CancellationToken, option, fixture.SourcePath);

    Assert.Equal(ExitCode.Success, result.ExitCode);
    Assert.Empty(result.StandardOutput);
    Assert.Empty(result.StandardError);
    Assert.Equal(option == "-S", File.Exists(fixture.AssemblyPath));
    Assert.False(File.Exists(fixture.ExecutablePath));
    Assert.False(File.Exists(fixture.PreprocessedPath));
  }

  [Fact(Timeout = 30_000)]
  public async Task NormalCompilationProducesExecutableAndRemovesIntermediateFiles()
  {
    using var fixture = new TestFixture("int main(void) { return 7; }");

    var result = await RunCompiler(TestContext.Current.CancellationToken, fixture.SourcePath);

    Assert.Equal(ExitCode.Success, result.ExitCode);
    Assert.True(File.Exists(fixture.ExecutablePath));
    Assert.False(File.Exists(fixture.PreprocessedPath));
    Assert.False(File.Exists(fixture.AssemblyPath));

    var executableResult = await RunProcess(new ProcessStartInfo
    {
      FileName = fixture.ExecutablePath,
      UseShellExecute = false,
      RedirectStandardOutput = true,
      RedirectStandardError = true,
      CreateNoWindow = true
    }, TestContext.Current.CancellationToken);
    Assert.Equal(7, executableResult.ExitCode);
  }

  [Fact(Timeout = 30_000)]
  public async Task AssemblyOnlyCompilationUsesHostTargetAndCleansPreprocessedFile()
  {
    using var fixture = new TestFixture("int main(void) { return 7; }");

    var result = await RunCompiler(TestContext.Current.CancellationToken, "-S", fixture.SourcePath);

    Assert.Equal(ExitCode.Success, result.ExitCode);
    Assert.Empty(result.StandardOutput);
    Assert.False(File.Exists(fixture.PreprocessedPath));
    Assert.Equal(ExpectedHostAssembly, File.ReadAllText(fixture.AssemblyPath));
    Assert.False(File.Exists(fixture.ExecutablePath));
  }

  [Fact(Timeout = 30_000)]
  public async Task AssemblyOnlyOutputCanBeLinkedAndExecuted()
  {
    using var fixture = new TestFixture("int main(void) { return 7; }");
    var result = await RunCompiler(TestContext.Current.CancellationToken, "-S", fixture.SourcePath);
    Assert.Equal(ExitCode.Success, result.ExitCode);
    Assert.False(File.Exists(fixture.ExecutablePath));

    var gccStartInfo = new ProcessStartInfo
    {
      FileName = "gcc",
      ArgumentList = { fixture.AssemblyPath, "-o", fixture.ExecutablePath },
      UseShellExecute = false,
      RedirectStandardOutput = true,
      RedirectStandardError = true
    };
    if (OperatingSystem.IsMacOS())
    {
      gccStartInfo.ArgumentList.Insert(0, "-arch");
      gccStartInfo.ArgumentList.Insert(1, "x86_64");
    }

    var gcc = await RunProcess(gccStartInfo, TestContext.Current.CancellationToken);
    Assert.Equal(ExitCode.Success, gcc.ExitCode);

    var executable = await RunProcess(new ProcessStartInfo
    {
      FileName = fixture.ExecutablePath,
      UseShellExecute = false,
      RedirectStandardOutput = true,
      RedirectStandardError = true
    }, TestContext.Current.CancellationToken);
    Assert.Equal(7, executable.ExitCode);
  }

  [Theory(Timeout = 30_000)]
  [InlineData("")]
  [InlineData("--parse")]
  [InlineData("--codegen")]
  [InlineData("-S")]
  public async Task CustomParserRejectsInvalidSyntaxAndCleansPreprocessedFile(string option)
  {
    using var fixture = new TestFixture("int main(void) { return 7 }");

    var result = await RunCompilerWithOption(TestContext.Current.CancellationToken, option, fixture.SourcePath);

    Assert.Equal(ExitCode.Failure, result.ExitCode);
    Assert.Contains("Expected Semicolon", result.StandardError);
    Assert.False(File.Exists(fixture.PreprocessedPath));
    Assert.False(File.Exists(fixture.AssemblyPath));
    Assert.False(File.Exists(fixture.ExecutablePath));
  }

  [Theory(Timeout = 30_000)]
  [MemberData(nameof(OverflowingIntegers))]
  public async Task OverflowingIntegerFailsWithDiagnosticAndCleansIntermediateFiles(string constant, string option)
  {
    using var fixture = new TestFixture($"int main(void) {{ return {constant}; }}");

    var result = await RunCompilerWithOption(TestContext.Current.CancellationToken, option, fixture.SourcePath);

    Assert.Equal(ExitCode.Failure, result.ExitCode);
    Assert.Empty(result.StandardOutput);
    Assert.Equal($"Error: integer constant is outside the supported int range.{Environment.NewLine}",
      result.StandardError);
    Assert.False(File.Exists(fixture.PreprocessedPath));
    Assert.False(File.Exists(fixture.AssemblyPath));
    Assert.False(File.Exists(fixture.ExecutablePath));
  }

  [Fact(Timeout = 30_000)]
  public async Task LexOnlyStopsBeforeParsing()
  {
    using var fixture = new TestFixture("int main(void) { return 7 }");

    var result = await RunCompiler(TestContext.Current.CancellationToken, "--lex", fixture.SourcePath);

    Assert.Equal(ExitCode.Success, result.ExitCode);
    Assert.Empty(result.StandardError);
    Assert.False(File.Exists(fixture.PreprocessedPath));
  }

  [Theory(Timeout = 30_000)]
  [InlineData("--lex", "--parse")]
  [InlineData("--lex", "--codegen")]
  [InlineData("--lex", "-S")]
  [InlineData("--parse", "--codegen")]
  [InlineData("--parse", "-S")]
  [InlineData("--codegen", "-S")]
  public async Task ConflictingStageFlagsFailBeforePreprocessingInEitherOrder(string first, string second)
  {
    using var fixture = new TestFixture("int main(void) { return 7; }");

    foreach (string[] arguments in new[]
    {
      new[] { first, fixture.SourcePath, second },
      new[] { second, fixture.SourcePath, first }
    })
    {
      var result = await RunCompilerWithoutGcc(TestContext.Current.CancellationToken, arguments);

      Assert.Equal(ExitCode.Failure, result.ExitCode);
      Assert.Empty(result.StandardOutput);
      Assert.Contains("Error: Options '--lex', '--parse', '--codegen', and '-S' cannot be combined.",
        result.StandardError);
      Assert.Contains("Usage: Compiler [--lex | --parse | --codegen | -S] <source-file>", result.StandardError);
      Assert.False(File.Exists(fixture.PreprocessedPath));
      Assert.False(File.Exists(fixture.AssemblyPath));
      Assert.False(File.Exists(fixture.ExecutablePath));
    }
  }

  [Theory(Timeout = 30_000)]
  [InlineData("-S")]
  [InlineData("--lex")]
  [InlineData("--parse")]
  [InlineData("--codegen")]
  public async Task DuplicateFlagsPrintErrorAndUsageAndFail(string option)
  {
    using var fixture = new TestFixture("int main(void) { return 7; }");

    var result = await RunCompiler(TestContext.Current.CancellationToken, option, option, fixture.SourcePath);

    Assert.Equal(ExitCode.Failure, result.ExitCode);
    Assert.Empty(result.StandardOutput);
    Assert.Contains($"Error: Option '{option}' cannot be repeated.", result.StandardError);
    Assert.Contains("Usage: Compiler [--lex | --parse | --codegen | -S] <source-file>", result.StandardError);
    Assert.False(File.Exists(fixture.PreprocessedPath));
    Assert.False(File.Exists(fixture.AssemblyPath));
    Assert.False(File.Exists(fixture.ExecutablePath));
  }

  [Theory(Timeout = 30_000)]
  [InlineData("-gcc")]
  [InlineData("--gcc")]
  [InlineData("-s")]
  [InlineData("--unknown")]
  [InlineData("-S=false")]
  [InlineData("--lex=true")]
  [InlineData("--parse=true")]
  [InlineData("--parse=false")]
  [InlineData("--codegen=true")]
  [InlineData("--codegen=false")]
  [InlineData("extra.c")]
  public async Task InvalidArgumentsWithSourcePrintErrorAndUsageAndProduceNoFiles(string argument)
  {
    using var fixture = new TestFixture("int main(void) { return 7; }");

    var result = await RunCompiler(TestContext.Current.CancellationToken, argument, fixture.SourcePath);

    Assert.Equal(ExitCode.Failure, result.ExitCode);
    Assert.Empty(result.StandardOutput);
    Assert.Contains($"Error: Unrecognized command or argument '{fixture.SourcePath}'.", result.StandardError);
    Assert.Contains("Usage: Compiler [--lex | --parse | --codegen | -S] <source-file>", result.StandardError);
    Assert.False(File.Exists(fixture.PreprocessedPath));
    Assert.False(File.Exists(fixture.AssemblyPath));
    Assert.False(File.Exists(fixture.ExecutablePath));
  }

  [Theory(Timeout = 30_000)]
  [InlineData("")]
  [InlineData("--lex")]
  [InlineData("--parse")]
  [InlineData("--codegen")]
  [InlineData("-S")]
  public async Task PreprocessingFailurePropagatesDiagnosticsAndFails(string option)
  {
    using var fixture = new TestFixture("int main(void) { return 0; }");
    File.WriteAllText(fixture.SourcePath, "#include <header-that-does-not-exist.h>\n");

    var result = await RunCompilerWithOption(TestContext.Current.CancellationToken, option, fixture.SourcePath);

    Assert.NotEqual(ExitCode.Success, result.ExitCode);
    Assert.Contains("header-that-does-not-exist.h", result.StandardError);
    Assert.False(File.Exists(fixture.PreprocessedPath));
    Assert.False(File.Exists(fixture.AssemblyPath));
    Assert.False(File.Exists(fixture.ExecutablePath));
  }

  [Fact(Timeout = 30_000)]
  public async Task PreprocessingUsesHostSpecificGccArguments()
  {
    using var fixture = new TestFixture("int main(void) { return 7; }");
    string targetCheck = OperatingSystem.IsMacOS()
      ? """
        case " $* " in
          *' -arch x86_64 '*) ;;
          *) echo 'missing x86_64 target' >&2; exit 1 ;;
        esac
        """
      : """
        case " $* " in
          *' -arch '*) echo 'unexpected macOS argument' >&2; exit 1 ;;
        esac
        """;
    string gccDirectory = fixture.CreateGcc(targetCheck + "\n/bin/cp \"$3\" \"$5\"");

    var result = await RunCompilerWithEnvironment(TestContext.Current.CancellationToken, ["-S", fixture.SourcePath], gccDirectory: gccDirectory);

    Assert.Equal(ExitCode.Success, result.ExitCode);
    Assert.Empty(result.StandardError);
    Assert.Contains("movl $7, %eax", File.ReadAllText(fixture.AssemblyPath));
    Assert.False(File.Exists(fixture.PreprocessedPath));
  }

  [Fact(Timeout = 30_000)]
  public async Task PreprocessingUsesX86_64TargetMacros()
  {
    using var fixture = new TestFixture("""
      #if defined(__x86_64__) && !defined(__aarch64__)
      int main(void) { return 7; }
      #else
      int main(void) { return 9; }
      #endif
      """);

    var result = await RunCompiler(TestContext.Current.CancellationToken, "-S", fixture.SourcePath);

    Assert.Equal(ExitCode.Success, result.ExitCode);
    Assert.Empty(result.StandardError);
    Assert.Equal(ExpectedHostAssembly, File.ReadAllText(fixture.AssemblyPath));
    Assert.False(File.Exists(fixture.PreprocessedPath));
  }

  [Theory(Timeout = 30_000)]
  [InlineData("")]
  [InlineData("--lex")]
  [InlineData("--parse")]
  [InlineData("--codegen")]
  [InlineData("-S")]
  public async Task PreprocessingFailureRemovesPartialOutput(string option)
  {
    using var fixture = new TestFixture("int main(void) { return 7; }");
    string gccDirectory = fixture.CreateGcc("""
      /bin/cp "$3" "$5"
      echo 'preprocessing failed' >&2
      exit 1
      """);

    var result = await RunCompilerWithEnvironment(TestContext.Current.CancellationToken,
      option == string.Empty ? [fixture.SourcePath] : [option, fixture.SourcePath],
      gccDirectory: gccDirectory);

    Assert.Equal(ExitCode.Failure, result.ExitCode);
    Assert.Equal($"preprocessing failed{Environment.NewLine}", result.StandardError);
    Assert.Empty(result.StandardOutput);
    Assert.False(File.Exists(fixture.PreprocessedPath));
    Assert.False(File.Exists(fixture.AssemblyPath));
    Assert.False(File.Exists(fixture.ExecutablePath));
  }

  [Theory(Timeout = 30_000)]
  [InlineData("")]
  [InlineData("--lex")]
  [InlineData("--parse")]
  [InlineData("--codegen")]
  [InlineData("-S")]
  public async Task ExistingIntermediateFilesBesideSourceArePreserved(string option)
  {
    using var fixture = new TestFixture("int main(void) { return 7; }");
    File.WriteAllText(fixture.PreprocessedPath, "hand-written preprocessed file");
    File.WriteAllText(fixture.AssemblyPath, "hand-written assembly file");

    var result = await RunCompilerWithOption(TestContext.Current.CancellationToken, option, fixture.SourcePath);

    Assert.Equal(ExitCode.Success, result.ExitCode);
    Assert.Empty(result.StandardError);
    Assert.Equal("hand-written preprocessed file", File.ReadAllText(fixture.PreprocessedPath));
    Assert.Equal(option == "-S" ? ExpectedHostAssembly : "hand-written assembly file",
      File.ReadAllText(fixture.AssemblyPath));
  }

  [Theory(Timeout = 30_000)]
  [MemberData(nameof(TemporaryDirectoryCases))]
  public async Task TemporaryDirectoryIsRemoved(string option, string source)
  {
    using var fixture = new TestFixture(source);
    string temporaryDirectory = fixture.CreateDirectory("tmp");

    await RunCompilerWithEnvironment(TestContext.Current.CancellationToken,
      option == string.Empty ? [fixture.SourcePath] : [option, fixture.SourcePath],
      temporaryDirectory: temporaryDirectory);

    Assert.Empty(Directory.GetDirectories(temporaryDirectory, "compiler-*"));
  }

  [Fact(Timeout = 30_000)]
  public async Task SourceNameStartingWithAtSignIsNotReadAsGccOptions()
  {
    using var fixture = new TestFixture("int main(void) { return 7; }", "@program.c");
    // GCC would read program.c as a response file of options for a relative @program.c argument.
    string responseFile = Path.Combine(fixture.DirectoryPath, "program.c");
    File.WriteAllText(responseFile, "--version");

    var result = await RunCompilerWithEnvironment(TestContext.Current.CancellationToken, ["@program.c"], workingDirectory: fixture.DirectoryPath);

    Assert.Equal(ExitCode.Success, result.ExitCode);
    Assert.Empty(result.StandardOutput);
    Assert.Empty(result.StandardError);
    Assert.Equal("--version", File.ReadAllText(responseFile));

    var executable = await RunProcess(new ProcessStartInfo
    {
      FileName = fixture.ExecutablePath,
      UseShellExecute = false,
      RedirectStandardOutput = true,
      RedirectStandardError = true
    }, TestContext.Current.CancellationToken);
    Assert.Equal(7, executable.ExitCode);
  }

  [Fact(Timeout = 30_000)]
  public async Task AssemblyWriteFailureReturnsDiagnosticAndCleansPreprocessedFile()
  {
    using var fixture = new TestFixture("int main(void) { return 7; }");
    Directory.CreateDirectory(fixture.AssemblyPath);

    var result = await RunCompiler(TestContext.Current.CancellationToken, "-S", fixture.SourcePath);

    Assert.Equal(ExitCode.Failure, result.ExitCode);
    Assert.Contains("Error: could not access a compiler file:", result.StandardError);
    Assert.DoesNotContain("Unhandled exception", result.StandardError);
    Assert.Empty(result.StandardOutput);
    Assert.False(File.Exists(fixture.PreprocessedPath));
    Assert.True(Directory.Exists(fixture.AssemblyPath));
    Assert.False(File.Exists(fixture.ExecutablePath));
  }

  [Fact(Timeout = 30_000)]
  public async Task MissingGccFailsWithDiagnostic()
  {
    using var fixture = new TestFixture("int main(void) { return 7; }");

    var result = await RunCompilerWithoutGcc(TestContext.Current.CancellationToken, fixture.SourcePath);

    Assert.Equal(ExitCode.Failure, result.ExitCode);
    Assert.Equal($"Error: could not start gcc. Ensure gcc is installed and available on PATH.{Environment.NewLine}",
      result.StandardError);
    Assert.Empty(result.StandardOutput);
    Assert.False(File.Exists(fixture.PreprocessedPath));
    Assert.False(File.Exists(fixture.AssemblyPath));
    Assert.False(File.Exists(fixture.ExecutablePath));
  }

  [Fact(Timeout = 30_000)]
  public async Task CompilerFailurePropagatesDiagnosticsAndFails()
  {
    using var fixture = new TestFixture("int main( { return 0; }");

    var result = await RunCompiler(TestContext.Current.CancellationToken, fixture.SourcePath);

    Assert.NotEqual(ExitCode.Success, result.ExitCode);
    Assert.Contains("error", result.StandardError, StringComparison.OrdinalIgnoreCase);
    Assert.False(File.Exists(fixture.PreprocessedPath));
  }

  [Fact(Timeout = 30_000)]
  public async Task LinkerFailurePrintsDriverErrorAndRemovesIntermediateFiles()
  {
    using var fixture = new TestFixture("int other(void) { return 7; }");

    var result = await RunCompiler(TestContext.Current.CancellationToken, fixture.SourcePath);

    Assert.NotEqual(ExitCode.Success, result.ExitCode);
    Assert.Contains("Error: could not assemble and link the program.", result.StandardError);
    Assert.Empty(result.StandardOutput);
    Assert.False(File.Exists(fixture.PreprocessedPath));
    Assert.False(File.Exists(fixture.AssemblyPath));
    Assert.False(File.Exists(fixture.ExecutablePath));
  }

  [Fact(Timeout = 30_000)]
  public async Task MissingSourceFailsWithDiagnostics()
  {
    using var fixture = new TestFixture("int main(void) { return 0; }");
    File.Delete(fixture.SourcePath);

    var result = await RunCompiler(TestContext.Current.CancellationToken, fixture.SourcePath);

    Assert.NotEqual(ExitCode.Success, result.ExitCode);
    Assert.Contains("No such file or directory", result.StandardError, StringComparison.OrdinalIgnoreCase);
  }

  [Theory(Timeout = 30_000)]
  [InlineData("--lex")]
  [InlineData("--parse")]
  [InlineData("--codegen")]
  [InlineData("-S")]
  public async Task StageFlagsRejectLexicalErrorsAndCleanIntermediateFiles(string option)
  {
    using var fixture = new TestFixture("int main(void) { return @; }");

    var result = await RunCompiler(TestContext.Current.CancellationToken, option, fixture.SourcePath);

    Assert.Equal(ExitCode.Failure, result.ExitCode);
    Assert.Empty(result.StandardOutput);
    Assert.Contains("Error:", result.StandardError);
    Assert.False(File.Exists(fixture.PreprocessedPath));
    Assert.False(File.Exists(fixture.AssemblyPath));
    Assert.False(File.Exists(fixture.ExecutablePath));
  }

  [Theory(Timeout = 30_000)]
  [InlineData("--lex")]
  [InlineData("--parse")]
  [InlineData("--codegen")]
  [InlineData("-S")]
  public async Task StageFlagsNeverInvokeAssemblerOrLinker(string option)
  {
    using var fixture = new TestFixture("int main(void) { return 7; }");
    string gccDirectory = fixture.CreateGcc("""
      if [ "$1" != '-E' ]; then
        echo 'unexpected assembler/linker invocation' >&2
        exit 1
      fi
      /bin/cp "$3" "$5"
      """);

    var result = await RunCompilerWithEnvironment(TestContext.Current.CancellationToken, [option, fixture.SourcePath], gccDirectory: gccDirectory);

    Assert.Equal(ExitCode.Success, result.ExitCode);
    Assert.Empty(result.StandardOutput);
    Assert.Empty(result.StandardError);
    Assert.False(File.Exists(fixture.PreprocessedPath));
    Assert.False(File.Exists(fixture.ExecutablePath));
    Assert.Equal(option == "-S", File.Exists(fixture.AssemblyPath));
    if (option == "-S")
      Assert.Equal(ExpectedHostAssembly, File.ReadAllText(fixture.AssemblyPath));
  }

  [Fact(Timeout = 30_000)]
  public async Task ProcessCapturesLargeStandardErrorWithoutDeadlocking()
  {
    const string line = "a line of stderr large enough in aggregate to fill a process pipe\n";
    var result = await RunProcess(Shell("""
      i=0
      while [ "$i" -lt 4096 ]; do
        printf 'a line of stderr large enough in aggregate to fill a process pipe\n' >&2
        i=$((i + 1))
      done
      printf 'finished\n'
      exit 7
      """), TestContext.Current.CancellationToken);

    Assert.Equal(7, result.ExitCode);
    Assert.Equal("finished\n", result.StandardOutput);
    Assert.Equal(string.Concat(Enumerable.Repeat(line, 4096)), result.StandardError);
  }

  [Fact(Timeout = 30_000)]
  public async Task AlreadyCanceledProcessDoesNotStart()
  {
    using var fixture = new TestFixture("unused");
    // A start attempt would throw for this missing executable, exposing a missing cancellation guard.
    var startInfo = new ProcessStartInfo(Path.Combine(fixture.DirectoryPath, "missing-executable"))
    {
      UseShellExecute = false,
      RedirectStandardOutput = true,
      RedirectStandardError = true
    };
    using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
    cancellation.Cancel();

    await Assert.ThrowsAnyAsync<OperationCanceledException>(() => RunProcess(startInfo, cancellation.Token));
  }

  [Theory(Timeout = 30_000)]
  [InlineData(false)]
  [InlineData(true)]
  public async Task CompilerCancellationStopsGccAndItsChild(bool useDeadline)
  {
    using var fixture = new TestFixture("int main(void) { return 7; }");
    string ready = fixture.SourcePath + ".ready";
    string gccDirectory = fixture.CreateGcc("""
      /bin/sleep 300 &
      child=$!
      ready="$3.ready"
      printf '%s %s\n' "$$" "$child" > "$ready.tmp"
      /bin/mv "$ready.tmp" "$ready"
      wait "$child"
      """);
    using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
    var run = RunCompilerWithEnvironment(cancellation.Token, [fixture.SourcePath], gccDirectory: gccDirectory);

    Process? parent = null;
    Process? child = null;
    try
    {
      // Wait for the child to exist before exercising cancellation, rather than guessing a startup delay.
      while (!File.Exists(ready))
      {
        if (run.IsCompleted)
          Assert.Fail($"Process exited before publishing its child PID: {await run}");
        await Task.Delay(10, TestContext.Current.CancellationToken);
      }

      string[] ids = (await File.ReadAllTextAsync(ready, TestContext.Current.CancellationToken))
        .Trim().Split(' ');
      parent = Process.GetProcessById(int.Parse(ids[0]));
      child = Process.GetProcessById(int.Parse(ids[1]));
      Assert.False(parent.HasExited);
      Assert.False(child.HasExited);

      if (useDeadline)
        cancellation.CancelAfter(TimeSpan.FromMilliseconds(50));
      else
        cancellation.Cancel();

      await Assert.ThrowsAnyAsync<OperationCanceledException>(() => run);
      await parent.WaitForExitAsync(TestContext.Current.CancellationToken);
      await child.WaitForExitAsync(TestContext.Current.CancellationToken);
      Assert.True(parent.HasExited);
      Assert.True(child.HasExited);
    }
    finally
    {
      cancellation.Cancel();
      try
      {
        await run.WaitAsync(TimeSpan.FromSeconds(5), CancellationToken.None);
      }
      catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
      {
      }
      finally
      {
        // Also clean up if an assertion fails while testing a broken cancellation implementation.
        foreach (var process in new[] { child, parent })
        {
          if (process is null)
            continue;
          using (process)
          {
            if (!process.HasExited)
              process.Kill(entireProcessTree: true);
          }
        }
      }
    }
  }

  private static ProcessStartInfo Shell(string script) => new("/bin/sh")
  {
    ArgumentList = { "-c", script },
    UseShellExecute = false,
    RedirectStandardOutput = true,
    RedirectStandardError = true
  };

  private static async Task<ProcessResult> RunProcess(ProcessStartInfo startInfo, CancellationToken cancellationToken)
  {
    cancellationToken.ThrowIfCancellationRequested();
    using var process = Process.Start(startInfo);
    Assert.NotNull(process);

    // Drain both pipes concurrently. Keep draining after cancellation while the process tree is killed.
    var standardOutput = process.StandardOutput.ReadToEndAsync(CancellationToken.None);
    var standardError = process.StandardError.ReadToEndAsync(CancellationToken.None);
    try
    {
      await process.WaitForExitAsync(cancellationToken);
      await Task.WhenAll(standardOutput, standardError).WaitAsync(cancellationToken);
      return new ProcessResult(process.ExitCode, await standardOutput, await standardError);
    }
    catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
    {
      try
      {
        process.Kill(entireProcessTree: true);
      }
      catch (InvalidOperationException) when (process.HasExited)
      {
        // It may have exited between the cancellation signal and Kill.
      }

      await process.WaitForExitAsync(CancellationToken.None)
        .WaitAsync(TimeSpan.FromSeconds(5), CancellationToken.None);
      await Task.WhenAll(standardOutput, standardError)
        .WaitAsync(TimeSpan.FromSeconds(5), CancellationToken.None);
      throw;
    }
    finally
    {
      Console.WriteLine($"Command: {startInfo.FileName} {string.Join(' ', startInfo.ArgumentList)}");
      Console.WriteLine($"Exit: {(process.HasExited ? process.ExitCode : "running")}; canceled: {cancellationToken.IsCancellationRequested}");
      WriteProcessOutput("stdout", standardOutput);
      WriteProcessOutput("stderr", standardError);
    }
  }

  private static void WriteProcessOutput(string stream, Task<string> output)
  {
    const int limit = 4096;
    if (!output.IsCompletedSuccessfully)
      Console.WriteLine($"{stream}: unavailable");
    else if (output.Result.Length > limit)
      Console.WriteLine($"{stream}: {output.Result[..limit]} [truncated]");
    else
      Console.WriteLine($"{stream}: {output.Result}");
  }

  private static string ExpectedHostAssembly => (OperatingSystem.IsMacOS()
    ? "\t.globl _main\n_main:\n\tmovl $7, %eax\n\tret\n"
    : "\t.globl main\nmain:\n\tmovl $7, %eax\n\tret\n\t.section .note.GNU-stack,\"\",@progbits\n")
    .Replace("\n", Environment.NewLine);

  private static Task<ProcessResult> RunCompilerWithOption(CancellationToken cancellationToken, string option, string sourcePath)
  {
    return option == string.Empty ? RunCompiler(cancellationToken, sourcePath) : RunCompiler(cancellationToken, option, sourcePath);
  }

  private static Task<ProcessResult> RunCompiler(CancellationToken cancellationToken, params string[] arguments)
  {
    return RunCompilerWithEnvironment(cancellationToken, arguments, withoutGcc: false);
  }

  private static Task<ProcessResult> RunCompilerWithoutGcc(CancellationToken cancellationToken, params string[] arguments)
  {
    return RunCompilerWithEnvironment(cancellationToken, arguments, withoutGcc: true);
  }

  private static async Task<ProcessResult> RunCompilerWithEnvironment(CancellationToken cancellationToken, string[] arguments, bool withoutGcc = false,
    string? gccDirectory = null, string? temporaryDirectory = null, string? workingDirectory = null)
  {
    var compilerAssembly = typeof(Lexer).Assembly.Location;
    var testAssembly = typeof(DriverTests).Assembly.Location;
    var runtimeConfiguration = Path.ChangeExtension(testAssembly, ".runtimeconfig.json");
    var dependencies = Path.ChangeExtension(testAssembly, ".deps.json");

    Assert.True(File.Exists(compilerAssembly), $"Build the compiler before running integration tests: {compilerAssembly}");

    var startInfo = new ProcessStartInfo
    {
      FileName = "dotnet",
      UseShellExecute = false,
      RedirectStandardOutput = true,
      RedirectStandardError = true,
      CreateNoWindow = true
    };
    Assert.True(File.Exists(runtimeConfiguration), $"Missing test runtime configuration: {runtimeConfiguration}");
    Assert.True(File.Exists(dependencies), $"Missing test dependency manifest: {dependencies}");

    startInfo.ArgumentList.Add("exec");
    startInfo.ArgumentList.Add("--runtimeconfig");
    startInfo.ArgumentList.Add(runtimeConfiguration);
    startInfo.ArgumentList.Add("--depsfile");
    startInfo.ArgumentList.Add(dependencies);
    startInfo.ArgumentList.Add(compilerAssembly);
    foreach (var argument in arguments)
      startInfo.ArgumentList.Add(argument);

    if (withoutGcc)
      startInfo.Environment["PATH"] = string.Empty;
    else if (gccDirectory is not null)
      startInfo.Environment["PATH"] = gccDirectory;

    if (workingDirectory is not null)
      startInfo.WorkingDirectory = workingDirectory;

    // Own the compiler's scratch directory so forcibly canceling it cannot leave temporary files behind.
    string? ownedTemporaryDirectory = temporaryDirectory is null
      ? Directory.CreateTempSubdirectory("compiler-process-test-").FullName
      : null;
    startInfo.Environment["TMPDIR"] = temporaryDirectory ?? ownedTemporaryDirectory;
    try
    {
      return await RunProcess(startInfo, cancellationToken);
    }
    finally
    {
      if (ownedTemporaryDirectory is not null)
        Directory.Delete(ownedTemporaryDirectory, recursive: true);
    }
  }

  private sealed record ProcessResult(int ExitCode, string StandardOutput, string StandardError);

  private sealed class TestFixture : IDisposable
  {
    private readonly string directory;

    public TestFixture(string source, string fileName = "program.c")
    {
      directory = Directory.CreateTempSubdirectory("compiler-test-").FullName;
      SourcePath = Path.Combine(directory, fileName);
      File.WriteAllText(SourcePath, source);
    }

    public string DirectoryPath => directory;
    public string SourcePath { get; }
    public string PreprocessedPath => Path.ChangeExtension(SourcePath, ".i");
    public string AssemblyPath => Path.ChangeExtension(SourcePath, ".s");
    public string ExecutablePath => Path.ChangeExtension(SourcePath, null);

    public string CreateDirectory(string name)
    {
      return Directory.CreateDirectory(Path.Combine(directory, name)).FullName;
    }

    public string CreateGcc(string script)
    {
      string gccDirectory = CreateDirectory("tools");
      string gccPath = Path.Combine(gccDirectory, "gcc");
      File.WriteAllText(gccPath, $"#!/bin/sh\nset -eu\n{script}\n");
      File.SetUnixFileMode(gccPath, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
      return gccDirectory;
    }

    public void Dispose()
    {
      Directory.Delete(directory, recursive: true);
    }
  }
}
