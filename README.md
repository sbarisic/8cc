FishCC C Compiler
=================

Fork of 8cc that compiles custom bytecode/assembler for VMs.
Compiles on Windows under Visual Studio 2017.
Some parts are written in C#. Bytecode/assembler specification comming soon.


```
// 64 bit registers
RAX  RBX  RCX  RDX  RSI  RDI  R8   R9   R10  R11
A0,  A1,  A2,  A3,  A4,  A5,  A6,  A7,  A8,  A9

// Lower 32 bit registers
EAX  RBX  ECX  EDX  ESI  EDI  R8D  R9D  R10D R11D
B0,  B1,  B2,  B3,  B4,  B5,  B6,  B7,  B8,  B9

// Lower 8 bit registers
AL   BL   CL   DL   SIL  DIL  R8B  R9B  R10B R11B
C0,  C1,  C2,  C3,  C4,  C5,  C6,  C7,  C8,  C9

// Lower 16 bit registers
AX   BX   CX   DX   SI   DI   R8W  R9W  R10W R11W
D0,  D1,  D2,  D3,  D4,  D5,  D6,  D7,  D8,  D9

// Other
SP,  BP



// Instructions
NOP

PUSH                                           - Push register to stack
POP                                            - Pop register from stack

MOV_CST_MEM_OFS_(N) REG, CONST1, CONST2        - Move constant CONST2 (N bytes long) to *(REG + CONST1)
MOV_REG REG2, REG1                             - Move REG1 into REG2

SUB_CST REG, CONST                             - Subtract CONST from REG and store in REG

CMP REG0, REG1                                 - Compare REG0 and REG1

JE LABEL                                       - Jump if zero

LEA REG, LABEL                                 - Loads address of label into register
```