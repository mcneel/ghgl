using System;
using System.Runtime.InteropServices;
using GLuint = System.UInt32;
using GLenum = System.UInt32;
using GLboolean = System.Byte;
using GLbitfield = System.UInt32;
using GLbyte = System.SByte;
using GLshort = System.Int16;
using GLint = System.Int32;
using GLsizei = System.Int32;
using GLintptr = System.IntPtr;
using GLsizeiptr = System.IntPtr;
//typedef unsigned char GLubyte;
//typedef unsigned short GLushort;
//typedef unsigned int GLuint;
using GLfloat = System.Single;
//typedef float GLclampf;
//typedef double GLdouble;
//typedef double GLclampd;
//typedef void GLvoid;
using HDC = System.IntPtr;
using HGLRC = System.IntPtr;

namespace ghgl
{
    /// <summary>
    /// Simple wrapper around OpenGL SDK for the needs of this project
    /// </summary>
    class OpenGL
    {
        public static bool Initialized { get; set; }

        public static void Initialize()
        {
            Initialized = true;
            _glBindBuffer = (glBindBufferProc)GetProc<glBindBufferProc>();
            _glDeleteBuffers = (glDeleteBuffersProc)GetProc<glDeleteBuffersProc>();
            _glGenBuffers = (glGenBuffersProc)GetProc<glGenBuffersProc>();
            _glBufferData = (glBufferDataProc)GetProc<glBufferDataProc>();

            _glAttachShader = (glAttachShaderProc)GetProc<glAttachShaderProc>();
            _glCompileShader = (glCompileShaderProc)GetProc<glCompileShaderProc>();
            _glDeleteShader = (glDeleteShaderProc)GetProc<glDeleteShaderProc>();
            _glDeleteProgram = (glDeleteProgramProc)GetProc<glDeleteProgramProc>();
            _glCreateShader = (glCreateShaderProc)GetProc<glCreateShaderProc>();
            _glCreateProgram = (glCreateProgramProc)GetProc<glCreateProgramProc>();
            _glShaderSource = (glShaderSourceProc)GetProc<glShaderSourceProc>();
            _glUseProgram = (glUseProgramProc)GetProc<glUseProgramProc>();
            _glGetShaderiv = (glGetShaderivProc)GetProc<glGetShaderivProc>();
            _glGetShaderInfoLog = (glGetShaderInfoLogProc)GetProc<glGetShaderInfoLogProc>();
            _glLinkProgram = (glLinkProgramProc)GetProc<glLinkProgramProc>();
            _glGetAttribLocation = (glGetAttribLocationProc)GetProc<glGetAttribLocationProc>();
            _glGetUniformLocation = (glGetUniformLocationProc)GetProc<glGetUniformLocationProc>();
            _glDisableVertexAttribArray = (glDisableVertexAttribArrayProc)GetProc<glDisableVertexAttribArrayProc>();
            _glEnableVertexAttribArray = (glEnableVertexAttribArrayProc)GetProc<glEnableVertexAttribArrayProc>();
            _glUniform1f = (glUniform1fProc)GetProc<glUniform1fProc>();
            _glUniform2f = (glUniform2fProc)GetProc<glUniform2fProc>();
            _glUniform3f = (glUniform3fProc)GetProc<glUniform3fProc>();
            _glUniform4f = (glUniform4fProc)GetProc<glUniform4fProc>();
            _glUniform1i = (glUniform1iProc)GetProc<glUniform1iProc>();
            _glUniform2i = (glUniform2iProc)GetProc<glUniform2iProc>();
            _glUniform3i = (glUniform3iProc)GetProc<glUniform3iProc>();
            _glUniform4i = (glUniform4iProc)GetProc<glUniform4iProc>();
            _glVertexAttribI1i = (glVertexAttribI1iProc)GetProc<glVertexAttribI1iProc>();
            _glVertexAttribI2i = (glVertexAttribI2iProc)GetProc<glVertexAttribI2iProc>();
            _glVertexAttribI3i = (glVertexAttribI3iProc)GetProc<glVertexAttribI3iProc>();
            _glVertexAttribI4i = (glVertexAttribI4iProc)GetProc<glVertexAttribI4iProc>();
            _glVertexAttribPointer = (glVertexAttribPointerProc)GetProc<glVertexAttribPointerProc>();
            _glVertexAttrib1f = (glVertexAttrib1fProc)GetProc<glVertexAttrib1fProc>();
            _glVertexAttrib2f = (glVertexAttrib2fProc)GetProc<glVertexAttrib2fProc>();
            _glVertexAttrib3f = (glVertexAttrib3fProc)GetProc<glVertexAttrib3fProc>();
            _glVertexAttrib4f = (glVertexAttrib4fProc)GetProc<glVertexAttrib4fProc>();
            _glPatchParameteri = (glPatchParameteriProc)GetProc<glPatchParameteriProc>();
            _glUniformMatrix3fv = (glUniformMatrix3fvProc)GetProc<glUniformMatrix3fvProc>();
            _glUniformMatrix4fv = (glUniformMatrix4fvProc)GetProc<glUniformMatrix4fvProc>();
            _glBindVertexArray = (glBindVertexArrayProc)GetProc<glBindVertexArrayProc>();
            _glDeleteVertexArrays = (glDeleteVertexArraysProc)GetProc<glDeleteVertexArraysProc>();
            _glGenVertexArrays = (glGenVertexArraysProc)GetProc<glGenVertexArraysProc>();
            _glActiveTexture = (glActiveTextureProc)GetProc<glActiveTextureProc>();
        }

