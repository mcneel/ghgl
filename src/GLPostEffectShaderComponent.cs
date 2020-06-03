using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using GhGlPep;
using Grasshopper.Kernel;
using Rhino;
using Rhino.PlugIns;
using Rhino.Render;
using Rhino.Render.PostEffects;
using Rhino.Runtime.InteropWrappers;
using Rhino.UI.Controls;

namespace ghgl
{
    enum PepSamplerTextureUnit : int
    {
        ColorRgba = 1,
        Depth = 2,
        NormalXyz = 3,
        AlbedoRgb = 4,
    }

    [Guid("E667D747-2667-4FCB-B784-6BB1F51CA566")]
    public class GLPostEffectShaderComponent : GLShaderComponentBase
    {
        public GLPostEffectShaderComponent() : base("GL Post Effect Shader", "Post Effect", "OpenGL Post-process renderer output using a shader")
        {
            _model.DrawMode = OpenGL.GL_TRIANGLES;
            _model.VertexShaderCode =
@"#version 330

layout(location = 0) in vec3 _vertex;

void main() {
  gl_Position = vec4(_vertex , 1.0);
}
";

            _model.FragmentShaderCode =
@"#version 330

uniform sampler2D _color_rgba;
uniform vec2 _framebuffer_size;

out vec4 fragment_color;

void main() {
  vec2 tc = gl_FragCoord.xy/_framebuffer_size;
  fragment_color = texture(_color_rgba, tc);
}
";
            _is_pep_component = true;
        }

        public override void AddedToDocument(GH_Document document)
        {
            base.AddedToDocument(document);

            new MyFactory(InstanceGuid);

            BuiltIn.Register("_color_rgba", "sampler2D", "derp", (location, display) =>
            {
                TextureHandleColorRgba = FetchTextureHandleColorRgba();

                const int texture_unit = (int)PepSamplerTextureUnit.ColorRgba;
                OpenGL.glUniform1i(location, texture_unit);
                OpenGL.glActiveTexture(OpenGL.GL_TEXTURE0 + texture_unit);
                OpenGL.glBindTexture(OpenGL.GL_TEXTURE_2D, TextureHandleColorRgba);
                OpenGL.glActiveTexture(OpenGL.GL_TEXTURE0);
            });

            BuiltIn.Register("_albedo_rgb", "sampler2D", "derp", (location, display) =>
            {
                if (TextureHandleAlbedoRgb == uint.MaxValue)
                    return;

                const int texture_unit = (int)PepSamplerTextureUnit.AlbedoRgb;
                OpenGL.glUniform1i(location, texture_unit);
                OpenGL.glActiveTexture(OpenGL.GL_TEXTURE0 + texture_unit);
                OpenGL.glBindTexture(OpenGL.GL_TEXTURE_2D, TextureHandleAlbedoRgb);
                OpenGL.glActiveTexture(OpenGL.GL_TEXTURE0);
            });

            BuiltIn.Register("_normal_xyz", "sampler2D", "derp", (location, display) =>
            {
                TextureHandleNormalXyz = FetchTextureHandleNormalXyz();

                const int texture_unit = (int)PepSamplerTextureUnit.NormalXyz;
                OpenGL.glUniform1i(location, texture_unit);
                OpenGL.glActiveTexture(OpenGL.GL_TEXTURE0 + texture_unit);
                OpenGL.glBindTexture(OpenGL.GL_TEXTURE_2D, TextureHandleNormalXyz);
                OpenGL.glActiveTexture(OpenGL.GL_TEXTURE0);
            });

            BuiltIn.Register("_depth", "sampler2D", "derp", (location, display) =>
            {
                if (TextureHandleDepth == uint.MaxValue)
                    return;

                const int texture_unit = (int)PepSamplerTextureUnit.Depth;
                OpenGL.glUniform1i(location, texture_unit);
                OpenGL.glActiveTexture(OpenGL.GL_TEXTURE0 + texture_unit);
                OpenGL.glBindTexture(OpenGL.GL_TEXTURE_2D, TextureHandleDepth);
                OpenGL.glActiveTexture(OpenGL.GL_TEXTURE0);
            });

            BuiltIn.Register("_framebuffer_size", "vec2", "derp", (location, display) =>
            {
                OpenGL.glUniform2f(location, FramebufferSize.Width, FramebufferSize.Height);
            });
        }

        public delegate uint FetchTextureHandleDelegate();

        public FetchTextureHandleDelegate FetchTextureHandleColorRgba = null;
        public FetchTextureHandleDelegate FetchTextureHandleNormalXyz = null;
        public FetchTextureHandleDelegate FetchTextureHandleAlbedoRgb = null;
        public FetchTextureHandleDelegate FetchTextureHandleDepth = null;

        private SizeF FramebufferSize
        {
            get; set;
        }

