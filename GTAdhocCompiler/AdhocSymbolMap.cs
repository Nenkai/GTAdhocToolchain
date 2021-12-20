using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GTAdhocCompiler
{
    public class AdhocSymbolMap
    {
        public Dictionary<string, AdhocSymbol> Symbols { get; set; } = new();

        /// <summary>
        /// Adds a new symbol to the map if it doesn't already exists.
        /// </summary>
        /// <param name="symbolName"></param>
        /// <returns>Symbol entity.</returns>
        public AdhocSymbol RegisterSymbol(string symbolName)
        {
            string identifier = ConvertSymbol(symbolName);

            if (!Symbols.TryGetValue(identifier, out var symbol))
            {
                symbol = new AdhocSymbol(Symbols.Count, identifier);
                Symbols.Add(identifier, symbol);
            }

            return symbol;
        }

        public bool TryGetSymbol(string symbolName, out AdhocSymbol symb)
        {
            string identifier = ConvertSymbol(symbolName);
            return Symbols.TryGetValue(identifier, out symb);
        }

        private string ConvertSymbol(string symbolName)
        {
            return symbolName switch
            {
                "==" => "__eq__", // Equals
                "!=" => "__ne__", // Not Equal
                ">=" => "__ge__", // Greater Equal
                ">" => "__gt__", // Greater Than
                "<=" => "__le__", // Lesser Equal
                "<" => "__lt__", // Lesser Than
                "!" => "__not__", // Logical Not

                // __minus__
                "+" => "__add__", // Add,
                "-" => "__min__", // Minus,
                "*" => "__mul__", // Multiply or Import Wildcard
                "/" => "__div__", // Division
                "^" => "__xor__", // Xor,
                "%" => "__mod__", // Modulo
                "**" => "__pow__", // Pow
                "<<" => "__lshift__", // Left Shift
                ">>" => "__rshift__", // Right Shift
                "~" => "__invert__", // Invert
                "|" => "__or__", // Or

                "-@" => "__uminus__", // Unary Minus
                "+@" => "__uplus__", // Unary Plus
                "--@" => "__pre_decr__", // Pre Decrementation,
                "++@" => "__pre_incr__", // Pre Incrementation
                "@--" => "__post_decr__", // Post Decrementation,
                "@++" => "__post_incr__", // Post Incrementation

                _ => symbolName,
            };
        }
    }
}
