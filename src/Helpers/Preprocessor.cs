namespace Helpers;

public class Preprocessor
{
  public static int Run(string inputFile, string preprocessedFile) =>
    Gcc.Run("-E", "-P", inputFile, "-o", preprocessedFile);
}
