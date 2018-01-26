using System.Collections.Generic;

namespace ghgl
{
    static class GLRecycleBin
    {
        static HashSet<uint> _shadersToDelete = new HashSet<uint>();
        static HashSet<uint> _programsToDelete = new HashSet<uint>();
        static HashSet<uint> _vbosToDelete = new HashSet<uint>();
        static HashSet<uint> _texturesToDelete = new HashSet<uint>();

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

        public static void Recycle()
        {
            foreach (var shader in _shadersToDelete)
                OpenGL.glDeleteShader(shader);
            foreach (var program in _programsToDelete)
                OpenGL.glDeleteProgram(program);
            foreach (var vbo in _vbosToDelete)
                OpenGL.glDeleteBuffers(1, new uint[] { vbo });
            foreach (var texture in _texturesToDelete)
                OpenGL.glDeleteTextures(1, new uint[] { texture });

            _shadersToDelete.Clear();
            _programsToDelete.Clear();
            _vbosToDelete.Clear();
            _texturesToDelete.Clear();
        }
    }
}
