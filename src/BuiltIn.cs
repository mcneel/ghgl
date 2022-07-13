using System;
using System.Collections.Generic;

namespace ghgl
{
    /// <summary>
    /// Built In Uniforms and Attributes that people typically need (matrices, lights, viewport info, time)
    /// Always use an underscore as the first character to help differentiate that the uniform really is a
    /// built in one and not a custom uniform that the user has specified
    /// </summary>
    class BuiltIn
    {
        static List<BuiltIn> _uniformBuiltins;
        public static DateTime _startTime;
        Action<int, Rhino.Display.DisplayPipeline> _setup;
        static Dictionary<uint, DateTime> _drawTimes;

        private BuiltIn(string name, string datatype, string description, Action<int, Rhino.Display.DisplayPipeline> setup)
        {
            Name = name;
            Description = description;
            DataType = datatype;
            _setup = setup;
        }

        public string Name { get; }
        public string Description { get; }
        public string DataType { get; }
        public static System.Drawing.Point MouseDownPosition { get; set; }

        public void Setup(uint program, Rhino.Display.DisplayPipeline dp)
        {
            if (_setup == null)
                return;
            string glname = Name;
            int arrayIndex = glname.IndexOf('[');
            if (arrayIndex > 0)
                glname = glname.Substring(0, arrayIndex);
            int uniformLocation = OpenGL.glGetUniformLocation(program, glname);
            if (uniformLocation >= 0)
                _setup(uniformLocation, dp);
        }

        static Rhino.Geometry.Light[] _preSR3Lights;
        static Rhino.Geometry.Light[] GetLightsHelper(Rhino.Display.DisplayPipeline pipeline)
        {
            var method = pipeline.GetType().GetMethod("GetLights");
            if (method == null)
            {
                if( _preSR3Lights==null )
                {
                    // just mimic the default light
                    var light = new Rhino.Geometry.Light();
                    light.LightStyle = Rhino.Geometry.LightStyle.CameraDirectional;
                    light.Direction = new Rhino.Geometry.Vector3d(1, -1, -3);
                    light.ShadowIntensity = 0.7;
                    _preSR3Lights = new Rhino.Geometry.Light[] { light };
                }
                return _preSR3Lights;
            }
            var lights = method.Invoke(pipeline, null) as Rhino.Geometry.Light[];
            return lights;
        }
        
