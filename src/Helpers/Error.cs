namespace Helpers;

// A problem in the user's C source, reported as a diagnostic instead of a crash.
public sealed class Error(string message) : Exception(message);
