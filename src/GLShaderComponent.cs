using System.Runtime.InteropServices;

namespace ghgl
{
    [Guid("E61CC873-5643-4154-B97F-3A743BE90AE8")]
    public class GLShaderComponent : GLShaderComponentBase
    {
        public GLShaderComponent() : base("GL Shader", "GL Shader", "OpenGL Drawing with a shader")
        {
            _model.VertexShaderCode =
      @"#version 330

layout(location = 0) in vec3 vertex;
layout(location = 1) in vec4 vcolor;

uniform mat4 _worldToClip;
out vec4 vertex_color;

void main() {
  vertex_color = vcolor;
  gl_Position = _worldToClip * vec4(vertex, 1.0);
}
";
            _model.FragmentShaderCode =
      @"#version 330

in vec4 vertex_color;
out vec4 fragment_color;

void main() {
  fragment_color = vertex_color;
}
";
        }

        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                var assembly = System.Reflection.Assembly.GetExecutingAssembly();
                var stream = assembly.GetManifestResourceStream("ghgl.resources.GLSL_Component_24x24.png");
                return new System.Drawing.Bitmap(stream);
            }
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddScriptVariableParameter("vertex", "vertex", "", Grasshopper.Kernel.GH_ParamAccess.list);
            pManager.AddScriptVariableParameter("vcolor", "vcolor", "", Grasshopper.Kernel.GH_ParamAccess.list);
        }

    }
}
