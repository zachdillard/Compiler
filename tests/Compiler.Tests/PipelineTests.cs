using Xunit;

namespace CompilerIntegrationTests;

public sealed class PipelineTests
{
  [Fact]
  public void Run_ChapterOneSource_ProducesExpectedAssembly()
  {
    const string source = "int main(void) { return 42; }";
    string expected = string.Join(Environment.NewLine,
      "\t.globl _main", "_main:", "\tmovl $42, %eax", "\tret", "");

    List<Token> tokens = Lexer.Run(source);
    C.Program program = Parser.Run(tokens);
    Assembly.Program assembly = Generator.Run(program);
    string text = Emitter.Run(assembly);

    Assert.Equal(expected, text);
  }
}
