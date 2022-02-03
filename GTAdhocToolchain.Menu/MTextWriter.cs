using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;

using System.IO;

namespace GTAdhocToolchain.Menu
{
    public class MTextWriter : IDisposable
    {
        public string OutputFileName { get; set; }

        /// <summary>
        /// Current Depth of the writer.
        /// </summary>
        public int Depth { get; set; } = 0;

        /// <summary>
        /// Sets or gets the indentation character.
        /// </summary>
        public char IndentationChar { get; set; } = ' ';

        public bool NewLineOnNewScope = false;

        /// <summary>
        /// Sets or gets the indentation size.
        /// </summary>
        public int IndentationSize = 2;

        private StreamWriter _sw;

        private bool _needNewLine;

        public bool Debug { get; set; }

        public MTextWriter(string fileName)
        {
            OutputFileName = fileName;
        }

        public void WriteNode(mNode node)
        {
            _sw = new StreamWriter(OutputFileName);
            node.WriteText(this);
        }

        public void Write(float value)
        {
            if (_needNewLine)
                DoNeededNewLine();

            _sw.Write(value.ToString(CultureInfo.InvariantCulture));
        }

        public void Write(int value)
        {
            if (_needNewLine)
                DoNeededNewLine();

            _sw.Write(value);
        }

        public void Write(uint value)
        {
            if (_needNewLine)
                DoNeededNewLine();

            _sw.Write(value);
        }

        public void WriteString(string str)
        {
            if (_needNewLine)
                DoNeededNewLine();

            _sw.Write(str);
        }

        public void WriteSpace()
        {
            if (_needNewLine)
                DoNeededNewLine();

            _sw.Write(' ');
        }

        public void WriteOpenScope()
        {
            if (NewLineOnNewScope)
            {
                NewLine();
                _sw.Write('{');

                Depth++;
                SetNeedNewLine();
            }
            else
            {
                _sw.Write(" {");

                Depth++;
                SetNeedNewLine();
            }
        }

        public void WriteEndScope()
        {
            Depth--;
            _sw.WriteLine();

            SetCurrentIndentation();
            _sw.Write('}');

            SetNeedNewLine();
        }

        public void SetNeedNewLine()
            => _needNewLine = true;

        private void DoNeededNewLine()
        {
            _sw.WriteLine();
            SetCurrentIndentation();
            _needNewLine = false;
        }


        private void NewLine()
        {
            if (_needNewLine)
                DoNeededNewLine();

            _sw.WriteLine();
            SetCurrentIndentation();
        }

        private void SetCurrentIndentation()
        {
             _sw.Write(new string(IndentationChar, Depth * IndentationSize));
        }

        public void Dispose()
        {
            _sw.Dispose();
            GC.SuppressFinalize(this);
        }

        public enum IndentType
        {
            Tabs, // Superior by the way
            Spaces, 
        }
    }
}
