using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using GTAdhocToolchain.Core.Instructions;

namespace GTAdhocToolchain.Core.Instructions
{
    public abstract class InstructionBase
    {
        public abstract AdhocInstructionType InstructionType { get; }
        public abstract string InstructionName { get; }

        public uint LineNumber { get; set; }

        public uint InstructionOffset { get; set; }

        public abstract void Serialize(AdhocStream stream);
        public abstract void Deserialize(AdhocStream stream);
        public abstract string Disassemble(bool asCompareMode = false);

        public virtual bool IsFunctionOrMethod()
        {
            return InstructionType == AdhocInstructionType.METHOD_DEFINE || InstructionType == AdhocInstructionType.FUNCTION_DEFINE
                    || InstructionType == AdhocInstructionType.METHOD_CONST || InstructionType == AdhocInstructionType.FUNCTION_CONST;
        }

        public static InstructionBase GetByType(AdhocInstructionType type)
        {
            return type switch
            {
                AdhocInstructionType.MODULE_DEFINE => new InsModuleDefine(),
                AdhocInstructionType.FUNCTION_CONST => new InsFunctionConst(),
                AdhocInstructionType.METHOD_DEFINE => new InsMethodDefine(),
                AdhocInstructionType.FUNCTION_DEFINE => new InsFunctionDefine(),
                AdhocInstructionType.METHOD_CONST => new InsMethodConst(),
                AdhocInstructionType.VARIABLE_EVAL => new InsVariableEvaluation(),
                AdhocInstructionType.CALL => new InsCall(),
                AdhocInstructionType.CALL_OLD => new InsCallOld(),
                AdhocInstructionType.JUMP_IF_FALSE => new InsJumpIfFalse(),
                AdhocInstructionType.FLOAT_CONST => new InsFloatConst(),
                AdhocInstructionType.ATTRIBUTE_PUSH => new InsAttributePush(),
                AdhocInstructionType.ASSIGN_POP => new InsAssignPop(),
                AdhocInstructionType.LEAVE => new InsLeaveScope(),
                AdhocInstructionType.VOID_CONST => new InsVoidConst(),
                AdhocInstructionType.SET_STATE => new InsSetState(),
                AdhocInstructionType.SET_STATE_OLD => new InsSetStateOld(),
                AdhocInstructionType.NIL_CONST => new InsNilConst(),
                AdhocInstructionType.ATTRIBUTE_DEFINE => new InsAttributeDefine(),
                AdhocInstructionType.BOOL_CONST => new InsBoolConst(),
                AdhocInstructionType.SOURCE_FILE => new InsSourceFile(),
                AdhocInstructionType.IMPORT => new InsImport(),
                AdhocInstructionType.STRING_CONST => new InsStringConst(),
                AdhocInstructionType.POP => new InsPop(),
                AdhocInstructionType.POP_OLD => new InsPopOld(),
                AdhocInstructionType.CLASS_DEFINE => new InsClassDefine(),
                AdhocInstructionType.ATTRIBUTE_EVAL => new InsAttributeEvaluation(),
                AdhocInstructionType.INT_CONST => new InsIntConst(),
                AdhocInstructionType.STATIC_DEFINE => new InsStaticDefine(),
                AdhocInstructionType.VARIABLE_PUSH => new InsVariablePush(),
                AdhocInstructionType.BINARY_OPERATOR => new InsBinaryOperator(),
                AdhocInstructionType.JUMP => new InsJump(),
                AdhocInstructionType.ELEMENT_EVAL => new InsElementEval(),
                AdhocInstructionType.STRING_PUSH => new InsStringPush(),
                AdhocInstructionType.JUMP_IF_TRUE => new InsJumpIfTrue(),
                AdhocInstructionType.EVAL => new InsEval(),
                AdhocInstructionType.BINARY_ASSIGN_OPERATOR => new InsBinaryAssignOperator(),
                AdhocInstructionType.LOGICAL_OR_OLD => new InsLogicalOrOld(),
                AdhocInstructionType.LOGICAL_OR => new InsLogicalOr(),
                AdhocInstructionType.LIST_ASSIGN => new InsListAssign(),
                AdhocInstructionType.LIST_ASSIGN_OLD => new InsListAssignOld(),
                AdhocInstructionType.ELEMENT_PUSH => new InsElementPush(),
                AdhocInstructionType.MAP_CONST => new InsMapConst(),
                AdhocInstructionType.MAP_CONST_OLD => new InsMapConstOld(),
                AdhocInstructionType.MAP_INSERT => new InsMapInsert(),
                AdhocInstructionType.UNARY_OPERATOR => new InsUnaryOperator(),
                AdhocInstructionType.LOGICAL_AND_OLD => new InsLogicalAndOld(),
                AdhocInstructionType.LOGICAL_AND => new InsLogicalAnd(),
                AdhocInstructionType.ARRAY_CONST => new InsArrayConst(),
                AdhocInstructionType.ARRAY_CONST_OLD => new InsArrayConstOld(),
                AdhocInstructionType.ARRAY_PUSH => new InsArrayPush(),
                AdhocInstructionType.UNARY_ASSIGN_OPERATOR => new InsUnaryAssignOperator(),
                AdhocInstructionType.SYMBOL_CONST => new InsSymbolConst(),
                AdhocInstructionType.OBJECT_SELECTOR => new InsObjectSelector(),
                AdhocInstructionType.LONG_CONST => new InsLongConst(),
                AdhocInstructionType.UNDEF => new InsUndef(),
                AdhocInstructionType.TRY_CATCH => new InsTryCatch(),
                AdhocInstructionType.THROW => new InsThrow(),
                AdhocInstructionType.ASSIGN => new InsAssign(),
                AdhocInstructionType.ASSIGN_OLD => new InsAssignOld(),
                AdhocInstructionType.U_INT_CONST => new InsUIntConst(),
                AdhocInstructionType.REQUIRE => new InsRequire(),
                AdhocInstructionType.U_LONG_CONST => new InsULongConst(),
                AdhocInstructionType.PRINT => new InsPrint(),
                AdhocInstructionType.MODULE_CONSTRUCTOR => new InsModuleConstructor(),
                AdhocInstructionType.VA_CALL => new InsVaCall(),
                AdhocInstructionType.NOP => new InsNop(),
                AdhocInstructionType.DOUBLE_CONST => new InsDoubleConst(),
                AdhocInstructionType.DELEGATE_DEFINE => new InsDelegateDefine(),
                AdhocInstructionType.JUMP_IF_NIL => new InsJumpIfNil(),
                AdhocInstructionType.LOGICAL_OPTIONAL => new InsLogicalOptional(),
                AdhocInstructionType.BYTE_CONST => new InsByteConst(),
                AdhocInstructionType.U_BYTE_CONST => new InsUByteConst(),
                AdhocInstructionType.SHORT_CONST => new InsShortConst(),
                AdhocInstructionType.U_SHORT_CONST => new InsUShortConst(),
                _ => throw new Exception($"Encountered unimplemented {type} instruction."),
            };
        }
    }

