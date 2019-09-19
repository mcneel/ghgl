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
        static DateTime _startTime;
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
                _startTime = DateTime.Now;
                Register("_colorBuffer", "sampler2D", "texture representing the current state of the color information in the viewport", (location, display) =>
                {
                    uint textureId = 0;
                    IntPtr texture2dPtr = Rhino7NativeMethods.RhTexture2dCreate();
                    if (Rhino7NativeMethods.RhTexture2dCapture(display.Viewport.ParentView.RuntimeSerialNumber, texture2dPtr, Rhino7NativeMethods.CaptureFormat.kRGBA))
                        textureId = Rhino7NativeMethods.RhTexture2dHandle(texture2dPtr);

                    const int textureUnit = (int)SamplerTextureUnit.ColorBuffer;
                    OpenGL.glUniform1i(location, textureUnit);
                    OpenGL.glActiveTexture(OpenGL.GL_TEXTURE0 + (uint)textureUnit);
                    OpenGL.glBindTexture(OpenGL.GL_TEXTURE_2D, textureId);
                    OpenGL.glActiveTexture(OpenGL.GL_TEXTURE0);
                    GLRecycleBin.AddTextureToDeleteList(texture2dPtr);
                });
                Register("_initialColorBuffer", "sampler2D", "texture representing the color information in the viewport before any shader components have executed", (location, display) =>
                {
                    IntPtr texture2dPtr = PerFrameCache.InitialColorBuffer;
                    uint textureId = 0;
                    textureId = Rhino7NativeMethods.RhTexture2dHandle(texture2dPtr);

                    const int textureUnit = (int)SamplerTextureUnit.InitialColorBuffer;
                    OpenGL.glUniform1i(location, textureUnit);
                    OpenGL.glActiveTexture(OpenGL.GL_TEXTURE0 + (uint)textureUnit);
                    OpenGL.glBindTexture(OpenGL.GL_TEXTURE_2D, textureId);
                    OpenGL.glActiveTexture(OpenGL.GL_TEXTURE0);
                    GLRecycleBin.AddTextureToDeleteList(texture2dPtr);
                });

                Register("_depthBuffer", "sampler2D", "texture representing the current state of the depth information in the viewport", (location, display) =>
                {
                    if (Rhino.RhinoApp.ExeVersion < 7)
                        throw new Exception("_depthBuffer uniform is only supported in Rhino 7 or above");

                    uint textureId = 0;
                    IntPtr texture2dPtr = Rhino7NativeMethods.RhTexture2dCreate();
                    if (Rhino7NativeMethods.RhTexture2dCapture(display.Viewport.ParentView.RuntimeSerialNumber, texture2dPtr, Rhino7NativeMethods.CaptureFormat.kDEPTH24))
                        textureId = Rhino7NativeMethods.RhTexture2dHandle(texture2dPtr);

                    const int textureUnit = (int)SamplerTextureUnit.DepthBuffer;
                    OpenGL.glUniform1i(location, textureUnit);
                    OpenGL.glActiveTexture(OpenGL.GL_TEXTURE0 + (uint)textureUnit);
                    OpenGL.glBindTexture(OpenGL.GL_TEXTURE_2D, textureId);
                    OpenGL.glActiveTexture(OpenGL.GL_TEXTURE0);
                    GLRecycleBin.AddTextureToDeleteList(texture2dPtr);
                });
                Register("_initialDepthBuffer", "sampler2D", "texture representing the depth information in the viewport before any shader components have executed", (location, display) =>
                {
                    IntPtr texture2dPtr = PerFrameCache.InitialDepthBuffer;
                    uint textureId = 0;
                    textureId = Rhino7NativeMethods.RhTexture2dHandle(texture2dPtr);

                    const int textureUnit = (int)SamplerTextureUnit.InitialDepthBuffer;
                    OpenGL.glUniform1i(location, textureUnit);
                    OpenGL.glActiveTexture(OpenGL.GL_TEXTURE0 + (uint)textureUnit);
                    OpenGL.glBindTexture(OpenGL.GL_TEXTURE_2D, textureId);
                    OpenGL.glActiveTexture(OpenGL.GL_TEXTURE0);
                    GLRecycleBin.AddTextureToDeleteList(texture2dPtr);
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
                Register("_worldToScreen", "mat4" ,"transformation from world to screen coordinates", (location, display) =>
                {
                    var xf = display.Viewport.GetTransform(Rhino.DocObjects.CoordinateSystem.World, Rhino.DocObjects.CoordinateSystem.Screen);
                    xf = xf.Transpose();
                    Rhino.Geometry.Transform m;
                    xf.TryGetInverse(out m);
                    m = m.Transpose();
                    float[] w2cn = new float[] {(float)m.M00, (float)m.M01, (float)m.M02, (float)m.M03,
                      (float)m.M10, (float)m.M11, (float)m.M12, (float)m.M13,
                      (float)m.M20, (float)m.M21, (float)m.M22, (float)m.M23};
                    OpenGL.glUniformMatrix4fv(location, 1, false, w2cn);
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
