using Helpers;
using System.Diagnostics;
using System.Runtime.Versioning;
using Xunit;

namespace CompilerIntegrationTests;

[SupportedOSPlatform("macos")]
public sealed class DriverTests
{
  [Fact]
  public void VersionDoesNotRequireGcc()
  {
    var result = RunCompilerWithoutGcc("--version");

    Assert.Equal(ExitCode.Success, result.ExitCode);
    Assert.Equal($"Compiler 0.1.0{Environment.NewLine}", result.StandardOutput);
    Assert.Empty(result.StandardError);
  }

  [Theory]
  [InlineData("--version", "-S")]
  [InlineData("--version", "-gcc")]
  [InlineData("--version", "--lex")]
  [InlineData("--version", "--parse")]
  [InlineData("--version", "--codegen")]
  [InlineData("--version", "--help")]
  [InlineData("--version", "--version")]
  [InlineData("--version", "missing.c")]
  [InlineData("missing.c", "--version")]
  public void VersionRejectsOtherArguments(string first, string second)
  {
    var result = RunCompilerWithoutGcc(first, second);

    Assert.Equal(ExitCode.Failure, result.ExitCode);
    Assert.Empty(result.StandardOutput);
    Assert.Contains("Usage: Compiler", result.StandardError);
  }

  [Fact]
  public void NoArgumentsPrintUsageAndFail()
  {
    var result = RunCompiler();

    Assert.Equal(ExitCode.Failure, result.ExitCode);
    Assert.Contains("Usage: Compiler [--lex | --parse | --codegen | -S] <source-file>", result.StandardError);
  }

