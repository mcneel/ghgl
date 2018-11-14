using System;

namespace ghgl
{
    class CompileError
    {
        readonly string _message;
        readonly Shader _parent;
        readonly int _lineNumber = -1;
        public CompileError(string message) : this(message, null)
        {
        }

        public CompileError(string message, Shader parent)
        {
            //ERROR: 0:9: error message
            if (message.StartsWith("ERROR:", StringComparison.OrdinalIgnoreCase))
                message = message.Substring("ERROR:".Length).Trim();

            int firstColon = message.IndexOf(':');
            if(firstColon>0)
            {
                int secondColon = message.IndexOf(':', firstColon + 1);
                if (secondColon > firstColon)
                {
                    int lineNumber;
                    string line = message.Substring(firstColon + 1, secondColon - firstColon - 1);
                    if(int.TryParse(line, out lineNumber))
                    {
                        _lineNumber = lineNumber;
                        message = message.Substring(secondColon + 1).Trim();
                    }
                }
            }

            _message = message.Trim();
            _parent = parent;
        }

        public int LineNumber { get => _lineNumber; }

        public Shader Shader { get => _parent; }

        public override string ToString()
        {
            string rc = _parent != null ? $"{_parent.ShaderType} Shader " : "";
            if (_lineNumber >= 0)
                rc += $"(line {_lineNumber}) ";
            return rc + _message;
        }
    }
}
