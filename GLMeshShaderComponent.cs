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
            _model.VertexShaderCode =
      @"#version 330

layout(location = 0) in vec3 _meshVertex;
uniform mat4 _worldToClip;

void main() 
{
  gl_Position = _worldToClip * vec4(_meshVertex,1);
}
";
            _model.FragmentShaderCode =
      @"#version 330

void main()
{
  gl_FragColor = vec4(0,1,0,1);
}
";
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddMeshParameter("Mesh", "M", "Input Mesh", GH_ParamAccess.list);
        }

        public override bool CanInsertParameter(GH_ParameterSide side, int index)
        {
            return base.CanInsertParameter(side, index) && index > 0;
        }

        protected override void SolveInstance(IGH_DataAccess data)
        {
            SolveInstanceHelper(data, 1);
            var list = new System.Collections.Generic.List<Rhino.Geometry.Mesh>();
            if (data.GetDataList(0, list))
            {
                foreach(var mesh in list)
                    _model.AddMesh(mesh);
            }
        }
    }
}
