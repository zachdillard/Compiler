program = Program(Function)
function = Function(identifier, instruction* body)
// dst has to be a Var, cannot store a value in a constant
instruction = Return(val) | Unary(unary_operator, val src, val dst)
val = Constant(int) | Var(identifier)
unary_operator = Complement | Negate