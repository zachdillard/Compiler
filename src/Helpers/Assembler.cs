namespace Helpers;

public class Assembler
{
  public static int Run(string assemblyFile, string outputFile) =>
    Gcc.Run(assemblyFile, "-o", outputFile);
}
