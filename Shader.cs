using System;
using System.Collections.Generic;
using System.Text;

namespace ghgl
{
    enum ShaderType
    {
        Vertex = 0,
        Geometry = 1,
        Fragment = 2,
        TessellationControl = 3,
        TessellationEval = 4
    }

    /// <summary>
    /// Represents a single GLSL shader that gets compiled and linked with other
    /// shaders to produce a program. 
    /// </summary>
    class Shader : System.ComponentModel.INotifyPropertyChanged
    {
        uint _shaderId; // OpenGL ID for this shader
        string _code = "";
        List<UniformDescription> _uniforms;
        List<AttributeDescription> _vertexAttributes;
        public Shader(ShaderType type, GLSLViewModel parent)
        {
            ShaderType = type;
        }

        public ShaderType ShaderType { get; }

        public uint ShaderId
        {
            get { return _shaderId; }
            set
            {
                if (_shaderId != value)
                {
                    GLRecycleBin.AddShaderToDeleteList(_shaderId);
                    _shaderId = value;
                    _uniforms = null;
                    _vertexAttributes = null;
                    OnPropertyChanged();
                }
            }
        }

        public string Code
        {
            get { return _code; }
            set
            {
                if (!string.Equals(_code, value, StringComparison.Ordinal))
                {
                    _code = value;
                    _uniforms = null;
                    _vertexAttributes = null;
                    ShaderId = 0;
                    OnPropertyChanged();
                }
            }
        }

        List<CompileError> _compileErrors = new List<CompileError>();
        public List<CompileError> CompileErrors {  get => _compileErrors; }

        public bool Compile()
        {
            if (ShaderId != 0)
                return _compileErrors.Count == 0;

            _compileErrors.Clear();

            // an empty string is considered fine
            if (string.IsNullOrWhiteSpace(Code))
                return true;


            uint rc = 0;
            if (!string.IsNullOrWhiteSpace(Code))
            {
                uint glShader = 0;
                switch (ShaderType)
                {
                    case ShaderType.Vertex:
                        glShader = OpenGL.GL_VERTEX_SHADER;
                        break;
                    case ShaderType.Fragment:
                        glShader = OpenGL.GL_FRAGMENT_SHADER;
                        break;
                    case ShaderType.Geometry:
                        glShader = OpenGL.GL_GEOMETRY_SHADER;
                        break;
                    case ShaderType.TessellationControl:
                        glShader = OpenGL.GL_TESS_CONTROL_SHADER;
                        break;
                    case ShaderType.TessellationEval:
                        glShader = OpenGL.GL_TESS_EVALUATION_SHADER;
                        break;
                }
                uint hShader = OpenGL.glCreateShader(glShader);
                OpenGL.glShaderSource(hShader, 1, new[] { Code }, null);
                OpenGL.glCompileShader(hShader);
                int success;
                OpenGL.glGetShaderiv(hShader, OpenGL.GL_COMPILE_STATUS, out success);
                if (1 != success)
                {
                    int maxLength;
                    OpenGL.glGetShaderiv(hShader, OpenGL.GL_INFO_LOG_LENGTH, out maxLength);
                    if (maxLength > 1)
                    {
                        int length;
                        var infolog = new StringBuilder(maxLength + 16);
                        OpenGL.glGetShaderInfoLog(hShader, maxLength, out length, infolog);

                        foreach (var line in infolog.ToString().Split('\n'))
                        {
                            if (string.IsNullOrWhiteSpace(line))
                                continue;
                            _compileErrors.Add(new CompileError(line, this));
                        }
                    }
                    OpenGL.glDeleteShader(hShader);
                }
                else
                {
                    rc = hShader;
                }
            }
            ShaderId = rc;
            return 0 != rc;
        }

        void ParseUniformsAndAttributes()
        {
            _uniforms = new List<UniformDescription>();
            _vertexAttributes = new List<AttributeDescription>();

            if (string.IsNullOrWhiteSpace(Code))
                return; //nothing to parse

            var lines = Code.Split('\n');

            foreach (var line in lines)
            {
                if (line.StartsWith("uniform"))
                {
                    var sub_lines = line.Split(' ', ';', '=');
                    string type = null;
                    string name = null;
                    for (int j = 1; j < sub_lines.Length; j++)
                    {
                        if (string.IsNullOrWhiteSpace(sub_lines[j]))
                            continue;
                        if (type == null)
                        {
                            type = sub_lines[j].Trim();
                            continue;
                        }
                        name = sub_lines[j].Trim();
                        break;
                    }
                    if (type != null && name != null)
                        _uniforms.Add(new UniformDescription { Name = name, DataType = type });
                }
                if (line.StartsWith("layout"))
                {
                    //layout(location = 0) in vec4 world_vertex;
                    int start = line.IndexOf('=');
                    int end = line.IndexOf(')');
                    if (start > "layout".Length && end > start)
                    {
                        var s = line.Substring(start + 1, end - (start + 1)).Trim();
                        int location;
                        if (int.TryParse(s, out location) && location >= 0)
                        {
                            start = line.IndexOf("in ", end, StringComparison.InvariantCulture);
                            if (start > end)
                            {
                                var items = line.Substring(start + "in ".Length).Trim().Split(' ', ';');
                                string datatype = items[0];
                                string name = items[1];
                                _vertexAttributes.Add(new AttributeDescription { Location = location, DataType = datatype, Name = name });
                            }
                        }
                    }
                }
                if (line.StartsWith("attribute"))
                {
                    var items = line.Substring("attribute ".Length).Split(new[] { ' ', ';' }, StringSplitOptions.RemoveEmptyEntries);
                    string datatype = items[0];
                    string name = items[1];
                    _vertexAttributes.Add(new AttributeDescription { DataType = datatype, Name = name });
                }
            }


        }
        public List<UniformDescription> GetUniforms()
        {
            if (_uniforms == null)
                ParseUniformsAndAttributes();
            return _uniforms;
        }

        public List<AttributeDescription> GetVertexAttributes()
        {
            if (_vertexAttributes == null)
                ParseUniformsAndAttributes();
            return _vertexAttributes;
        }

        void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string memberName = null)
        {
            PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(memberName));
        }
        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;

    }
}