        static Delegate GetProc<T>()
        {
            string name = typeof(T).Name;
            name = name.Substring(0, name.Length - "Proc".Length);
            IntPtr ptr = wglGetProcAddress(name);
            return Marshal.GetDelegateForFunctionPointer(ptr, typeof(T));
        }

        public const uint GL_TRUE = 1;
        public const uint GL_FALSE = 0;

        /* BlendingFactorDest */
        public const uint GL_ZERO = 0;
        public const uint GL_ONE = 1;
        public const uint GL_SRC_COLOR = 0x0300;
        public const uint GL_ONE_MINUS_SRC_COLOR = 0x0301;
        public const uint GL_SRC_ALPHA = 0x0302;
        public const uint GL_ONE_MINUS_SRC_ALPHA = 0x0303;
        public const uint GL_DST_ALPHA = 0x0304;
        public const uint GL_ONE_MINUS_DST_ALPHA = 0x0305;


        public const uint GL_BYTE = 0x1400;
        public const uint GL_UNSIGNED_BYTE = 0x1401;
        public const uint GL_SHORT = 0x1402;
        public const uint GL_UNSIGNED_SHORT = 0x1403;
        public const uint GL_INT = 0x1404;
        public const uint GL_UNSIGNED_INT = 0x1405;
        public const uint GL_FLOAT = 0x1406;
        public const uint GL_2_BYTES = 0x1407;
        public const uint GL_3_BYTES = 0x1408;
        public const uint GL_4_BYTES = 0x1409;
        public const uint GL_DOUBLE = 0x140A;

        public const uint GL_VERTEX_PROGRAM_POINT_SIZE = 0x8642;
        public const uint GL_BLEND = 0x0BE2;

        public const uint GL_NO_ERROR = 0;

        public const uint GL_ARRAY_BUFFER = 0x8892;
        public const uint GL_ELEMENT_ARRAY_BUFFER = 0x8893;

        public const uint GL_STREAM_DRAW = 0x88E0;
        public const uint GL_STREAM_READ = 0x88E1;
        public const uint GL_STREAM_COPY = 0x88E2;
        public const uint GL_STATIC_DRAW = 0x88E4;
        public const uint GL_STATIC_READ = 0x88E5;
        public const uint GL_STATIC_COPY = 0x88E6;
        public const uint GL_DYNAMIC_DRAW = 0x88E8;
        public const uint GL_DYNAMIC_READ = 0x88E9;
        public const uint GL_DYNAMIC_COPY = 0x88EA;


