using System;
using System.Collections.Generic;
using System.Drawing;
using GH_IO.Serialization;
using Grasshopper.GUI;
using Grasshopper.GUI.Canvas;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Attributes;
using Grasshopper.Kernel.Parameters;
using Grasshopper.Kernel.Types;
using Rhino.Display;
using Rhino.Geometry;

namespace ghgl
{
    /// <summary>
    /// Base class for GL Shader components. Most of the heavy lifting is done in this class
    /// and the subclasses just specialize a little bit
    /// </summary>
    public abstract class GLShaderComponentBase : GH_Component, IGH_VariableParameterComponent
    {
        internal GLSLViewModel _model = new GLSLViewModel();

        static uint _viewSerialNumber;
        static IntPtr _hglrc;
        static bool _initializeCallbackSet;
        static IdleRedraw _idleRedraw;
        int _majorVersion = 0;
        int _minorVersion = 2;

        protected GLShaderComponentBase(string name, string nickname, string description)
          : base(name, nickname, description, "Display", "Preview")
        {
            if (!_initializeCallbackSet)
            {
                // set up callback to perform one time OpenGL initialization during the
                // next draw function
                _initializeCallbackSet = true;
                GlslifyPackage.Initialize();
                DisplayPipeline.DrawForeground += DisplayPipeline_DrawForeground;
                var doc = Rhino.RhinoDoc.ActiveDoc;
                doc?.Views.Redraw();
            }
            _model.PropertyChanged += ModelPropertyChanged;
        }

        public static void RedrawViewportControl()
        {
            if (Grasshopper.Instances.ActiveCanvas != null)
            {
                var ctrls = Grasshopper.Instances.ActiveCanvas.Controls;
                if (ctrls != null)
                {
                    for (int i = 0; i < ctrls.Count; i++)
                        ctrls[i].Refresh();
                }

            }
        }

        public static bool AnimationTimerEnabled
        {
            get
            {
                return _idleRedraw != null;
            }
            set
            {
                if( value )
                {
                    if (_idleRedraw == null)
                    {
                        _idleRedraw = new IdleRedraw();
                        Rhino.RhinoApp.Idle += _idleRedraw.PerformRedraw;
                    }
                }
                else
                {
                    if(_idleRedraw!=null)
                    {
                        Rhino.RhinoApp.Idle -= _idleRedraw.PerformRedraw;
                        _idleRedraw = null;
                    }
                }
            }
        }

        private static void RhinoApp_Idle(object sender, EventArgs e)
        {
            throw new NotImplementedException();
        }