        public static List<BuiltIn> GetUniformBuiltIns()
        {
            if( _uniformBuiltins==null)
            {
                // "Start stime" is now based on when the shader was last compiled. Setting it here does not allow 
                // for time based shaders to "start over" if/when they're based on overall frame time/counts.
                // Resetting the start time here, means that time continues to ellapse even when the shader isn't
                // running. Until we have a way to start and stop a shader at a given point, allow for edits, and 
                // then continue, picking up where we left off...this is the only "easy" way of doing this for now.
                //_startTime = DateTime.Now;
                Register("_colorBuffer", "sampler2D", "texture representing the color information in the viewport before any shader components have executed", (location, display) =>
                {
                    IntPtr texture2dPtr = PerFrameCache.InitialColorBuffer;
                    uint textureId = 0;
                    textureId = Rhino7NativeMethods.RhTexture2dHandle(texture2dPtr);

                    const int textureUnit = (int)SamplerTextureUnit.InitialColorBuffer;
                    OpenGL.glUniform1i(location, textureUnit);
                    OpenGL.glActiveTexture(OpenGL.GL_TEXTURE0 + (uint)textureUnit);
                    OpenGL.glBindTexture(OpenGL.GL_TEXTURE_2D, textureId);
                    OpenGL.glActiveTexture(OpenGL.GL_TEXTURE0);
                });

                Register("_depthBuffer", "sampler2D", "texture representing the depth information in the viewport before any shader components have executed", (location, display) =>
                {
                    IntPtr texture2dPtr = PerFrameCache.InitialDepthBuffer;
                    uint textureId = 0;
                    textureId = Rhino7NativeMethods.RhTexture2dHandle(texture2dPtr);

                    const int textureUnit = (int)SamplerTextureUnit.InitialDepthBuffer;
                    OpenGL.glUniform1i(location, textureUnit);
                    OpenGL.glActiveTexture(OpenGL.GL_TEXTURE0 + (uint)textureUnit);
                    OpenGL.glBindTexture(OpenGL.GL_TEXTURE_2D, textureId);
                    OpenGL.glActiveTexture(OpenGL.GL_TEXTURE0);
                });

                Register("_previousColorBuffer", "sampler2D", "The rhino viewport after the last shader components executed", (location, display) =>
                {
                    IntPtr texture2dPtr = PerFrameCache.PostColorBuffer;
                    uint textureId = 0;
                    textureId = Rhino7NativeMethods.RhTexture2dHandle(texture2dPtr);

                    const int textureUnit = (int)SamplerTextureUnit.BaseSampler;
                    OpenGL.glUniform1i(location, textureUnit);
                    OpenGL.glActiveTexture(OpenGL.GL_TEXTURE0 + (uint)textureUnit);
                    OpenGL.glBindTexture(OpenGL.GL_TEXTURE_2D, textureId);
                    OpenGL.glActiveTexture(OpenGL.GL_TEXTURE0);
                });

                Register("_worldToClip", "mat4", "transformation from world to clipping coordinates", (location, display) =>
                {
                    float[] w2c = display.GetOpenGLWorldToClip(true);
                    OpenGL.glUniformMatrix4fv(location, 1, false, w2c);

                });
                Register("_viewportSize", "vec2", "width and height of viewport in pixels", (location, display) =>
                {
                    var viewportSize = display.Viewport.Size;
                    OpenGL.glUniform2f(location, (float)viewportSize.Width, (float)viewportSize.Height);
                });
                Register("_frameSize", "vec2", "width and height of overall rendered frame in pixels", (location, display) =>
                {
                    var frameSize = display.FrameSize;
                    OpenGL.glUniform2f(location, (float)frameSize.Width, (float)frameSize.Height);
                });
                Register("_mousePosition", "vec2", "current mouse postion", (location, display) =>
                {
                    var mouse = display.Viewport.ParentView.ScreenToClient(System.Windows.Forms.Control.MousePosition);
                    mouse.Y = display.Viewport.Size.Height - mouse.Y - 1;

                    OpenGL.glUniform2f(location, mouse.X, mouse.Y);
                });
                Register("_mouseDownPosition", "vec2", "current mouse postion", (location, display) =>
                {
                    if (MouseDownPosition == null)
                        MouseDownPosition = new System.Drawing.Point(-1, -1);

                    if (System.Windows.Forms.Control.MouseButtons == System.Windows.Forms.MouseButtons.None)
                    {
                        // No mouse button is being pressed...reset the "down" position to reflect that...
                        var mdp = MouseDownPosition;
                        mdp.X = mdp.Y = -1;
                        MouseDownPosition = mdp;
                    }
                    
                    // If the "down" position hasn't been set yet, set it to the current mouse position...
                    if (MouseDownPosition.X == -1 && MouseDownPosition.Y == -1)
                    {
                        // Note: Mouse down position is stored in screen coordinates and translated to 
                        //       client coordinates prior to sending it to the shader... This is done
                        //       so that a mouse down in one viewport doesn't confuse the shader running
                        //       in another viewport.
                        MouseDownPosition = System.Windows.Forms.Control.MousePosition;
                    }

                    var mouse = display.Viewport.ParentView.ScreenToClient(MouseDownPosition);
                    mouse.Y = display.Viewport.Size.Height - mouse.Y - 1;

                    OpenGL.glUniform2f(location, mouse.X, mouse.Y);
                });
                Register("_mouseState", "ivec3", "current mouse postion and button state", (location, display) =>
                {
                    bool lmb = System.Windows.Forms.Control.MouseButtons == System.Windows.Forms.MouseButtons.Left;
                    bool rmb = System.Windows.Forms.Control.MouseButtons == System.Windows.Forms.MouseButtons.Right;
                    bool mmb = System.Windows.Forms.Control.MouseButtons == System.Windows.Forms.MouseButtons.Middle;
                    OpenGL.glUniform3i(location, lmb?1:0, mmb?1:0, rmb?1:0);
                });
                Register("_worldToCamera", "mat4", "transformation from world to camera coordinates", (location, display) =>
                {
                    float[] w2c = display.GetOpenGLWorldToCamera(true);
                    OpenGL.glUniformMatrix4fv(location, 1, false, w2c);
                });
                Register("_worldToCameraNormal", "mat3", "transformation to apply to normals for world to camera", (location, display) =>
                {
                    var xf = display.Viewport.GetTransform(Rhino.DocObjects.CoordinateSystem.World, Rhino.DocObjects.CoordinateSystem.Camera);
                    xf = xf.Transpose();
                    Rhino.Geometry.Transform m;
                    xf.TryGetInverse(out m);
                    m = m.Transpose();
                    float[] w2cn = new float[] {(float)m.M00, (float)m.M01, (float)m.M02,
                      (float)m.M10, (float)m.M11, (float)m.M12,
                      (float)m.M20, (float)m.M21, (float)m.M22};
                    OpenGL.glUniformMatrix3fv(location, 1, false, w2cn);
                });
                Register("_worldToScreen", "mat4" ,"transformation from world to screen coordinates. Screen origin is upper left", (location, display) =>
                {
                    var m = display.Viewport.GetTransform(Rhino.DocObjects.CoordinateSystem.World, Rhino.DocObjects.CoordinateSystem.Screen);
                    m = m.Transpose();
                    float[] w2s = new float[] {(float)m.M00, (float)m.M01, (float)m.M02, (float)m.M03,
                      (float)m.M10, (float)m.M11, (float)m.M12, (float)m.M13,
                      (float)m.M20, (float)m.M21, (float)m.M22, (float)m.M23,
                      (float)m.M30, (float)m.M31, (float)m.M32, (float)m.M33};
                    // flip z
                    w2s[2] = -w2s[2];
                    w2s[6] = -w2s[6];
                    w2s[10] = -w2s[10];
                    w2s[14] = -w2s[14];
                    OpenGL.glUniformMatrix4fv(location, 1, false, w2s);
                });
                Register("_cameraToClip", "mat4", "transformation from camera to clipping coordinates", (location, display) =>
                {
                    float[] c2c = display.GetOpenGLCameraToClip();
                    OpenGL.glUniformMatrix4fv(location, 1, false, c2c);
                });
                Register("_time", "float", "total elapsed seconds since starting GL Component", (location, display) =>
                {
                    var span = DateTime.Now - _startTime;
                    double seconds = span.TotalSeconds;
                    OpenGL.glUniform1f(location, (float)seconds);
                });
                Register("_date", "vec4", "year, month, day, time in seconds", (location, display) =>
                {
                    var date = DateTime.Now;
                    OpenGL.glUniform4f(location, (float)date.Year, (float)date.Month, (float)date.Day, (float)date.TimeOfDay.TotalSeconds);
                });
                Register("_timeDelta", "float", "seconds since the last time this view was drawn", (location, display) =>
                {
                    if (_drawTimes == null)
                        _drawTimes = new Dictionary<uint, DateTime>();
                    uint viewSerialNumber = display.Viewport.ParentView.RuntimeSerialNumber;
                    DateTime previousTime;
                    double seconds = 0;
                    if( _drawTimes.TryGetValue(viewSerialNumber, out previousTime))
                    {
                        var span = DateTime.Now - previousTime;
                        seconds = span.TotalSeconds;
                    }
                    _drawTimes[viewSerialNumber] = DateTime.Now;
                    OpenGL.glUniform1f(location, (float)seconds);
                });
                Register("_cameraLocation", "vec3", "world location of camera", (location, display) =>
                {
                    var camLoc = display.Viewport.CameraLocation;
                    OpenGL.glUniform3f(location, (float)camLoc.X, (float)camLoc.Y, (float)camLoc.Z);
                });
                Register("_cameraNear", "float", "near clip plane distance in camera coordinates", (location, display) =>
                {
                    display.Viewport.GetFrustum(out double l, out double r, out double b, out double t, out double near, out double far);
                    OpenGL.glUniform1f(location, (float)near);
                });
                Register("_cameraFar", "float", "farclip  plane distance in camera coordinates", (location, display) =>
                {
                    display.Viewport.GetFrustum(out double l, out double r, out double b, out double t, out double near, out double far);
                    OpenGL.glUniform1f(location, (float)far);
                });
                Register("_parallelViewport", "int", "1 if viewport has a parallel projection. 0 if perspective", (location, display) =>
                {
                    int parallel = display.Viewport.IsParallelProjection ? 1:0;
                    OpenGL.glUniform1i(location, parallel);
                });
                const int maxlights = 4;
                Register("_lightCount", "int", "number of lights in the scene", (location, display) =>
                {
                    // Use reflection until 6.3 goes to release candidate. GetLights is not available until 6.3
                    //var lights = display.GetLights();
                    var lights = GetLightsHelper(display);
                    int count = lights.Length < maxlights ? lights.Length : 4;
                    OpenGL.glUniform1i(location, count);
                });

                Register($"_lightPosition[{maxlights}]", "vec3", "array of four light positions", (location, display) =>
                {
                    // Use reflection until 6.3 goes to release candidate. GetLights is not available until 6.3
                    //var lights = display.GetLights();
                    var lights = GetLightsHelper(display);
                    float[] v = new float[3 * maxlights];
                    for (int i = 0; i < lights.Length; i++)
                    {
                        if (i >= maxlights)
                            break;
                        var loc = lights[i].Location;
                        v[i * 3] = (float)loc.X;
                        v[i * 3 + 1] = (float)loc.Y;
                        v[i * 3 + 2] = (float)loc.Z;
                    }
                    OpenGL.glUniform3fv(location, maxlights, v);
                });
                Register($"_lightDirection[{maxlights}]", "vec3", "array of four light direction vectors", (location, display) =>
                {
                    // Use reflection until 6.3 goes to release candidate. GetLights is not available until 6.3
                    //var lights = display.GetLights();
                    var lights = GetLightsHelper(display);
                    float[] v = new float[3 * maxlights];
                    for(int i=0; i<lights.Length; i++)
                    {
                        if (i >= maxlights)
                            break;
                        var direction = lights[i].Direction;
                        v[i * 3] = (float)direction.X;
                        v[i * 3 + 1] = (float)direction.Y;
                        v[i * 3 + 2] = (float)direction.Z;
                    }
                    OpenGL.glUniform3fv(location, maxlights, v);
                });
                Register($"_lightInCameraSpace[{maxlights}]", "vec3", "array of four light posiitons in camera space", (location, display) =>
                {
                    // Use reflection until 6.3 goes to release candidate. GetLights is not available until 6.3
                    //var lights = display.GetLights();
                    var lights = GetLightsHelper(display);
                    int[] v = new int[maxlights];
                    for (int i = 0; i < lights.Length; i++)
                    {
                        if (i >= maxlights)
                            break;

                        bool isCameraLight = lights[i].CoordinateSystem == Rhino.DocObjects.CoordinateSystem.Camera;
                        v[i] = isCameraLight ? 1 : 0;
                    }
                    OpenGL.glUniform1iv(location, maxlights, v);
                });

                _uniformBuiltins.Sort((a, b) => (a.Name.CompareTo(b.Name)));
            }
            return _uniformBuiltins;
        }

        public static List<BuiltIn> GetAttributeBuiltIns()
        {
            List<BuiltIn> rc = new List<BuiltIn>();
            rc.Add(new BuiltIn("_meshVertex", "vec3", "mesh vertex location", null));
            rc.Add(new BuiltIn("_meshNormal", "vec3", "mesh normal", null));
            rc.Add(new BuiltIn("_meshTextureCoordinate", "vec2", "mesh texture coordinate", null));
            rc.Add(new BuiltIn("_meshVertexColor", "vec4", "mesh vertex color", null));
            return rc;
        }

        static void Register(string name, string datatype, string description, Action<int, Rhino.Display.DisplayPipeline> setup)
        {
            if (_uniformBuiltins == null)
                _uniformBuiltins = new List<BuiltIn>();
            var builtin = new BuiltIn(name, datatype, description, setup);
            _uniformBuiltins.Add(builtin);
        }
    }
}
