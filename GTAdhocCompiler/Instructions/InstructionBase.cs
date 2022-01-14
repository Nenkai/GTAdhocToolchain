using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GTAdhocCompiler.Instructions
{
    public abstract class InstructionBase
    {
        public abstract AdhocInstructionType InstructionType { get; }
        public abstract string InstructionName { get; }

        public int LineNumber { get; set; }
    }

    public enum AdhocInstructionType : byte
    {
        ARRAY_CONST_OLD,
        ASSIGN_OLD,
        ATTRIBUTE_DEFINE,
        ATTRIBUTE_PUSH,
        BINARY_ASSIGN_OPERATOR,
        BINARY_OPERATOR,
        CALL,
        CLASS_DEFINE,
        EVAL,
        FLOAT_CONST,
        FUNCTION_DEFINE,
        IMPORT,
        INT_CONST,
        JUMP,
        JUMP_IF_TRUE,
        JUMP_IF_FALSE,
        LIST_ASSIGN_OLD,
        LOCAL_DEFINE,
        LOGICAL_AND_OLD,
        LOGICAL_OR_OLD,
        METHOD_DEFINE,
        MODULE_DEFINE,
        NIL_CONST,
        NOP,
        POP_OLD,
        PRINT,
        REQUIRE,
        SET_STATE_OLD,
        STATIC_DEFINE,
        STRING_CONST,
        STRING_PUSH,
        THROW,
        TRY_CATCH,
        UNARY_ASSIGN_OPERATOR,
        UNARY_OPERATOR,
        UNDEF,
        VARIABLE_PUSH,
        ATTRIBUTE_EVAL,
        VARIABLE_EVAL,
        SOURCE_FILE,
        FUNCTION_CONST,
        METHOD_CONST,
        MAP_CONST_OLD,
        LONG_CONST,
        ASSIGN,
        LIST_ASSIGN,
        CALL_OLD,
        OBJECT_SELECTOR, // Also known as SELF_SELECTOR earlier than GT5P Demo
        SYMBOL_CONST,
        LEAVE, // Also known as CODE_CONST earlier than GT5P Demo
        ARRAY_CONST,
        ARRAY_PUSH,
        MAP_CONST,
        MAP_INSERT,
        POP,
        SET_STATE,
        VOID_CONST,
        ASSIGN_POP,
        U_INT_CONST,
        U_LONG_CONST,
        DOUBLE_CONST,
        ELEMENT_PUSH,
        ELEMENT_EVAL,
        LOGICAL_AND,
        LOGICAL_OR,
        BOOL_CONST,
        MODULE_CONSTRUCTOR,
        VA_CALL,
        CODE_EVAL,

        // GT Sport
        UNK_69,
        UNK_70,
        UNK_71,
    }
}
