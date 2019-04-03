using System.Collections.Generic;

namespace ghgl
{
    static class GLRecycleBin
    {
        static readonly HashSet<uint> _shadersToDelete = new HashSet<uint>();
        static readonly HashSet<uint> _programsToDelete = new HashSet<uint>();
        static readonly HashSet<uint> _vbosToDelete = new HashSet<uint>();
        static readonly HashSet<uint> _texturesToDelete = new HashSet<uint>();
        static readonly HashSet<System.IntPtr> _texturePtrsToDelete = new HashSet<System.IntPtr>();

        public static void AddShaderToDeleteList(uint shader)
        {
            if (0 != shader)
                _shadersToDelete.Add(shader);
        }
        public static void AddProgramToDeleteList(uint program)
        {
            if (0 != program)
                _programsToDelete.Add(program);
        }

        public static void AddVboToDeleteList(uint vbo)
        {
            if (0 != vbo)
                _vbosToDelete.Add(vbo);
        }

        public static void AddTextureToDeleteList(uint textureId)
        {
            if (0 != textureId)
                _texturesToDelete.Add(textureId);
        }

        public static void AddTextureToDeleteList(System.IntPtr texture2dPtr)
        {
            if (texture2dPtr != System.IntPtr.Zero)
                _texturePtrsToDelete.Add(texture2dPtr);

        }

        public static void Recycle()
        {
            foreach (var shader in _shadersToDelete)
                OpenGL.glDeleteShader(shader);
            foreach (var program in _programsToDelete)
                OpenGL.glDeleteProgram(program);
            foreach (var vbo in _vbosToDelete)
                OpenGL.glDeleteBuffers(1, new[] { vbo });
            foreach (var texture in _texturesToDelete)
                OpenGL.glDeleteTextures(1, new[] { texture });
            foreach (var texturePtr in _texturePtrsToDelete)
                Rhino7NativeMethods.RhTexture2dDelete(texturePtr);

            _shadersToDelete.Clear();
            _programsToDelete.Clear();
            _vbosToDelete.Clear();
            _texturesToDelete.Clear();
            _texturePtrsToDelete.Clear();
        }
    }
}
