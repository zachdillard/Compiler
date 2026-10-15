# unary operators

- new intermediate representation between the c parser ast and assembly ast
- assembly generation is broken into several smaller passes
- function prologue and epilogue is added to asm ouput
- `movl source, destination`

## stack

- `RSP` (stack pointer) holds address of the top of the stack
- stack grows to lower memory addresses, pushing = decrementing RSP, top = lowest address
- `push $3`
  - pushes the value of three to the top of the stack
  - decrements RSP by 8 bytes
- `pop %rax`
  - copies value at the top of the stack to the register
  - increments RSP by 8 bytes
- values pushed are 8 bytes, can't push only 4 bytes on x64
- stack frame
  - when a function is called, it allocates some memory at the top of the stack by
    decrementing the stack pointer
  - space to store local variables and temp values
  - before function returns, it deallocates the stack frame
  - `RBP` usually points to the base of the stack frame, a.k.a the base pointer
  - everything within the frame is referred to relative to the base pointer
  - every address in the current frame is lower than the address stored in the base pointer
- prologue
  - save the address of the base of the caller's frame onto the stack
  - make the top of the stack the base of the new stack frame?
  - decrement the stack pointer by _n_ bytes
- epilogue
  - restores the caller's stack frame by resetting RSP and RBP back to the previous values
  - copy the value of the base pointer into the stack pointer
  - pop the top of the stack and store that value into the base pointer

## lexer

- `~` bitwise operator
- `-` negation operator
- `--` decrement operator (compiler should reject it)

valid expressions include

- `-2`
- `-(-2)`
- `--2`
- `~2`
- `~~2`

implementation

- define regular expression for each new token
- check input against new regexes
- choose longest matching when multiple regexes match
- 

## target output

```asm
    .globl main
main:
    pushq   %rbp
    movq     %rsp, %rbp
    subq     $8, %rsp
  ❶ movl     $2, ❷ -4(%rbp)
  ❸ negl     -4(%rbp)
  ❹ movl     -4(%rbp), %r10d
  ❺ movl     %r10d, -8(%rbp)
  ❻ notl     -8(%rbp)
  ❼ movl     -8(%rbp), %eax
    movq     %rbp, %rsp
    popq     %rbp
    ret
```
