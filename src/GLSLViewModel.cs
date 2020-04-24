using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Rhino.Geometry;
using Grasshopper.Kernel;

namespace ghgl
{
    enum SamplerTextureUnit : int
    {
        InitialColorBuffer = 1,
        InitialDepthBuffer = 2,
        BaseSampler = 3
    }

    class GLSLViewModel : System.ComponentModel.INotifyPropertyChanged
    {
        const double DefaultLineWidth = 3.0;
        const double DefaultPointSize = 8.0;

        readonly Shader[] _shaders = new Shader[(int)ShaderType.Fragment+1];
        bool _compileFailed;
        uint _programId;
        double _glLineWidth = DefaultLineWidth;
        double _glPointSize = DefaultPointSize;
        uint _drawMode;
        readonly DateTime _startTime = DateTime.Now;
        bool _depthTestingEnabled = true;
        bool _depthWritingEnabled = true;

        public GLSLViewModel()
        {
            for (int i = 0; i < (int)ShaderType.Fragment+1; i++)
            {
                _shaders[i] = new Shader((ShaderType)i, this);
                _shaders[i].PropertyChanged += OnShaderChanged;
            }
        }

        public bool Modified
        {
            get; set;
        }

        private void OnShaderChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Code")
            {
                Modified = true;
                ProgramId = 0;
            }
        }

        void SetCode(int which, string v, [System.Runtime.CompilerServices.CallerMemberName] string memberName = null)
        {
            if (!string.Equals(_shaders[which].Code, v, StringComparison.Ordinal))
            {
                _shaders[which].Code = v;
                OnPropertyChanged(memberName);
            }
        }

        public string TransformFeedbackShaderCode
        {
            get => _shaders[(int)ShaderType.TransformFeedbackVertex].Code;
            set => SetCode((int)ShaderType.TransformFeedbackVertex, value);
        }

        public string VertexShaderCode
        {
            get => _shaders[(int)ShaderType.Vertex].Code;
            set => SetCode((int)ShaderType.Vertex, value);
        }
        public string TessellationControlCode
        {
            get => _shaders[(int)ShaderType.TessellationControl].Code;
            set => SetCode((int)ShaderType.TessellationControl, value);
        }
        public string TessellationEvalualtionCode
        {
            get => _shaders[(int)ShaderType.TessellationEval].Code;
            set => SetCode((int)ShaderType.TessellationEval, value);
        }
        public string FragmentShaderCode
        {
            get => _shaders[(int)ShaderType.Fragment].Code;
            set => SetCode((int)ShaderType.Fragment, value);
        }
        public string GeometryShaderCode
        {
            get => _shaders[(int)ShaderType.Geometry].Code;
            set => SetCode((int)ShaderType.Geometry, value);
        }

        public Shader GetShader(ShaderType which)
        {
            return _shaders[(int)which];
        }

        public string GetCode(ShaderType type)
        {
            switch (type)
            {
                case ShaderType.TransformFeedbackVertex:
                    return TransformFeedbackShaderCode;
                case ShaderType.Vertex:
                    return VertexShaderCode;
                case ShaderType.Geometry:
                    return GeometryShaderCode;
                case ShaderType.TessellationControl:
                    return TessellationControlCode;
                case ShaderType.TessellationEval:
                    return TessellationEvalualtionCode;
                case ShaderType.Fragment:
                    return FragmentShaderCode;
            }
            return "";
        }

        public void SetCode(ShaderType type, string code)
        {
            switch (type)
            {
                case ShaderType.TransformFeedbackVertex:
                    TransformFeedbackShaderCode = code;
                    break;
                case ShaderType.Vertex:
                    VertexShaderCode = code;
                    break;
                case ShaderType.Geometry:
                    GeometryShaderCode = code;
                    break;
                case ShaderType.TessellationControl:
                    TessellationControlCode = code;
                    break;
                case ShaderType.TessellationEval:
                    TessellationEvalualtionCode = code;
                    break;
                case ShaderType.Fragment:
                    FragmentShaderCode = code;
                    break;
            }
        }

        public uint ProgramId
        {
            get { return _programId; }
            set
            {
                if (_programId != value)
                {
                    if(RecycleCurrentProgram)
                      GLRecycleBin.AddProgramToDeleteList(_programId);
                    _programId = value;
                    RecycleCurrentProgram = true;
                    OnPropertyChanged();
                }
            }
        }

        public bool RecycleCurrentProgram { get; set; } = true;

        public double glLineWidth
        {
            get { return _glLineWidth; }
            set
            {
                if (_glLineWidth != value && value > 0)
                {
                    _glLineWidth = value;
                    OnPropertyChanged();
                }
            }
        }

        public double glPointSize
        {
            get { return _glPointSize; }
            set
            {
                if (_glPointSize != value && value > 0)
                {
                    _glPointSize = value;
                    OnPropertyChanged();
                }
            }
        }