        public const int GL_FRAGMENT_SHADER = 0x8B30;
        public const int GL_VERTEX_SHADER = 0x8B31;
        public const int GL_GEOMETRY_SHADER = 0x8DD9;
        public const int GL_TESS_EVALUATION_SHADER = 0x8E87;
        public const int GL_TESS_CONTROL_SHADER = 0x8E88;

        //DRAW MODE
        public const uint GL_POINTS = 0x0000;
        public const uint GL_LINES = 0x0001;
        public const uint GL_LINE_LOOP = 0x0002;
        public const uint GL_LINE_STRIP = 0x0003;
        public const uint GL_TRIANGLES = 0x0004;
        public const uint GL_TRIANGLE_STRIP = 0x0005;
        public const uint GL_TRIANGLE_FAN = 0x0006;
        public const uint GL_QUADS = 0x0007;
        public const uint GL_QUAD_STRIP = 0x0008;
        public const uint GL_POLYGON = 0x0009;
        public const uint GL_LINES_ADJACENCY = 0x000A;
        public const uint GL_LINE_STRIP_ADJACENCY = 0x000B;
        public const uint GL_TRIANGLES_ADJACENCY = 0x000C;
        public const uint GL_TRIANGLE_STRIP_ADJACENCY = 0x000D;
        public const uint GL_PATCHES = 0x000E;

        public const uint GL_PATCH_VERTICES = 0x8E72;


        public const GLuint GL_DELETE_STATUS = 0x8B80;
        public const GLuint GL_COMPILE_STATUS = 0x8B81;
        public const GLuint GL_LINK_STATUS = 0x8B82;
        public const GLuint GL_VALIDATE_STATUS = 0x8B83;
        public const GLuint GL_INFO_LOG_LENGTH = 0x8B84;


        public const uint GL_TEXTURE0 = 0x84C0;
        public const uint GL_TEXTURE1 = 0x84C1;
        public const uint GL_TEXTURE2 = 0x84C2;
        public const uint GL_TEXTURE3 = 0x84C3;
        public const uint GL_TEXTURE4 = 0x84C4;
        public const uint GL_TEXTURE5 = 0x84C5;
        public const uint GL_TEXTURE6 = 0x84C6;
        public const uint GL_TEXTURE7 = 0x84C7;
        public const uint GL_TEXTURE8 = 0x84C8;
        public const uint GL_TEXTURE9 = 0x84C9;
        public const uint GL_TEXTURE10 = 0x84CA;
        public const uint GL_TEXTURE11 = 0x84CB;
        public const uint GL_TEXTURE12 = 0x84CC;
        public const uint GL_TEXTURE13 = 0x84CD;
        public const uint GL_TEXTURE14 = 0x84CE;
        public const uint GL_TEXTURE15 = 0x84CF;
        public const uint GL_TEXTURE16 = 0x84D0;
        public const uint GL_TEXTURE17 = 0x84D1;
        public const uint GL_TEXTURE18 = 0x84D2;
        public const uint GL_TEXTURE19 = 0x84D3;
        public const uint GL_TEXTURE20 = 0x84D4;
        public const uint GL_TEXTURE21 = 0x84D5;
        public const uint GL_TEXTURE22 = 0x84D6;
        public const uint GL_TEXTURE23 = 0x84D7;
        public const uint GL_TEXTURE24 = 0x84D8;
        public const uint GL_TEXTURE25 = 0x84D9;
        public const uint GL_TEXTURE26 = 0x84DA;
        public const uint GL_TEXTURE27 = 0x84DB;
        public const uint GL_TEXTURE28 = 0x84DC;
        public const uint GL_TEXTURE29 = 0x84DD;
        public const uint GL_TEXTURE30 = 0x84DE;
        public const uint GL_TEXTURE31 = 0x84DF;
        public const uint GL_ACTIVE_TEXTURE = 0x84E0;

        public const uint GL_TEXTURE_1D = 0x0DE0;
        public const uint GL_TEXTURE_2D = 0x0DE1;
        public const uint GL_RGB = 0x1907;
        public const uint GL_RGBA = 0x1908;
        public const uint GL_BGR = 0x80E0;
        public const uint GL_BGRA = 0x80E1;

