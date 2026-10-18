namespace Helpers;

public class Assembler
{
  public static int Run(string assemblyFile, string outputFile) =>
    GCC.Run(assemblyFile, "-o", outputFile);
}
