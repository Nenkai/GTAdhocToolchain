# GTAdhocToolchain
A toolchain for the Gran Turismo scripting language, Adhoc, from C#. 

Adhoc is a scripting language that is used for roughly 99% of the entire games's logic, the native code serving mostly solely as the engine.

The toolchain contains the following:
* **Adhoc Script Compiler** (`.ad` -> `.adc` for Adhoc Version 12, GT PSP to GT Sport) - Ongoing expetimental support for Version 7 (GT4 Online)
* Adhoc Project Builder
* VS Code Extension (syntax highlighting mostly)
* Script Disassembler (`.adc` to assembly-like syntax)
* Menu Layout Reader/Serializer (`mproject/mwidget`)
* Script and Menu Layout Packager (GT6 `.mpackage`)
* Asset Packager (`.gpb`)
* Compare scripts for dissasembly matching

## Current State

The toolchain is capable of compiling fully working original and custom projects (see [OpenAdhoc](https://github.com/Nenkai/OpenAdhoc)).

## Adhoc Language Specifications

Adhoc is very similar to javascript but with a few changes and additions. Refer to the [language spec](LANGUAGE_SPECIFICATION.md).

## TODOs

* Improve syntatic analysis during compilation.
* Local Variable Storage improvements, such as recycling variable slots when prior variables are no longer used within a scope.
* Further document the language and instructions themselves.
* Examples
* Possibly tests
* API Documentation

## Compiler
A fork of [esprima-dotnet](https://github.com/Nenkai/esprima-dotnet) is used to lex and parse the Adhoc code into an abstract syntax tree prior to compiling.
