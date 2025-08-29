using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GTAdhocToolchain.Core;

public class AdhocConstants
{
    /// <summary>
    /// Static namespace navigation
    /// </summary>
    public const string OPERATOR_STATIC = "::";

    public const string UNARY_OPERATOR_LOGICAL_NOT = "!";
    public const string UNARY_OPERATOR_MINUS = "-@";
    public const string UNARY_OPERATOR_PLUS = "+@";
    public const string UNARY_OPERATOR_BITWISE_INVERT = "~";

    /// <summary>
    /// Post increment variable
    /// </summary>
    public const string UNARY_OPERATOR_POST_INCREMENT = "@++";

    /// <summary>
    /// Pre-increment variable
    /// </summary>
    public const string UNARY_OPERATOR_PRE_INCREMENT = "++@";

    /// <summary>
    /// Post-increment variable
    /// </summary>
    public const string UNARY_OPERATOR_POST_DECREMENT = "@--";

    /// <summary>
    /// Pre-decrement variable
    /// </summary>
    public const string UNARY_OPERATOR_PRE_DECREMENT = "--@";

    /// <summary>
    /// Equal to
    /// </summary>
    public const string OPERATOR_EQUAL = "==";

    /// <summary>
    /// Not equal to
    /// </summary>
    public const string OPERATOR_NOT_EQUAL = "!=";

    /// <summary>
    /// Logical Lesser than
    /// </summary>
    public const string OPERATOR_LESS_THAN = "<";

    /// <summary>
    /// Logical Greater than
    /// </summary>
    public const string OPERATOR_GREATER_THAN = ">";

    /// <summary>
    /// Logical Less or equal to
    /// </summary>
    public const string OPERATOR_LESS_OR_EQUAL = "<=";

    /// <summary>
    /// Logical Greater Or Equal to
    /// </summary>
    public const string OPERATOR_GREATER_OR_EQUAL = ">=";

    /// <summary>
    /// Addition
    /// </summary>
    public const string OPERATOR_ADD = "+";

    /// <summary>
    /// Subtraction
    /// </summary>
    public const string OPERATOR_SUBTRACT = "-";

    /// <summary>
    /// Multiplication/Import
    /// </summary>
    public const string OPERATOR_MULTIPLY = "*";

    /// <summary>
    /// Division
    /// </summary>
    public const string OPERATOR_DIVIDE = "/";

    /// <summary>
    /// Modulus
    /// </summary>
    public const string OPERATOR_MODULO = "%";

    /// <summary>
    /// Exponentiation/Power
    /// </summary>
    public const string OPERATOR_POWER = "**";

    /// <summary>
    /// Bitwise AND
    /// </summary>
    public const string OPERATOR_BITWISE_AND = "&";

    /// <summary>
    /// Bitwise OR
    /// </summary>
    public const string OPERATOR_BITWISE_OR = "|";

    /// <summary>
    /// Bitwise XOR
    /// </summary>
    public const string OPERATOR_BITWISE_XOR = "^";

    /// <summary>
    /// Binary Right Shift
    /// </summary>
    public const string OPERATOR_RIGHT_SHIFT = ">>";

    /// <summary>
    /// Binary Left Shift
    /// </summary>
    public const string OPERATOR_LEFT_SHIFT = "<<";

    /// <summary>
    /// Array evaluation
    /// </summary>
    public const string OPERATOR_SUBSCRIPT = "[]";

    public const string OPERATOR_IMPORT_ALL = "*";

    /// <summary>
    /// Similar to "this", refers to current object
    /// </summary>
    public const string SELF = "self";

    /// <summary>
    /// Null, undefined
    /// </summary>
    public const string NIL = "nil";
    public const string OBJECT = "Object";
    public const string SYSTEM = "System";
    public const string ITERATOR = "iterator";
}
