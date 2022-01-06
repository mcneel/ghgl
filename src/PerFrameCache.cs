using System;
using System.Collections.Generic;

namespace ghgl
{
    static class PerFrameCache
    {
        class PerFrameLifetimeObject : IDisposable
        {
            public void Dispose()
            {
                PerFrameCache.EndFrame();
            }
        }

        static Dictionary<string, IntPtr> _componentSamplers = new Dictionary<string, IntPtr>();

        public static IDisposable BeginFrame(Rhino.Display.DisplayPipeline display, List<GLShaderComponentBase> components)
        {
            // check to see if any components use _colorBuffer or _depthBuffer
            bool usesInitialColorBuffer = false;
            bool usesInitialDepthBuffer = false;
            foreach (var component in components)
            {
                if (!usesInitialColorBuffer && component._model.TryGetUniformType("_colorBuffer", out string dataType, out int arrayLength))
                    usesInitialColorBuffer = true;
                if (!usesInitialDepthBuffer && component._model.TryGetUniformType("_depthBuffer", out dataType, out arrayLength))
                    usesInitialDepthBuffer = true;
            }
            if (usesInitialColorBuffer)
            {
                IntPtr texture2dPtr = Rhino7NativeMethods.RhTexture2dCreate();
                Rhino7NativeMethods.RhTexture2dCapture(display, texture2dPtr, Rhino7NativeMethods.CaptureFormat.kRGBA);
                InitialColorBuffer = texture2dPtr;
            }
            if (usesInitialDepthBuffer)
            {
                IntPtr texture2dPtr = Rhino7NativeMethods.RhTexture2dCreate();
                Rhino7NativeMethods.RhTexture2dCapture(display, texture2dPtr, Rhino7NativeMethods.CaptureFormat.kDEPTH24);
                InitialDepthBuffer = texture2dPtr;
            }

            foreach (var texture in _componentSamplers.Values)
                GLRecycleBin.AddTextureToDeleteList(texture);
            _componentSamplers.Clear();

            // figure out list of per component depth and color textures that need to be created/retrieved
            foreach (var component in components)
            {
                string[] samplers = component._model.GetUniformsAndAttributes(0).GetComponentSamplers();
                foreach (var sampler in samplers)
                    _componentSamplers[sampler.ToLowerInvariant()] = IntPtr.Zero;
            }

            return new PerFrameLifetimeObject();
        }

        public static IntPtr InitialColorBuffer { get; private set; }
        public static IntPtr InitialDepthBuffer { get; private set; }
        public static IntPtr PostColorBuffer { get; set; }

        /// <summary>
        /// Return true if the output color buffer from a given component is used downstream
        /// </summary>
        /// <param name="component"></param>
        /// <returns></returns>
        public static bool IsColorTextureUsed(GLShaderComponentBase component)
        {
            string id = $"{component.InstanceGuid}:color".ToLowerInvariant();
            return _componentSamplers.ContainsKey(id);
        }

        /// <summary>
        /// Return true if the output depth buffer from a given component is used downstream
        /// </summary>
        /// <param name="component"></param>
        /// <returns></returns>
        public static bool IsDepthTextureUsed(GLShaderComponentBase component)
        {
            string id = $"{component.InstanceGuid}:depth".ToLowerInvariant();
            return _componentSamplers.ContainsKey(id);
        }

        public static void SaveColorTexture(GLShaderComponentBase component, IntPtr ptrTexture)
        {
            string id = $"{component.InstanceGuid}:color".ToLowerInvariant();
            _componentSamplers[id] = ptrTexture;
        }
        public static void SaveDepthTexture(GLShaderComponentBase component, IntPtr ptrTexture)
        {
            string id = $"{component.InstanceGuid}:depth".ToLowerInvariant();
            _componentSamplers[id] = ptrTexture;
        }

        public static uint GetTextureId(string name)
        {
            if(_componentSamplers.TryGetValue(name.ToLowerInvariant(), out IntPtr ptrTexture))
                return Rhino7NativeMethods.RhTexture2dHandle(ptrTexture);
            return 0;
        }

        public static System.Drawing.Bitmap GetTextureImage(GLShaderComponentBase component, bool colorBuffer)
        {
            string id = colorBuffer ?
                $"{component.InstanceGuid}:color".ToLowerInvariant() :
                $"{component.InstanceGuid}:depth".ToLowerInvariant();
            if (_componentSamplers.TryGetValue(id.ToLowerInvariant(), out IntPtr ptrColorTexture))
            {
                GLShaderComponentBase.ActivateGlContext();
                return Rhino7NativeMethods.RhTexture2dToDib(ptrColorTexture);
            }
            return null;
        }

        public static void EndFrame()
        {
            GLRecycleBin.AddTextureToDeleteList(InitialColorBuffer);
            InitialColorBuffer = IntPtr.Zero;
            GLRecycleBin.AddTextureToDeleteList(InitialDepthBuffer);
            InitialDepthBuffer = IntPtr.Zero;
            GLRecycleBin.AddTextureToDeleteList(PostColorBuffer);
            PostColorBuffer = IntPtr.Zero;
        }
    }
}
