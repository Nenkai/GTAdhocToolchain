# Introduced with Adhoc
### `null` = `nil`
Every dereferenced value is a `nil`, instead of a `null`.

### Modules, Classes, Attributes and Statics

##### Modules & Statics
Modules have a completely different meaning in Adhoc. Think of them as your usual javascript classes.

Classes also exist, but are only used to *define* types that inherit from the base Adhoc objects.

Either of them supports functions **and** methods.

Modules also act as namespaces, and can be browsed.

```js
module MyModule
{
   function myFunction() { ... }
   method myMethod() { ... } // Note the keyword 'method'
}
```

For statics, the keyword `static` is used. They are accessed in a C++ namespace manner.
```java
module StaticModule
{
  static PI = 3.14;
}

// Access static field
var pi = StaticModule::PI;

// Navigating through engine modules to call a function
pdistd::MPjson::Encode(/* ... */);
```

##### Attributes
Properties are called `attributes` in adhoc. They are defined with the `attribute` keyword, just like how you would declare a `var` or `static`.

* Without value: `attribute myAttribute` - Will be defaulted to `nil`
* With value: `attribute myAttribute = []`

```java
class Dog
{
   attribute name;
}
```

Attributes can also be defined in modules.

##### Class Constructors
Class constructors are defined with the `__init__` method identifier.
Local attributes are accessed using the `self` keyword.

```java
class Dog
{
   attribute name;
   
   method __init__(name)
   {
      self.name = name;
   }
}

var obj = MyObject("FooBar");
```

To access an attrbibute:
```java
var name = obj.name;
```

##### Class Inheritance

```java
class BetterObject
{
  ...
}

class EvenBetterObject extends BetterObject
{
 ...
}
```

### Strings & Interpolation
There is only one type of string declaration, quotes.
```js
var str = "Hello world!";
var combinedStrings = "hello"
                      "world!";

var interpolated = "hello, %{name}!"; // Notice %, instead of $ in javascript.
```

### Map
Maps are Key/Value collections, similar to javascript's map or C#'s dictionaries. Adhoc supports them natively.
```js
var myMap = Map();
var myMap2 = [:]; // Shortcut to creation
var myMapWithElements = ["MyKey":"MyValue", "MyKey2": "MyValue2"]; // Creation with 2 pairs

myMap["hello"] = "world!";
myMap.getMapCount(); // 1
myMapWithElements.getMapCount(); // 2
```

### Foreach
Adhoc supports `foreach` clauses out of the box.
```csharp
var arr = ["one", "two", "three"];
var combined;
foreach (var i in arr)
  combined += i + " ";

// combined = "one two three "
```

Also works with maps.
```js
var map = ["Name": "Bob", Age": 18];
foreach (var [key, value] in map) // Pair deconstruction
{
    // ...
}
```

### Macros
Supported as C supports it (Compiler does not yet have a pre-processor).
```c
#define SET_INDEX(#INDEX) \
  arr[#INDEX] = #INDEX;
  
var arr = Array();
SET_INDEX(0);

// arr[0] = 0;
```

### Native Number Types
`Bool`, `Int`, `UInt`, `Long`, `ULong`, `Float`, `Double` respectively are all natively built-in types.

### Imports
Imports are mostly used java-like.
```java
import main::*; // Imports all modules within the main module.
import main::pdistd::MWebAd::webad::*; // Imports all modules within a specific module path.
```

### Includes
C-type includes are supported.
```c
#include "projects/gt6/my_project/myinclude.ad"
```

### Function Expression Variable Capture
Variables outside function expressions are captured.
```js
var myVariable = 0;
var myFunc = function (){
  return myVariable + 100;
}
```

### Code allowed everywhere
Top level, in module or class bodies, code is allowed everywhere.
```js
module MyModule
{
    attribute myAttribute = [];
    myAttribute.push("hello world");
}
```

Module extensions are also allowed within function themselves.
```js
function myFunction()
{
    module main
    {
        // Anything put here will be part of the "main" module.
        // Declaring a static variable will make it belong to the "main" module.
    }
}
```

### Undefs
Undefs let you undefine functions or static symbols.

```js
function myFunction()
{
   ...
}

undef myFunction; // "myFunction" is undefined, now nil if called.
```

### Operator Overloading
Adhoc supports fully overloading operators.
```java
class OperatorOverloadClassTest
{
    attribute value = "";
    
    method __init__(val)
    {
        self.value = val;
    }

    method __add__(val) // Needs to be the internal label for a designated operator, in this case, __add__ = +
    {
       return OperatorOverloadClassTest(value + val);
    }
}
var obj = MyOperatorOverloadingClass();
obj += "hello world!";
// obj.value is now "hello world!"
```

### Static Scopes
Static fields can be accessed from any module or class depth.

```js
module RootModule
{
  static sStaticField;

  module ChildModule
  {
    function setParentField()
    {
      sStaticfield = "hello world!";
    }
  }
}
```

### Async/Await (GT6 and above)
NOTE: Most of the syntax was mostly made up due to unknowns surrounding the original syntax.

```js
async function myAsyncFunction() // Must mark as async
{
  var result = await () => getObject();
}
```

### Finally Clauses
NOTE: Most of the syntax was mostly made up due to unknowns surrounding the original syntax.

```js
function func(context) // Must mark as async
{
  CursorUtil::setCursor(context, "wait"); // Set cursor to waiting mode
  finally() => { CursorUtil::setCursor(context, "cursor_chrome"); } // Restore cursor back to normal incase an exception is thrown
}
```

### Yield Statements
Mostly unknown, may be similar to unity's yield statement where the runtime waits for the next frame.

```js
function func(context)
{
  yield;
}
```

### Requires
[TODO]

### Pass by reference
[TODO]

### Not supported
* Anything modern ECMAScript-ish features (arguably not needed).
* `let`, `const` keywords are not implemented.
* `for..in` and `for..of` are replaced by the much more convenient `foreach`.
* `===`, `!==` operators
* Dynamic objects `var obj = {}`

### And more
That have yet to be figured.
