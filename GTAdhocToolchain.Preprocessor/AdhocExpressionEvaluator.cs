using Esprima;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.IO;
using Esprima.Ast;

namespace GTAdhocToolchain.Preprocessor
{
    public class AdhocExpressionEvaluator
    {
        static Dictionary<string, int> BinaryPrecedence = new()
        {
            { "**", 1 },
            { "*", 2 },
            { "/",  2 },
            { "%",  2 },
            { "+",  3 },
            { "-",  3 },
            { ">>",  4 },
            { "<<",  4 },
            { ">=",  5 },
            { "<=",  5 },
            { "<",  5 },
            { ">",  5 },
            { "==",  6 },
            { "!=",  6 },
            { "&" , 7 },
            { "^" , 8 },
            { "|" , 9 },
            { "&&" , 10 },
            { "||" , 11 }
        };

        static Dictionary<string, Func<int, int, int>> BinaryOperations = new()
        {
            { "**", (a, b) => (int)Math.Pow(a, b) },
            { "*", (a, b) => a * b },
            { "/",  (a, b) => a / b },
            { "%",  (a, b) => a % b },
            { "+",  (a, b) => a + b },
            { "-",  (a, b) => a - b },
            { ">>",  (a, b) => a >> b },
            { "<<",  (a, b) => a << b },
            { ">=",  (a, b) => a >= b ? 1 : 0 },
            { "<=",  (a, b) => a <= b ? 1 : 0 },
            { "<",  (a, b) => a < b ? 1 : 0 },
            { ">",  (a, b) => a > b ? 1 : 0 },
            { "==",  (a, b) => a == b ? 1 : 0 },
            { "!=",  (a, b) => a != b ? 1 : 0 },
            { "&" , (a, b) => a & b },
            { "^" , (a, b) => a ^ b },
            { "|" , (a, b) => a | b },
            { "&&" , (a, b) => (a != 0 && b != 0) ? 1 : 0 },
            { "||" , (a, b) => (a != 0 || b != 0) ? 1 : 0 }
        };

        static Dictionary<string, Func<int, int>> UnaryOperations = new()
        {
            { "!", (v) => v == 0 ? 1 : 0 },
            { "~",  (v) => ~v },
            { "+",  (v) => v },
            { "-",  (v) => -v },
        };

        private List<Token> _tokens;
        private int _index = 0;
        private Token _lookahead;

        public AdhocExpressionEvaluator(List<Token> tokens)
        {
            _tokens = tokens;
            _lookahead = _tokens[0];
        }

        public int Evaluate()
        {
            return EvalTernary();
        }

        public int EvalTernary()
        {
            int c = EvalBinary(10);
            if (_lookahead?.Value as string != "?")
                return c;

            NextToken();
            int v1 = EvalBinary(10);

            if (_lookahead.Value as string != ":")
                throw new Exception("Expected ':' after '?' for ternary expression");

            NextToken();
            int v2 = EvalBinary(10);

            return c == 0 ? v2 : v1;
        }

        public int EvalBinary(int p)
        {
            if (p == 0)
            {
                return EvalUnary();
            }
            else
            {
                int value = EvalBinary(p - 1);

                while (_lookahead != null && BinaryPrecedence.TryGetValue(_lookahead.Value as string, out int prec) && prec == p)
                {
                    var op = BinaryOperations[_lookahead.Value as string];
                    NextToken();

                    var otherValue = EvalBinary(p - 1);
                    value = op(value, otherValue);
                }

                return value;
            }
        }

        public int EvalUnary()
        {
            if (UnaryOperations.TryGetValue(_lookahead.Value as string, out Func<int, int> op))
            {
                NextToken();
                return op(EvalUnary());
            }
            else if (_lookahead.Value as string == "(")
            {
                NextToken();
                var v = Evaluate();

                // Check parenthesis
                if (_lookahead?.Value as string != ")")
                    throw new Exception("missing closing parenthesis");

                NextToken();
                return v;
            }
            else if (_lookahead.Type == TokenType.Identifier) // Identifier check
            {
                throw new Exception("Not supported");
            }
            else if (_lookahead.Type == TokenType.BooleanLiteral)
            {
                var v = Boolean.Parse(_lookahead.Value as string);
                NextToken();
                return v ? 1 : 0;
            }
            else if (_lookahead.Type == TokenType.NumericLiteral)
            {
                var v = int.Parse(_lookahead.Value as string);
                NextToken();
                return (int)v;
            }
            else
            {
                throw new Exception("Syntax error");
            }
        }

        public void NextToken()
        {
            _index++;

            if (_index < _tokens.Count)
                _lookahead = _tokens[_index];
            else
                _lookahead = null;
        }
    }
}
