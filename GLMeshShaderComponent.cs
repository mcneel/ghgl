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
layout(location = 1) in vec3 _meshNormal;

uniform mat4 _worldToClip;
out vec4 color;

void main() 
{
  color = vec4(_meshNormal, 1);
  gl_Position = _worldToClip * vec4(_meshVertex,1);
}
";

                _model.FragmentShaderCode =
@"#version 330

in vec4 color;
void main()
{
  gl_FragColor = color;
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
