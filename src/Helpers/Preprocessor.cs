using System.ComponentModel;
using System.Diagnostics;

namespace Helpers;

public class Preprocessor
{
  public static int Run(string inputFile, string preprocessedFile)
  {
    var startInfo = new ProcessStartInfo
    {
      FileName = "gcc",
      UseShellExecute = false,
      RedirectStandardError = true,
      CreateNoWindow = true,
      ArgumentList =
      {
        "-E",
        "-P",
        inputFile,
        "-o",
        preprocessedFile
      }
    };

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
