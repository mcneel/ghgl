using System;
using System.Runtime.InteropServices;

namespace ghgl
{
    static class Rhino7NativeMethods
    {
        public enum CaptureFormat : uint
        {
            kRGB = 1,  // 24 bits per pixel
            kRGBA = 2,  // 32 bits per pixel
            kBGR = 3,
            kBGRA = 4,
            kDEPTH = 5,
            kDEPTH16 = 6,  // 16 bit float values
            kDEPTH24 = 7,  // 24 bit float values
            kDEPTH32 = 8,  // 32 bit float values
            kC8 = 9,  // 8 bit components, 32 bits per pixel, layout in memory=(C,C,C,1)
            kA8 = 10, // 8 bit components, 32 bits per pixel, layout in memory=(0,0,0,A)
            kRGB4 = 11,
            kRGBA4 = 12,
            kRGBA2 = 13,
            kR3G3B2 = 14,
            kRGB10 = 15,
            kRGB10_A2 = 16,
            kRGB12 = 17,
            kRGBA12 = 18,
            kRGB16 = 19,
            kRGBA16 = 20,
            kLUMINANCE4 = 21,
            kLUMINANCE8 = 22,
            kALPHA4 = 23,
            kALPHA8 = 24,
            kALPHA12 = 25,
            kALPHA16 = 26,
            kINTENSITY = 27,
            kINTENSITY4 = 28,
            kINTENSITY8 = 29,
            kINTENSITY12 = 30,
            kINTENSITY16 = 31,
            kRGBA8 = 32,
            kCOMPRESSED_RGB = 48,
            kCOMPRESSED_RGBA = 49,
            kCOMPRESSED_RGB_DXT1 = 50,
            kCOMPRESSED_RGBA_DXT1 = 51,
            kCOMPRESSED_RGBA_DXT3 = 52,
            kCOMPRESSED_RGBA_DXT5 = 53,
            kRGB16F = 64,
            kRGBA16F = 65,
            kALPHA16F = 66,
            kINTENSITY16F = 67,
            kRGB32F = 80,
            kRGBA32F = 81,
            kALPHA32F = 82,
            kINTENSITY32F = 83,
            kDEPTH_STENCIL = 100,
        }

        public static IntPtr RhTexture2dCreate()
        {
            if (Rhino.Runtime.HostUtils.RunningOnWindows)
                return WindowsMethods.RhTexture2dCreate();
            else
                return MacMethods.RhTexture2dCreate();
        }

        public static void RhTexture2dDelete(IntPtr ptrTexture2d)
        {
            if (Rhino.Runtime.HostUtils.RunningOnWindows)
                WindowsMethods.RhTexture2dDelete(ptrTexture2d);
            else
                MacMethods.RhTexture2dDelete(ptrTexture2d);
        }

        public static uint RhTexture2dHandle(IntPtr ptrTexture2d)
        {
            if (Rhino.Runtime.HostUtils.RunningOnWindows)
                return WindowsMethods.RhTexture2dHandle(ptrTexture2d);
            else
                return MacMethods.RhTexture2dHandle(ptrTexture2d);
        }

        public static bool RhTexture2dCapture(Rhino.Display.DisplayPipeline pipeline, IntPtr ptrTexture2d, CaptureFormat captureFormat)
        {
            var fieldInfo = pipeline.GetType().GetField("m_ptr", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            IntPtr ptrPipeline = (IntPtr)fieldInfo.GetValue(pipeline);
            if (Rhino.Runtime.HostUtils.RunningOnWindows)
                return WindowsMethods.RhTexture2dPipelineCapture(ptrPipeline, ptrTexture2d, captureFormat);
            else
            {
                var viewSerialNumber = pipeline.Viewport.ParentView.RuntimeSerialNumber;
                return MacMethods.RhTexture2dCapture(viewSerialNumber, ptrTexture2d, captureFormat);
            }
        }

        public static System.Drawing.Bitmap RhTexture2dToDib(IntPtr ptrTexture2d)
        {
            if (Rhino.Runtime.HostUtils.RunningOnWindows)
            {
                IntPtr ptrRhinoDib = WindowsMethods.RhTexture2dToBitmap(ptrTexture2d);

                var type = typeof(Rhino.Runtime.Interop).Assembly.GetType("Rhino.Runtime.InteropWrappers.RhinoDib");
                var mi = type.GetMethod("ToBitmap", new Type[] { typeof(IntPtr), typeof(bool) });
                System.Drawing.Bitmap rc = mi.Invoke(null, new object[] { ptrRhinoDib, true }) as System.Drawing.Bitmap;
                return rc;
            }
            return null;
        }


        class WindowsMethods
        {
            const string RHINOCORE_LIB = "RhinoCore.dll";

            [DllImport(RHINOCORE_LIB)]
            public static extern IntPtr RhTexture2dCreate();

            [DllImport(RHINOCORE_LIB)]
            public static extern void RhTexture2dDelete(IntPtr ptrTexture2d);

            [DllImport(RHINOCORE_LIB)]
            public static extern uint RhTexture2dHandle(IntPtr ptrTexture2d);

            [DllImport(RHINOCORE_LIB)]
            [return: MarshalAs(UnmanagedType.U1)]
            public static extern bool RhTexture2dPipelineCapture(IntPtr ptrPipeline, IntPtr ptrTexture2d, CaptureFormat captureFormat);

            [DllImport(RHINOCORE_LIB)]
            public static extern IntPtr RhTexture2dToBitmap(IntPtr ptrTexture2d);
        }
        class MacMethods
        {
            const string RHINOCORE_LIB = "__Internal";

            [DllImport(RHINOCORE_LIB)]
            public static extern IntPtr RhTexture2dCreate();

            [DllImport(RHINOCORE_LIB)]
            public static extern void RhTexture2dDelete(IntPtr ptrTexture2d);

            [DllImport(RHINOCORE_LIB)]
            public static extern uint RhTexture2dHandle(IntPtr ptrTexture2d);

            [DllImport(RHINOCORE_LIB)]
            [return: MarshalAs(UnmanagedType.U1)]
            public static extern bool RhTexture2dCapture(uint viewSerialNumber, IntPtr ptrTexture2d, CaptureFormat captureFormat);
        }
    }
}
