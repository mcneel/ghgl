using System;
using System.Collections.Generic;
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
    public abstract class GLShaderComponentBase : GH_Component, IGH_VariableParameterComponent
    {
        internal GLSLViewModel _model = new GLSLViewModel();

        static uint _viewSerialNumber;
        static IntPtr _hglrc;
        static bool _initializeCallbackSet;
        static Eto.Forms.UITimer _animationTimer;

        protected GLShaderComponentBase(string name, string nickname, string description)
          : base(name, nickname, description, "Display", "Preview")
        {
            if (!_initializeCallbackSet)
            {
                _initializeCallbackSet = true;
                DisplayPipeline.DrawForeground += DisplayPipeline_DrawForeground;
                var doc = Rhino.RhinoDoc.ActiveDoc;
                doc?.Views.Redraw();
            }
            _model.PropertyChanged += ModelPropertyChanged;
        }

        public static bool AnimationTimerEnabled
        {
            get
            {
                return _animationTimer != null;
            }
            set
            {
                if( value )
                {
                    if (_animationTimer == null)
                    {
                        _animationTimer = new Eto.Forms.UITimer();
                        _animationTimer.Interval = 0.05;
                        _animationTimer.Elapsed += (s, e) =>
                        {
                            var doc = Rhino.RhinoDoc.ActiveDoc;
                            if (doc != null)
                                doc.Views.Redraw();
                        };
                        _animationTimer.Start();
                    }
                }
                else
                {
                    if(_animationTimer!=null)
                    {
                        _animationTimer.Stop();
                        _animationTimer.Dispose();
                        _animationTimer = null;
                    }
                }
            }
        }

        private void ModelPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "DrawMode":
                case "glLineWidth":
                case "glPointSize":
                    ExpirePreview(true);
                    break;
            }
        }

        private void DisplayPipeline_DrawForeground(object sender, DrawEventArgs e)
        {
            DisplayPipeline.DrawForeground -= DisplayPipeline_DrawForeground;
            if (!OpenGL.Initialized)
                OpenGL.Initialize();

            _hglrc = OpenGL.wglGetCurrentContext();
            _viewSerialNumber = e.Display.Viewport.ParentView.RuntimeSerialNumber;
        }

        public override Guid ComponentGuid => GetType().GUID;
        public override GH_Exposure Exposure => GH_Exposure.tertiary;

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
        }

        public override bool Write(GH_IWriter writer)
        {
            bool rc = base.Write(writer);
            if (rc)
            {
                writer.SetVersion("GLShader", 0, 1, 0);
                rc = _model.Write(writer);
            }
            return rc;
        }

        public override bool Read(GH_IReader reader)
        {
            bool rc = base.Read(reader) &&
              _model.Read(reader);
            return rc;
        }

        public static bool ActivateGlContext()
        {
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

        protected override void SolveInstance(IGH_DataAccess data)
        {
            SolveInstanceHelper(data, 0);
        }

        protected void SolveInstanceHelper(IGH_DataAccess data, int startIndex)
        {
            if (!ActivateGlContext())
                return;

            if (!_model.CompileProgram())
            {
                foreach (var err in _model.AllCompileErrors())
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, err.ToString());
                }
            }

            _model.ClearData();

            for (int i = startIndex; i < Params.Input.Count; i++)
            {
                string varname = Params.Input[i].NickName;
                string datatype;
                if (_model.TryGetUniformType(varname, out datatype))
                {
                    IGH_Goo destination = null;
                    if (Params.Input[i].Access == GH_ParamAccess.item)
                        data.GetData(i, ref destination);
                    else
                    {
                        List<IGH_Goo> list = new List<IGH_Goo>();
                        data.GetDataList(i, list);
                        destination = list[0];
                    }
                    switch (datatype)
                    {
                        case "int":
                            {
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
                                _model.AddUniform(varname, value);
                                break;
                            }
                        case "float":
                            {
                                double value;
                                destination.CastTo(out value);
                                if (!destination.CastTo(out value))
                                {
                                    int ival;
                                    if (destination.CastTo(out ival))
                                        value = ival;
                                }
                                _model.AddUniform(varname, (float)value);
                                break;
                            }
                        case "vec3":
                            {
                                Point3d point;
                                if (destination.CastTo(out point))
                                {
                                    float x = (float)point.X;
                                    float y = (float)point.Y;
                                    float z = (float)point.Z;
                                    _model.AddUniform(varname, new Point3f(x, y, z));
                                }
                                break;
                            }
                        case "vec4":
                            {
                                if (destination.TypeName == "Colour")
                                {
                                    System.Drawing.Color color;
                                    if (destination.CastTo(out color))
                                    {
                                        Vec4 v4 = new Vec4(color.R / 255.0f, color.G / 255.0f, color.B / 255.0f, color.A / 255.0f);
                                        _model.AddUniform(varname, v4);
                                    }
                                }
                                break;
                            }
                        case "bool":
                            {
                                bool value;
                                if (!destination.CastTo(out value))
                                {
                                    int ivalue;
                                    if (destination.CastTo(out ivalue))
                                        value = ivalue != 0;
                                }
                                _model.AddUniform(varname, value ? 1 : 0);
                                break;
                            }
                        case "sampler2D":
                            {
                                //Try casting to a string first. This will be interpreted as a path to an image file
                                string path;
                                if (destination.CastTo(out path))
                                {
                                    _model.AddSampler2DUniform(varname, path);
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
                                _model.AddAttribute(varname, location, ints);
                            if (floats != null && floats.Length > 0)
                                _model.AddAttribute(varname, location, floats);
                        }
                        if (destination.Count < 1 && datatype == "int")
                        {
                            List<int> int_destination = new List<int>();
                            if (data.GetDataList(i, int_destination) && int_destination.Count > 0)
                            {
                                int[] ints = int_destination.ToArray();
                                if (ints != null && ints.Length > 0)
                                    _model.AddAttribute(varname, location, ints);
                            }

                        }
                    }
                    if (datatype == "vec3")
                    {
                        //vec3 -> point3d
                        List<Point3d> destination = new List<Point3d>();
                        if (data.GetDataList(i, destination))
                        {
                            Point3f[] vec3_array = new Point3f[destination.Count];
                            for (int index = 0; index < destination.Count; index++)
                            {
                                float x = (float)destination[index].X;
                                float y = (float)destination[index].Y;
                                float z = (float)destination[index].Z;
                                vec3_array[index] = new Point3f(x, y, z);
                            }
                            _model.AddAttribute(varname, location, vec3_array);
                        }
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
                            _model.AddAttribute(varname, location, vec4_array);
                        }
                    }
                }
            }
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
            readonly Action _doubleClickAction;
            public GlShaderComponentAttributes(IGH_Component component, Action doubleClickAction)
              : base(component)
            {
                _doubleClickAction = doubleClickAction;
            }

            public override GH_ObjectResponse RespondToMouseDoubleClick(GH_Canvas sender, GH_CanvasMouseEvent e)
            {
                _doubleClickAction();
                return base.RespondToMouseDoubleClick(sender, e);
            }
        }

        public override void CreateAttributes()
        {
            Attributes = new GlShaderComponentAttributes(this, OpenEditor);
        }

        void AppendModeHelper(System.Windows.Forms.ToolStripMenuItem parent, string name, uint mode)
        {
            var tsi_sub = new System.Windows.Forms.ToolStripMenuItem(name, null, (s, e) => _model.DrawMode = mode);
            tsi_sub.Checked = (_model.DrawMode == mode);
            parent.DropDown.Items.Add(tsi_sub);
        }

        public override void AppendAdditionalMenuItems(System.Windows.Forms.ToolStripDropDown menu)
        {
            base.AppendAdditionalMenuItems(menu);

            var tsi = new System.Windows.Forms.ToolStripMenuItem("&Edit code...", null, (sender, e) => { OpenEditor(); });
            tsi.Font = new System.Drawing.Font(tsi.Font, System.Drawing.FontStyle.Bold);
            menu.Items.Add(tsi);

            tsi = new System.Windows.Forms.ToolStripMenuItem("Animate", null, (sender, e) =>
            {
                AnimationTimerEnabled = !AnimationTimerEnabled;
            });
            tsi.Checked = AnimationTimerEnabled;
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
        }

        void OpenEditor()
        {
            bool animationEnabled = AnimationTimerEnabled;
            string savedVS = _model.VertexShaderCode;
            string savedGS = _model.GeometryShaderCode;
            string savedTC = _model.TessellationControlCode;
            string savedTE = _model.TessellationEvalualtionCode;
            string savedFS = _model.FragmentShaderCode;
            string savedXfrmFeedbackVertex = _model.TransformFeedbackShaderCode;

            var dlg = new GLSLEditorDialog(_model);
            var parent = Rhino.UI.Runtime.PlatformServiceProvider.Service.GetEtoWindow(Grasshopper.Instances.DocumentEditor.Handle);

            if (!dlg.ShowModal(parent))
            {
                _model.VertexShaderCode = savedVS;
                _model.GeometryShaderCode = savedGS;
                _model.FragmentShaderCode = savedFS;
                _model.TessellationControlCode = savedTC;
                _model.TessellationEvalualtionCode = savedTE;
                _model.TransformFeedbackShaderCode = savedXfrmFeedbackVertex;
            }
            //recompile shader if necessary
            if (_model.ProgramId == 0)
                ExpireSolution(true);
            AnimationTimerEnabled = animationEnabled;
        }

        public override void DrawViewportWires(IGH_PreviewArgs args)
        {
            if (!OpenGL.Initialized)
                OpenGL.Initialize();

            _hglrc = OpenGL.wglGetCurrentContext();
            _viewSerialNumber = args.Display.Viewport.ParentView.RuntimeSerialNumber;
            _model.Draw(args.Display);
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
        }
    }
}
