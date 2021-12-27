# GTAdhocCompiler
A compiler for the Gran Turismo scripting language, Adhoc, from C#. Most of this project is still heavily work in progress, and most basic to intermediate logic is functional and produces valid code for Adhoc Version 12 (GT PSP -> GT Sport).

Adhoc is a scripting language that is used for roughly 99% of the entire games's logic, the native code serving mostly solely as the engine.

## Adhoc Language Specifications

Adhoc is very similar to javascript but with a few changes and additions.

## Compiler
A fork of [esprima-dotnet](https://github.com/Nenkai/esprima-dotnet) is used to lex and parse the Adhoc code into an abstract syntax tree prior to compiling.
