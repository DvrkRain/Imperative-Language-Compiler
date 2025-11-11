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

- Lexical Analysis
- Syntax Analysis (New!)

## Expected language examples

```skb
var a : integer;            // int a;
var a : integer is 10;      // int a = 10;
var a is 10;                // var a = 10;  a is of type int id induced from 10

type int is integer;        // Alias for integer type
var a : int;                // Same as var a : integer

type point is record        // Declaring struct with two integer fields
	int x;
	int y;
end;

var p1 : point;
var arr : array [3] int;    // int arr[3];
```

```skb
var a : integer;
a := 10;                    // Assignment

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

