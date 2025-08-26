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
        public const string Warning_UsingOptional_Code = "USING_OPTIONAL";
        public const string Warning_UsingNullCoalescing_Code = "USING_NULL_COALESCING";
        public const string Warning_UsingDelegateCode = "USING_DELEGATE";

        public static Dictionary<string, string> Warnings = new()
        {
            { Warning_UsingAwait_Code, "Async/Await is only available in GT6's Adhoc Version 12 and above" },
            { Warning_UsingVaCall_Code, "Variadic Calls (aka VA_CALL instruction) is only available in GT6's Adhoc Version 12 and above" },
            { Warning_UsingOptional_Code, "Chained optional expressions (?. , ?[) is only available in GT Sport's Adhoc Version 12 and above" },
            { Warning_UsingNullCoalescing_Code, "Null coalescing (??) is only available in GT Sport's Adhoc Version 12 and above."},
            { Warning_UsingDelegateCode, "Delegates are only available in GT Sport's Adhoc Version 12 and above. "},
        };

        public const string Error_InvalidListAssignmentArgumentInSubroutine = "Only identifiers can be used in list assignment in function/method parameters.";

        public const string Error_ContinueWithoutContextualScope = "Continue keyword must be in a loop or break context.";
        public const string Error_BreakWithoutContextualScope = "Break keyword must be in a loop or break context.";

        public const string Error_ModuleOrClassNameInvalid = "Class or module name must have a valid identifier.";
        public const string Error_ClassNameIsStatic = "Class name must be an identifier without a scope path and not static (::).";

        public const string Error_TryClauseNotBody = "Try clause must contain a body statement.";
        public const string Error_CatchClauseNotBody = "Catch clause must contain a body statement.";
        public const string Error_CatchClauseParameterNotIdentifier = "Catch clause parameter must be an identifier.";

        public const string Error_ForLoopInitializationType = "Unsupported for loop inititialization type";
        public const string Error_ForeachDeclarationNotVariableOrList = "Expected foreach to have a variable declaration, or list assignment expression.";

        public const string Error_SwitchAlreadyHasDefault = "Switch statement already has a default case.";

        public const string Error_SubroutineWithoutIdentifier = "Expected subroutine to have an identifier.";
        public const string Error_InvalidParameterValueAssignment = "Subroutine parameter default value assignment must be an identifier to a literal. (i.e 'value = 0')";

        public const string Error_StatementExpressionOnly = "Only assignment, call, increment, decrement, await, and new object expressions can be used as a statement";

        public const string Error_ImportWildcardWithAlias = "Wildcard import cannot be aliased.";

        public const string Error_ArrayPatternNoElements = "Array pattern has no elements.";

        public const string Error_VariableDeclarationIsNull = "Variable declaration for id is null.";

        public const string Error_UnsupportedUnaryOprationOnMemberExpression = "Unsupported unary operation on member expression";

        public const string Error_ForeachUnsupported = "Foreach statements are not supported in Adhoc Versions below 11.";
        public const string Error_MapUnsupported = "Map constants are not supported in Adhoc Versions lower than 11.";
        public const string Error_SelfUnsupported = "self is not supported in Adhoc Versions lower than 7.";
        public const string Error_ListAssignementRestElementUnsupported = "List assignment rest element '...' is only supported in Adhoc version 12 and later.";
        public const string Error_DelegatesUnsupported = "Delegates are only available starting from Adhoc version 12, starting GT Sport.";
        public const string Error_OptionalComputedMemberUnsupported = "Optional '?[' is only available starting from Adhoc version 12, starting GT Sport.";
        public const string Error_OptionalMemberUnsupported = "Optional '?.' is only available starting from Adhoc version 12, starting GT Sport.";
        public const string Error_NullCoalescingUnsupported = "Nullish coalescing (??) is only available from Adhoc version 12, starting GT Sport";

        public const string Error_VaCall_MissingFunctionTarget = "Missing FunctionObject target and argument array in variadic function call";
        public const string Error_VaCall_MissingArguments = "Missing argument array in variadic function call";

        public const string Error_NilNotValidVarialbleName = "nil is not a valid variable name";

        public const string Error_DefaultParameterValuesUnsupported = "Default parameter values are only supported in Adhoc version 7 and above.";
        public const string Error_DefaultAttributeValuesUnsupported = "Default attribute values are only available in Adhoc version 7 and above.";
    }
}
