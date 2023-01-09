using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Globalization;

using GTAdhocToolchain.Menu.Fields;

namespace GTAdhocToolchain.Menu
{
    public class MTextIO
    {
        public string FileName { get; set; }

        public StreamReader Stream { get; set; }

        public bool ReadingArray { get; set; }

        public const char ARRAY_START = '[';
        public const char ARRAY_END = ']';
        public const char SCOPE_START = '{';
        public const char SCOPE_END = '}';
        public const char STRING_SCOPE = '"';

        public const char NUMBER_PERIOD = '.';
        public const char NUMBER_COMMA = ',';
        public const char NUMBER_MINUS = '-';

        public const char TOKEN_ESCAPER = '\'';
        public const char FIELD_SEPARATOR = ':';

        private StringBuilder _sb = new StringBuilder();

        public MTextIO(string fileName)
        {
            FileName = fileName;
        }

        public mNode Read()
        {
            // To parse float's .
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
            CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
            CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;

            using var file = File.Open(FileName, FileMode.Open);
            Stream = new StreamReader(file);

            byte[] magic = new byte[4];
            file.Read(magic);
            if (Encoding.ASCII.GetString(magic) == "MPRJ")
            {
                throw new Exception("Attempted to read Binary MPRJ with MTextIO.");
            }

            Stream.BaseStream.Position = 0;

            var rootNode = new mNode();
            rootNode.IsRoot = true;
            rootNode.Read(this);
            
            return rootNode;
        }

        public string GetToken()
        {
            ReadingArray = false;
            _sb.Clear();

            bool escapeToken = false;
            while (!Stream.EndOfStream)
            {
                var nextChar = Stream.Peek();
                if (nextChar == -1)
                    return null;

                var c = (char)nextChar;
                if (_sb.Length != 0) // Check for potential termination, first
                {
                    if (IsDiscardableChar(c))
                    {
                        return _sb.ToString();
                    }
                    else if (c == SCOPE_START || c == ARRAY_START)
                    {
                        return _sb.ToString();
                    }
                    else if (c == TOKEN_ESCAPER)
                    {
                        if (!escapeToken) // Only start
                            throw new UISyntaxError($"Unexpected token escape identifier end, start not specified");

                        Advance();
                        return _sb.ToString();
                    }
                    else if (c == ARRAY_END)
                    {
                        if (_sb.Length == 1) // Only start
                            throw new UISyntaxError($"Unexpected array length definition end, no number specified.");

                        _sb.Append((char)Stream.Read());
                        return _sb.ToString();
                    }
                }

                if (IsDiscardableChar(c))
                {
                    Advance();
                    continue;
                }

                if (ReadingArray)
                {
                    if (!char.IsDigit(c))
                        throw new UISyntaxError($"Expected number in array length definition, got {c}.");

                    _sb.Append((char)Stream.Read());
                }
                else
                {
                    if (char.IsLetterOrDigit(c) || c == '_') // Valid identifier?
                    {
                        _sb.Append((char)Stream.Read());
                    }
                    else if (c == TOKEN_ESCAPER)
                    {
                        if (_sb.Length != 0) // Only start
                            throw new UISyntaxError($"Unexpected token escape identifier, token is already started.");

                        escapeToken = true;
                        Advance();
                    }
                    else if (c == FIELD_SEPARATOR) 
                    {
                        if (!escapeToken)
                            throw new UISyntaxError($"Unexpected token separator, token is not escaped.");

                        _sb.Append((char)Stream.Read());
                    }
                    else if (c == ARRAY_START)
                    {
                        ReadingArray = true;
                        _sb.Append((char)Stream.Read());
                    }
                    else if (c == SCOPE_START || c == SCOPE_END)
                    {
                        Advance();
                        return c.ToString();
                    }
                }
            }

            return _sb.ToString();
        }

        public string GetNumberToken()
        {
            bool negated = false;
            bool comma = false;
            bool lastComma = false;
            _sb.Clear();

            while (!Stream.EndOfStream)
            {
                var nextChar = Stream.Peek();
                if (nextChar == -1)
                    return null;

                var c = (char)nextChar;
                if (_sb.Length != 0) // Check for potential termination
                {
                    if ((c != NUMBER_PERIOD && c != NUMBER_PERIOD) && (IsDiscardableChar(c) || !IsNumberChar(c)))
                    {
                        if (lastComma) // TODO: Check if the game accepts it anyway
                            throw new UISyntaxError($"Unexpected number token ended by number comma.");

                        return _sb.ToString();
                    }
                }
                else
                {
                    if (IsDiscardableChar(c))
                    {
                        Advance();
                        continue;
                    }
                }

                if (c == NUMBER_MINUS)
                {
                    // Commented due to '1.16115E-13'
                    // if (negated) 
                    //   throw new UISyntaxError($"Unexpected '-' for number token, was already present.");

                    negated = true;
                    _sb.Append((char)Stream.Read());
                }
                else if (c == NUMBER_PERIOD || c == NUMBER_PERIOD) // Ensure to parse commas as periods, to be nice
                {
                    if (_sb.Length == 0)
                        throw new UISyntaxError($"Unexpected '.' for number token, at the beginning of token");

                    if (comma == true)
                        throw new UISyntaxError($"Unexpected '.' for number token, number already has a comma");

                    Advance();
                    _sb.Append('.');

                    comma = true;
                    lastComma = true;
                    continue;
                }
                else if (char.IsDigit(c) || c == 'E') // 1.16115E-13
                {
                    _sb.Append((char)Stream.Read());
                }
                else
                {
                    throw new UISyntaxError($"Unexpected number token syntax error, got {c}.");
                }

                lastComma = false;
            }

            return _sb.ToString();
        }

        public string GetString()
        {
            _sb.Clear();

            bool started = false;
            while (true)
            {
                var nextChar = Stream.Peek();
                if (nextChar == -1)
                {
                    throw new UISyntaxError($"Unexpected EOF.");
                }

                var c = (char)nextChar;

                if (!started)
                {
                    if (IsDiscardableChar(c))
                    {
                        Advance();
                    }
                    else if (c == STRING_SCOPE)
                    {
                        Advance();
                        started = true;
                    }
                    else
                    {
                        throw new UISyntaxError($"Unexpected string token '{c}'.");
                    }
                }
                else
                {
                    if (c == STRING_SCOPE)
                    {
                        Advance();
                        return _sb.ToString();
                    }
                    else
                    {
                        _sb.Append((char)Stream.Read());
                    }
                }
            }
        }

        private bool IsDiscardableChar(char c)
            => c == ' ' || c == '\n' || c == '\t' || c == '\r';

        private bool IsNumberChar(char c)
            => char.IsDigit(c) || c == '-' || c == 'E';

        private void Advance()
            => Stream.Read();
    }
}