  [Theory]
  [InlineData("--help")]
  [InlineData("-h")]
  public void HelpDescribesOptionsWithoutGcc(string option)
  {
    var result = RunCompilerWithoutGcc(option);

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

  [Theory]
  [InlineData("-S")]
  [InlineData("--lex")]
  [InlineData("--parse")]
  [InlineData("--codegen")]
  [InlineData("--assembly")]
  [InlineData("-unknown")]
  [InlineData("--version=false")]
  public void MalformedArgumentsPrintUsageAndFail(string argument)
  {
    var result = RunCompiler(argument);

    Assert.Equal(ExitCode.Failure, result.ExitCode);
    Assert.Contains("Usage: Compiler [--lex | --parse | --codegen | -S] <source-file>", result.StandardError);
  }

  [Theory]
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
  public void NonCSourceIsRejectedWithoutGccAndPreserved(string fileName, string option)
  {
    const string source = "int main(void) { return 7; }";
    using var fixture = new TestFixture(source, fileName);

    var result = option == string.Empty
      ? RunCompilerWithoutGcc(fixture.SourcePath)
      : RunCompilerWithoutGcc(option, fixture.SourcePath);

    Assert.Equal(ExitCode.Failure, result.ExitCode);
    Assert.Equal($"Error: source file must have a .c extension.{Environment.NewLine}", result.StandardError);
    Assert.Empty(result.StandardOutput);
    Assert.Equal(source, File.ReadAllText(fixture.SourcePath));
  }

  [Theory]
  [InlineData("--lex")]
  [InlineData("--parse")]
  [InlineData("--codegen")]
  public void IntermediateStageProducesNoOutputAndRemovesPreprocessedFile(string option)
  {
    using var fixture = new TestFixture("int main(void) {\n\treturn 7;\n}\n");

    var result = RunCompiler(option, fixture.SourcePath);

    Assert.Equal(ExitCode.Success, result.ExitCode);
    Assert.Empty(result.StandardOutput);
    Assert.Empty(result.StandardError);
    Assert.False(File.Exists(fixture.PreprocessedPath));
    Assert.False(File.Exists(fixture.AssemblyPath));
    Assert.False(File.Exists(fixture.ExecutablePath));
  }

  [Theory]
  [InlineData("--lex", false)]
  [InlineData("--lex", true)]
  [InlineData("--parse", false)]
  [InlineData("--parse", true)]
  [InlineData("--codegen", false)]
  [InlineData("--codegen", true)]
  [InlineData("-S", false)]
  [InlineData("-S", true)]
  public void StageOptionCanAppearBeforeOrAfterSource(string option, bool sourceFirst)
  {
    using var fixture = new TestFixture("int main(void) { return 7; }");

    var result = sourceFirst
      ? RunCompiler(fixture.SourcePath, option)
      : RunCompiler(option, fixture.SourcePath);

    Assert.Equal(ExitCode.Success, result.ExitCode);
    Assert.Empty(result.StandardOutput);
    Assert.Empty(result.StandardError);
    Assert.Equal(option == "-S", File.Exists(fixture.AssemblyPath));
    Assert.False(File.Exists(fixture.ExecutablePath));
    Assert.False(File.Exists(fixture.PreprocessedPath));
  }

  [Fact]
  public void NormalCompilationProducesExecutableAndRemovesIntermediateFiles()
  {
    using var fixture = new TestFixture("int main(void) { return 7; }");

    var result = RunCompiler(fixture.SourcePath);

    Assert.Equal(ExitCode.Success, result.ExitCode);
    Assert.True(File.Exists(fixture.ExecutablePath));
    Assert.False(File.Exists(fixture.PreprocessedPath));
    Assert.False(File.Exists(fixture.AssemblyPath));

    var executableResult = Process.Start(new ProcessStartInfo
    {
      FileName = fixture.ExecutablePath,
      UseShellExecute = false,
      RedirectStandardOutput = true,
      RedirectStandardError = true,
      CreateNoWindow = true
    });

    Assert.NotNull(executableResult);
    executableResult.WaitForExit();
    Assert.Equal(7, executableResult.ExitCode);
  }

  [Fact]
  public void CustomAssemblyOnlyCompilationWritesAssemblyAndCleansPreprocessedFile()
  {
    using var fixture = new TestFixture("int main(void) { return 7; }");

    var result = RunCompiler("-S", fixture.SourcePath);

    Assert.Equal(ExitCode.Success, result.ExitCode);
    Assert.Empty(result.StandardOutput);
    Assert.False(File.Exists(fixture.PreprocessedPath));
    Assert.Equal("\t.globl _main\n_main:\n\tmovl $7, %eax\n\tret\n".Replace("\n", Environment.NewLine),
      File.ReadAllText(fixture.AssemblyPath));
    Assert.False(File.Exists(fixture.ExecutablePath));
  }

  [Fact]
  public void AssemblyOnlyOutputCanBeLinkedAndExecuted()
  {
    using var fixture = new TestFixture("int main(void) { return 7; }");
    var result = RunCompiler("-S", fixture.SourcePath);
    Assert.Equal(ExitCode.Success, result.ExitCode);
    Assert.False(File.Exists(fixture.ExecutablePath));

    using var gcc = Process.Start(new ProcessStartInfo
    {
      FileName = "gcc",
      ArgumentList = { "-arch", "x86_64", fixture.AssemblyPath, "-o", fixture.ExecutablePath },
      UseShellExecute = false,
      RedirectStandardOutput = true,
      RedirectStandardError = true
    });
    Assert.NotNull(gcc);
    string diagnostics = gcc.StandardError.ReadToEnd();
    gcc.WaitForExit();
    Assert.True(gcc.ExitCode == 0, diagnostics);

    using var executable = Process.Start(new ProcessStartInfo
    {
      FileName = fixture.ExecutablePath,
      UseShellExecute = false,
      RedirectStandardOutput = true,
      RedirectStandardError = true
    });
    Assert.NotNull(executable);
    executable.WaitForExit();
    Assert.Equal(7, executable.ExitCode);
  }

  [Theory]
  [InlineData("")]
  [InlineData("--parse")]
  [InlineData("--codegen")]
  [InlineData("-S")]
  public void CustomParserRejectsInvalidSyntaxAndCleansPreprocessedFile(string option)
  {
    using var fixture = new TestFixture("int main(void) { return 7 }");

    var result = RunCompilerWithOption(option, fixture.SourcePath);

    Assert.Equal(ExitCode.Failure, result.ExitCode);
    Assert.Contains("Expected Semicolon", result.StandardError);
    Assert.False(File.Exists(fixture.PreprocessedPath));
    Assert.False(File.Exists(fixture.AssemblyPath));
    Assert.False(File.Exists(fixture.ExecutablePath));
  }

  [Theory]
  [InlineData("2147483648", "")]
  [InlineData("999999999999999999999999", "")]
  [InlineData("2147483648", "--lex")]
  [InlineData("999999999999999999999999", "--lex")]
  [InlineData("2147483648", "--parse")]
  [InlineData("999999999999999999999999", "--parse")]
  [InlineData("2147483648", "--codegen")]
  [InlineData("999999999999999999999999", "--codegen")]
  [InlineData("2147483648", "-S")]
  [InlineData("999999999999999999999999", "-S")]
  public void OverflowingIntegerFailsWithDiagnosticAndCleansIntermediateFiles(string constant, string option)
  {
    using var fixture = new TestFixture($"int main(void) {{ return {constant}; }}");

    var result = RunCompilerWithOption(option, fixture.SourcePath);

    Assert.Equal(ExitCode.Failure, result.ExitCode);
    Assert.Empty(result.StandardOutput);
    Assert.Equal($"Error: integer constant is outside the supported int range.{Environment.NewLine}",
      result.StandardError);
    Assert.False(File.Exists(fixture.PreprocessedPath));
    Assert.False(File.Exists(fixture.AssemblyPath));
    Assert.False(File.Exists(fixture.ExecutablePath));
  }

  [Fact]
  public void LexOnlyStopsBeforeParsing()
  {
    using var fixture = new TestFixture("int main(void) { return 7 }");

    var result = RunCompiler("--lex", fixture.SourcePath);

    Assert.Equal(ExitCode.Success, result.ExitCode);
    Assert.Empty(result.StandardError);
    Assert.False(File.Exists(fixture.PreprocessedPath));
  }

  [Theory]
  [InlineData("--lex", "--parse")]
  [InlineData("--lex", "--codegen")]
  [InlineData("--lex", "-S")]
  [InlineData("--parse", "--codegen")]
  [InlineData("--parse", "-S")]
  [InlineData("--codegen", "-S")]
  public void ConflictingStageFlagsFailBeforePreprocessingInEitherOrder(string first, string second)
  {
    using var fixture = new TestFixture("int main(void) { return 7; }");

    foreach (string[] arguments in new[]
    {
      new[] { first, fixture.SourcePath, second },
      new[] { second, fixture.SourcePath, first }
    })
    {
      var result = RunCompilerWithoutGcc(arguments);

      Assert.Equal(ExitCode.Failure, result.ExitCode);
      Assert.Empty(result.StandardOutput);
      Assert.Contains("Usage: Compiler [--lex | --parse | --codegen | -S] <source-file>", result.StandardError);
      Assert.False(File.Exists(fixture.PreprocessedPath));
      Assert.False(File.Exists(fixture.AssemblyPath));
      Assert.False(File.Exists(fixture.ExecutablePath));
    }
  }

  [Theory]
  [InlineData("-S")]
  [InlineData("--lex")]
  [InlineData("--parse")]
  [InlineData("--codegen")]
  public void DuplicateFlagsPrintUsageAndFail(string option)
  {
    using var fixture = new TestFixture("int main(void) { return 7; }");

    var result = RunCompiler(option, option, fixture.SourcePath);

    Assert.Equal(ExitCode.Failure, result.ExitCode);
    Assert.Empty(result.StandardOutput);
    Assert.Contains("Usage: Compiler [--lex | --parse | --codegen | -S] <source-file>", result.StandardError);
    Assert.False(File.Exists(fixture.PreprocessedPath));
    Assert.False(File.Exists(fixture.AssemblyPath));
    Assert.False(File.Exists(fixture.ExecutablePath));
  }

  [Theory]
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
  public void InvalidArgumentsWithSourcePrintUsageAndProduceNoFiles(string argument)
  {
    using var fixture = new TestFixture("int main(void) { return 7; }");

    var result = RunCompiler(argument, fixture.SourcePath);

    Assert.Equal(ExitCode.Failure, result.ExitCode);
    Assert.Empty(result.StandardOutput);
    Assert.Contains("Usage: Compiler [--lex | --parse | --codegen | -S] <source-file>", result.StandardError);
    Assert.False(File.Exists(fixture.PreprocessedPath));
    Assert.False(File.Exists(fixture.AssemblyPath));
    Assert.False(File.Exists(fixture.ExecutablePath));
  }

  [Theory]
  [InlineData("")]
  [InlineData("--lex")]
  [InlineData("--parse")]
  [InlineData("--codegen")]
  [InlineData("-S")]
  public void PreprocessingFailurePropagatesDiagnosticsAndFails(string option)
  {
    using var fixture = new TestFixture("int main(void) { return 0; }");
    File.WriteAllText(fixture.SourcePath, "#include <header-that-does-not-exist.h>\n");

    var result = RunCompilerWithOption(option, fixture.SourcePath);

    Assert.NotEqual(ExitCode.Success, result.ExitCode);
    Assert.Contains("header-that-does-not-exist.h", result.StandardError);
    Assert.False(File.Exists(fixture.PreprocessedPath));
    Assert.False(File.Exists(fixture.AssemblyPath));
    Assert.False(File.Exists(fixture.ExecutablePath));
  }

  [Fact]
  public void PreprocessingPassesX86_64TargetToGcc()
  {
    using var fixture = new TestFixture("int main(void) { return 7; }");
    string gccDirectory = fixture.CreateGcc("""
      case " $* " in
        *' -arch x86_64 '*) ;;
        *) echo 'missing x86_64 target' >&2; exit 1 ;;
      esac
      /bin/cp "$3" "$5"
      """);

    var result = RunCompilerWithEnvironment(["-S", fixture.SourcePath], gccDirectory: gccDirectory);

    Assert.Equal(ExitCode.Success, result.ExitCode);
    Assert.Empty(result.StandardError);
    Assert.Contains("movl $7, %eax", File.ReadAllText(fixture.AssemblyPath));
    Assert.False(File.Exists(fixture.PreprocessedPath));
  }

  [Fact]
  public void PreprocessingUsesX86_64TargetMacros()
  {
    using var fixture = new TestFixture("""
      #if defined(__x86_64__) && !defined(__aarch64__)
      int main(void) { return 7; }
      #else
      int main(void) { return 9; }
      #endif
      """);

    var result = RunCompiler("-S", fixture.SourcePath);

    Assert.Equal(ExitCode.Success, result.ExitCode);
    Assert.Empty(result.StandardError);
    Assert.Equal("\t.globl _main\n_main:\n\tmovl $7, %eax\n\tret\n".Replace("\n", Environment.NewLine),
      File.ReadAllText(fixture.AssemblyPath));
    Assert.False(File.Exists(fixture.PreprocessedPath));
  }

  [Theory]
  [InlineData("")]
  [InlineData("--lex")]
  [InlineData("--parse")]
  [InlineData("--codegen")]
  [InlineData("-S")]
  public void PreprocessingFailureRemovesPartialOutput(string option)
  {
    using var fixture = new TestFixture("int main(void) { return 7; }");
    string gccDirectory = fixture.CreateGcc("""
      /bin/cp "$3" "$5"
      echo 'preprocessing failed' >&2
      exit 1
      """);

    var result = RunCompilerWithEnvironment(
      option == string.Empty ? [fixture.SourcePath] : [option, fixture.SourcePath],
      gccDirectory: gccDirectory);

    Assert.Equal(ExitCode.Failure, result.ExitCode);
    Assert.Equal($"preprocessing failed{Environment.NewLine}", result.StandardError);
    Assert.Empty(result.StandardOutput);
    Assert.False(File.Exists(fixture.PreprocessedPath));
    Assert.False(File.Exists(fixture.AssemblyPath));
    Assert.False(File.Exists(fixture.ExecutablePath));
  }

  [Fact]
  public void PreprocessedCleanupFailureReturnsDiagnostic()
  {
    using var fixture = new TestFixture("int main(void) { return 7; }");
    Directory.CreateDirectory(fixture.PreprocessedPath);

    var result = RunCompiler(fixture.SourcePath);

    Assert.Equal(ExitCode.Failure, result.ExitCode);
    Assert.Contains("Error: could not access a compiler file:", result.StandardError);
    Assert.DoesNotContain("Unhandled exception", result.StandardError);
    Assert.Empty(result.StandardOutput);
    Assert.True(Directory.Exists(fixture.PreprocessedPath));
    Assert.False(File.Exists(fixture.AssemblyPath));
    Assert.False(File.Exists(fixture.ExecutablePath));
  }

  [Fact]
  public void AssemblyWriteFailureReturnsDiagnosticAndCleansPreprocessedFile()
  {
    using var fixture = new TestFixture("int main(void) { return 7; }");
    Directory.CreateDirectory(fixture.AssemblyPath);

    var result = RunCompiler("-S", fixture.SourcePath);

    Assert.Equal(ExitCode.Failure, result.ExitCode);
    Assert.Contains("Error: could not access a compiler file:", result.StandardError);
    Assert.DoesNotContain("Unhandled exception", result.StandardError);
    Assert.Empty(result.StandardOutput);
    Assert.False(File.Exists(fixture.PreprocessedPath));
    Assert.True(Directory.Exists(fixture.AssemblyPath));
    Assert.False(File.Exists(fixture.ExecutablePath));
  }

  [Theory]
  [InlineData(0)]
  [InlineData(1)]
  public void AssemblyCleanupFailureReturnsDiagnostic(int assemblerExitCode)
  {
    using var fixture = new TestFixture("int main(void) { return 7; }");
    string gccDirectory = fixture.CreateGcc($$"""
      if [ "$1" = '-E' ]; then
        /bin/cp "$3" "$5"
        exit 0
      fi
      for argument in "$@"; do
        case "$argument" in
          *.s)
            /bin/rm "$argument"
            /bin/mkdir "$argument"
            ;;
        esac
      done
      echo 'assembler diagnostic' >&2
      exit {{assemblerExitCode}}
      """);

    var result = RunCompilerWithEnvironment([fixture.SourcePath], gccDirectory: gccDirectory);

    Assert.Equal(ExitCode.Failure, result.ExitCode);
    Assert.Contains("assembler diagnostic", result.StandardError);
    Assert.Contains("Error: could not access a compiler file:", result.StandardError);
    Assert.DoesNotContain("Unhandled exception", result.StandardError);
    Assert.Empty(result.StandardOutput);
    Assert.False(File.Exists(fixture.PreprocessedPath));
    Assert.True(Directory.Exists(fixture.AssemblyPath));
    Assert.False(File.Exists(fixture.ExecutablePath));
  }

  [Fact]
  public void MissingGccFailsWithDiagnostic()
  {
    using var fixture = new TestFixture("int main(void) { return 7; }");

    var result = RunCompilerWithoutGcc(fixture.SourcePath);

    Assert.Equal(ExitCode.Failure, result.ExitCode);
    Assert.Equal($"Error: could not start gcc. Ensure gcc is installed and available on PATH.{Environment.NewLine}",
      result.StandardError);
    Assert.Empty(result.StandardOutput);
    Assert.False(File.Exists(fixture.PreprocessedPath));
    Assert.False(File.Exists(fixture.AssemblyPath));
    Assert.False(File.Exists(fixture.ExecutablePath));
  }

  [Fact]
  public void CompilerFailurePropagatesDiagnosticsAndFails()
  {
    using var fixture = new TestFixture("int main( { return 0; }");

    var result = RunCompiler(fixture.SourcePath);

    Assert.NotEqual(ExitCode.Success, result.ExitCode);
    Assert.Contains("error", result.StandardError, StringComparison.OrdinalIgnoreCase);
    Assert.False(File.Exists(fixture.PreprocessedPath));
  }

  [Fact]
  public void LinkerFailurePrintsDriverErrorAndRemovesIntermediateFiles()
  {
    using var fixture = new TestFixture("int other(void) { return 7; }");

    var result = RunCompiler(fixture.SourcePath);

    Assert.NotEqual(ExitCode.Success, result.ExitCode);
    Assert.Contains("Error: could not assemble and link the program.", result.StandardError);
    Assert.Empty(result.StandardOutput);
    Assert.False(File.Exists(fixture.PreprocessedPath));
    Assert.False(File.Exists(fixture.AssemblyPath));
    Assert.False(File.Exists(fixture.ExecutablePath));
  }

  [Fact]
  public void MissingSourceFailsWithDiagnostics()
  {
    using var fixture = new TestFixture("int main(void) { return 0; }");
    File.Delete(fixture.SourcePath);

    var result = RunCompiler(fixture.SourcePath);

    Assert.NotEqual(ExitCode.Success, result.ExitCode);
    Assert.Contains("No such file or directory", result.StandardError, StringComparison.OrdinalIgnoreCase);
  }

  [Theory]
  [InlineData("--lex")]
  [InlineData("--parse")]
  [InlineData("--codegen")]
  [InlineData("-S")]
  public void StageFlagsRejectLexicalErrorsAndCleanIntermediateFiles(string option)
  {
    using var fixture = new TestFixture("int main(void) { return @; }");

    var result = RunCompiler(option, fixture.SourcePath);

    Assert.Equal(ExitCode.Failure, result.ExitCode);
    Assert.Empty(result.StandardOutput);
    Assert.Contains("Error:", result.StandardError);
    Assert.False(File.Exists(fixture.PreprocessedPath));
    Assert.False(File.Exists(fixture.AssemblyPath));
    Assert.False(File.Exists(fixture.ExecutablePath));
  }

  [Theory]
  [InlineData("--lex")]
  [InlineData("--parse")]
  [InlineData("--codegen")]
  [InlineData("-S")]
  public void StageFlagsNeverInvokeAssemblerOrLinker(string option)
  {
    using var fixture = new TestFixture("int main(void) { return 7; }");
    string gccDirectory = fixture.CreateGcc("""
      if [ "$1" != '-E' ]; then
        echo 'unexpected assembler/linker invocation' >&2
        exit 1
      fi
      /bin/cp "$3" "$5"
      """);

    var result = RunCompilerWithEnvironment([option, fixture.SourcePath], gccDirectory: gccDirectory);

    Assert.Equal(ExitCode.Success, result.ExitCode);
    Assert.Empty(result.StandardOutput);
    Assert.Empty(result.StandardError);
    Assert.False(File.Exists(fixture.PreprocessedPath));
    Assert.False(File.Exists(fixture.ExecutablePath));
    Assert.Equal(option == "-S", File.Exists(fixture.AssemblyPath));
    if (option == "-S")
      Assert.Equal("\t.globl _main\n_main:\n\tmovl $7, %eax\n\tret\n".Replace("\n", Environment.NewLine),
        File.ReadAllText(fixture.AssemblyPath));
  }

  private static ProcessResult RunCompilerWithOption(string option, string sourcePath)
  {
    return option == string.Empty ? RunCompiler(sourcePath) : RunCompiler(option, sourcePath);
  }

  private static ProcessResult RunCompiler(params string[] arguments)
  {
    return RunCompilerWithEnvironment(arguments, withoutGcc: false);
  }

  private static ProcessResult RunCompilerWithoutGcc(params string[] arguments)
  {
    return RunCompilerWithEnvironment(arguments, withoutGcc: true);
  }

  private static ProcessResult RunCompilerWithEnvironment(string[] arguments, bool withoutGcc = false, string? gccDirectory = null)
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

    using var process = Process.Start(startInfo);
    Assert.NotNull(process);

    var standardOutput = process.StandardOutput.ReadToEnd();
    var standardError = process.StandardError.ReadToEnd();
    process.WaitForExit();

    return new ProcessResult(process.ExitCode, standardOutput, standardError);
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

    public string SourcePath { get; }
    public string PreprocessedPath => Path.ChangeExtension(SourcePath, ".i");
    public string AssemblyPath => Path.ChangeExtension(SourcePath, ".s");
    public string ExecutablePath => Path.ChangeExtension(SourcePath, null);

    public string CreateGcc(string script)
    {
      string gccDirectory = Directory.CreateDirectory(Path.Combine(directory, "tools")).FullName;
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
