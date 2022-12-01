using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GTAdhocToolchain.Compiler
{
    public class CompilationMessages
    {
        public const string Warning_UsingAwait_Code = "USING_AWAIT";
        public const string Warning_UsingVaCall_Code = "USING_VACALL";
        public static Dictionary<string, string> Warnings = new()
        {
            { Warning_UsingAwait_Code, "Async/Await is only available in GT6 and above" },
            { Warning_UsingVaCall_Code, "Variable Calls/Spread Syntax (<function>.(...<arg>) aka VA_CALL instruction) is only available in GT6 and above" },
        };
        
        public const string Error_ContinueWithoutContextualScope = "Continue keyword must be in a loop or break context.";
        public const string Error_BreakWithoutContextualScope = "Break keyword must be in a loop or break context.";

        public const string Error_ModuleOrClassNameInvalid = "Class or module name must have a valid identifier.";
        public const string Error_ClassNameIsStatic = "Class name must be an identifier without a scope path and not static (::).";

        public const string Error_TryClauseNotBody = "Try clause must contain a body statement.";
        public const string Error_CatchClauseNotBody = "Catch clause must contain a body statement.";
        public const string Error_CatchClauseParameterNotIdentifier = "Catch clause parameter must be an identifier.";

        public const string Error_ForLoopInitializationType = "Unsupported for loop inititialization type";
        public const string Error_ForeachDeclarationNotVariable = "Expected foreach to have a variable declaration.";

        public const string Error_SwitchAlreadyHasDefault = "Switch statement already has a default case.";

        public const string Error_SubroutineWithoutIdentifier = "Expected subroutine to have an identifier.";
        public const string Error_InvalidParameterValueAssignment = "Subroutine parameter default value assignment must be an identifier to a literal. (i.e 'value = 0')";

        public const string Error_StatementExpressionOnly = "Only assignment, call, increment, decrement, await, and new object expressions can be used as a statement";

        public const string Error_ImportDeclarationEmpty = "Import declaration is empty.";

        public const string Error_ArrayPatternNoElements = "Array pattern has no elements.";

        public const string Error_VariableDeclarationIsNull = "Variable declaration for id is null.";

        public const string Error_UnsupportedUnaryOprationOnMemberExpression = "Unsupported unary operation on member expression";

        public const string Error_ForeachUnsupported = "Foreach statements are not supported in Adhoc Versions below 12.";
        public const string Error_MapUnsupported = "Map constants are not supported in Adhoc Versions lower than 11.";
        public const string Error_SelfUnsupported = "'self is not supported in Adhoc Versions lower than 10.";


    }
}
