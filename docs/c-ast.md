```asdl
program = Program(function_definition)
function_definition = Function(identifier name, statement body)
statement = Return(exp)
exp = Constant(int)
```

Every C AST node implements `Helpers.INode`. Its `Accept` method dispatches to
the matching overload on `Helpers.IVisitor<TResult>`. Both interfaces are defined
in `src/Helpers/IVisitor.cs`. Visitors define their own result type and visit child
nodes by calling `Accept` on them.

`Helpers.SyntaxVisitor` implements `IVisitor<string>` to display the parser's output:

```csharp
using Helpers;

var program = Parser.Run(Lexer.Run("int main(void) { return 2; }"));
Console.WriteLine(SyntaxVisitor.Run(program));
```

```text
Program
  Function(name="main")
    Return
      Constant(2)
```

`SyntaxVisitor.Run` also accepts individual nodes to print a subtree.
