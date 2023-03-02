# Introduced or changed with Adhoc
## Nulls: `null` = `nil`
Every dereferenced value is a `nil`, instead of a `null`.

## Modules, Classes, Attributes and Statics

### Modules & Statics
Modules have a completely different meaning in Adhoc. Think of them as C++ namespaces, they can be navigated to.

Classes also exist, but are only used to *define* types that inherit from the base Adhoc objects.

Either of them supports functions **and** methods.

```js
module MyModule
{
   function myFunction() { ... }
   method myMethod() { ... } // Note the keyword 'method'
}
```

For statics, the keyword `static` is used. They are accessed in a C++ namespace manner, or through `[]` indexing:
```java
module StaticModule
{
  static PI = 3.14;
}

// Access static field
var pi = StaticModule::PI;

// Access a static field by name
var pi = StaticModule["PI"];

// Navigating through engine modules to call a function
pdistd::MPjson::Encode(/* ... */);
```

### Attributes
Properties are called attributes in adhoc. They are defined with the `attribute` keyword, just like how you would declare a `var` or `static`.

* Without value: `attribute myAttribute` - Will be defaulted to `nil`
* With value: `attribute myAttribute = []`

```java
class Dog
{
   attribute name;
}
```

Attributes can also be defined in modules.

### Class Constructors
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

To access an attribute:
```java
var name = obj.name;
```

### Class Inheritance

The `:` token instead of `extends` in Javascript or Java.
```java
class BetterObject
{
  ...
}

class EvenBetterObject : BetterObject
{
 ...
}
```

### Module Constructors
Module constructors are completely new in Adhoc. These are mostly used for initializing UI widgets with user data or with UI method events.
They allow defining a constructor for any object that will run once the UI system sees a new object registered (i.e `appendChild` onto a composite)
```js
var myObject = someWidget.doCopy();
module (myObject)
{
   attribute myAttr;
   
   method onCancel()
   {
      // ...
   }
}

myObject.myAttr; // ❌ myAttr is not yet defined!
```

## Strings & Interpolation
There is only one type of string declaration, quotes.
```js
var str = "Hello world!";
var combinedStrings = "hello"
                      "world!";

var interpolated = "hello, %{name}!"; // Notice %, instead of $ in javascript.
```

## Maps
Maps are Key/Value collections, similar to javascript's map or C#'s dictionaries. Adhoc supports them natively.
```js
var myMap = Map();
var myMap2 = [:]; // Shortcut to creation
var myMapWithElements = ["MyKey":"MyValue", "MyKey2": "MyValue2"]; // Creation with 2 pairs

myMap["hello"] = "world!";
myMap.getMapCount(); // 1
myMapWithElements.getMapCount(); // 2
```

## Foreach
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

## Macros/Preprocessing
Supported as C supports it (Compiler does not yet have a pre-processor). **NOTE** - the toolchain does not support this yet.
```c
#define SET_INDEX(#INDEX) \
  arr[#INDEX] = #INDEX;
  
var arr = Array();
SET_INDEX(0);

// arr[0] = 0;
```

## Native Number Types
`UInt`, `Long`, `ULong`, `Double` respectively are all natively built-in types starting from GT PSP Adhoc) ontop of `Bool`, `Int`,  `Float`.

## Imports
Imports are mostly used python-like.
```java
import main::*; // Imports/copies all modules from the specified module into the current one.
import myModule::myFunction; // Imports/copies a static/function into the current one.
import myModule::myStatic as obj; // Imports a static into an object.
```

## Includes
C-type includes are supported.
```c
#include "projects/gt6/my_project/myinclude.ad"
```

## Function Expression Variable Capture
Variables outside function expressions are captured.
```js
var myVariable = 0;
var myFunc = function (){
  return myVariable + 100;
}
```

## Code allowed everywhere
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

## Undefs
Undefs let you undefine functions or static symbols.

```js
function myFunction()
{
   ...
}

undef myFunction; // "myFunction" is undefined, now nil if called.
```

## Operator Overloading
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

## Static Scopes
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

## Async/Await (GT6 and above)
```js
async function myAsyncFunction() // Must mark as async
{
  var result = await getObject();
}
```

## Finalizer Statements
Finalizer statements allows running code once the current module is finalized/cleaned up.

```js
function func(context)
{
  CursorUtil::setCursor(context, "wait"); // Set cursor to waiting mode
  finally
  {
      // This will be executed once the module is finalized.
      // It will not execute immediately.
      CursorUtil::setCursor(context, "cursor_chrome");
  } 
}
```

## Yield Statements
Mostly unknown, may be similar to unity's yield statement where the runtime waits for the next frame.

```js
function func(context)
{
  yield;
}
```

## Requires
Requires allows importing all contents of a script onto the current one.
```js
require "MyScript.adc"
```

## Symbols
Similar to [Javascript's Symbols](https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Global_Objects/Symbol), they are defined with single quotes.
```js
var symbol = 'my cool symbol';
```
## Variadic Function calls
Calling functions with their arguments being represented by an array can be called using the `call()` keyword:
```js
function sum(arg1, arg2)
{
   return arg1 + arg2;
}

// call(func, argument array) - NOTE: function or method must be script defined.
call(sum, [9, 10]); // 21
```

## Function Rest Parameters
Identical to javascript except the syntax is swapped around.
```js
function myFunction(args...) // Not ...args!
{
    ...
}
```

## Identifier Literals
Identifier literals allow defining identifiers with normally illegal characters incase you have to.
```js
var `my totally valid identifier` = "hello world";

module `my module`
{

}
```

## Delegates (GT Sport and above)
```cs
function myFunc()
{
    return "hello world";
}

delegate myDelegate;
myDelegate = myFunc; // This will not override myDelegate with a function, rather assign a function to the delegate
return myDelegate(); // "hello world"
```

## Pass by reference
[TODO]

## Object Selectors
[TODO]

## Not supported
* Anything modern ECMAScript-ish features (arguably not needed).
* `let`, `const` keywords are not implemented.
* `for..in` and `for..of` are replaced by the much more convenient `foreach`.
* `===`, `!==` operators
* Dynamic objects `var obj = {}`

### And more
That have yet to be figured.
