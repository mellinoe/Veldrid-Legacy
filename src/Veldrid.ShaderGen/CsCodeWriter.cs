using System;
using System.IO;
using System.Text;

namespace Veldrid.ShaderGen
{
    public class CsCodeWriter
    {
        private readonly StreamWriter _sw;
        private StringBuilder _pendingIndentation = new StringBuilder();

        private int _indentLevel = 0;

        public CsCodeWriter(StreamWriter sw)
        {
            _sw = sw;
        }

        public void Using(string ns)
        {
            WriteLine($"using {ns};");
        }

        public CodeBlock PushBlock() => PushBlock(null, null);
        public CodeBlock PushBlock(string header) => PushBlock(header, null);
        public CodeBlock PushBlock(string header, string trailing)
        {
            return new CodeBlock(this, header, trailing);
        }

        public IfDefSection PushIfDef(string condition)
        {
            return new IfDefSection(this, condition);
        }

        public void IncreaseIndentation()
        {
            _indentLevel += 4;
            _pendingIndentation.Append("    ");
        }

        public void DecreaseIndentation()
        {
            _indentLevel -= 4;
            _pendingIndentation.Remove(_pendingIndentation.Length - 4, 4);
        }

        public void WriteLine() => WriteLine(string.Empty);

        public void WriteLine(string text)
        {
            EmitPendingIndentation();
            _sw.Write(text);
            AdvanceLine();
        }

        public void WriteLine(char c)
        {
            EmitPendingIndentation();
            _sw.Write(c);
            AdvanceLine();
        }

        public void Write(string text)
        {
            EmitPendingIndentation();
            _sw.Write(text);
        }

        private void AdvanceLine()
        {
            _sw.WriteLine();
            for (int i = 0; i < _indentLevel; i++)
            {
                _pendingIndentation.Append(' ');
            }
        }

        private void EmitPendingIndentation()
        {
            _sw.Write(_pendingIndentation.ToString());
            _pendingIndentation.Clear();
        }

        public class CodeBlock : IDisposable
        {
            private readonly CsCodeWriter _cw;
            private readonly string _trailing;

            public CodeBlock(CsCodeWriter cw, string header, string trailing)
            {
                _cw = cw;
                _trailing = trailing;
                if (header != null)
                {
                    _cw.WriteLine(header);
                }

                _cw.WriteLine("{");
                _cw.IncreaseIndentation();
            }

            public void Dispose()
            {
                _cw.DecreaseIndentation();
                _cw.Write("}");
                if (_trailing != null)
                {
                    _cw.Write(_trailing);
                }
                _cw.WriteLine();
            }
        }

        public class IfDefSection : IDisposable
        {
            private readonly CsCodeWriter _cw;
            private readonly string _condition;

            public IfDefSection(CsCodeWriter cw, string condition)
            {
                _cw = cw;
                _condition = condition;
                _cw.WriteLine($"#if {condition}");
            }

            public void Dispose()
            {
                _cw.WriteLine($"#endif // {_condition}");
            }
        }
    }
}
