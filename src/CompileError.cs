using System;

namespace ghgl
{
    class CompileError
    {
        readonly string _message;

        public CompileError(string message) : this(message, null) { }

        public CompileError(string message, Shader parent)
        {
            LineNumber = -1;
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
                        LineNumber = LineNumber;
                        message = message.Substring(secondColon + 1).Trim();
                    }
                }
            }

            _message = message.Trim();
            Shader = parent;
        }

        public int LineNumber { get; }

        public Shader Shader { get; }

        public override string ToString()
        {
            string rc = Shader != null ? $"{Shader.ShaderType} Shader " : "";
            if (LineNumber >= 0)
                rc += $"(line {LineNumber}) ";
            return rc + _message;
        }
    }
}