        public const uint GL_TEXTURE_MAG_FILTER = 0x2800;
        public const uint GL_TEXTURE_MIN_FILTER = 0x2801;
        public const uint GL_TEXTURE_WRAP_S = 0x2802;
        public const uint GL_TEXTURE_WRAP_T = 0x2803;
        public const uint GL_NEAREST = 0x2600;
        public const uint GL_LINEAR = 0x2601;
        public const uint GL_CLAMP = 0x2900;
        public const uint GL_REPEAT = 0x2901;


        const string OPENGL_LIB = "opengl32.dll";
        [DllImport(OPENGL_LIB)]
        public static extern void glBlendFunc(GLenum sfactor, GLenum dfactor);

        [DllImport(OPENGL_LIB)]
        public static extern void glLineWidth(float width);

        [DllImport(OPENGL_LIB)]
        public static extern void glPointSize(float size);

        [DllImport(OPENGL_LIB)]
        public static extern GLenum glGetError();

        [DllImport(OPENGL_LIB)]
        public static extern IntPtr wglGetProcAddress(string function);

        [DllImport(OPENGL_LIB)]
        public static extern void glEnable(GLenum cap);

        [DllImport(OPENGL_LIB)]
        public static extern void glDrawArrays(GLenum mode, GLint first, GLsizei count);

        [DllImport(OPENGL_LIB)]
        public static extern void glDrawElements(GLenum mode, GLsizei count, GLenum type, System.IntPtr indices);

        [DllImport(OPENGL_LIB)]
        public static extern void glBindTexture(GLenum target, GLuint texture);

        [DllImport(OPENGL_LIB)]
        public static extern void glGenTextures(GLsizei n, [MarshalAs(UnmanagedType.LPArray)]GLuint[] textures);

        [DllImport(OPENGL_LIB)]
        public static extern void glTexImage2D(GLenum target, GLint level, GLint internalformat, GLsizei width, GLsizei height, GLint border, GLenum format, GLenum type, IntPtr pixels);

        [DllImport(OPENGL_LIB)]
        public static extern void glDeleteTextures(GLsizei n, [MarshalAs(UnmanagedType.LPArray)]GLuint[] textures);

        [DllImport(OPENGL_LIB)]
        public static extern void glTexParameteri(GLenum target, GLenum pname, GLint param);


        [DllImport(OPENGL_LIB)]
        public static extern HGLRC wglGetCurrentContext();

        [DllImport(OPENGL_LIB)]
        public static extern bool wglMakeCurrent(HDC hdc, HGLRC hglrc);

        [DllImport("User32.dll")]
        public static extern HDC GetDC(IntPtr hWnd);

        [DllImport("gdi32.dll")]
        public static extern byte[] gluErrorString(uint errCode);

        delegate void glBindBufferProc(GLenum target, GLuint buffer);
        static glBindBufferProc _glBindBuffer;
        public static void glBindBuffer(GLenum target, GLuint buffer)
        {
            _glBindBuffer(target, buffer);
        }

        delegate void glDeleteBuffersProc(GLsizei n, GLuint[] buffers);
        static glDeleteBuffersProc _glDeleteBuffers;
        public static void glDeleteBuffers(GLsizei n, GLuint[] buffers)
        {
            _glDeleteBuffers(n, buffers);
        }

        delegate void glGenBuffersProc(GLsizei n, [MarshalAs(UnmanagedType.LPArray)]GLuint[] buffers);
        static glGenBuffersProc _glGenBuffers;
        public static void glGenBuffers(GLsizei n, out GLuint[] buffers)
        {
            buffers = new GLuint[n];
            _glGenBuffers(n, buffers);
        }

        delegate void glBufferDataProc(GLenum target, GLsizeiptr size, IntPtr data, GLenum usage);
        static glBufferDataProc _glBufferData;
        public static void glBufferData(GLenum target, GLsizeiptr size, IntPtr data, GLenum usage)
        {
            _glBufferData(target, size, data, usage);
        }

        //delegate void glBufferSubDataProc(GLenum target, GLintptr offset, GLsizeiptr size, IntPtr data);


