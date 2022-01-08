# GTAdhocCompiler
A compiler for the Gran Turismo scripting language, Adhoc, from C#. 

Most of this project is still mostly work in progress, the compiler produces valid custom game code for Adhoc Version 12 (GT PSP -> GT Sport) and certain projects can be fully recompiled from source.

Adhoc is a scripting language that is used for roughly 99% of the entire games's logic, the native code serving mostly solely as the engine.

## Adhoc Language Specifications

Adhoc is very similar to javascript but with a few changes and additions. Refer to the [language spec](LANGUAGE_SPECIFICATION.md).

## TODOs

* Improve syntatic analysis during compilation.
* Local Variable Storage improvements, such as recycling variable slots when prior variables are no longer used within a scope.
* Further document the language and instructions themselves.
* Implement Map `[:]` syntax
* Implement async/await
* Examples
* Possibly tests
* API Documentation

## Compiler
A fork of [esprima-dotnet](https://github.com/Nenkai/esprima-dotnet) is used to lex and parse the Adhoc code into an abstract syntax tree prior to compiling.
