using System;

namespace ghgl
{
    /// <summary>
    /// Helper class used for OnIdle redrawing when animations are enabled
    /// </summary>
    class IdleRedraw
    {
        public void PerformRedraw(object sender, EventArgs e)
        {
            var doc = Rhino.RhinoDoc.ActiveDoc;
            if (doc != null)
            {
                doc.Views.Redraw();

                var display = Rhino.RhinoDoc.ActiveDoc.Views.ActiveView.DisplayPipeline;
                IntPtr texture2dPtr = Rhino7NativeMethods.RhTexture2dCreate();
                if (Rhino7NativeMethods.RhTexture2dCapture(display, texture2dPtr, Rhino7NativeMethods.CaptureFormat.kRGBA))
                {
                    PerFrameCache.PostColorBuffer = texture2dPtr;
                }
            }
                
        }
    }
}