        delegate void glAttachShaderProc(GLuint program, GLuint shader);
        static glAttachShaderProc _glAttachShader;
        public static void glAttachShader(GLuint program, GLuint shader)
        {
            _glAttachShader(program, shader);
        }

        delegate void glCompileShaderProc(GLuint shader);
        static glCompileShaderProc _glCompileShader;
        public static void glCompileShader(GLuint shader)
        {
            _glCompileShader(shader);
        }

        delegate void glDeleteShaderProc(GLuint shader);
        static glDeleteShaderProc _glDeleteShader;
        public static void glDeleteShader(GLuint shader)
        {
            _glDeleteShader(shader);
        }

        delegate void glDeleteProgramProc(GLuint shader);
        static glDeleteProgramProc _glDeleteProgram;
        public static void glDeleteProgram(GLuint program)
        {
            _glDeleteProgram(program);
        }

        delegate GLuint glCreateShaderProc(GLenum type);
        static glCreateShaderProc _glCreateShader;
        public static GLuint glCreateShader(GLenum type)
        {
            return _glCreateShader(type);
        }

        delegate GLuint glCreateProgramProc();
        static glCreateProgramProc _glCreateProgram;
        public static GLuint glCreateProgram()
        {
            return _glCreateProgram();
        }

        delegate void glShaderSourceProc(GLuint shader, GLsizei count, string[] source, GLint[] length);
        static glShaderSourceProc _glShaderSource;
        public static void glShaderSource(GLuint shader, GLsizei count, string[] source, GLint[] length)
        {
            _glShaderSource(shader, count, source, length);
        }

        delegate void glUseProgramProc(GLuint program);
        static glUseProgramProc _glUseProgram;
        public static void glUseProgram(GLuint program)
        {
            _glUseProgram(program);
        }

        delegate void glGetShaderivProc(GLuint shader, GLenum pname, out GLint parameter);
        static glGetShaderivProc _glGetShaderiv;
        public static void glGetShaderiv(GLuint shader, GLenum pname, out GLint parameter)
        {
            _glGetShaderiv(shader, pname, out parameter);
        }

        delegate void glGetShaderInfoLogProc(GLuint shader, GLsizei bufSize, out GLsizei length, System.Text.StringBuilder infoLog);
        static glGetShaderInfoLogProc _glGetShaderInfoLog;
        public static void glGetShaderInfoLog(GLuint shader, GLsizei bufSize, out GLsizei length, System.Text.StringBuilder infoLog)
        {
            _glGetShaderInfoLog(shader, bufSize, out length, infoLog);
        }

        delegate void glLinkProgramProc(GLuint program);
        static glLinkProgramProc _glLinkProgram;
        public static void glLinkProgram(GLuint program)
        {
            _glLinkProgram(program);
        }

        delegate GLint glGetAttribLocationProc(GLuint program, string name);
        static glGetAttribLocationProc _glGetAttribLocation;
        public static GLint glGetAttribLocation(GLuint program, string name)
        {
            return _glGetAttribLocation(program, name);
        }

        delegate GLint glGetUniformLocationProc(GLuint program, string name);
        static glGetUniformLocationProc _glGetUniformLocation;
        public static GLint glGetUniformLocation(GLuint program, string name)
        {
            return _glGetUniformLocation(program, name);
        }

        delegate void glDisableVertexAttribArrayProc(GLuint index);
        static glDisableVertexAttribArrayProc _glDisableVertexAttribArray;
        public static void glDisableVertexAttribArray(GLuint index)
        {
            _glDisableVertexAttribArray(index);
        }

        delegate void glEnableVertexAttribArrayProc(GLuint index);
        static glEnableVertexAttribArrayProc _glEnableVertexAttribArray;
        public static void glEnableVertexAttribArray(GLuint index)
        {
            _glEnableVertexAttribArray(index);
        }

        delegate void glUniform1fProc(GLint location, GLfloat v0);
        static glUniform1fProc _glUniform1f;
        public static void glUniform1f(GLint location, GLfloat v0)
        {
            _glUniform1f(location, v0);
        }