    public enum AdhocInstructionType : byte
    {
        /// <summary>
        /// Also known as ARRAY_PUSH (not the new one)
        /// </summary>
        ARRAY_CONST_OLD = 0,
        ASSIGN_OLD = 1,
        ATTRIBUTE_DEFINE = 2,
        ATTRIBUTE_PUSH = 3,
        BINARY_ASSIGN_OPERATOR = 4,
        BINARY_OPERATOR = 5,
        CALL = 6,
        CLASS_DEFINE = 7,
        EVAL = 8,
        FLOAT_CONST = 9,
        FUNCTION_DEFINE = 10,
        IMPORT = 11,
        INT_CONST = 12,
        JUMP = 13,
        JUMP_IF_TRUE = 14, // Also known as JUMP_NOT_ZERO
        JUMP_IF_FALSE = 15, // Also known as JUMP_ZERO
        LIST_ASSIGN_OLD = 16,
        LOCAL_DEFINE = 17,
        LOGICAL_AND_OLD = 18,
        LOGICAL_OR_OLD = 19,
        METHOD_DEFINE = 20,
        MODULE_DEFINE = 21,
        NIL_CONST = 22,
        NOP = 23,
        POP_OLD = 24,
        PRINT = 25,
        REQUIRE = 26,
        SET_STATE_OLD = 27,
        STATIC_DEFINE = 28,
        STRING_CONST = 29,
        STRING_PUSH = 30,
        THROW = 31,
        TRY_CATCH = 32,
        UNARY_ASSIGN_OPERATOR = 33,
        UNARY_OPERATOR = 34,
        UNDEF = 35,
        VARIABLE_PUSH = 36,
        ATTRIBUTE_EVAL = 37,
        VARIABLE_EVAL = 38,
        SOURCE_FILE = 39,

        // GTHD Release (V10)
        FUNCTION_CONST = 40,
        METHOD_CONST = 41,
        MAP_CONST_OLD = 42,
        LONG_CONST = 43,
        ASSIGN = 44,
        LIST_ASSIGN = 45,
        CALL_OLD = 46,

        // GT5P JP Demo (V10)
        OBJECT_SELECTOR = 47, // Also known as SELF_SELECTOR earlier than GT5P Demo
        SYMBOL_CONST = 48,
        LEAVE = 49, // Also known as CODE_CONST earlier than GT5P Demo

        // V11
        ARRAY_CONST = 50, 
        ARRAY_PUSH = 51,
        MAP_CONST = 52,
        MAP_INSERT = 53,
        POP = 54,
        SET_STATE = 55,
        VOID_CONST = 56,
        ASSIGN_POP = 57,

        // GT5P Spec 3 (V12)
        U_INT_CONST = 58,
        U_LONG_CONST = 59,
        DOUBLE_CONST = 60,

        // GT5 TT Challenge (V12)
        ELEMENT_PUSH = 61,
        ELEMENT_EVAL = 62,
        LOGICAL_AND = 63,
        LOGICAL_OR = 64,
        BOOL_CONST = 65,
        MODULE_CONSTRUCTOR = 66,

        // GT6 (V12)
        VA_CALL = 67,
        CODE_EVAL = 68,

        // GT Sport (V12)
        DELEGATE_DEFINE = 69,
        JUMP_IF_NIL = 70,
        LOGICAL_OPTIONAL = 71,

        // GT7 (V13)
        BYTE_CONST = 72,
        U_BYTE_CONST = 73,
        SHORT_CONST = 74,
        U_SHORT_CONST = 75,
    }
}
