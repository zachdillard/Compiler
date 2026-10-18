namespace Helpers;

public class Writer
{
  public static void Run(string assembly, string assemblyFile) =>
    File.WriteAllText(assemblyFile, assembly);
}
