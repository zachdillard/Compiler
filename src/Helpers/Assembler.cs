using System.ComponentModel;
using System.Diagnostics;

namespace Helpers;

public class Assembler
{
  public static int Run(string assemblyFile, string outputFile, bool x86_64 = false)
  {
    var startInfo = new ProcessStartInfo
    {
      FileName = "gcc",
      UseShellExecute = false,
      RedirectStandardError = true,
      CreateNoWindow = true,
      ArgumentList =
      {
        assemblyFile,
        "-o",
        outputFile
      }
    };

    // The custom emitter follows the book's x86-64 target, including on Apple Silicon.
    if (x86_64 && OperatingSystem.IsMacOS())
    {
      startInfo.ArgumentList.Insert(0, "-arch");
      startInfo.ArgumentList.Insert(1, "x86_64");
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
    finally
    {
      File.Delete(assemblyFile);
    }
  }
}