        delegate void glUniform2fProc(GLint location, GLfloat v0, GLfloat v1);
        static glUniform2fProc _glUniform2f;
        public static void glUniform2f(GLint location, GLfloat v0, GLfloat v1)
        {
            _glUniform2f(location, v0, v1);
        }

        delegate void glUniform3fProc(GLint location, GLfloat v0, GLfloat v1, GLfloat v2);
        static glUniform3fProc _glUniform3f;
        public static void glUniform3f(GLint location, GLfloat v0, GLfloat v1, GLfloat v2)
        {
            _glUniform3f(location, v0, v1, v2);
        }

        delegate void glUniform4fProc(GLint location, GLfloat v0, GLfloat v1, GLfloat v2, GLfloat v3);
        static glUniform4fProc _glUniform4f;
        public static void glUniform4f(GLint location, GLfloat v0, GLfloat v1, GLfloat v2, GLfloat v3)
        {
            _glUniform4f(location, v0, v1, v2, v3);
        }

        delegate void glUniform1iProc(GLint location, GLint v0);
        static glUniform1iProc _glUniform1i;
        public static void glUniform1i(GLint location, GLint v0)
        {
            _glUniform1i(location, v0);
        }

        delegate void glUniform2iProc(GLint location, GLint v0, GLint v1);
        static glUniform2iProc _glUniform2i;
        public static void glUniform2i(GLint location, GLint v0, GLint v1)
        {
            _glUniform2i(location, v0, v1);
        }

        delegate void glUniform3iProc(GLint location, GLint v0, GLint v1, GLint v2);
        static glUniform3iProc _glUniform3i;
        public static void glUniform3i(GLint location, GLint v0, GLint v1, GLint v2)
        {
            _glUniform3i(location, v0, v1, v2);
        }

        delegate void glUniform4iProc(GLint location, GLint v0, GLint v1, GLint v2, GLint v3);
        static glUniform4iProc _glUniform4i;
        public static void glUniform4i(GLint location, GLint v0, GLint v1, GLint v2, GLint v3)
        {
            _glUniform4i(location, v0, v1, v2, v3);
        }

        delegate void glVertexAttribI1iProc(GLuint index, GLint x);
        static glVertexAttribI1iProc _glVertexAttribI1i;
        public static void glVertexAttribI1i(GLuint index, GLint x)
        {
            _glVertexAttribI1i(index, x);
        }

        delegate void glVertexAttribI2iProc(GLuint index, GLint x, GLint y);
        static glVertexAttribI2iProc _glVertexAttribI2i;
        public static void glVertexAttribI2i(GLuint index, GLint x, GLint y)
        {
            _glVertexAttribI2i(index, x, y);
        }

        delegate void glVertexAttribI3iProc(GLuint index, GLint x, GLint y, GLint z);
        static glVertexAttribI3iProc _glVertexAttribI3i;
        public static void glVertexAttribI3i(GLuint index, GLint x, GLint y, GLint z)
        {
            _glVertexAttribI3i(index, x, y, z);
        }

        delegate void glVertexAttribI4iProc(GLuint index, GLint x, GLint y, GLint z, GLint w);
        static glVertexAttribI4iProc _glVertexAttribI4i;
        public static void glVertexAttribI4i(GLuint index, GLint x, GLint y, GLint z, GLint w)
        {
            _glVertexAttribI4i(index, x, y, z, w);
        }

        delegate void glVertexAttrib1fProc(GLuint index, GLfloat x);
        static glVertexAttrib1fProc _glVertexAttrib1f;
        public static void glVertexAttrib1f(GLuint index, GLfloat x)
        {
            _glVertexAttrib1f(index, x);
        }

        delegate void glVertexAttrib2fProc(GLuint index, GLfloat x, GLfloat y);
        static glVertexAttrib2fProc _glVertexAttrib2f;
        public static void glVertexAttrib2f(GLuint index, GLfloat x, GLfloat y)
        {
            _glVertexAttrib2f(index, x, y);
        }

        delegate void glVertexAttrib3fProc(GLuint index, GLfloat x, GLfloat y, GLfloat z);
        static glVertexAttrib3fProc _glVertexAttrib3f;
        public static void glVertexAttrib3f(GLuint index, GLfloat x, GLfloat y, GLfloat z)
        {
            _glVertexAttrib3f(index, x, y, z);
        }

