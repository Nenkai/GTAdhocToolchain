# Introduced with Adhoc
### `null` = `nil`
Every dereferenced value is a `nil`, instead of a `null`.

### Modules
Modules have a completely different meaning in Adhoc. Think of them as your usual javascript classes.

Classes also exist, but are only used to *define* types that inherit from the base Adhoc objects.

Either of them supports functions **and** methods.

Modules also act as namespaces, and can be browsed.

```js
module MyModule
{
   function myFunction() { ... }
   myMethod() { ... }
}
```

##### Class Inheritance

```
class BetterObject extends System::Object
{
  ...
}

class EvenBetterObject extends BetterObject
{
 ...
}
```

### Static Members
Modules that are declared as static within another module are accessed in a C++ namespace manner.

Example: `pdistd::MPjson::Encode(...)`.

### Strings & Interpolation
There is only one type of string declaration, quotes.
```js
var str = "Hello world!";
var multiLineString = "hello"
                      "world!";

var interpolated = "hello, %{name}!"; // Notice %, instead of $ in javascript.
```

### Foreach
Adhoc supports `foreach` clauses out of the box.
```js
var arr = ["one", "two", "three"];
var combined;
foreach (var i in arr)
{
  combined += i + " ";
}

// combined = "one two three "
```

Also works with maps.
```
var map = ["Name":"Age"];
foreach (var [name, age] in map) // Pair deconstruction
{
    // ...
}
```

### Map
Maps are Key/Value collections, similar to javascript's map or C#'s dictionaries. Adhoc supports them natively.
```js
var myMap = Map();
var myMap2 = [:]; // Shortcut to creation
var myMapWithElements ["MyKey":"MyValue", "MyKey2": "MyValue2"]; // Creation with 2 pairs

myMap["hello"] = "world!";
myMap.getMapCount(); // 1
myMapWithElements.getMapCount(); // 1
```

### Macros
Supported as C supports it.
```c
#define SET_INDEX(#INDEX) \
  arr[#INDEX] = #INDEX;
  
var arr = Array();
SET_INDEX(0);

// arr[0] = 0;
```

### Native Number Types
`Byte`, `UByte`, `Short`, `UShort`, `Int`, `UInt`, `Long`, `ULong`, `Float`, `Double` respectively are all natively built-in types.

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

### Module/Class properties
Properties are called `attributes` in adhoc. They are defined with the `attribute` keyword, just like how you would declare a `var` or `static`.

Without value: 
- `attribute myAttribute` - Will be defaulted to `nil`

With value: 
- `attribute myAttribute = []`

### Code allowed everywhere
Top level, in module or class bodies, code is allowed everywhere.
```
module MyModule
{
    attribute myAttribute = [];
    myAttribute.push("hello world");
}
```

Module extensions are also allowed within function themselves.
```
function myFunction()
{
    module main
    {
        // Anything put here will be part of the "main" module.
        // Declaring a static variable will make it belong to the "main" module.
    }
}
```

### Requires
[TODO]

### Not supported
* Anything modern ECMAScript-ish features (arguably not needed).
* `let`, `const` keywords are not implemented.
* `for..in` and `for..of` are replaced by the much more convenient `foreach`.

### Possibly supported (needs investigation)
* Async/Await

### And more
That have yet to be figured.
