---
marp: true
theme: default
class: lead
paginate: true
---

<style type="text/css">
    .team_name {
        display: flex;
        flex-direction: row;
        gap: 2em;
        align-items: center;
        font-size: 2em;
    }

    h1 {
        color: black;
    }
    
</style>

<div class="team_name">
    <div>
        <h1>Team 33: Skebob</h1>
    </div>
    <div>
        <img src="skebob.jpg" alt="Skebob picture" ></img>
    </div>
</div>


---


## Team Members

- Timur Salakhov (@claymix)
- Vyacheslav Molchanov (@uberch)

---

## Technology Stack

- **Source language:** Interpreted
- **Implementation language:** C#
- **Parser:** Hand-Written
- **Target platform:** .NET

---

## <center>Tests<center>

---

Test 1: Simple Declaration

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

---

Test 2: Statements

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