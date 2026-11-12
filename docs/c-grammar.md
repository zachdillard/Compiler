```ebnf
<program> ::= <function>
<function> ::= "int" <identifier> "(" "void" ")" "{" <statement> "}"
<statement> ::= "return" <expression> ";"
<expression> ::= <int> | <unary_operator> <expression> | "(" <expression> ")"
<unary_operator> ::= "-" | "~"
<identifier> ::= ? An Identifier token ?
<int> ::= ? A Constant token ?
```