        public uint DrawMode
        {
            get { return _drawMode; }
            set
            {
                if (_drawMode != value && _drawMode <= OpenGL.GL_PATCHES)
                {
                    _drawMode = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool DepthTestingEnabled
        {
            get { return _depthTestingEnabled; }
            set
            {
                if (_depthTestingEnabled != value)
                {
                    _depthTestingEnabled = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool DepthWritingEnabled
        {
            get { return _depthWritingEnabled; }
            set
            {
                if (_depthWritingEnabled != value)
                {
                    _depthWritingEnabled = value;
                    OnPropertyChanged();
                }
            }
        }

        void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string memberName = null)
        {
            PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(memberName));

            if (memberName == "VertexShaderCode" || memberName == "TessellationControlCode" || memberName == "TessellationEvalualtionCode"
              || memberName == "FragmentShaderCode" || memberName == "GeometryShaderCode")
            {
                ProgramId = 0;
                _compileFailed = false;
            }
        }
        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;

        List<CompileError> _compileErrors = new List<CompileError>();
        public CompileError[] AllCompileErrors()
        {
            List<CompileError> errors = new List<CompileError>(_compileErrors);
            foreach (var shader in _shaders)
                errors.AddRange(shader.CompileErrors);
            return errors.ToArray();
        }

        public bool CompileProgram()
        {
            if (ProgramId != 0)
                return true;
            if (_compileFailed)
                return false;

            GLShaderComponentBase.ActivateGlContext();

            GLRecycleBin.Recycle();

            _compileErrors.Clear();
            bool compileSuccess = true;
            foreach (var shader in _shaders)
                compileSuccess = shader.Compile() && compileSuccess;

            // we want to make sure that at least a vertex and fragment shader
            // exist before making a program
            if (string.IsNullOrWhiteSpace(_shaders[(int)ShaderType.Vertex].Code))
            {
                _compileErrors.Add(new CompileError("A vertex shader is required to create a GL program"));
                compileSuccess = false;
            }
            if (string.IsNullOrWhiteSpace(_shaders[(int)ShaderType.Fragment].Code))
            {
                _compileErrors.Add(new CompileError("A fragment shader is required to create a GL program"));
                compileSuccess = false;
            }

            if (compileSuccess)
            {
                ProgramId = OpenGL.glCreateProgram();
                foreach (var shader in _shaders)
                {
                    if (shader.ShaderId != 0)
                        OpenGL.glAttachShader(ProgramId, shader.ShaderId);
                }

                OpenGL.glLinkProgram(ProgramId);

                string errorMsg;
                if (OpenGL.ErrorOccurred(out errorMsg))
                {
                    OpenGL.glDeleteProgram(_programId);
                    ProgramId = 0;
                    _compileErrors.Add(new CompileError(errorMsg));
                }
                // Reset the start time every time a shader is compiled to ensure that any time-based shader "starts over"
                // Time should only ellapse when the shader is actually running. Until we have a way to start and stop a
                // shader at a given point, allow for edits, and then continue, picking up where we left off...this is the
                // only "easy" way of doing this for now.
                BuiltIn._startTime = DateTime.Now;
            }
            _compileFailed = (ProgramId == 0);
            return ProgramId != 0;
        }

        public bool Write(GH_IO.Serialization.GH_IWriter writer)
        {
            writer.SetString("VertexShader", VertexShaderCode);
            writer.SetString("GeometryShader", GeometryShaderCode);
            writer.SetString("FragmentShader", FragmentShaderCode);
            writer.SetString("TessCtrlShader", TessellationControlCode);
            writer.SetString("TessEvalShader", TessellationEvalualtionCode);
            writer.SetString("TransformFeedbackVertexShader", TransformFeedbackShaderCode);
            writer.SetDouble("glLineWidth", glLineWidth);
            writer.SetDouble("glPointSize", glPointSize);
            writer.SetInt32("DrawMode", (int)DrawMode);

            writer.SetBoolean("DepthTestingEnabled", DepthTestingEnabled);
            writer.SetBoolean("DepthWritingEnabled", DepthWritingEnabled);
            return true;
        }

        public bool Read(GH_IO.Serialization.GH_IReader reader)
        {
            string s = "";
            VertexShaderCode = reader.TryGetString("VertexShader", ref s) ? s : "";
            GeometryShaderCode = reader.TryGetString("GeometryShader", ref s) ? s : "";
            FragmentShaderCode = reader.TryGetString("FragmentShader", ref s) ? s : "";
            TessellationControlCode = reader.TryGetString("TessCtrlShader", ref s) ? s : "";
            TessellationEvalualtionCode = reader.TryGetString("TessEvalShader", ref s) ? s : "";
            TransformFeedbackShaderCode = reader.TryGetString("TransformFeedbackVertexShader", ref s) ? s : "";
            double d = 0;
            if (reader.TryGetDouble("glLineWidth", ref d))
                glLineWidth = d;
            if (reader.TryGetDouble("glPointSize", ref d))
                glPointSize = d;
            int i = 0;
            if (reader.TryGetInt32("DrawMode", ref i))
                DrawMode = (uint)i;

            bool b = true;
            if (reader.TryGetBoolean("DepthTestingEnabled", ref b))
                DepthTestingEnabled = b;
            if (reader.TryGetBoolean("DepthWritingEnabled", ref b))
                DepthWritingEnabled = b;

            return true;
        }

        /// <summary>
        /// Get the data type for a uniform in this program (all shaders)
        /// </summary>
        /// <param name="name">name of uniform to try and get a type for</param>
        /// <param name="dataType"></param>
        /// <returns></returns>
        public bool TryGetUniformType(string name, out string dataType, out int arrayLength)
        {
            dataType = "";
            arrayLength = 0;
            foreach (var shader in _shaders)
            {
                var uniforms = shader.GetUniforms();
                foreach (UniformDescription uni in uniforms)
                {
                    if (uni.Name == name)
                    {
                        dataType = uni.DataType;
                        arrayLength = uni.ArrayLength;
                        return true;
                    }
                }
            }
            return false;
        }

        public bool TryGetAttributeType(string name, out string dataType, out int location)
        {
            dataType = "";
            location = -1;
            foreach (var shader in _shaders)
            {
                var attributes = shader.GetVertexAttributes();
                foreach (AttributeDescription attrib in attributes)
                {
                    if (attrib.Name == name)
                    {
                        dataType = attrib.DataType;
                        location = attrib.Location;
                        return true;
                    }
                }
            }
            return false;
        }


        public class UniformData<T>
        {
            public UniformData(string name, int arrayLength, T[] value)
            {
                Name = name;
                ArrayLength = arrayLength;
                Data = value;
            }

            public string Name { get; private set; }
            public int ArrayLength { get; private set; }
            public T[] Data { get; private set; }

            public string ToJsonString(int indent)
            {
                string threeTypeValue = "";
                if (typeof(T) == typeof(int) || typeof(T) == typeof(float))
                    threeTypeValue = $"type:\"float\", value: {Data[0]}";
                if (typeof(T) == typeof(Point3f))
                {
                    threeTypeValue = $"type:\"vec3\", value: new THREE.Vector3({Data[0]})";
                }
                if (typeof(T) == typeof(Vec4))
                    threeTypeValue = $"type:\"vec4\", value: new THREE.Vector4({Data[0]})";

                string s = "".PadLeft(indent) + $"{Name} : {{ {threeTypeValue} }}";
                return s;
            }
        }

        public class SamplerUniformData
        {
            uint _textureId;
            System.Drawing.Bitmap _bitmap;

            public SamplerUniformData(string name, string path)
            {
                Name = name;
                Path = path;
            }

            public SamplerUniformData(string name, System.Drawing.Bitmap bmp)
            {
                Name = name;
                _bitmap = bmp;
                Path = "<bitmap>";
            }
            public string Name { get; private set; }
            public string Path { get; private set; }
            public System.Drawing.Bitmap GetBitmap()
            {
                if (_bitmap == null)
                {
                    try
                    {
                        string localPath = Path;
                        if (Path.StartsWith("http://", StringComparison.InvariantCultureIgnoreCase) ||
                            Path.StartsWith("https://", StringComparison.InvariantCultureIgnoreCase))
                        {
                            using (var client = new System.Net.WebClient())
                            {
                                var stream = client.OpenRead(Path);
                                var bmp = new System.Drawing.Bitmap(stream);
                                _bitmap = bmp;
                            }
                        }
                        else
                        {
                            var bmp = new System.Drawing.Bitmap(localPath);
                            _bitmap = bmp;
                        }
                    }
                    catch(Exception)
                    {

                    }
                }
                return _bitmap;
            }

            public uint TextureId
            {
                get { return _textureId; }
                set
                {
                    if (_textureId != value)
                    {
                        GLRecycleBin.AddTextureToDeleteList(_textureId);
                        _textureId = value;
                    }
                }
            }
            public static uint CreateTexture(System.Drawing.Bitmap bmp)
            {
                uint textureId;
                try
                {
                    var rect = new System.Drawing.Rectangle(0, 0, bmp.Width, bmp.Height);
                    uint[] textures = { 0 };
                    OpenGL.glGenTextures(1, textures);
                    OpenGL.glBindTexture(OpenGL.GL_TEXTURE_2D, textures[0]);

                    if (bmp.PixelFormat == System.Drawing.Imaging.PixelFormat.Format24bppRgb)
                    {
                        var bmpData = bmp.LockBits(rect, System.Drawing.Imaging.ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
                        OpenGL.glTexImage2D(OpenGL.GL_TEXTURE_2D, 0, (int)OpenGL.GL_RGB, bmpData.Width, bmpData.Height, 0, OpenGL.GL_BGR, OpenGL.GL_UNSIGNED_BYTE, bmpData.Scan0);
                        bmp.UnlockBits(bmpData);
                    }
                    else
                    {
                        var bmpData = bmp.LockBits(rect, System.Drawing.Imaging.ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                        OpenGL.glTexImage2D(OpenGL.GL_TEXTURE_2D, 0, (int)OpenGL.GL_RGBA, bmpData.Width, bmpData.Height, 0, OpenGL.GL_BGRA, OpenGL.GL_UNSIGNED_BYTE, bmpData.Scan0);
                        bmp.UnlockBits(bmpData);
                    }
                    textureId = textures[0];
                    // See warning on
                    // https://www.khronos.org/opengl/wiki/Common_Mistakes#Automatic_mipmap_generation
                    OpenGL.glEnable(OpenGL.GL_TEXTURE_2D);
                    OpenGL.glGenerateMipmap(OpenGL.GL_TEXTURE_2D);
                    OpenGL.glTexParameteri(OpenGL.GL_TEXTURE_2D, OpenGL.GL_TEXTURE_WRAP_S, (int)OpenGL.GL_REPEAT);
                    OpenGL.glTexParameteri(OpenGL.GL_TEXTURE_2D, OpenGL.GL_TEXTURE_WRAP_T, (int)OpenGL.GL_REPEAT);
                    OpenGL.glTexParameteri(OpenGL.GL_TEXTURE_2D, OpenGL.GL_TEXTURE_MIN_FILTER, (int)OpenGL.GL_LINEAR_MIPMAP_LINEAR);
                    OpenGL.glTexParameteri(OpenGL.GL_TEXTURE_2D, OpenGL.GL_TEXTURE_MAG_FILTER, (int)OpenGL.GL_LINEAR);
                    OpenGL.glBindTexture(OpenGL.GL_TEXTURE_2D, 0);
                }
                catch (Exception)
                {
                    textureId = 0;
                }

                return textureId;
            }
        }

        public class MeshData
        {
            uint _triangleIndexBuffer;
            uint _linesIndexBuffer;
            uint _vertexVbo;
            uint _normalVbo;
            uint _textureCoordVbo;
            uint _colorVbo;
            public MeshData(Mesh mesh)
            {
                Mesh = mesh;
            }
            public Mesh Mesh { get; private set; }

            public uint TriangleIndexBuffer
            {
                get { return _triangleIndexBuffer; }
                set
                {
                    if(_triangleIndexBuffer!=value)
                    {
                        GLRecycleBin.AddVboToDeleteList(_triangleIndexBuffer);
                        _triangleIndexBuffer = value;
                    }
                }
            }

            public uint LinesIndexBuffer
            {
                get { return _linesIndexBuffer; }
                set
                {
                    if (_linesIndexBuffer != value)
                    {
                        GLRecycleBin.AddVboToDeleteList(_linesIndexBuffer);
                        _linesIndexBuffer = value;
                    }
                }
            }

            public uint VertexVbo
            {
                get { return _vertexVbo; }
                set
                {
                    if(_vertexVbo!=value)
                    {
                        GLRecycleBin.AddVboToDeleteList(_vertexVbo);
                        _vertexVbo = value;
                    }
                }
            }

            public uint NormalVbo
            {
                get { return _normalVbo; }
                set
                {
                    if(_normalVbo!=value)
                    {
                        GLRecycleBin.AddVboToDeleteList(_normalVbo);
                        _normalVbo = value;
                    }
                }
            }

            public uint TextureCoordVbo
            {
                get { return _textureCoordVbo; }
                set
                {
                    if(_textureCoordVbo!=value)
                    {
                        GLRecycleBin.AddVboToDeleteList(_textureCoordVbo);
                        _textureCoordVbo = value;
                    }
                }
            }

            public uint ColorVbo
            {
                get { return _colorVbo; }
                set
                {
                    if(_colorVbo!=value)
                    {
                        GLRecycleBin.AddVboToDeleteList(_colorVbo);
                        _colorVbo = value;
                    }
                }
            }
        }

        public class UniformsAndAttributes
        {
            public UniformsAndAttributes(List<SamplerUniformData> samplerCache)
            {
                _samplerCache = samplerCache;
            }

            public string[] GetComponentSamplers()
            {
                HashSet<string> items = new HashSet<string>();
                foreach(var item in _sampler2DUniforms)
                {
                    if (item.Path.EndsWith(":color") || item.Path.EndsWith(":depth"))
                        items.Add(item.Path);
                }
                string[] rc = new string[items.Count];
                items.CopyTo(rc);
                return rc;
            }

            public void AddMesh(Mesh mesh)
            {
                _meshes.Add(new MeshData(mesh));
            }

            public void AddUniform(string name, int[] values, int arrayLength)
            {
                _intUniforms.Add(new UniformData<int>(name, arrayLength, values));
            }
            public void AddUniform(string name, float[] values, int arrayLength)
            {
                _floatUniforms.Add(new UniformData<float>(name, arrayLength, values));
            }
            public void AddUniform(string name, Point3f[] values, int arrayLength)
            {
                _vec3Uniforms.Add(new UniformData<Point3f>(name, arrayLength, values));
            }
            public void AddUniform(string name, Vec4[] values, int arrayLength)
            {
                _vec4Uniforms.Add(new UniformData<Vec4>(name, arrayLength, values));
            }
            public void AddSampler2DUniform(string name, string path)
            {
                var data = new SamplerUniformData(name, path);

                bool isComponentOutput = path.EndsWith(":color") || path.EndsWith(":depth");

                if (!isComponentOutput)
                {
                    //try to find a cached item first
                    for (int i = 0; i < _samplerCache.Count; i++)
                    {
                        var sampler = _samplerCache[i];
                        if (string.Equals(sampler.Path, path, StringComparison.OrdinalIgnoreCase))
                        {
                            data.TextureId = sampler.TextureId;
                            _samplerCache.RemoveAt(i);
                            break;
                        }
                    }
                }
                _sampler2DUniforms.Add(data);
            }
            public void AddSampler2DUniform(string name, System.Drawing.Bitmap bmp)
            {
                var data = new SamplerUniformData(name, bmp);
                _sampler2DUniforms.Add(data);
            }

            public void AddAttribute(string name, int location, int[] value)
            {
                _intAttribs.Add(new GLAttribute<int>(name, location, value));
            }
            public void AddAttribute(string name, int location, float[] value)
            {
                _floatAttribs.Add(new GLAttribute<float>(name, location, value));
            }
            public void AddAttribute(string name, int location, Point2f[] value)
            {
                _vec2Attribs.Add(new GLAttribute<Point2f>(name, location, value));
            }
            public void AddAttribute(string name, int location, Point3f[] value)
            {
                _vec3Attribs.Add(new GLAttribute<Point3f>(name, location, value));
            }
            public void AddAttribute(string name, int location, Vec4[] value)
            {
                _vec4Attribs.Add(new GLAttribute<Vec4>(name, location, value));
            }

            public void Draw(Rhino.Display.DisplayPipeline display, uint programId, uint drawMode)
            {
                SetupGLUniforms(programId);

                int totalCount = 1;
                if (_meshes != null && _meshes.Count > 1)
                    totalCount = _meshes.Count;

                for (int i = 0; i < totalCount; i++)
                {
                    int element_count = SetupGLAttributes(i, programId);
                    if (element_count < 1)
                        continue;

                    if (_meshes.Count > i)
                    {
                        var data = _meshes[i];
                        if (drawMode == OpenGL.GL_LINES && data.LinesIndexBuffer == 0)
                        {
                            uint[] buffers;
                            OpenGL.glGenBuffers(1, out buffers);
                            data.LinesIndexBuffer = buffers[0];
                            OpenGL.glBindBuffer(OpenGL.GL_ELEMENT_ARRAY_BUFFER, data.LinesIndexBuffer);

                            int[] indices = new int[6 * data.Mesh.Faces.TriangleCount + 8 * data.Mesh.Faces.QuadCount];
                            int current = 0;
                            foreach (var face in data.Mesh.Faces)
                            {
                                indices[current++] = face.A;
                                indices[current++] = face.B;
                                indices[current++] = face.B;
                                indices[current++] = face.C;
                                if (face.IsTriangle)
                                {
                                    indices[current++] = face.C;
                                    indices[current++] = face.A;
                                }
                                if (face.IsQuad)
                                {
                                    indices[current++] = face.C;
                                    indices[current++] = face.D;
                                    indices[current++] = face.D;
                                    indices[current++] = face.A;
                                }
                            }

                            var handle = GCHandle.Alloc(indices, GCHandleType.Pinned);
                            IntPtr pointer = handle.AddrOfPinnedObject();
                            IntPtr size = new IntPtr(sizeof(int) * indices.Length);
                            OpenGL.glBufferData(OpenGL.GL_ELEMENT_ARRAY_BUFFER, size, pointer, OpenGL.GL_STATIC_DRAW);
                            handle.Free();
                        }

                        if (drawMode != OpenGL.GL_LINES && data.TriangleIndexBuffer == 0)
                        {
                            uint[] buffers;
                            OpenGL.glGenBuffers(1, out buffers);
                            data.TriangleIndexBuffer = buffers[0];
                            OpenGL.glBindBuffer(OpenGL.GL_ELEMENT_ARRAY_BUFFER, data.TriangleIndexBuffer);
                            int[] indices = new int[3 * (data.Mesh.Faces.TriangleCount + 2 * data.Mesh.Faces.QuadCount)];
                            int current = 0;
                            foreach (var face in data.Mesh.Faces)
                            {
                                indices[current++] = face.A;
                                indices[current++] = face.B;
                                indices[current++] = face.C;
                                if (face.IsQuad)
                                {
                                    indices[current++] = face.C;
                                    indices[current++] = face.D;
                                    indices[current++] = face.A;
                                }
                            }

                            var handle = GCHandle.Alloc(indices, GCHandleType.Pinned);
                            IntPtr pointer = handle.AddrOfPinnedObject();
                            IntPtr size = new IntPtr(sizeof(int) * indices.Length);
                            OpenGL.glBufferData(OpenGL.GL_ELEMENT_ARRAY_BUFFER, size, pointer, OpenGL.GL_STATIC_DRAW);
                            handle.Free();
                        }

                        if (drawMode == OpenGL.GL_LINES && data.LinesIndexBuffer != 0)
                        {
                            OpenGL.glBindBuffer(OpenGL.GL_ELEMENT_ARRAY_BUFFER, data.LinesIndexBuffer);
                            int indexCount = 6 * data.Mesh.Faces.TriangleCount + 8 * data.Mesh.Faces.QuadCount;
                            OpenGL.glDrawElements(drawMode, indexCount, OpenGL.GL_UNSIGNED_INT, IntPtr.Zero);
                            OpenGL.glBindBuffer(OpenGL.GL_ELEMENT_ARRAY_BUFFER, 0);
                        }
                        if (drawMode != OpenGL.GL_LINES && data.TriangleIndexBuffer != 0)
                        {
                            OpenGL.glBindBuffer(OpenGL.GL_ELEMENT_ARRAY_BUFFER, data.TriangleIndexBuffer);
                            int indexCount = 3 * (data.Mesh.Faces.TriangleCount + 2 * data.Mesh.Faces.QuadCount);
                            OpenGL.glDrawElements(drawMode, indexCount, OpenGL.GL_UNSIGNED_INT, IntPtr.Zero);
                            OpenGL.glBindBuffer(OpenGL.GL_ELEMENT_ARRAY_BUFFER, 0);
                        }
                    }
                    else
                    {
                        OpenGL.glDrawArrays(drawMode, 0, element_count);
                        OpenGL.glBindBuffer(OpenGL.GL_ARRAY_BUFFER, 0);
                    }
                }
                foreach (var item in _intAttribs)
                    DisableVertexAttribArray(item.Location);
                foreach (var item in _floatAttribs)
                    DisableVertexAttribArray(item.Location);
                foreach (var item in _vec2Attribs)
                    DisableVertexAttribArray(item.Location);
                foreach (var item in _vec3Attribs)
                    DisableVertexAttribArray(item.Location);
                foreach (var item in _vec4Attribs)
                    DisableVertexAttribArray(item.Location);
            }


            public void SetupGLUniforms(uint programId)
            {
                foreach (var uniform in _intUniforms)
                {
                    int arrayLength = uniform.ArrayLength;
                    int location = OpenGL.glGetUniformLocation(programId, uniform.Name);
                    if (-1 != location)
                    {
                        if (arrayLength < 1)
                            OpenGL.glUniform1i(location, uniform.Data[0]);
                        else if (uniform.Data.Length >= arrayLength)
                            OpenGL.glUniform1iv(location, arrayLength, uniform.Data);
                    }
                }
                foreach (var uniform in _floatUniforms)
                {
                    int arrayLength = uniform.ArrayLength;
                    int location = OpenGL.glGetUniformLocation(programId, uniform.Name);
                    if (-1 != location)
                    {
                        if (arrayLength < 1)
                            OpenGL.glUniform1f(location, uniform.Data[0]);
                        else if (uniform.Data.Length >= arrayLength)
                            OpenGL.glUniform1fv(location, arrayLength, uniform.Data);
                    }
                }
                foreach (var uniform in _vec3Uniforms)
                {
                    int arrayLength = uniform.ArrayLength;
                    int location = OpenGL.glGetUniformLocation(programId, uniform.Name);
                    if (-1 != location)
                    {
                        if (arrayLength < 1)
                            OpenGL.glUniform3f(location, uniform.Data[0].X, uniform.Data[0].Y, uniform.Data[0].Z);
                        else if (uniform.Data.Length >= arrayLength)
                        {
                            float[] data = new float[arrayLength * 3];
                            for (int i = 0; i < arrayLength; i++)
                            {
                                data[i * 3] = uniform.Data[i].X;
                                data[i * 3 + 1] = uniform.Data[i].Y;
                                data[i * 3 + 2] = uniform.Data[i].Z;
                            }
                            OpenGL.glUniform3fv(location, arrayLength, data);
                        }
                    }
                }
                foreach (var uniform in _vec4Uniforms)
                {
                    int arrayLength = uniform.ArrayLength;
                    int location = OpenGL.glGetUniformLocation(programId, uniform.Name);
                    if (-1 != location)
                    {
                        if (arrayLength < 1)
                            OpenGL.glUniform4f(location, uniform.Data[0]._x, uniform.Data[0]._y, uniform.Data[0]._z, uniform.Data[0]._w);
                        else if (uniform.Data.Length >= arrayLength)
                        {
                            float[] data = new float[arrayLength * 4];
                            for (int i = 0; i < arrayLength; i++)
                            {
                                data[i * 4] = uniform.Data[i]._x;
                                data[i * 4 + 1] = uniform.Data[i]._y;
                                data[i * 4 + 2] = uniform.Data[i]._z;
                                data[i * 4 + 3] = uniform.Data[i]._w;
                            }
                            OpenGL.glUniform4fv(location, arrayLength, data);
                        }
                    }
                }

                int currentTexture = (int)SamplerTextureUnit.BaseSampler;
                foreach (var uniform in _sampler2DUniforms)
                {
                    int location = OpenGL.glGetUniformLocation(programId, uniform.Name);
                    if (-1 != location)
                    {
                        if (uniform.Path.EndsWith(":color") || uniform.Path.EndsWith(":depth"))
                            uniform.TextureId = PerFrameCache.GetTextureId(uniform.Path);

                        if (0 == uniform.TextureId)
                        {
                            if (uniform.Path.EndsWith(":color") || uniform.Path.EndsWith(":depth"))
                                uniform.TextureId = PerFrameCache.GetTextureId(uniform.Path);
                            else
                                uniform.TextureId = SamplerUniformData.CreateTexture(uniform.GetBitmap());
                        }
                        if (uniform.TextureId != 0)
                        {
                            OpenGL.glActiveTexture(OpenGL.GL_TEXTURE0 + (uint)currentTexture);
                            OpenGL.glBindTexture(OpenGL.GL_TEXTURE_2D, uniform.TextureId);
                            OpenGL.glUniform1i(location, currentTexture);
                            OpenGL.glActiveTexture(OpenGL.GL_TEXTURE0);
                            currentTexture++;
                        }
                    }
                }
            }

            public int SetupGLAttributes(int index, uint programId)
            {
                int element_count = 0;
                if (_meshes.Count >= (index + 1) && _meshes[index]!=null)
                {
                    var data = _meshes[index];
                    var mesh = data.Mesh;
                    if (mesh == null)
                        return 0;
                    element_count = mesh.Vertices.Count;
                    int location = OpenGL.glGetAttribLocation(programId, "_meshVertex");
                    if (location >= 0)
                    {
                        if (data.VertexVbo == 0)
                        {
                            uint[] buffers;
                            OpenGL.glGenBuffers(1, out buffers);
                            data.VertexVbo = buffers[0];
                            OpenGL.glBindBuffer(OpenGL.GL_ARRAY_BUFFER, data.VertexVbo);
                            IntPtr size = new IntPtr(3 * sizeof(float) * mesh.Vertices.Count);
                            var points = mesh.Vertices.ToPoint3fArray();
                            var handle = GCHandle.Alloc(points, GCHandleType.Pinned);
                            IntPtr pointer = handle.AddrOfPinnedObject();
                            OpenGL.glBufferData(OpenGL.GL_ARRAY_BUFFER, size, pointer, OpenGL.GL_STREAM_DRAW);
                            handle.Free();
                        }
                        OpenGL.glBindBuffer(OpenGL.GL_ARRAY_BUFFER, data.VertexVbo);
                        OpenGL.glEnableVertexAttribArray((uint)location);
                        OpenGL.glVertexAttribPointer((uint)location, 3, OpenGL.GL_FLOAT, 0, 0, IntPtr.Zero);
                    }
                    location = OpenGL.glGetAttribLocation(programId, "_meshNormal");
                    if (location >= 0)
                    {
                        if (data.NormalVbo == 0 && mesh.Normals.Count == mesh.Vertices.Count)
                        {
                            uint[] buffers;
                            OpenGL.glGenBuffers(1, out buffers);
                            data.NormalVbo = buffers[0];
                            OpenGL.glBindBuffer(OpenGL.GL_ARRAY_BUFFER, data.NormalVbo);
                            IntPtr size = new IntPtr(3 * sizeof(float) * mesh.Normals.Count);
                            var normals = mesh.Normals.ToFloatArray();
                            var handle = GCHandle.Alloc(normals, GCHandleType.Pinned);
                            IntPtr pointer = handle.AddrOfPinnedObject();
                            OpenGL.glBufferData(OpenGL.GL_ARRAY_BUFFER, size, pointer, OpenGL.GL_STREAM_DRAW);
                            handle.Free();
                        }
                        if (data.NormalVbo != 0)
                        {
                            OpenGL.glBindBuffer(OpenGL.GL_ARRAY_BUFFER, data.NormalVbo);
                            OpenGL.glEnableVertexAttribArray((uint)location);
                            OpenGL.glVertexAttribPointer((uint)location, 3, OpenGL.GL_FLOAT, 0, 0, IntPtr.Zero);
                        }
                        else
                        {
                            OpenGL.glDisableVertexAttribArray((uint)location);
                            OpenGL.glVertexAttrib3f((uint)location, 0, 0, 0);
                        }
                    }

                    location = OpenGL.glGetAttribLocation(programId, "_meshTextureCoordinate");
                    if (location >= 0)
                    {
                        if (data.TextureCoordVbo == 0 && mesh.TextureCoordinates.Count == mesh.Vertices.Count)
                        {
                            uint[] buffers;
                            OpenGL.glGenBuffers(1, out buffers);
                            data.TextureCoordVbo = buffers[0];
                            OpenGL.glBindBuffer(OpenGL.GL_ARRAY_BUFFER, data.TextureCoordVbo);
                            IntPtr size = new IntPtr(2 * sizeof(float) * mesh.TextureCoordinates.Count);
                            var tcs = mesh.TextureCoordinates.ToFloatArray();
                            var handle = GCHandle.Alloc(tcs, GCHandleType.Pinned);
                            IntPtr pointer = handle.AddrOfPinnedObject();
                            OpenGL.glBufferData(OpenGL.GL_ARRAY_BUFFER, size, pointer, OpenGL.GL_STREAM_DRAW);
                            handle.Free();
                        }
                        if (data.TextureCoordVbo != 0)
                        {
                            OpenGL.glBindBuffer(OpenGL.GL_ARRAY_BUFFER, data.TextureCoordVbo);
                            OpenGL.glEnableVertexAttribArray((uint)location);
                            OpenGL.glVertexAttribPointer((uint)location, 2, OpenGL.GL_FLOAT, 0, 0, IntPtr.Zero);
                        }
                        else
                        {
                            OpenGL.glDisableVertexAttribArray((uint)location);
                            OpenGL.glVertexAttrib2f((uint)location, 0, 0);
                        }
                    }

                    location = OpenGL.glGetAttribLocation(programId, "_meshVertexColor");
                    if (location >= 0)
                    {
                        if (data.ColorVbo == 0 && mesh.VertexColors.Count == mesh.Vertices.Count)
                        {
                            uint[] buffers;
                            OpenGL.glGenBuffers(1, out buffers);
                            data.ColorVbo = buffers[0];
                            OpenGL.glBindBuffer(OpenGL.GL_ARRAY_BUFFER, data.ColorVbo);
                            IntPtr size = new IntPtr(4 * sizeof(float) * mesh.VertexColors.Count);
                            int colorCount = mesh.VertexColors.Count;

                            float[] colors = new float[colorCount * 4];
                            for (int i = 0; i < colorCount; i++)
                            {
                                var color = mesh.VertexColors[i];
                                colors[4 * i] = color.R / 255.0f;
                                colors[4 * i + 1] = color.G / 255.0f;
                                colors[4 * i + 2] = color.B / 255.0f;
                                colors[4 * i + 3] = color.A / 255.0f;
                            }
                            var handle = GCHandle.Alloc(colors, GCHandleType.Pinned);
                            IntPtr pointer = handle.AddrOfPinnedObject();
                            OpenGL.glBufferData(OpenGL.GL_ARRAY_BUFFER, size, pointer, OpenGL.GL_STREAM_DRAW);
                            handle.Free();
                        }
                        if (data.ColorVbo != 0)
                        {
                            OpenGL.glBindBuffer(OpenGL.GL_ARRAY_BUFFER, data.ColorVbo);
                            OpenGL.glEnableVertexAttribArray((uint)location);
                            OpenGL.glVertexAttribPointer((uint)location, 4, OpenGL.GL_FLOAT, 0, 0, IntPtr.Zero);
                        }
                        else
                        {
                            OpenGL.glDisableVertexAttribArray((uint)location);
                            OpenGL.glVertexAttrib2f((uint)location, 0, 0);
                        }
                    }
                }

                foreach (var item in _intAttribs)
                {
                    if (element_count == 0)
                        element_count = item.Items.Length;
                    if (element_count > item.Items.Length && item.Items.Length > 1)
                        element_count = item.Items.Length;

                    if (item.Location < 0)
                    {
                        item.Location = OpenGL.glGetAttribLocation(programId, item.Name);
                    }
                    if (item.Location >= 0)
                    {
                        uint location = (uint)item.Location;
                        if (1 == item.Items.Length)
                        {
                            OpenGL.glDisableVertexAttribArray(location);
                            OpenGL.glVertexAttribI1i(location, item.Items[0]);
                        }
                        else
                        {
                            if (item.VboHandle == 0)
                            {
                                uint[] buffers;
                                OpenGL.glGenBuffers(1, out buffers);
                                item.VboHandle = buffers[0];
                                OpenGL.glBindBuffer(OpenGL.GL_ARRAY_BUFFER, item.VboHandle);
                                IntPtr size = new IntPtr(sizeof(int) * item.Items.Length);
                                var handle = GCHandle.Alloc(item.Items, GCHandleType.Pinned);
                                IntPtr pointer = handle.AddrOfPinnedObject();
                                OpenGL.glBufferData(OpenGL.GL_ARRAY_BUFFER, size, pointer, OpenGL.GL_STREAM_DRAW);
                                handle.Free();
                            }
                            OpenGL.glBindBuffer(OpenGL.GL_ARRAY_BUFFER, item.VboHandle);
                            OpenGL.glEnableVertexAttribArray(location);
                            OpenGL.glVertexAttribPointer(location, 1, OpenGL.GL_INT, 0, sizeof(int), IntPtr.Zero);
                        }
                    }
                }
                foreach (var item in _floatAttribs)
                {
                    if (element_count == 0)
                        element_count = item.Items.Length;
                    if (element_count > item.Items.Length && item.Items.Length > 1)
                        element_count = item.Items.Length;

                    if (item.Location < 0)
                    {
                        item.Location = OpenGL.glGetAttribLocation(programId, item.Name);
                    }
                    if (item.Location >= 0)
                    {
                        uint location = (uint)item.Location;
                        if (1 == item.Items.Length)
                        {
                            OpenGL.glDisableVertexAttribArray(location);
                            OpenGL.glVertexAttrib1f(location, item.Items[0]);
                        }
                        else
                        {
                            if (item.VboHandle == 0)
                            {
                                uint[] buffers;
                                OpenGL.glGenBuffers(1, out buffers);
                                item.VboHandle = buffers[0];
                                OpenGL.glBindBuffer(OpenGL.GL_ARRAY_BUFFER, item.VboHandle);
                                IntPtr size = new IntPtr(sizeof(float) * item.Items.Length);
                                var handle = GCHandle.Alloc(item.Items, GCHandleType.Pinned);
                                IntPtr pointer = handle.AddrOfPinnedObject();
                                OpenGL.glBufferData(OpenGL.GL_ARRAY_BUFFER, size, pointer, OpenGL.GL_STREAM_DRAW);
                                handle.Free();
                            }
                            OpenGL.glBindBuffer(OpenGL.GL_ARRAY_BUFFER, item.VboHandle);
                            OpenGL.glEnableVertexAttribArray(location);
                            OpenGL.glVertexAttribPointer(location, 1, OpenGL.GL_FLOAT, 0, sizeof(float), IntPtr.Zero);
                        }
                    }
                }
                foreach (var item in _vec2Attribs)
                {
                    if (element_count == 0)
                        element_count = item.Items.Length;
                    if (element_count > item.Items.Length && item.Items.Length > 1)
                        element_count = item.Items.Length;

                    if (item.Location < 0)
                    {
                        item.Location = OpenGL.glGetAttribLocation(programId, item.Name);
                    }
                    if (item.Location >= 0)
                    {
                        uint location = (uint)item.Location;
                        if (1 == item.Items.Length)
                        {
                            OpenGL.glDisableVertexAttribArray(location);
                            Point2f v = item.Items[0];
                            OpenGL.glVertexAttrib2f(location, v.X, v.Y);
                        }
                        else
                        {
                            if (item.VboHandle == 0)
                            {
                                uint[] buffers;
                                OpenGL.glGenBuffers(1, out buffers);
                                item.VboHandle = buffers[0];
                                OpenGL.glBindBuffer(OpenGL.GL_ARRAY_BUFFER, item.VboHandle);
                                IntPtr size = new IntPtr(2 * sizeof(float) * item.Items.Length);
                                var handle = GCHandle.Alloc(item.Items, GCHandleType.Pinned);
                                IntPtr pointer = handle.AddrOfPinnedObject();
                                OpenGL.glBufferData(OpenGL.GL_ARRAY_BUFFER, size, pointer, OpenGL.GL_STREAM_DRAW);
                                handle.Free();
                            }
                            OpenGL.glBindBuffer(OpenGL.GL_ARRAY_BUFFER, item.VboHandle);
                            OpenGL.glEnableVertexAttribArray(location);
                            OpenGL.glVertexAttribPointer(location, 2, OpenGL.GL_FLOAT, 0, 2 * sizeof(float), IntPtr.Zero);
                        }
                    }
                }
                foreach (var item in _vec3Attribs)
                {
                    if (element_count == 0)
                        element_count = item.Items.Length;
                    if (element_count > item.Items.Length && item.Items.Length > 1)
                        element_count = item.Items.Length;

                    if (item.Location < 0)
                    {
                        item.Location = OpenGL.glGetAttribLocation(programId, item.Name);
                    }
                    if (item.Location >= 0)
                    {
                        uint location = (uint)item.Location;
                        if (1 == item.Items.Length)
                        {
                            OpenGL.glDisableVertexAttribArray(location);
                            Point3f v = item.Items[0];
                            OpenGL.glVertexAttrib3f(location, v.X, v.Y, v.Z);
                        }
                        else
                        {
                            if (item.VboHandle == 0)
                            {
                                uint[] buffers;
                                OpenGL.glGenBuffers(1, out buffers);
                                item.VboHandle = buffers[0];
                                OpenGL.glBindBuffer(OpenGL.GL_ARRAY_BUFFER, item.VboHandle);
                                IntPtr size = new IntPtr(3 * sizeof(float) * item.Items.Length);
                                var handle = GCHandle.Alloc(item.Items, GCHandleType.Pinned);
                                IntPtr pointer = handle.AddrOfPinnedObject();
                                OpenGL.glBufferData(OpenGL.GL_ARRAY_BUFFER, size, pointer, OpenGL.GL_STREAM_DRAW);
                                handle.Free();
                            }
                            OpenGL.glBindBuffer(OpenGL.GL_ARRAY_BUFFER, item.VboHandle);
                            OpenGL.glEnableVertexAttribArray(location);
                            OpenGL.glVertexAttribPointer(location, 3, OpenGL.GL_FLOAT, 0, 3 * sizeof(float), IntPtr.Zero);
                        }
                    }
                }
                foreach (var item in _vec4Attribs)
                {
                    if (element_count == 0)
                        element_count = item.Items.Length;
                    if (element_count > item.Items.Length && item.Items.Length > 1)
                        element_count = item.Items.Length;

                    if (item.Location < 0)
                    {
                        item.Location = OpenGL.glGetAttribLocation(programId, item.Name);
                    }
                    if (item.Location >= 0)
                    {
                        uint location = (uint)item.Location;
                        if (1 == item.Items.Length)
                        {
                            OpenGL.glDisableVertexAttribArray(location);
                            Vec4 v = item.Items[0];
                            OpenGL.glVertexAttrib4f(location, v._x, v._y, v._z, v._w);
                        }
                        else
                        {
                            if (item.VboHandle == 0)
                            {
                                uint[] buffers;
                                OpenGL.glGenBuffers(1, out buffers);
                                item.VboHandle = buffers[0];
                                OpenGL.glBindBuffer(OpenGL.GL_ARRAY_BUFFER, item.VboHandle);
                                IntPtr size = new IntPtr(4 * sizeof(float) * item.Items.Length);
                                var handle = GCHandle.Alloc(item.Items, GCHandleType.Pinned);
                                IntPtr pointer = handle.AddrOfPinnedObject();
                                OpenGL.glBufferData(OpenGL.GL_ARRAY_BUFFER, size, pointer, OpenGL.GL_STREAM_DRAW);
                                handle.Free();
                            }
                            OpenGL.glBindBuffer(OpenGL.GL_ARRAY_BUFFER, item.VboHandle);
                            OpenGL.glEnableVertexAttribArray(location);
                            OpenGL.glVertexAttribPointer(location, 4, OpenGL.GL_FLOAT, 0, 4 * sizeof(float), IntPtr.Zero);
                        }
                    }
                }
                return element_count;
            }


            public void ClearData()
            {
                foreach (var data in _meshes)
                {
                    data.TriangleIndexBuffer = 0;
                    data.LinesIndexBuffer = 0;
                    data.NormalVbo = 0;
                    data.TextureCoordVbo = 0;
                    data.VertexVbo = 0;
                }
                _meshes.Clear();
                _intUniforms.Clear();
                _floatUniforms.Clear();
                _vec3Uniforms.Clear();
                _vec4Uniforms.Clear();

                foreach (var attr in _intAttribs)
                    GLRecycleBin.AddVboToDeleteList(attr.VboHandle);
                _intAttribs.Clear();
                foreach (var attr in _floatAttribs)
                    GLRecycleBin.AddVboToDeleteList(attr.VboHandle);
                _floatAttribs.Clear();
                foreach (var attr in _vec2Attribs)
                    GLRecycleBin.AddVboToDeleteList(attr.VboHandle);
                _vec2Attribs.Clear();
                foreach (var attr in _vec3Attribs)
                    GLRecycleBin.AddVboToDeleteList(attr.VboHandle);
                _vec3Attribs.Clear();
                foreach (var attr in _vec4Attribs)
                    GLRecycleBin.AddVboToDeleteList(attr.VboHandle);
                _vec4Attribs.Clear();

                _samplerCache.AddRange(_sampler2DUniforms);
                while (_samplerCache.Count > 10)
                {
                    var sampler = _samplerCache[0];
                    GLRecycleBin.AddTextureToDeleteList(sampler.TextureId);
                    _samplerCache.RemoveAt(0);
                }
                _sampler2DUniforms.Clear();

            }

            public string UniformsToJsonString(int indent, List<UniformDescription> builtInsUsed)
            {
                var sb = new System.Text.StringBuilder();
                sb.AppendLine("".PadLeft(2) + "uniforms: {");
                var chunks = new List<string>();

                foreach(var builtin in builtInsUsed)
                {
                    string threeTypeValue = "";
                    if (builtin.DataType == "float")
                        threeTypeValue = "type:\"float\", value: 0.0";
                    if (builtin.DataType == "vec3")
                    {
                        threeTypeValue = "type:\"vec3\", value: new THREE.Vector3(0,0,0)";
                        if( builtin.ArrayLength>0)
                            threeTypeValue = "type:\"vec3v\", value: [new THREE.Vector3(1,-1,-3),new THREE.Vector3(0,0,0),new THREE.Vector3(0,0,0),new THREE.Vector3(0,0,0)]";
                    }
                    if (builtin.DataType == "vec4")
                        threeTypeValue = "type:\"vec4\", value: new THREE.Vector4(0,0,0,0)";
                    if (builtin.DataType == "mat3")
                        threeTypeValue = "type:\"mat3\", value: new Float32Array( [1,0,0,0,1,0,0,0,1] )";
                    string s = "".PadLeft(indent) + $"{builtin.Name} : {{ {threeTypeValue} }}";
                    chunks.Add(s);
                }

                _intUniforms.ForEach((u) => chunks.Add(u.ToJsonString(indent + 2)));
                _floatUniforms.ForEach((u) => chunks.Add(u.ToJsonString(indent + 2)));
                _vec3Uniforms.ForEach((u) => chunks.Add(u.ToJsonString(indent + 2)));
                _vec4Uniforms.ForEach((u) => chunks.Add(u.ToJsonString(indent + 2)));
                
                for(int i=0; i<chunks.Count; i++)
                {
                    sb.Append(chunks[i]);
                    if (i < (chunks.Count - 1))
                        sb.Append(",");
                    sb.AppendLine();
                }

                sb.Append("".PadLeft(2) + "}");
                return sb.ToString();
            }

            readonly List<SamplerUniformData> _samplerCache;
            readonly public List<MeshData> _meshes = new List<MeshData>();
            readonly List<UniformData<int>> _intUniforms = new List<UniformData<int>>();
            readonly List<UniformData<float>> _floatUniforms = new List<UniformData<float>>();
            readonly List<UniformData<Point3f>> _vec3Uniforms = new List<UniformData<Point3f>>();
            readonly public List<UniformData<Vec4>> _vec4Uniforms = new List<UniformData<Vec4>>();
            readonly List<SamplerUniformData> _sampler2DUniforms = new List<SamplerUniformData>();

            readonly public List<GLAttribute<int>> _intAttribs = new List<GLAttribute<int>>();
            readonly public List<GLAttribute<float>> _floatAttribs = new List<GLAttribute<float>>();
            readonly public List<GLAttribute<Point2f>> _vec2Attribs = new List<GLAttribute<Point2f>>();
            readonly public List<GLAttribute<Point3f>> _vec3Attribs = new List<GLAttribute<Point3f>>();
            readonly public List<GLAttribute<Vec4>> _vec4Attribs = new List<GLAttribute<Vec4>>();
        }
        readonly List<UniformsAndAttributes> _uniformAndAttributeIterations = new List<UniformsAndAttributes>();

        public UniformsAndAttributes GetUniformsAndAttributes(int iteration)
        {
            while (iteration >= _uniformAndAttributeIterations.Count)
                _uniformAndAttributeIterations.Add(new UniformsAndAttributes(_samplerCache));
            return _uniformAndAttributeIterations[iteration];
        }

        public bool TryGetVec4Uniform(int iteration, string name, out Vec4[] data)
        {
            data = new Vec4[0];
            foreach(var u in _uniformAndAttributeIterations[iteration]._vec4Uniforms)
            {
                if( u.Name == name)
                {
                    data = u.Data;
                    return true;
                }
            }
            return false;
        }

        readonly List<SamplerUniformData> _samplerCache = new List<SamplerUniformData>();

        public void ClearData()
        {
            foreach (var iteration in _uniformAndAttributeIterations)
                iteration.ClearData();
            _uniformAndAttributeIterations.Clear();
        }

        public void Draw(Rhino.Display.DisplayPipeline display, GLShaderComponentBase component)
        {
            uint programId = ProgramId;
            if (programId == 0)
                return;

            bool currentDepthTestingEnabled = OpenGL.IsEnabled(OpenGL.GL_DEPTH_TEST);
            if (currentDepthTestingEnabled != _depthTestingEnabled)
            {
                if (_depthTestingEnabled)
                    OpenGL.glEnable(OpenGL.GL_DEPTH_TEST);
                else
                    OpenGL.glDisable(OpenGL.GL_DEPTH_TEST);
            }
            if (!_depthWritingEnabled)
                OpenGL.glDepthMask((byte)OpenGL.GL_FALSE);

            uint[] vao;
            OpenGL.glGenVertexArrays(1, out vao);
            OpenGL.glBindVertexArray(vao[0]);
            OpenGL.glUseProgram(programId);

            // TODO: Parse shader and figure out the proper number to place here
            if (OpenGL.GL_PATCHES == DrawMode)
                OpenGL.glPatchParameteri(OpenGL.GL_PATCH_VERTICES, 1);

            float linewidth = (float)glLineWidth;
            OpenGL.glLineWidth(linewidth);
            float pointsize = (float)glPointSize;
            OpenGL.glPointSize(pointsize);

            // Define standard uniforms
            foreach (var builtin in BuiltIn.GetUniformBuiltIns())
                builtin.Setup(programId, display);

            if (OpenGL.GL_POINTS == DrawMode)
                OpenGL.glEnable(OpenGL.GL_VERTEX_PROGRAM_POINT_SIZE);
            OpenGL.glEnable(OpenGL.GL_BLEND);
            OpenGL.glBlendFunc(OpenGL.GL_SRC_ALPHA, OpenGL.GL_ONE_MINUS_SRC_ALPHA);

            foreach (var iteration in _uniformAndAttributeIterations)
                iteration.Draw(display, programId, DrawMode);

            OpenGL.glDisable(OpenGL.GL_BLEND);
            OpenGL.glBindVertexArray(0);
            OpenGL.glDeleteVertexArrays(1, vao);
            OpenGL.glUseProgram(0);

            if (currentDepthTestingEnabled != _depthTestingEnabled)
            {
                if (currentDepthTestingEnabled)
                    OpenGL.glEnable(OpenGL.GL_DEPTH_TEST);
                else
                    OpenGL.glDisable(OpenGL.GL_DEPTH_TEST);
            }
            if (!_depthWritingEnabled)
                OpenGL.glDepthMask((byte)OpenGL.GL_TRUE);

            // capture output color and depth buffer if they are needed downstream
            bool saveColor = true;//            PerFrameCache.IsColorTextureUsed(component);
            if( saveColor )
            {
                IntPtr texture2dPtr = Rhino7NativeMethods.RhTexture2dCreate();
                if (Rhino7NativeMethods.RhTexture2dCapture(display, texture2dPtr, Rhino7NativeMethods.CaptureFormat.kRGBA))
                    PerFrameCache.SaveColorTexture(component, texture2dPtr);
            }
            bool saveDepth = true;//            PerFrameCache.IsDepthTextureUsed(component);
            if(saveDepth)
            {
                IntPtr texture2dPtr = Rhino7NativeMethods.RhTexture2dCreate();
                if (Rhino7NativeMethods.RhTexture2dCapture(display, texture2dPtr, Rhino7NativeMethods.CaptureFormat.kDEPTH24))
                    PerFrameCache.SaveDepthTexture(component, texture2dPtr);
            }
        }

        static void DisableVertexAttribArray(int location)
        {
            if (location >= 0)
                OpenGL.glDisableVertexAttribArray((uint)location);
        }

        public void SaveAs(string filename)
        {
            var text = new System.Text.StringBuilder();
            if( !string.IsNullOrWhiteSpace(TransformFeedbackShaderCode))
            {
                text.AppendLine("[transformfeedback vertex shader]");
                text.AppendLine(TransformFeedbackShaderCode);
            }

            text.AppendLine("[vertex shader]");
            text.AppendLine(VertexShaderCode);
            if( !string.IsNullOrWhiteSpace(GeometryShaderCode) )
            {
                text.AppendLine("[geometry shader]");
                text.AppendLine(GeometryShaderCode);
            }
            if( !string.IsNullOrWhiteSpace(TessellationControlCode))
            {
                text.AppendLine("[tessctrl shader]");
                text.AppendLine(TessellationControlCode);
            }
            if( !string.IsNullOrWhiteSpace(TessellationEvalualtionCode))
            {
                text.AppendLine("[tesseval shader]");
                text.AppendLine(TessellationEvalualtionCode);
            }
            text.AppendLine("[fragment shader]");
            text.AppendLine(FragmentShaderCode);
            System.IO.File.WriteAllText(filename, text.ToString());
        }

        int MinAttributeLength(UniformsAndAttributes attrs)
        {
            int length = -1;
            foreach (var attr in attrs._intAttribs)
            {
                if (length < 0 || attr.Items.Length < length)
                    length = attr.Items.Length;
            }
            foreach (var attr in attrs._floatAttribs)
            {
                if (length < 0 || attr.Items.Length < length)
                    length = attr.Items.Length;
            }
            foreach (var attr in attrs._vec2Attribs)
            {
                if (length < 0 || attr.Items.Length < length)
                    length = attr.Items.Length;
            }
            foreach (var attr in attrs._vec3Attribs)
            {
                if (length < 0 || attr.Items.Length < length)
                    length = attr.Items.Length;
            }
            foreach (var attr in attrs._vec4Attribs)
            {
                if (length < 0 || attr.Items.Length < length)
                    length = attr.Items.Length;
            }
            return length;
        }

        static string ToJsonString<T>(string name, List<T> data, int indent, bool indices=false)
        {
            var sb = new System.Text.StringBuilder();
            string padding = "".PadLeft(indent);
            if( indices)
                sb.AppendLine(padding + $"{name} : [");
            else
                sb.AppendLine(padding + $"{name} : new Float32Array([");
            padding = "".PadLeft(indent + 2);
            int lineBreakOn = 6;
            bool startLine = true;
            for (int i = 0; i < data.Count; i++)
            {
                if (startLine)
                    sb.Append(padding);
                startLine = false;
                sb.Append(data[i].ToString());
                if (i < (data.Count - 1))
                    sb.Append(",");
                if (i % lineBreakOn == lineBreakOn)
                {
                    sb.AppendLine();
                    startLine = true;
                }
            }
            if (!startLine)
                sb.AppendLine();

            if(indices)
                sb.Append("".PadLeft(indent) + "]");
            else
                sb.Append("".PadLeft(indent) + "])");
            return sb.ToString();
        }

        public void ExportToHtml(string filename, GH_Component component)
        {
            string baseFilename = System.IO.Path.GetFileNameWithoutExtension(filename);
            string dirName = System.IO.Path.GetDirectoryName(filename);

            // create the javascript companion file that contains the attribute data
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("var ghglAttributes = [");

            int meshCount = 0;
            if (_uniformAndAttributeIterations.Count > 0)
                meshCount = _uniformAndAttributeIterations[0]._meshes.Count;

            int count = meshCount > 0 ? meshCount : _uniformAndAttributeIterations.Count;

            for( int i=0; i<count; i++ )
            {
                if (i > 0)
                    sb.AppendLine("  , {");
                else
                    sb.AppendLine("  {");

                UniformsAndAttributes iterationData = meshCount>0 ?
                    _uniformAndAttributeIterations[0] : 
                    _uniformAndAttributeIterations[i];

                List<string> chunks = new List<string>();

                var allUniforms = new List<UniformDescription>(_shaders[(int)ShaderType.Vertex].GetUniforms());
                allUniforms.AddRange(_shaders[(int)ShaderType.Fragment].GetUniforms());
                var builtInDictionary = new Dictionary<string, UniformDescription>();
                foreach (var uniform in allUniforms)
                {
                    builtInDictionary[uniform.Name] = uniform;
                }
                List<UniformDescription> builtInsUsed = new List<UniformDescription>(builtInDictionary.Values);
                chunks.Add(iterationData.UniformsToJsonString(2, builtInsUsed));

                if( meshCount>0 )
                {
                    var meshData = iterationData._meshes[i];

                    int[] indices = new int[3 * (meshData.Mesh.Faces.TriangleCount + 2 * meshData.Mesh.Faces.QuadCount)];
                    int current = 0;
                    foreach (var face in meshData.Mesh.Faces)
                    {
                        indices[current++] = face.A;
                        indices[current++] = face.B;
                        indices[current++] = face.C;
                        if (face.IsQuad)
                        {
                            indices[current++] = face.C;
                            indices[current++] = face.D;
                            indices[current++] = face.A;
                        }
                    }
                    chunks.Add(ToJsonString("meshIndices", new List<int>(indices), 2, true));

                    if( meshData.VertexVbo != 0)
                    {
                        List<Point3f> pts = new List<Point3f>(meshData.Mesh.Vertices.ToPoint3fArray());
                        chunks.Add(ToJsonString("_meshVertex", pts, 2));
                    }
                    if( meshData.NormalVbo != 0)
                    {
                        List<Vector3f> normals = new List<Vector3f>(meshData.Mesh.Normals.Count);
                        foreach (var normal in meshData.Mesh.Normals)
                            normals.Add(normal);
                        chunks.Add(ToJsonString("_meshNormal", normals, 2));
                    }
                    if( meshData.TextureCoordVbo != 0)
                    {
                        List<Point2f> tcs = new List<Point2f>(meshData.Mesh.TextureCoordinates.Count);
                        foreach (var tc in meshData.Mesh.TextureCoordinates)
                            tcs.Add(tc);
                        chunks.Add(ToJsonString("_meshTextureCoordinate", tcs, 2));
                    }
                    if(meshData.ColorVbo != 0)
                    {
                        List<Vec4> colors = new List<Vec4>(meshData.Mesh.VertexColors.Count);
                        foreach( var color in meshData.Mesh.VertexColors)
                        {
                            Vec4 v4 = new Vec4(color.R / 255.0f, color.G / 255.0f, color.B / 255.0f, color.A / 255.0f);
                            colors.Add(v4);
                        }
                        chunks.Add(ToJsonString("_meshVertexColor", colors, 2));
                    }
                }

                if (VertexShaderCode.Contains("gl_VertexID"))
                {
                    int length = MinAttributeLength(iterationData);
                    int[] items = new int[length];
                    for (int j = 0; j < items.Length; j++)
                        items[j] = j;
                    GLAttribute<int> vertexIds = new GLAttribute<int>("_vertex_id", -1, items);
                    chunks.Add(vertexIds.ToJsonString(2));
                }

                foreach (var attr in iterationData._intAttribs)
                {
                    string attrAsJson = attr.ToJsonString(2);
                    if (!string.IsNullOrWhiteSpace(attrAsJson))
                        chunks.Add(attrAsJson);
                }
                foreach (var attr in iterationData._floatAttribs)
                {
                    string attrAsJson = attr.ToJsonString(2);
                    if (!string.IsNullOrWhiteSpace(attrAsJson))
                        chunks.Add(attrAsJson);
                }
                foreach (var attr in iterationData._vec2Attribs)
                {
                    string attrAsJson = attr.ToJsonString(2);
                    if (!string.IsNullOrWhiteSpace(attrAsJson))
                        chunks.Add(attrAsJson);
                }
                foreach (var attr in iterationData._vec3Attribs)
                {
                    string attrAsJson = attr.ToJsonString(2);
                    if (!string.IsNullOrWhiteSpace(attrAsJson))
                        chunks.Add(attrAsJson);
                }
                foreach (var attr in iterationData._vec4Attribs)
                {
                    string attrAsJson = attr.ToJsonString(2);
                    if (!string.IsNullOrWhiteSpace(attrAsJson))
                        chunks.Add(attrAsJson);
                }
                for (int j = 0; j < chunks.Count; j++)
                {
                    sb.Append(chunks[j]);
                    if (j < (chunks.Count - 1))
                        sb.Append(",");
                    sb.AppendLine();
                }
                sb.AppendLine("  }");
            }

            sb.AppendLine("];");
            string jsfilepath = System.IO.Path.Combine(dirName, baseFilename + ".js");
            string javascriptData = sb.ToString();
            System.IO.File.WriteAllText(jsfilepath, javascriptData);
            
            System.Reflection.Assembly a = typeof(GLSLViewModel).Assembly;
            var stream = a.GetManifestResourceStream("ghgl.resources.threejs_template.html");
            var sr = new System.IO.StreamReader(stream);
            string contents = sr.ReadToEnd();
            stream.Close();

            contents = contents.Replace("_JAVASCRIPT_FILENAME_", baseFilename + ".js");

            sb = new System.Text.StringBuilder();
            bool positionHandled = false;
            string positionFiller = "";
            var attrs = _uniformAndAttributeIterations[0];
            if (VertexShaderCode.Contains("gl_VertexID"))
            {
                sb.AppendLine("      geometry.addAttribute('_vertex_id', new THREE.BufferAttribute( ghglAttributes[i]._vertex_id, 1 ));");
                positionFiller = "      geometry.addAttribute('position', new THREE.BufferAttribute( ghglAttributes[i]._vertex_id, 1 ));";
            }
            
            foreach (var attr in attrs._intAttribs)
            {
                if (attr.Name == "position")
                    positionHandled = true;
                sb.AppendLine($"          geometry.addAttribute('{attr.Name}', new THREE.BufferAttribute( ghglAttributes[i].{attr.Name}, 1 ));");
                positionFiller = $"          geometry.addAttribute('position', new THREE.BufferAttribute( ghglAttributes[i].{attr.Name}, 1 ));";
            }
            foreach( var attr in attrs._floatAttribs)
            {
                if (attr.Name == "position")
                    positionHandled = true;
                sb.AppendLine($"          geometry.addAttribute('{attr.Name}', new THREE.BufferAttribute( ghglAttributes[i].{attr.Name}, 1 ));");
                positionFiller = $"          geometry.addAttribute('position', new THREE.BufferAttribute( ghglAttributes[i].{attr.Name}, 1 ));";
            }
            foreach (var attr in attrs._vec4Attribs)
            {
                if (attr.Name == "position")
                    positionHandled = true;
                sb.AppendLine($"          geometry.addAttribute('{attr.Name}', new THREE.BufferAttribute( ghglAttributes[i].{attr.Name}, 4 ));");
                positionFiller = $"          geometry.addAttribute('position', new THREE.BufferAttribute( ghglAttributes[i].{attr.Name}, 4 ));";
            }
            foreach (var attr in attrs._vec2Attribs)
            {
                if (attr.Name == "position")
                    positionHandled = true;
                sb.AppendLine($"          geometry.addAttribute('{attr.Name}', new THREE.BufferAttribute( ghglAttributes[i].{attr.Name}, 2 ));");
                positionFiller = $"          geometry.addAttribute('position', new THREE.BufferAttribute( ghglAttributes[i].{attr.Name}, 2 ));";
            }
            foreach (var attr in attrs._vec3Attribs)
            {
                if (attr.Name == "position")
                    positionHandled = true;
                sb.AppendLine($"          geometry.addAttribute('{attr.Name}', new THREE.BufferAttribute( ghglAttributes[i].{attr.Name}, 3 ));");
                positionFiller = $"          geometry.addAttribute('position', new THREE.BufferAttribute( ghglAttributes[i].{attr.Name}, 3 ));";
            }

            if( attrs._meshes.Count>0)
            {
                sb.AppendLine($"          geometry.setIndex(ghglAttributes[i].meshIndices);");
                if (attrs._meshes[0].VertexVbo!=0)
                {
                    sb.AppendLine($"          geometry.addAttribute('_meshVertex', new THREE.BufferAttribute( ghglAttributes[i]._meshVertex, 3 ));");
                    positionFiller = $"          geometry.addAttribute('position', new THREE.BufferAttribute( ghglAttributes[i]._meshVertex, 3 ));";
                }
                if(attrs._meshes[0].NormalVbo!=0)
                    sb.AppendLine($"          geometry.addAttribute('_meshNormal', new THREE.BufferAttribute( ghglAttributes[i]._meshNormal, 3 ));");
                if(attrs._meshes[0].TextureCoordVbo!=0)
                    sb.AppendLine($"          geometry.addAttribute('_meshTextureCoordinate', new THREE.BufferAttribute( ghglAttributes[i]._meshTextureCoordinate, 2 ));");
                if(attrs._meshes[0].ColorVbo!=0)
                    sb.AppendLine($"          geometry.addAttribute('_meshVertexColor', new THREE.BufferAttribute( ghglAttributes[i]._meshVertexColor, 4 ));");
            }

            if ( !positionHandled )
            {
                sb.AppendLine(positionFiller);
            }

            string geometry_attributes = sb.ToString();
            contents = contents.Replace("_GEOMETRY_ATTRIBUTES_", geometry_attributes);
            contents = contents.Replace("_VERTEX_SHADER_", _shaders[(int)ShaderType.Vertex].ToWebGL1Code());
            contents = contents.Replace("_FRAGMENT_SHADER_", _shaders[(int)ShaderType.Fragment].ToWebGL1Code());

            if (DrawMode == OpenGL.GL_LINES)
                contents = contents.Replace("_THREE_OBJECT_TYPE_", "LineSegments");
            else if (DrawMode == OpenGL.GL_LINE_STRIP)
                contents = contents.Replace("_THREE_OBJECT_TYPE_", "Line");
            else if (DrawMode == OpenGL.GL_POINTS)
                contents = contents.Replace("_THREE_OBJECT_TYPE_", "Points");
            else
                contents = contents.Replace("_THREE_OBJECT_TYPE_", "Mesh");

            var bg = Rhino.ApplicationSettings.AppearanceSettings.ViewportBackgroundColor;
            string sBg = $"{bg.R / 255.0}, {bg.G / 255.0}, {bg.B / 255.0}";
            contents = contents.Replace("_BACKGROUND_COLOR_", sBg);

            var viewport = Rhino.RhinoDoc.ActiveDoc.Views.ActiveView.MainViewport;
            contents = contents.Replace("_CAMERA_LOCATION_", viewport.CameraLocation.ToString());
            double frLeft, frRight, frTop, frBottom, frNear, frFar;
            viewport.GetFrustum(out frLeft, out frRight, out frBottom, out frTop, out frNear, out frFar);
            contents = contents.Replace("_FRUSTUM_WIDTH_", (frRight - frLeft).ToString());
            contents = contents.Replace("_FRUSTUM_NEAR_", frNear.ToString());
            contents = contents.Replace("_FRUSTUM_FAR_", frFar.ToString());

            //Make some controls based on the component's input
            var guiControls = new List<GuiItem>();
            for( int i=0; i<component.Params.Input.Count; i++)
            {
                var slider = component.Params.Input[i].Sources[0] as Grasshopper.Kernel.Special.GH_NumberSlider;
                if( slider!=null)
                {
                    var gi = new GuiItem();
                    gi.Name = component.Params.Input[i].Name;
                    gi.Value = (float)slider.Slider.Value;
                    gi.Minimum = (float)slider.Slider.Minimum;
                    gi.Maximum = (float)slider.Slider.Maximum;
                    guiControls.Add(gi);
                    continue;
                }
                Vec4[] v4;
                if(TryGetVec4Uniform(0, component.Params.Input[i].Name, out v4))
                {
                    if( v4.Length==1)
                    {
                        var gi = new GuiItem();
                        gi.Name = component.Params.Input[i].Name;
                        gi.Color = v4[0];
                        gi.IsColor = true;
                        guiControls.Add(gi);
                    }
                }
            }

            sb = new System.Text.StringBuilder();
            if(guiControls.Count>0)
            {
                sb.AppendLine($"        var gui = new dat.GUI({{ height : {guiControls.Count} * 32 - 1 }});");
                sb.AppendLine("        var GuiControl = function() {");
                foreach(var ctrl in guiControls)
                {
                    if (ctrl.IsColor)
                        sb.AppendLine($"            this.{ctrl.Name} = [{(int)(ctrl.Color._x*255)}," +
                            $" {(int)(ctrl.Color._y*255)}, {(int)(ctrl.Color._z*255)}, {(int)(ctrl.Color._w*255)}];");
                    else
                        sb.AppendLine($"            this.{ctrl.Name} = {ctrl.Value};");
                }
                sb.AppendLine("        };");
                sb.AppendLine("        let text = new GuiControl();");

                foreach(var ctrl in guiControls)
                {
                    if (ctrl.IsColor)
                    {
                        sb.AppendLine($"        gui.addColor(text, '{ctrl.Name}').onChange(function(value) {{");
                        sb.AppendLine("             for (let i = 0; i < sceneObjects.length; i++) {");
                        sb.AppendLine($"                sceneObjects[i].material.uniforms.{ctrl.Name}.value = ");
                        sb.AppendLine($"                    new THREE.Vector4(value[0]/255.0,value[1]/255.0,value[2]/255.0,value[3]/255.0);");
                        sb.AppendLine("             }");
                        sb.AppendLine("        });");
                    }
                    else
                    {
                        sb.AppendLine($"        gui.add(text, '{ctrl.Name}', {ctrl.Minimum}, {ctrl.Maximum}).onChange(function(value) {{");
                        sb.AppendLine("             for (let i = 0; i < sceneObjects.length; i++) {");
                        sb.AppendLine($"                sceneObjects[i].material.uniforms.{ctrl.Name}.value = value;");
                        sb.AppendLine("             }");
                        sb.AppendLine("        });");
                    }
                }
            }
            contents = contents.Replace("_GUI_CONTROLS_", sb.ToString());

            string htmlfilepath = System.IO.Path.Combine(dirName, baseFilename + ".html");
            System.IO.File.WriteAllText(htmlfilepath, contents);
        }
    }

    class GuiItem
    {
        public string Name { get; set; }
        public float Value { get; set; }
        public float Minimum { get; set; }
        public float Maximum { get; set; }
        public Vec4 Color { get; set; }
        public bool IsColor { get; set; }
    }
}
