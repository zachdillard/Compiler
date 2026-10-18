using System.ComponentModel;
using System.Diagnostics;

namespace Helpers;

public static class Gcc
{
  public static int Run(params string[] arguments)
  {
    var startInfo = new ProcessStartInfo
    {
      FileName = "gcc",
      UseShellExecute = false,
      RedirectStandardError = true,
      CreateNoWindow = true
    };

    foreach (var argument in arguments)
      startInfo.ArgumentList.Add(argument);

    // The custom emitter follows the book's x86-64 target, including on Apple Silicon.
    if (OperatingSystem.IsMacOS())
    {
      startInfo.ArgumentList.Add("-arch");
      startInfo.ArgumentList.Add("x86_64");
    }

    try
    {
      using var process = new Process { StartInfo = startInfo };
      process.Start();

      var diagnostics = process.StandardError.ReadToEnd();
      process.WaitForExit();

      Console.Error.Write(diagnostics);
      return process.ExitCode;
    }
    catch (Win32Exception)
    {
      Console.Error.WriteLine("Error: could not start gcc. Ensure gcc is installed and available on PATH.");
      return ExitCode.Failure;
    }
    catch (InvalidOperationException)
    {
      Console.Error.WriteLine("Error: could not start gcc.");
      return ExitCode.Failure;
    }
  }
}
