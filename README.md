# Imperative Language Compiler

Compiler for Imperative Language written on C#

## Authors

- Timur Salakhov (@claymix)
- Vyacheslav Molchanov (@uberch)

## Technology Stack

- **Source language:** Interpreted
- **Implementation language:** C#
- **Parser:** Hand-Written
- **Target platform:** .NET


## Features

- Lexical Analysis ([docs/Lexical-Analysis](./docs/Lexical-Analysis.md))
- Syntax Analysis ([docs/Syntax-Analysis](./docs/Syntax-Analysis.md))
- Semantic Analysis ([docs/Semantic-Analysis](./docs/Semantic-Analysis.md))
- CodeGen into IL code ([docs/Code-Generation](./docs/CodeGen.md))

## Expected language examples

More examples you can find in [tests](./compiler-csharp/tests/) directory

```skb
var a : integer;
var a : integer is 10;
var a is 10;

type int is integer;
var a : int;

type point is record
	int x;
	int y;
end;

var p1 : point;
var arr : array [3] int;
```

```skb
var a : integer;
a := 10;

for i in 10..20 loop
	print i, 2*i;
end;

if a < 20 then
	print false
else
	print true
end;

var power : integer is 0;
var num : integer is 64;
while num > 1 loop
	power := power + 1;
	num := num / 2;
end;
```

