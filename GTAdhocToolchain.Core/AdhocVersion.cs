using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GTAdhocToolchain.Core;

public struct AdhocVersion
{
    public uint VersionNumber;

    public AdhocVersion(uint version)
    {
        VersionNumber = version;
    }

    public readonly bool HasSymbolTable() => VersionNumber >= 9;

    public readonly bool HasReservedLocalInFrame() => VersionNumber <= 10;

    /// <summary>
    /// Whether the SOURCE_FILE instruction is supported.
    /// </summary>
    /// <returns></returns>
    public readonly bool HasSourceFileInstructionSupport() => VersionNumber >= 7;

    /// <summary>
    /// Whether class inheritance should emit from System::Object rather than Object.
    /// </summary>
    /// <returns></returns>
    public readonly bool ObjectInheritsFromSystemObject() => VersionNumber >= 10;

    /// <summary>
    /// Whether in 'if' statements, the else 'jump' is always emitted, regardless of if an 'else' statement exists.
    /// </summary>
    /// <returns></returns>
    public readonly bool IfConditionAlwaysHasAlternateJump() => VersionNumber >= 7 && VersionNumber < 11;

    /// <summary>
    /// Whether the 'foreach' statement is supported.
    /// </summary>
    /// <returns></returns>
    public readonly bool HasForeachSupport() => VersionNumber >= 11;

    /// <summary>
    /// Whether operators should use their internal names in symbols, rather than just their operator characters. <br/>
    /// eg: '*' will be converted to '__mul__'
    /// </summary>
    /// <returns></returns>
    public readonly bool ShouldUseInternalOperatorNames() => VersionNumber >= 12;

    /// <summary>
    /// Whether function parameters can have default values.
    /// </summary>
    /// <returns></returns>
    public readonly bool HasSupportForFunctionParametersDefaultValues() => VersionNumber >= 7;

    /// <summary>
    /// Whether delegates *can* be supported on this version.
    /// </summary>
    /// <returns></returns>
    public readonly bool IsMinimumVersionForDelegateSupport() => VersionNumber >= 12;

    /// <summary>
    /// Whether optional expressions *can* be supported on this version.
    /// </summary>
    /// <returns></returns>
    public readonly bool IsMinimumVersionForOptionalSupport() => VersionNumber >= 12;

    /// <summary>
    /// Returns whether the self keyword is supported.
    /// </summary>
    /// <returns></returns>
    public readonly bool HasSelfSupport() => VersionNumber >= 7;

    /// <summary>
    /// Whether a local variable should be allocated for 'self'.
    /// </summary>
    /// <returns></returns>
    public readonly bool ShouldAllocateVariableForSelf() => VersionNumber <= 10;

    /// <summary>
    /// Whether a static should be defined for each subroutine declaration.
    /// </summary>
    /// <returns></returns>
    public readonly bool ShouldDefineFunctionAsStaticVariables() => VersionNumber >= 10;

    /// <summary>
    /// Returns whether maps are supported.
    /// </summary>
    /// <returns></returns>
    public readonly bool HasMapSupport() => VersionNumber >= 11;

    /// <summary>
    /// Returns whether new ARRAY_CONST is supported.
    /// </summary>
    /// <returns></returns>
    public readonly bool HasNewArrayConstSupport() => VersionNumber >= 11;

    /// <summary>
    /// Whether STRING_PUSH is emitted for empty strings.
    /// </summary>
    /// <returns></returns>
    public readonly bool ShouldUseStringPushForEmptyStrings() => VersionNumber >= 11;

    /// <summary>
    /// Whether a POP should be inserted in ternary operations.
    /// </summary>
    /// <returns></returns>
    public readonly bool ShouldPopInTernary() => VersionNumber >= 11;

    /// <summary>
    /// Whether a EVAL should be inserted on unary * expressions.
    /// </summary>
    /// <returns></returns>
    public readonly bool ShouldEvalInIndirection() => VersionNumber >= 11;

    /// <summary>
    /// Returns whether NOP should always be emitted on scope open/close/empty statement.
    /// </summary>
    /// <returns></returns>
    public readonly bool IsNopAlwaysEmitted() => VersionNumber < 7;

    /// <summary>
    /// Whether to return VOID_CONST for function returns without a return value.
    /// </summary>
    /// <returns></returns>
    public readonly bool ShouldReturnVoidForEmptyFunctionReturn() => VersionNumber >= 11;

    public readonly bool ShouldAlwaysClearLocalsOnScopeLeave() => VersionNumber < 10;

    public readonly bool ShouldPopOnReturnStatementWithValue() => VersionNumber < 11;

    /// <summary>
    /// Whether the BYTE_CONST instruction is supported.
    /// </summary>
    /// <returns></returns>
    public readonly bool HasByteSupport() => VersionNumber >= 13;

    /// <summary>
    /// Whether the U_BYTE_CONST instruction is supported.
    /// </summary>
    /// <returns></returns>
    public readonly bool HasUByteSupport() => VersionNumber >= 13;

    /// <summary>
    /// Whether the SHORT_CONST instruction is supported.
    /// </summary>
    /// <returns></returns>
    public readonly bool HasShortSupport() => VersionNumber >= 13;

    /// <summary>
    /// Whether the U_SHORT_CONST instruction is supported.
    /// </summary>
    /// <returns></returns>
    public readonly bool HasUShortSupport() => VersionNumber >= 13;

