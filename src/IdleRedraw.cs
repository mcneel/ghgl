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
                doc.Views.Redraw();
        }
    }
}
