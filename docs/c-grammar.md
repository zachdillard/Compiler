```ebnf
<program> ::= <function>
<function> ::= "int" <identifier> "(" "void" ")" "{" <statement> "}"
<statement> ::= "return" <expression> ";"
<expression> ::= <int>
<identifier> ::= ? An Identifier token ?
<int> ::= ? A Constant token ?
```
