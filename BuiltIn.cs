using System;
using System.Collections.Generic;

namespace ghgl
{
    class BuiltIn
    {
        static List<BuiltIn> _uniformBuiltins;
        static DateTime _startTime;
        Action<int, Rhino.Display.DisplayPipeline> _setup;

        private BuiltIn(string name, Action<int, Rhino.Display.DisplayPipeline> setup)
        {
            Name = name;
            _setup = setup;
        }

        public string Name { get; private set; }

        public void Setup(uint program, Rhino.Display.DisplayPipeline dp)
        {
            int uniformLocation = OpenGL.glGetUniformLocation(program, Name);
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
                Register("_worldToClip", (location, display) =>
                {
                    float[] w2c = display.GetOpenGLWorldToClip(true);
                    OpenGL.glUniformMatrix4fv(location, 1, false, w2c);

                });
                Register("_viewportSize", (location, display) =>
                {
                    var viewportSize = display.Viewport.Size;
                    OpenGL.glUniform2f(location, (float)viewportSize.Width, (float)viewportSize.Height);
                });
                Register("_worldToCamera", (location, display) =>
                {
                    float[] w2c = display.GetOpenGLWorldToCamera(true);
                    OpenGL.glUniformMatrix4fv(location, 1, false, w2c);
                });
                Register("_worldToCameraNormal", (location, display) =>
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
                Register("_cameraToClip", (location, display) =>
                {
                    float[] c2c = display.GetOpenGLCameraToClip();
                    OpenGL.glUniformMatrix4fv(location, 1, false, c2c);
                });
                Register("_time", (location, display) =>
                {
                    var span = DateTime.Now - _startTime;
                    double seconds = span.TotalSeconds;
                    OpenGL.glUniform1f(location, (float)seconds);
                });
                Register("_cameraLocation", (location, display) =>
                {
                    var camLoc = display.Viewport.CameraLocation;
                    OpenGL.glUniform3f(location, (float)camLoc.X, (float)camLoc.Y, (float)camLoc.Z);
                });
                Register("_lightCount", (location, display) =>
                {
                    // Use reflection until 6.3 goes to release candidate. GetLights is not available until 6.3
                    //var lights = display.GetLights();
                    var lights = GetLightsHelper(display);
                    OpenGL.glUniform1i(location, lights.Length);
                });
                for (int i = 0; i < 4; i++)
                {
                    int current = i;
                    Register($"_light{current+1}Position", (location, display) =>
                    {
                        // Use reflection until 6.3 goes to release candidate. GetLights is not available until 6.3
                        //var lights = display.GetLights();
                        var lights = GetLightsHelper(display);
                        if (lights.Length > current)
                        {
                            var lightLocation = lights[current].Location;
                            OpenGL.glUniform3f(location, (float)lightLocation.X, (float)lightLocation.Y, (float)lightLocation.Z);
                        }
                    });
                    Register($"_light{current + 1}Direction", (location, display) =>
                    {
                        // Use reflection until 6.3 goes to release candidate. GetLights is not available until 6.3
                        //var lights = display.GetLights();
                        var lights = GetLightsHelper(display);
                        if (lights.Length > current)
                        {
                            var direction = lights[current].Direction;
                            OpenGL.glUniform3f(location, (float)direction.X, (float)direction.Y, (float)direction.Z);
                        }
                    });
                }

                _uniformBuiltins.Sort((a, b) => (a.Name.CompareTo(b.Name)));
            }
            return _uniformBuiltins;
        }

        static void Register(string name, Action<int, Rhino.Display.DisplayPipeline> setup)
        {
            if (_uniformBuiltins == null)
                _uniformBuiltins = new List<BuiltIn>();
            var builtin = new BuiltIn(name, setup);
            _uniformBuiltins.Add(builtin);
        }
    }
}