        private void ModelPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "DrawMode":
                case "glLineWidth":
                case "glPointSize":
                case "DepthTestingEnabled":
                case "DepthWritingEnabled":
                    ExpirePreview(true);
                    break;
            }
        }

        // One time setup function to get the OpenGL functions initialized for use
        private void DisplayPipeline_DrawForeground(object sender, DrawEventArgs e)
        {
            DisplayPipeline.DrawForeground -= DisplayPipeline_DrawForeground;
            if (!OpenGL.Initialized)
                OpenGL.Initialize();

            _hglrc = OpenGL.wglGetCurrentContext();
            var view = e.Display.Viewport.ParentView;
            if (view == null)
                view = e.RhinoDoc.Views.ActiveView;
            _viewSerialNumber = view.RuntimeSerialNumber;
        }

        public override Guid ComponentGuid => GetType().GUID;
        public override GH_Exposure Exposure => GH_Exposure.tertiary;

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Color", "C", "Color Buffer", GH_ParamAccess.item);
            pManager.AddTextParameter("Depth", "D", "Depth Buffer", GH_ParamAccess.item);
        }

        public override bool Write(GH_IWriter writer)
        {
            bool rc = base.Write(writer);
            if (rc)
            {
                writer.SetVersion("GLShader", _majorVersion, _minorVersion, 0);
                rc = _model.Write(writer);
                writer.SetInt32("PreviewSortOrder", PreviewSortOrder);
            }
            return rc;
        }

        public override bool Read(GH_IReader reader)
        {
            bool rc = base.Read(reader) &&
              _model.Read(reader);
            if( rc )
            {
                int order = PreviewSortOrder;
                if (reader.TryGetInt32("PreviewSortOrder", ref order))
                    PreviewSortOrder = order;
            }
            var version = reader.GetVersion("GLShader");
            _majorVersion = version.major;
            _minorVersion = version.minor;
            return rc;
        }

        public static bool ActivateGlContext()
        {
            if (!OpenGL.IsAvailable)
                return false;

            // just assume GL context is active for now
            if (Rhino.Runtime.HostUtils.RunningOnOSX)
                return true;

            if (OpenGL.wglGetCurrentContext() != IntPtr.Zero)
                return true;

            if (IntPtr.Zero == _hglrc)
                return false;

            RhinoView view = RhinoView.FromRuntimeSerialNumber(_viewSerialNumber);
            if (null == view)
            {
                _hglrc = IntPtr.Zero;
                _viewSerialNumber = 0;
                return false;
            }
            var hwnd = view.Handle;
            var hdc = OpenGL.GetDC(hwnd);
            OpenGL.wglMakeCurrent(hdc, _hglrc);
            return true;
        }

        internal string VersionErrorMessage
        {
            get
            {
                return "This version of GhGL is for Rhino 7 or above and will not work in Rhino 6.\nEither run TestPackageManager and choose to use GhGL version 0.6.4 or use Rhino 7";
            }
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            SolveInstanceHelper(DA, 0);
        }

        protected void SolveInstanceHelper(IGH_DataAccess data, int startIndex)
        {
            if (Rhino.RhinoApp.ExeVersion == 6)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, VersionErrorMessage);
            }

            if (!_conduitEnabled)
            {
                Rhino.Display.DisplayPipeline.PostDrawObjects += PostDrawObjects;
                _conduitEnabled = true;
            }
            _componentsNeedSorting = true;

            if (!ActivateGlContext())
                return;

            if (!OpenGL.IsAvailable)
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Unable to access required OpenGL features");

            if ( _majorVersion>0 || _minorVersion>1 )
            {
                data.SetData(0, $"{InstanceGuid}:color");
                data.SetData(1, $"{InstanceGuid}:depth");
            }

            if (data.Iteration == 0)
            {
                if (!_model.CompileProgram())
                {
                    foreach (var err in _model.AllCompileErrors())
                    {
                        AddRuntimeMessage(GH_RuntimeMessageLevel.Error, err.ToString());
                    }
                }

                _model.ClearData();
            }
            var uniformsAndAttributes = _model.GetUniformsAndAttributes(data.Iteration);

            for (int i = startIndex; i < Params.Input.Count; i++)
            {
                List<IGH_Goo> destinationList = null;
                if (Params.Input[i].Access == GH_ParamAccess.item)
                {
                    IGH_Goo destination = null;
                    data.GetData(i, ref destination);
                    destinationList = new List<IGH_Goo>(new[] { destination });
                }
                else
                {
                    destinationList = new List<IGH_Goo>();
                    data.GetDataList(i, destinationList);
                }

                string varname = Params.Input[i].NickName;
                if (varname.Contains("["))
                    varname = varname.Substring(0, varname.IndexOf('['));
                string datatype;
                int arrayLength;
                if (_model.TryGetUniformType(varname, out datatype, out arrayLength))
                {
                    switch (datatype)
                    {
                        case "int":
                            {
                                int[] values = new int[destinationList.Count];
                                for (int j = 0; j < values.Length; j++)
                                {
                                    IGH_Goo destination = destinationList[j];
                                    int value;
                                    if (!destination.CastTo(out value))
                                    {
                                        double dvalue;
                                        if (destination.CastTo(out dvalue))
                                            value = (int)dvalue;
                                        else
                                        {
                                            bool bvalue;
                                            if (destination.CastTo(out bvalue))
                                                value = bvalue ? 1 : 0;
                                        }
                                    }
                                    values[j] = value;
                                }

                                // hack for iterations
                                if (arrayLength == 0 && data.Iteration < destinationList.Count)
                                    values[0] = values[data.Iteration];

                                uniformsAndAttributes.AddUniform(varname, values, arrayLength);
                                break;
                            }
                        case "float":
                            {
                                float[] values = new float[destinationList.Count];
                                for (int j = 0; j < values.Length; j++)
                                {
                                    IGH_Goo destination = destinationList[j];
                                    double value;
                                    destination.CastTo(out value);
                                    if (!destination.CastTo(out value))
                                    {
                                        int ival;
                                        if (destination.CastTo(out ival))
                                            value = ival;
                                    }
                                    values[j] = (float)value;
                                }

                                // hack for iterations
                                if (arrayLength == 0 && data.Iteration < destinationList.Count)
                                    values[0] = values[data.Iteration];

                                uniformsAndAttributes.AddUniform(varname, values, arrayLength);
                                break;
                            }
                        case "vec3":
                            {
                                Point3f[] values = GooListToPoint3fArray(destinationList);

                                // hack for iterations
                                if (arrayLength == 0 && data.Iteration < destinationList.Count)
                                    values[0] = values[data.Iteration];

                                if ( values != null )
                                    uniformsAndAttributes.AddUniform(varname, values, arrayLength);
                                break;
                            }
                        case "vec4":
                            {
                                Vec4[] values = new Vec4[destinationList.Count];
                                for (int j = 0; j < values.Length; j++)
                                {
                                    IGH_Goo destination = destinationList[j];
                                    if (destination.TypeName == "Colour")
                                    {
                                        System.Drawing.Color color;
                                        if (destination.CastTo(out color))
                                        {
                                            values[j] = new Vec4(color.R / 255.0f, color.G / 255.0f, color.B / 255.0f, color.A / 255.0f);
                                        }
                                    }
                                }

                                // hack for iterations
                                if (arrayLength == 0 && data.Iteration < destinationList.Count)
                                    values[0] = values[data.Iteration];

                                uniformsAndAttributes.AddUniform(varname, values, arrayLength);
                                break;
                            }
                        case "bool":
                            {
                                int[] values = new int[destinationList.Count];
                                for (int j = 0; j < values.Length; j++)
                                {
                                    IGH_Goo destination = destinationList[j];
                                    bool value;
                                    if (!destination.CastTo(out value))
                                    {
                                        int ivalue;
                                        if (destination.CastTo(out ivalue))
                                            value = ivalue != 0;
                                    }
                                    values[j] = value ? 1 : 0;
                                }

                                // hack for iterations
                                if (arrayLength == 0 && data.Iteration < destinationList.Count)
                                    values[0] = values[data.Iteration];

                                uniformsAndAttributes.AddUniform(varname, values, arrayLength);
                                break;
                            }
                        case "sampler2D":
                            {
                                IGH_Goo destination = destinationList[0];
                                //Try casting to a string first. This will be interpreted as a path to an image file
                                string path;
                                if (destination.CastTo(out path))
                                {
                                    bool isComponentInput = false;
                                    // see if path refers to a component's output parameter
                                    if(path.EndsWith(":color") || path.EndsWith(":depth"))
                                    {
                                        string id = path.Substring(0, path.IndexOf(":"));
                                        isComponentInput = Guid.TryParse(id, out Guid compId);
                                    }

                                    if (!isComponentInput)
                                    {
                                        bool isUrl = path.StartsWith("http:/", StringComparison.InvariantCultureIgnoreCase) ||
                                            path.StartsWith("https:/", StringComparison.InvariantCultureIgnoreCase);
                                        if (!isUrl && !System.IO.File.Exists(path))
                                        {
                                            var ghdoc = OnPingDocument();
                                            if (ghdoc != null)
                                            {
                                                string workingDirectory = System.IO.Path.GetDirectoryName(ghdoc.FilePath);
                                                path = System.IO.Path.GetFileName(path);
                                                path = System.IO.Path.Combine(workingDirectory, path);
                                            }
                                        }
                                    }

                                    uniformsAndAttributes.AddSampler2DUniform(varname, path);
                                }
                                else
                                {
                                    System.Drawing.Bitmap bmp;
                                    if( destination.CastTo(out bmp) )
                                    {
                                        uniformsAndAttributes.AddSampler2DUniform(varname, bmp);
                                    }
                                }

                                break;
                            }
                    }
                    continue;
                }

                int location;
                if (_model.TryGetAttributeType(varname, out datatype, out location))
                {
                    if (datatype == "int" || datatype == "float")
                    {
                        List<double> destination = new List<double>();
                        if (data.GetDataList(i, destination))
                        {
                            int[] ints = datatype == "int" ? new int[destination.Count] : null;
                            float[] floats = ints == null ? new float[destination.Count] : null;
                            for (int index = 0; index < destination.Count; index++)
                            {
                                if (ints != null)
                                    ints[index] = (int)destination[index];
                                if (floats != null)
                                    floats[index] = (float)destination[index];
                            }
                            if (ints != null && ints.Length > 0)
                                uniformsAndAttributes.AddAttribute(varname, location, ints);
                            if (floats != null && floats.Length > 0)
                                uniformsAndAttributes.AddAttribute(varname, location, floats);
                        }
                        if (destination.Count < 1 && datatype == "int")
                        {
                            List<int> int_destination = new List<int>();
                            if (data.GetDataList(i, int_destination) && int_destination.Count > 0)
                            {
                                int[] ints = int_destination.ToArray();
                                if (ints != null && ints.Length > 0)
                                    uniformsAndAttributes.AddAttribute(varname, location, ints);
                            }

                        }
                    }
                    if ( datatype == "vec2" )
                    {
                        //vec2 -> point2d
                        Point2f[] vec2_array = GooListToPoint2fArray(destinationList);
                        if (vec2_array != null)
                            uniformsAndAttributes.AddAttribute(varname, location, vec2_array);
                    }
                    if (datatype == "vec3")
                    {
                        //vec3 -> point3d
                        Point3f[] vec3_array = GooListToPoint3fArray(destinationList);
                        if( vec3_array!=null )
                            uniformsAndAttributes.AddAttribute(varname, location, vec3_array);
                    }
                    if (datatype == "vec4")
                    {
                        List<System.Drawing.Color> destination = new List<System.Drawing.Color>();
                        if (data.GetDataList(i, destination))
                        {
                            Vec4[] vec4_array = new Vec4[destination.Count];
                            for (int index = 0; index < destination.Count; index++)
                            {
                                Color4f color = new Color4f(destination[index]);
                                vec4_array[index] = new Vec4(color.R, color.G, color.B, color.A);
                            }
                            uniformsAndAttributes.AddAttribute(varname, location, vec4_array);
                        }
                    }

                    continue;
                }

                // If we get here, we don't have a reference to this input in our code yet.
                // See if the input is an upstream texture since we know how to handle those
                if (destinationList.Count>0 && destinationList[0].CastTo(out string upstreamSampler))
                {
                    // see if path refers to a component's output parameter
                    if (upstreamSampler.EndsWith(":color") || upstreamSampler.EndsWith(":depth"))
                    {
                        string id = upstreamSampler.Substring(0, upstreamSampler.IndexOf(":"));
                        bool isComponentInput = Guid.TryParse(id, out Guid compId);
                        if (isComponentInput)
                            uniformsAndAttributes.AddSampler2DUniform(varname, upstreamSampler);
                    }
                }


            }
        }

        static Point2f[] GooListToPoint2fArray(List<IGH_Goo> list)
        {
            int count = list.Count;
            if (count < 1 || list[0] == null)
                return null;

            Point2d point;
            if (list[0].CastTo(out point))
            {
                Point2f[] vec2_array = new Point2f[count];
                for (int i = 0; i < count; i++)
                {
                    if (list[i].CastTo(out point))
                    {
                        float x = (float)point.X;
                        float y = (float)point.Y;
                        vec2_array[i] = new Point2f(x, y);
                    }
                }
                return vec2_array;
            }

            Vector2d vector;
            if (list[0].CastTo(out vector))
            {
                Point2f[] vec2_array = new Point2f[count];
                for (int i = 0; i < count; i++)
                {
                    if (list[i].CastTo(out vector))
                    {
                        float x = (float)vector.X;
                        float y = (float)vector.Y;
                        vec2_array[i] = new Point2f(x, y);
                    }
                }
                return vec2_array;
            }
            return null;
        }

        static Point3f[] GooListToPoint3fArray(List<IGH_Goo> list)
        {
            int count = list.Count;
            if (count < 1 || list[0]==null)
                return null;

            Point3d point;
            if( list[0].CastTo(out point) )
            {
                Point3f[] vec3_array = new Point3f[count];
                for( int i=0; i<count; i++ )
                {
                    if (list[i].CastTo(out point))
                    {
                        float x = (float)point.X;
                        float y = (float)point.Y;
                        float z = (float)point.Z;
                        vec3_array[i] = new Point3f(x, y, z);
                    }
                }
                return vec3_array;
            }

            Vector3d vector;
            if( list[0].CastTo(out vector) )
            {
                Point3f[] vec3_array = new Point3f[count];
                for (int i = 0; i < count; i++)
                {
                    if (list[i].CastTo(out vector))
                    {
                        float x = (float)vector.X;
                        float y = (float)vector.Y;
                        float z = (float)vector.Z;
                        vec3_array[i] = new Point3f(x, y, z);
                    }
                }
                return vec3_array;
            }
            return null;
        }

        protected void ReportErrors(string errorMessage)
        {
            if (string.IsNullOrWhiteSpace(errorMessage))
                return;
            foreach (var line in errorMessage.Split('\n'))
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, line.Trim());
        }

        class GlShaderComponentAttributes : GH_ComponentAttributes
        {
            readonly GLShaderComponentBase _component;
            readonly Action _doubleClickAction;
            public GlShaderComponentAttributes(GLShaderComponentBase component, Action doubleClickAction)
              : base(component)
            {
                _component = component;
                _doubleClickAction = doubleClickAction;
            }

            public override GH_ObjectResponse RespondToMouseDoubleClick(GH_Canvas sender, GH_CanvasMouseEvent e)
            {
                _doubleClickAction();
                return base.RespondToMouseDoubleClick(sender, e);
            }

            public override void SetupTooltip(PointF point, GH_TooltipDisplayEventArgs e)
            {
                // Allow the base class to set up the tooltip.
                // It will handle those cases where the mouse is over a state icon.
                base.SetupTooltip(point, e);

                try
                {
                    using (var colorBuffer = PerFrameCache.GetTextureImage(_component, true))
                    using (var depthBuffer = PerFrameCache.GetTextureImage(_component, false))
                    {
                        if( colorBuffer!=null && depthBuffer!=null )
                        {
                            var size = colorBuffer.Size;
                            size.Width /= 2;
                            var bmp = new System.Drawing.Bitmap(size.Width, size.Height);
                            using (var g = System.Drawing.Graphics.FromImage(bmp))
                            {
                                g.DrawImage(colorBuffer, Rectangle.FromLTRB(0, 0, size.Width, size.Height / 2));
                                g.DrawImage(depthBuffer, Rectangle.FromLTRB(0, size.Height / 2, size.Width, size.Height));
                            }
                            e.Description = "Output Color/Depth Buffers";
                            e.Diagram = bmp;
                            //e.Diagram = GH_IconTable.ResizeImage(colorBuffer, new Size(300, 200),
                            //System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic,
                            //System.Drawing.Imaging.PixelFormat.Format24bppRgb);
                        }
                    }
                }
                catch { /* no action required. */ }
            }
        }

        public override void CreateAttributes()
        {
            Attributes = new GlShaderComponentAttributes(this, OpenEditor);
        }

        void AppendModeHelper(System.Windows.Forms.ToolStripMenuItem parent, string name, uint mode)
        {
            var tsi_sub = new System.Windows.Forms.ToolStripMenuItem(name, null, (s, e) => {
                _model.DrawMode = mode;
                RedrawViewportControl();
            });
            tsi_sub.Checked = (_model.DrawMode == mode);
            parent.DropDown.Items.Add(tsi_sub);
        }

        public override void AppendAdditionalMenuItems(System.Windows.Forms.ToolStripDropDown menu)
        {
            base.AppendAdditionalMenuItems(menu);

            var tsi = new System.Windows.Forms.ToolStripMenuItem("&Edit code...", null, (sender, e) => { OpenEditor(); });
            tsi.Font = new System.Drawing.Font(tsi.Font, System.Drawing.FontStyle.Bold);
            menu.Items.Add(tsi);

            tsi = new System.Windows.Forms.ToolStripMenuItem("Sort Order");
            for(int i=1; i<=10; i++)
            {
                int order = i;
                var tsi_sub = new System.Windows.Forms.ToolStripMenuItem(i.ToString(), null, (s, e) => {
                    PreviewSortOrder = order;
                    SortComponents();
                    var doc = Rhino.RhinoDoc.ActiveDoc;
                    if (doc != null)
                        doc.Views.Redraw();
                });
                tsi_sub.Checked = (PreviewSortOrder == order);
                tsi.DropDown.Items.Add(tsi_sub);
            }
            menu.Items.Add(tsi);

            tsi = new System.Windows.Forms.ToolStripMenuItem("Draw Mode");
            bool isGenericGLComponent = this is GLShaderComponent;
            AppendModeHelper(tsi, "GL_POINTS", OpenGL.GL_POINTS);
            AppendModeHelper(tsi, "GL_LINES", OpenGL.GL_LINES);
            if (isGenericGLComponent)
            {
                AppendModeHelper(tsi, "GL_LINE_LOOP", OpenGL.GL_LINE_LOOP);
                AppendModeHelper(tsi, "GL_LINE_STRIP", OpenGL.GL_LINE_STRIP);
            }
            AppendModeHelper(tsi, "GL_TRIANGLES", OpenGL.GL_TRIANGLES);
            if (isGenericGLComponent)
            {
                AppendModeHelper(tsi, "GL_TRIANGLE_STRIP", OpenGL.GL_TRIANGLE_STRIP);
                AppendModeHelper(tsi, "GL_TRIANGLE_FAN", OpenGL.GL_TRIANGLE_FAN);
                // The following are deprecated in core profile. I don't think we should add support for them
                //AppendModeHelper(tsi, "GL_QUADS", OpenGL.GL_QUADS);
                //AppendModeHelper(tsi, "GL_QUAD_STRIP", OpenGL.GL_QUAD_STRIP);
                //AppendModeHelper(tsi, "GL_POLYGON", OpenGL.GL_POLYGON);
                AppendModeHelper(tsi, "GL_LINES_ADJACENCY", OpenGL.GL_LINES_ADJACENCY);
                AppendModeHelper(tsi, "GL_LINE_STRIP_ADJACENCY", OpenGL.GL_LINE_STRIP_ADJACENCY);
                AppendModeHelper(tsi, "GL_TRIANGLES_ADJACENCY", OpenGL.GL_TRIANGLES_ADJACENCY);
                AppendModeHelper(tsi, "GL_TRIANGLE_STRIP_ADJACENCY", OpenGL.GL_TRIANGLE_STRIP_ADJACENCY);
                // Not yet, this may require a completely different component
                //AppendModeHelper(tsi, "GL_PATCHES", OpenGL.GL_PATCHES);
            }
            menu.Items.Add(tsi);

            tsi = new System.Windows.Forms.ToolStripMenuItem("glLineWidth");
            Menu_AppendTextItem(tsi.DropDown, $"{_model.glLineWidth:F2}", (s, e) => MenuKeyDown(s, e, true), Menu_SingleDoubleValueTextChanged, true, 200, true);
            menu.Items.Add(tsi);
            tsi = new System.Windows.Forms.ToolStripMenuItem("glPointSize");
            Menu_AppendTextItem(tsi.DropDown, $"{_model.glPointSize:F2}", (s, e) => MenuKeyDown(s, e, false), Menu_SingleDoubleValueTextChanged, true, 200, true);
            menu.Items.Add(tsi);

            tsi = new System.Windows.Forms.ToolStripMenuItem("Depth Testing", null, (sender, e) =>
            {
                _model.DepthTestingEnabled = !_model.DepthTestingEnabled;
            });
            tsi.Checked = _model.DepthTestingEnabled;
            menu.Items.Add(tsi);

            tsi = new System.Windows.Forms.ToolStripMenuItem("Depth Writing", null, (sender, e) =>
            {
                _model.DepthWritingEnabled = !_model.DepthWritingEnabled;
            });
            tsi.Checked = _model.DepthWritingEnabled;
            menu.Items.Add(tsi);

            tsi = new System.Windows.Forms.ToolStripMenuItem("Export...", null, (sender, e) => { Export(); });
            menu.Items.Add(tsi);
        }

        void Menu_SingleDoubleValueTextChanged(GH_MenuTextBox sender, string text)
        {
            if ((text.Length == 0))
            {
                sender.TextBoxItem.ForeColor = System.Drawing.SystemColors.WindowText;
            }
            else
            {
                double d;
                if ((GH_Convert.ToDouble(text, out d, GH_Conversion.Secondary) && d > 0))
                    sender.TextBoxItem.ForeColor = System.Drawing.SystemColors.WindowText;
                else
                    sender.TextBoxItem.ForeColor = System.Drawing.Color.Red;
            }
        }

        void MenuKeyDown(GH_MenuTextBox sender, System.Windows.Forms.KeyEventArgs e, bool lineWidth)
        {
            switch (e.KeyCode)
            {
                case System.Windows.Forms.Keys.Enter:
                    string text = sender.Text;
                    e.Handled = true;
                    double val;
                    if ((GH_Convert.ToDouble(text, out val, GH_Conversion.Secondary)) && val > 0)
                    {
                        if (lineWidth)
                            _model.glLineWidth = val;
                        else
                            _model.glPointSize = val;
                        ExpirePreview(true);
                    }
                    break;
                case System.Windows.Forms.Keys.Escape:
                    sender.CloseEntireMenuStructure();
                    break;
            }
            RedrawViewportControl();
        }

        void OpenEditor()
        {
            string savedVS = _model.VertexShaderCode;
            string savedGS = _model.GeometryShaderCode;
            string savedTC = _model.TessellationControlCode;
            string savedTE = _model.TessellationEvalualtionCode;
            string savedFS = _model.FragmentShaderCode;
            string savedXfrmFeedbackVertex = _model.TransformFeedbackShaderCode;

            var dlg = new GLSLEditorDialog(_model, false, NickName);
            var parent = Rhino.UI.Runtime.PlatformServiceProvider.Service.GetEtoWindow(Grasshopper.Instances.DocumentEditor.Handle);
            _model.Modified = false;
            dlg.Closed += (s, e) =>
            {
                if (!dlg.Canceled)
                {
                    if (_model.Modified)
                    {
                        var doc = OnPingDocument();
                        doc?.Modified();
                    }
                }
                else
                {
                    _model.VertexShaderCode = savedVS;
                    _model.GeometryShaderCode = savedGS;
                    _model.FragmentShaderCode = savedFS;
                    _model.TessellationControlCode = savedTC;
                    _model.TessellationEvalualtionCode = savedTE;
                    _model.TransformFeedbackShaderCode = savedXfrmFeedbackVertex;
                }
                _model.Modified = false;
                //recompile shader if necessary
                if (_model.ProgramId == 0)
                    ExpireSolution(true);
            };
            dlg.Owner = parent;
            dlg.Show();
        }

        void Export()
        {
            var saveDlg = new Eto.Forms.SaveFileDialog();
            saveDlg.Filters.Add(new Eto.Forms.FileFilter("HTML file", new string[] { "html" }));
            var parent = Rhino.UI.Runtime.PlatformServiceProvider.Service.GetEtoWindow(Grasshopper.Instances.DocumentEditor.Handle);
            var ghDocPath = OnPingDocument()?.FilePath;
            if( !string.IsNullOrWhiteSpace(ghDocPath))
            {
                string path = System.IO.Path.GetDirectoryName(ghDocPath);
                string filename = System.IO.Path.GetFileNameWithoutExtension(ghDocPath);
                path = System.IO.Path.Combine(path, filename + ".html");
                saveDlg.FileName = path;
            }
            if (saveDlg.ShowDialog(parent) == Eto.Forms.DialogResult.Ok)
            {
                _model.ExportToHtml(saveDlg.FileName, this);
            }
        }

        public static void UpdateContext(DrawEventArgs args)
        {
            _hglrc = OpenGL.wglGetCurrentContext();
            _viewSerialNumber = args.Display.Viewport.ParentView.RuntimeSerialNumber;
        }

        static bool _drawViewportWiresCalled = false;
        static bool _conduitEnabled = false;
        static void PostDrawObjects(object sender, DrawEventArgs args)
        {
            if (!_drawViewportWiresCalled)
                return;
            _drawViewportWiresCalled = false;

            if (_componentsNeedSorting)
                SortComponents();


            if (!OpenGL.Initialized)
                OpenGL.Initialize();
            if (!OpenGL.IsAvailable)
                return;
            UpdateContext(args);

            using (IDisposable lifetimeObject = PerFrameCache.BeginFrame(args.Display, _activeShaderComponents))
            {
                foreach (var component in _activeShaderComponents)
                {
                    if (component.Hidden)
                        continue;

                    if (!GLSLEditorDialog.EditorsOpen && !AnimationTimerEnabled)
                    {
                        string dataType;
                        int arrayLength;
                        if (component._model.TryGetUniformType("_time", out dataType, out arrayLength) ||
                            component._model.TryGetUniformType("_date", out dataType, out arrayLength))
                            AnimationTimerEnabled = true;
                    }

                    component._model.Draw(args.Display, component);
                }
            }
            GLRecycleBin.Recycle();
        }

        public override void DrawViewportWires(IGH_PreviewArgs args)
        {
            _drawViewportWiresCalled = true;
        }

        public int PreviewSortOrder { get; set; } = 5;

        static List<GLShaderComponentBase> _activeShaderComponents = new List<GLShaderComponentBase>();
        static bool _componentsNeedSorting = true;

        static void AddToActiveComponentList(GLShaderComponentBase comp)
        {
            AnimationTimerEnabled = false;
            foreach(var component in _activeShaderComponents)
            {
                if (comp == component)
                    return;
            }
            _activeShaderComponents.Add(comp);
            SortComponents();
        }
        static void SortComponents()
        {
            _componentsNeedSorting = false;
            _activeShaderComponents.Sort((x, y) => {
                var xUpstream = x.RequiredUpstreamComponents();
                var yUpstream = y.RequiredUpstreamComponents();

                foreach(var component in xUpstream)
                {
                    // if x depends on y, then y must be drawn first
                    if (component == y)
                        return 1;
                }
                foreach(var component in yUpstream)
                {
                    // if y depends on x, then x must be drawn first
                    if (component == x)
                        return -1;
                }

                if (x.PreviewSortOrder < y.PreviewSortOrder)
                    return -1;
                if (x.PreviewSortOrder > y.PreviewSortOrder)
                    return 1;
                return 0;
            });
        }

        /// <summary>
        /// If this component uses texture inputs that are the result of other
        /// components, then those other components must be drawn before this
        /// component (they are "upstream")
        /// </summary>
        /// <returns></returns>
        HashSet<GLShaderComponentBase> RequiredUpstreamComponents()
        {
            var upstream = new HashSet<GLShaderComponentBase>();

            // Just assume that all sampler inputs are the same across iterations.
            var uniforms = _model.GetUniformsAndAttributes(0);
            var samplers = uniforms.GetComponentSamplers();
            HashSet<Guid> ids = new HashSet<Guid>();
            foreach(var sampler in samplers)
            {
                string id = sampler.Substring(0, sampler.IndexOf(":"));
                if (Guid.TryParse(id, out Guid componentId))
                    ids.Add(componentId);
            }

            foreach(var id in ids)
            {
                foreach(var component in _activeShaderComponents)
                {
                    if (component == this)
                        continue;
                    if (component.InstanceGuid == id)
                        upstream.Add(component);
                }
            }
            return upstream;
        }

        public override void DocumentContextChanged(GH_Document document, GH_DocumentContext context)
        {
            if (context == GH_DocumentContext.Unloaded || context == GH_DocumentContext.Close)
            {
                _activeShaderComponents.Clear();
            }
            if (context == GH_DocumentContext.Loaded)
            {
                AddToActiveComponentList(this);
            }
            base.DocumentContextChanged(document, context);
        }
        public override void MovedBetweenDocuments(GH_Document oldDocument, GH_Document newDocument)
        {
            base.MovedBetweenDocuments(oldDocument, newDocument);
        }
        public override void AddedToDocument(GH_Document document)
        {
            AddToActiveComponentList(this);
            base.AddedToDocument(document);
        }
        public override void RemovedFromDocument(GH_Document document)
        {
            _activeShaderComponents.Remove(this);
            base.RemovedFromDocument(document);
        }

        public IGH_Param CreateParameter(GH_ParameterSide side, int index)
        {
            if (side == GH_ParameterSide.Input)
                return new Param_ScriptVariable
                {
                    NickName = GH_ComponentParamServer.InventUniqueNickname("xyzuvwst", Params.Input),
                    Name = NickName,
                    Description = "Script variable " + NickName,
                    Access = GH_ParamAccess.list
                };
            return null;
        }

        bool IGH_VariableParameterComponent.DestroyParameter(GH_ParameterSide side, int index)
        {
            return true;
        }

        public virtual bool CanInsertParameter(GH_ParameterSide side, int index)
        {
            return side == GH_ParameterSide.Input;
        }

        bool IGH_VariableParameterComponent.CanRemoveParameter(GH_ParameterSide side, int index)
        {
            return (this as IGH_VariableParameterComponent).CanInsertParameter(side, index);
        }

        void IGH_VariableParameterComponent.VariableParameterMaintenance()
        {
            foreach (IGH_Param param in Params.Input)
                param.Name = param.NickName;
        }
    }
}
