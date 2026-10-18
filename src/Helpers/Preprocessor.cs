namespace Helpers;

public class Preprocessor
{
  public static int Run(string inputFile, string preprocessedFile) =>
    GCC.Run("-E", "-P", inputFile, "-o", preprocessedFile);
}
