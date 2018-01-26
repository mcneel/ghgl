using System;
using System.Runtime.InteropServices;
using Grasshopper.Kernel;

namespace ghgl
{
    [Guid("FF4EB1F7-7AD6-47CD-BD2D-C50C87E13703")]
    public class GLMeshShaderComponent : GLShaderComponentBase
    {
        public GLMeshShaderComponent() : base("GL Mesh Shader", "GL Mesh", "OpenGL Drawing mesh with a shader")
        {
            _model.DrawMode = OpenGL.GL_TRIANGLES;
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddMeshParameter("Mesh", "M", "Input Mesh", GH_ParamAccess.item);
        }

        public override bool CanInsertParameter(GH_ParameterSide side, int index)
        {
            return base.CanInsertParameter(side, index) && index > 0;
        }
    }
}
