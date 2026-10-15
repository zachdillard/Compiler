using Helpers;
using System.Diagnostics;
using Xunit;

namespace CompilerIntegrationTests;

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
    Assert.Contains("Usage: Compiler [-S] [--lex] <source-file>", result.StandardError);
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
    Assert.Contains("--version", result.StandardOutput);
  }

  [Theory]
  [InlineData("-S")]
  [InlineData("--assembly")]
  [InlineData("-unknown")]
  [InlineData("--version=false")]
  public void MalformedArgumentsPrintUsageAndFail(string argument)
  {
    var result = RunCompiler(argument);

    Assert.Equal(ExitCode.Failure, result.ExitCode);
    Assert.Contains("Usage: Compiler [-S] [--lex] <source-file>", result.StandardError);
  }

  [Fact]
  public void LexOnlyProducesNoOutputAndRemovesPreprocessedFile()
  {
    using var fixture = new TestFixture("int main(void) {\n\treturn 7;\n}\n");

    var result = RunCompiler("--lex", fixture.SourcePath);

    Assert.Equal(ExitCode.Success, result.ExitCode);
    Assert.Empty(result.StandardOutput);
    Assert.False(File.Exists(fixture.PreprocessedPath));
    Assert.False(File.Exists(fixture.AssemblyPath));
    Assert.False(File.Exists(fixture.ExecutablePath));
  }

  [Fact]
  public void SourceFileCanAppearBetweenOptions()
  {
    using var fixture = new TestFixture("int main(void) { return 7; }");

    var result = RunCompiler("-S", fixture.SourcePath, "--lex");

    Assert.Equal(ExitCode.Success, result.ExitCode);
    Assert.Empty(result.StandardError);
    Assert.False(File.Exists(fixture.AssemblyPath));
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
  public void CustomParserRejectsInvalidSyntaxAndCleansPreprocessedFile()
  {
    using var fixture = new TestFixture("int main(void) { return 7 }");

    var result = RunCompiler(fixture.SourcePath);

    Assert.Equal(ExitCode.Failure, result.ExitCode);
    Assert.Contains("Expected Semicolon", result.StandardError);
    Assert.False(File.Exists(fixture.PreprocessedPath));
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

  [Fact]
  public void LexOnlyAcceptsAllFlagsAndStopsBeforeParsing()
  {
    using var fixture = new TestFixture("int main(void) { return 7 }");

    var result = RunCompiler("-S", "--lex", fixture.SourcePath);

    Assert.Equal(ExitCode.Success, result.ExitCode);
    Assert.Empty(result.StandardOutput);
    Assert.Empty(result.StandardError);
    Assert.False(File.Exists(fixture.PreprocessedPath));
    Assert.False(File.Exists(fixture.AssemblyPath));
    Assert.False(File.Exists(fixture.ExecutablePath));
  }

  [Theory]
  [InlineData("-S")]
  [InlineData("--lex")]
  public void DuplicateFlagsPrintUsageAndFail(string option)
  {
    using var fixture = new TestFixture("int main(void) { return 7; }");

    var result = RunCompiler(option, option, fixture.SourcePath);

    Assert.Equal(ExitCode.Failure, result.ExitCode);
    Assert.Empty(result.StandardOutput);
    Assert.Contains("Usage: Compiler [-S] [--lex] <source-file>", result.StandardError);
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
  [InlineData("extra.c")]
  public void InvalidArgumentsWithSourcePrintUsageAndProduceNoFiles(string argument)
  {
    using var fixture = new TestFixture("int main(void) { return 7; }");

    var result = RunCompiler(argument, fixture.SourcePath);

    Assert.Equal(ExitCode.Failure, result.ExitCode);
    Assert.Empty(result.StandardOutput);
    Assert.Contains("Usage: Compiler [-S] [--lex] <source-file>", result.StandardError);
    Assert.False(File.Exists(fixture.PreprocessedPath));
    Assert.False(File.Exists(fixture.AssemblyPath));
    Assert.False(File.Exists(fixture.ExecutablePath));
  }

  [Fact]
  public void PreprocessingFailurePropagatesDiagnosticsAndFails()
  {
    using var fixture = new TestFixture("int main(void) { return 0; }");
    File.WriteAllText(fixture.SourcePath, "#include <header-that-does-not-exist.h>\n");

    var result = RunCompiler(fixture.SourcePath);

    Assert.NotEqual(ExitCode.Success, result.ExitCode);
    Assert.Contains("header-that-does-not-exist.h", result.StandardError);
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

  private static ProcessResult RunCompiler(params string[] arguments)
  {
    return RunCompilerWithEnvironment(arguments, withoutGcc: false);
  }

  private static ProcessResult RunCompilerWithoutGcc(params string[] arguments)
  {
    return RunCompilerWithEnvironment(arguments, withoutGcc: true);
  }

  private static ProcessResult RunCompilerWithEnvironment(string[] arguments, bool withoutGcc)
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

    public TestFixture(string source)
    {
      directory = Directory.CreateTempSubdirectory("compiler-test-").FullName;
      SourcePath = Path.Combine(directory, "program.c");
      File.WriteAllText(SourcePath, source);
    }

    public string SourcePath { get; }
    public string PreprocessedPath => Path.ChangeExtension(SourcePath, ".i");
    public string AssemblyPath => Path.ChangeExtension(SourcePath, ".s");
    public string ExecutablePath => Path.ChangeExtension(SourcePath, null);

    public void Dispose()
    {
      Directory.Delete(directory, recursive: true);
    }
  }
}
