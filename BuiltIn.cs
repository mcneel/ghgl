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

        static Rhino.Geometry.Light[] GetLightsHelper(Rhino.Display.DisplayPipeline pipeline)
        {
            var method = pipeline.GetType().GetMethod("GetLights");
            if (method == null)
                return new Rhino.Geometry.Light[0];
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
