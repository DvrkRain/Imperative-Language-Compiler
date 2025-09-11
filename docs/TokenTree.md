#Lexer.TokenTree namespace documentation

Token is abstract class for storing any type of tokens in purpose to send it to the syntax analyser later.

Token class has 3 child classes:

* **Identifier** represents the name of variable or function or type alias.
* **Dedicated** represents dedicated word (including operators, delimeters etc).
* **Numeral** is abstract class for numerals

## Identifier class

Identifier additionally stores string with identifier name.

## Dedicated class

Dedicated class stores code of dedicated word for further usage in syntax analyzer.

## Numeral class
Numeral class has two childs:

* **Integer** represents integer numbers.
* **Real** represents real numbers.

# List of dedicated words
* **var**
* **type**
* **.**

* **integer**
* **real**
* **boolean**
* **record**
* **array**

* **true**
* **false**

* **is**
* **:**
* **\(**
* **\)**
* **\[**
* **\]**
* **:=**

* **while**
* **loop**

* **for**
* **in**
* **reverse**

* **if**
* **then**
* **else**

* **print**

* **routine**
* **=>**
* **end**

* **and**
* **or**
* **xor**
* **not**

* **<**
* **<=**
* **==**
* **>=**
* **>**
* **=**
* **/=**

* **+**
* **-**
* **\***
* **\/**
* **%**
