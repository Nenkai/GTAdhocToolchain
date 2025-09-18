using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GTAdhocToolchain.Core;

public class AdhocSymbolMap
{
    public Dictionary<string, AdhocSymbol> Symbols { get; set; } = [];

    /// <summary>
    /// case#/in#/catch# etc variable counter
    /// </summary>
    public int TempVariableCounter { get; set; } = 0;

    /// <summary>
    /// fin# variable counter (counted aside)
    /// </summary>
    public int FinalizerTempVariableCounter { get; set; } = 0;

    /// <summary>
    /// Adds a new symbol to the map if it doesn't already exist.
    /// </summary>
    /// <param name="symbolName"></param>
    /// <returns>Symbol entity.</returns>
    public AdhocSymbol RegisterSymbol(string symbolName, bool convertToOperand = true, bool isHexSequence = false)
    {
        string identifier = convertToOperand ? ConvertSymbol(symbolName) : symbolName;

        if (!Symbols.TryGetValue(identifier, out var symbol))
        {
            symbol = new AdhocSymbol(Symbols.Count, identifier, isHexSequence);
            Symbols.Add(identifier, symbol);
        }

        return symbol;
    }

    public bool TryGetSymbol(string symbolName, out AdhocSymbol symb)
    {
        string identifier = ConvertSymbol(symbolName);
        return Symbols.TryGetValue(identifier, out symb);
    }

    private static string ConvertSymbol(string symbolName)
    {
        return symbolName switch
        {
            AdhocConstants.OPERATOR_EQUAL => "__eq__",
            AdhocConstants.OPERATOR_NOT_EQUAL => "__ne__",
            AdhocConstants.OPERATOR_GREATER_OR_EQUAL => "__ge__",
            AdhocConstants.OPERATOR_GREATER_THAN => "__gt__",
            AdhocConstants.OPERATOR_LESS_OR_EQUAL => "__le__",
            AdhocConstants.OPERATOR_LESS_THAN => "__lt__",
            AdhocConstants.UNARY_OPERATOR_LOGICAL_NOT => "__not__",

            // __minus__
            AdhocConstants.OPERATOR_ADD => "__add__",
            AdhocConstants.OPERATOR_SUBTRACT => "__sub__", 
            AdhocConstants.OPERATOR_MULTIPLY => "__mul__", 
            AdhocConstants.OPERATOR_DIVIDE => "__div__", 
            AdhocConstants.OPERATOR_BITWISE_XOR => "__xor__", 
            AdhocConstants.OPERATOR_MODULO => "__mod__",
            AdhocConstants.OPERATOR_POWER => "__pow__",
            AdhocConstants.OPERATOR_LEFT_SHIFT => "__lshift__", 
            AdhocConstants.OPERATOR_RIGHT_SHIFT => "__rshift__",
            AdhocConstants.UNARY_OPERATOR_BITWISE_INVERT => "__invert__",
            AdhocConstants.OPERATOR_BITWISE_OR => "__or__",
            AdhocConstants.OPERATOR_BITWISE_AND => "__and__",

            AdhocConstants.UNARY_OPERATOR_MINUS => "__uminus__",
            AdhocConstants.UNARY_OPERATOR_PLUS => "__uplus__",
            AdhocConstants.UNARY_OPERATOR_PRE_DECREMENT => "__pre_decr__",
            AdhocConstants.UNARY_OPERATOR_PRE_INCREMENT => "__pre_incr__",
            AdhocConstants.UNARY_OPERATOR_POST_DECREMENT => "__post_decr__",
            AdhocConstants.UNARY_OPERATOR_POST_INCREMENT => "__post_incr__",

            _ => symbolName,
        };
    }
}