    /// <summary>
    /// Whether the U_INT_CONST instruction is supported.
    /// </summary>
    /// <returns></returns>
    public readonly bool HasUIntSupport() => VersionNumber >= 12;

    /// <summary>
    /// Whether the U_LONG_CONST instruction is supported.
    /// </summary>
    /// <returns></returns>
    public readonly bool HasULongSupport() => VersionNumber >= 12;

    /// <summary>
    /// Whether the DOUBLE_CONST instruction is supported.
    /// </summary>
    /// <returns></returns>
    public readonly bool HasDoubleSupport() => VersionNumber >= 12;

    /// <summary>
    /// Whether the BOOL_CONST instruction is supported.
    /// </summary>
    /// <returns></returns>
    public readonly bool HasBoolSupport() => VersionNumber >= 12;

    /// <summary>
    /// Whether the ATTRIBUTE_EVAL instruction is supported.
    /// </summary>
    /// <returns></returns>
    public readonly bool HasAttributeEvalSupport() => VersionNumber >= 7;

    /// <summary>
    /// Whether the ATTRIBUTE_EVAL instruction is supported.
    /// </summary>
    /// <returns></returns>
    public readonly bool HasVariableEvalSupport() => VersionNumber >= 7;

    /// <summary>
    /// Whether the ASSIGN_POP instruction is supported.
    /// </summary>
    /// <returns></returns>
    public readonly bool HasAssignPopSupport() => VersionNumber >= 11;

    /// <summary>
    /// Whether the new ASSIGN instruction is supported.
    /// </summary>
    /// <returns></returns>
    public readonly bool HasNewAssignSupport() => VersionNumber >= 10;

    /// <summary>
    /// Whether the new LIST_ASSIGN instruction is supported.
    /// </summary>
    /// <returns></returns>
    public readonly bool HasNewListAssignSupport() => VersionNumber >= 11;

    /// <summary>
    /// Whether the new POP instruction is supported.
    /// </summary>
    /// <returns></returns>
    public readonly bool HasNewPopSupport() => VersionNumber >= 10;

    /// <summary>
    /// Whether the new SET_STATE instruction is supported.
    /// </summary>
    /// <returns></returns>
    public readonly bool HasNewSetStateSupport() => VersionNumber >= 10;

    /// <summary>
    /// Whether a SET_STATE should always be emitted in functions, regardless of if a top-level return value already emits one.
    /// </summary>
    /// <returns></returns>
    public readonly bool ShouldAlwaysEmitSetStateInFunctions() => VersionNumber < 10;

    public readonly bool UseVoidInsteadOfNop() => VersionNumber >= 11;

    /// <summary>
    /// Whether NIL_CONST should be emitted for static declarations on no value assigned.
    /// </summary>
    /// <returns></returns>
    public readonly bool ShouldInsertNilForStaticDefinition() => VersionNumber >= 7 && VersionNumber < 10;

    /// <summary>
    /// Whether default attribute values are supported.
    /// </summary>
    /// <returns></returns>
    public readonly bool HasSupportForAttributeDefinitionDefaultValues() => VersionNumber >= 7;

    /// <summary>
    /// Whether '...' is supported in functions and list assignments
    /// </summary>
    /// <returns></returns>
    public readonly bool SupportsRestElement() => VersionNumber >= 12;

    /// <summary>
    /// Whether aliasing is supported in import statements.
    /// </summary>
    /// <returns></returns>
    public readonly bool SupportsImportAlias() => VersionNumber >= 10;

    /// <summary>
    /// Whether the new BINARY_ASSIGN_OPERATOR instruction is supported.
    /// </summary>
    /// <returns></returns>
    public readonly bool HasBinaryAssignSupport() => VersionNumber >= 5;

    /// <summary>
    /// Whether to use LOGICAL_OR/LOGICAL_AND rather than LOGICAL_OR_OLD/LOGICAL_AND_OLD
    /// </summary>
    /// <returns></returns>
    public readonly bool UsesNewLogicalInstructions() => VersionNumber >= 11;

    /// <summary>
    /// Whether ELEMENT_EVAL is supported rather than use '[]' operator.
    /// </summary>
    /// <returns></returns>
    public readonly bool HasElementEvalSupport() => VersionNumber >= 12;

    /// <summary>
    /// Whether ELEMENT_PUSH is supported rather than use '[]' operator.
    /// </summary>
    /// <returns></returns>
    public readonly bool HasElementPushSupport() => VersionNumber >= 12;

    /// <summary>
    /// Whether to emit EVAL on function call.
    /// </summary>
    /// <returns></returns>
    public readonly bool ShouldEvalOnCall() => VersionNumber < 11;

    /// <summary>
    /// Whether to emit LEAVE, used to trim the stack of unused variables.
    /// </summary>
    /// <returns></returns>
    public readonly bool HasLeaveSupport() => VersionNumber >= 11;

    /// <summary>
    /// Whether expressions should be evaluated before their EVAL or PUSH instruction is emitted.
    /// </summary>
    /// <returns></returns>
    public readonly bool ExpressionBeforeEvalOrPush() => VersionNumber >= 11;

    /// <summary>
    /// Whether to use new stack with split locals and statics.
    /// </summary>
    /// <returns></returns>
    public readonly bool UsesNewSplitStack() => VersionNumber >= 11; 
}