        delegate void glVertexAttrib4fProc(GLuint index, GLfloat x, GLfloat y, GLfloat z, GLfloat w);
        static glVertexAttrib4fProc _glVertexAttrib4f;
        public static void glVertexAttrib4f(GLuint index, GLfloat x, GLfloat y, GLfloat z, GLfloat w)
        {
            _glVertexAttrib4f(index, x, y, z, w);
        }

        delegate void glVertexAttribPointerProc(GLuint index, GLint size, GLenum type, GLboolean normalized, GLsizei stride, IntPtr pointer);
        static glVertexAttribPointerProc _glVertexAttribPointer;
        public static void glVertexAttribPointer(GLuint index, GLint size, GLenum type, GLboolean normalized, GLsizei stride, IntPtr pointer)
        {
            _glVertexAttribPointer(index, size, type, normalized, stride, pointer);
        }

        delegate void glPatchParameteriProc(GLenum pname, GLint value);
        static glPatchParameteriProc _glPatchParameteri;
        public static void glPatchParameteri(GLenum pname, GLint value)
        {
            _glPatchParameteri(pname, value);
        }

        delegate void glUniformMatrix3fvProc(GLint location, GLsizei count, GLboolean transpose, [MarshalAs(UnmanagedType.LPArray)]GLfloat[] value);
        static glUniformMatrix3fvProc _glUniformMatrix3fv;
        public static void glUniformMatrix3fv(GLint location, GLsizei count, bool transpose, GLfloat[] value)
        {
            _glUniformMatrix3fv(location, count, transpose ? (byte)1 : (byte)0, value);
        }

        delegate void glUniformMatrix4fvProc(GLint location, GLsizei count, GLboolean transpose, [MarshalAs(UnmanagedType.LPArray)]GLfloat[] value);
        static glUniformMatrix4fvProc _glUniformMatrix4fv;
        public static void glUniformMatrix4fv(GLint location, GLsizei count, bool transpose, GLfloat[] value)
        {
            _glUniformMatrix4fv(location, count, transpose ? (byte)1 : (byte)0, value);
        }

        delegate void glBindVertexArrayProc(GLuint array);
        static glBindVertexArrayProc _glBindVertexArray;
        public static void glBindVertexArray(GLuint array)
        {
            _glBindVertexArray(array);
        }

        delegate void glDeleteVertexArraysProc(GLsizei n, GLuint[] arrays);
        static glDeleteVertexArraysProc _glDeleteVertexArrays;
        public static void glDeleteVertexArrays(GLsizei n, GLuint[] arrays)
        {
            _glDeleteVertexArrays(n, arrays);
        }

        delegate void glGenVertexArraysProc(GLsizei n, [MarshalAs(UnmanagedType.LPArray)]GLuint[] arrays);
        static glGenVertexArraysProc _glGenVertexArrays;
        public static void glGenVertexArrays(GLsizei n, out GLuint[] arrays)
        {
            arrays = new GLuint[n];
            _glGenVertexArrays(n, arrays);
        }

        delegate void glActiveTextureProc(GLenum id);
        static glActiveTextureProc _glActiveTexture;
        public static void glActiveTexture(GLenum id)
        {
            _glActiveTexture(id);
        }


        //    glActiveTexture(GL_TEXTURE0);
        //    glBindTexture(GL_TEXTURE_2D, Base);


        public static bool ErrorOccurred(out string errorMessage)
        {
            errorMessage = "";
            GLenum nGLError = glGetError();
            bool error_occurred = (nGLError != GL_NO_ERROR);
            if (error_occurred)
            {
                string glerrStr;

                if (nGLError == 0x0506)
                    glerrStr = "Invalid Framebuffer operation";
                else
                {
                    byte[] err = gluErrorString(nGLError);
                    glerrStr = System.Text.Encoding.ASCII.GetString(err);
                }
                errorMessage = $"OpenGL error {nGLError} : {glerrStr}";
            }
            return error_occurred;
        }

    }
}