        private uint TextureHandleColorRgba
        {
            get; set;
        } = uint.MaxValue;

        private uint TextureHandleAlbedoRgb
        {
            get; set;
        } = uint.MaxValue;

        private uint TextureHandleNormalXyz
        {
            get; set;
        } = uint.MaxValue;

        private uint TextureHandleDepth
        {
            get; set;
        } = uint.MaxValue;

        public delegate void TriggerUpdateDelegate();

        public static List<TriggerUpdateDelegate> TriggerUpdateDelegates { get; } = new List<TriggerUpdateDelegate>();

        public static void ExecuteTriggerUpdateDelegates(object sender, EventArgs e)
        {
            foreach(TriggerUpdateDelegate trigger_update_delegate in TriggerUpdateDelegates)
            {
                trigger_update_delegate();
            }
        }

        public static bool PepAnimationTimerEnabled
        {
            get
            {
                return _pep_animation_timer_enabled;
            }
            set
            {
                if (value)
                {
                    if (!_pep_animation_timer_enabled)
                    {
                        RhinoApp.Idle += ExecuteTriggerUpdateDelegates;
                    }
                }
                else
                {
                    if(_pep_animation_timer_enabled)
                    {
                        RhinoApp.Idle -= ExecuteTriggerUpdateDelegates;
                    }
                }

                _pep_animation_timer_enabled = value;
            }
        }

        private static bool _pep_animation_timer_enabled = false;

        public override void DrawViewportWires(IGH_PreviewArgs args)
        {
            PepAnimationTimerEnabled = false;
            AddToActiveComponentList(this);
        }

        public void ExecutePostEffect(int width, int height, Rectangle viewport)
        {
            TextureHandleColorRgba = uint.MaxValue;
            TextureHandleNormalXyz = uint.MaxValue;

            FramebufferSize = new SizeF(width, height);

            OpenGL.glGenFramebuffers(1, out uint[] fbos);
            OpenGL.glGenRenderbuffers(1, out uint[] render_buffers);

            uint fbo = fbos[0];
            uint render_buffer = render_buffers[0];

            OpenGL.glBindRenderbuffer(OpenGL.GL_RENDERBUFFER, render_buffer);
            OpenGL.glRenderbufferStorage(OpenGL.GL_RENDERBUFFER, OpenGL.GL_RGBA32F, width, height);
            OpenGL.glBindFramebuffer(OpenGL.GL_FRAMEBUFFER, fbo);
            OpenGL.glFramebufferRenderbuffer(OpenGL.GL_FRAMEBUFFER, OpenGL.GL_COLOR_ATTACHMENT0, OpenGL.GL_RENDERBUFFER, render_buffer);

            uint[] draw_buffers = new uint[] { OpenGL.GL_COLOR_ATTACHMENT0 };
            OpenGL.glDrawBuffers(1, draw_buffers);

            OpenGL.glViewport(viewport.Left, viewport.Top, viewport.Width, viewport.Height);
            OpenGL.glScissor(viewport.Left, viewport.Top, viewport.Width, viewport.Height);

            //ON_4iRect viewport = ConvertRectOriginFromTopToBottom(dib_subsize, height);
            //glViewport(viewport.left, viewport.bottom, viewport.Width(), viewport.Height());
            //glScissor(viewport.left, viewport.bottom, viewport.Width(), viewport.Height());
            //glDisable(GL_CULL_FACE);
            //glDisable(GL_DEPTH_TEST);

            //if (state.m_current_program > 0)
            //{
            //    gl->glUseProgram(0);
            //}

            //gl->glUseProgram(program);

            //gl->glActiveTexture(GL_TEXTURE0);
            //glBindTexture(GL_TEXTURE_2D, image_tex);
            //glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MIN_FILTER, GL_NEAREST);
            //glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MAG_FILTER, GL_NEAREST);

            _model.DepthTestingEnabled = false;
            _model.DrawMode = OpenGL.GL_TRIANGLE_FAN;
            _model.Draw(null, this);

            if(TextureHandleColorRgba == uint.MaxValue)
                TextureHandleColorRgba = FetchTextureHandleColorRgba();

            if (TextureHandleColorRgba != uint.MaxValue)
            {
                OpenGL.glBindTexture(OpenGL.GL_TEXTURE_2D, TextureHandleColorRgba);
                OpenGL.glCopyTexSubImage2D(OpenGL.GL_TEXTURE_2D, 0, viewport.Left, viewport.Top, viewport.Left, viewport.Top, viewport.Width, viewport.Height);
            }

            OpenGL.glBindFramebuffer(OpenGL.GL_FRAMEBUFFER, 0);
            OpenGL.glDeleteRenderbuffers(1, render_buffers);
            OpenGL.glDeleteFramebuffers(1, fbos);
        }
    }
}
