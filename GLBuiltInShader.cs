using System;
using GH_IO.Serialization;
using Grasshopper.GUI;
using Grasshopper.GUI.Canvas;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Attributes;
using Grasshopper.Kernel.Parameters;
using Grasshopper.Kernel.Types;
using System.Runtime.InteropServices;
using Rhino.Runtime.InteropWrappers;

namespace ghgl
{
    [Guid("2DAA8A06-B6E1-4A19-AD41-86E3C3F8617F")]
    public class GLBuiltInShader : GH_Component
    {
        GLSLViewModel _model = new GLSLViewModel();
        string _resourceName = "";
        string _defines = "";

        public GLBuiltInShader()
            : base("GL BuiltIn Shader", "GL BuiltIn", "Update internal Rhino Shader", "Display", "Preview")
        {
        }

        public override Guid ComponentGuid => GetType().GUID;
        public override GH_Exposure Exposure => GH_Exposure.hidden;
        
        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Resource", "R", "Resource Name", GH_ParamAccess.item);
            pManager.AddTextParameter("Defines", "D", "defines", GH_ParamAccess.item, "");
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
                writer.SetString("ResourceName", _resourceName);
                writer.SetString("Defines", _defines);

            }
            return rc;
        }

        public override bool Read(GH_IReader reader)
        {
            bool rc = base.Read(reader) &&
              _model.Read(reader);
            reader.TryGetString("ResourceName", ref _resourceName);
            reader.TryGetString("Defines", ref _defines);
            return rc;
        }

        bool _firstTime = true;

        protected override void SolveInstance(IGH_DataAccess data)
        {
            string resourceName = "";
            string defines = "";
            data.GetData(0, ref resourceName);
            data.GetData(1, ref defines);
            defines = defines.Replace("\\n", "\n");
            if (!resourceName.Equals(_resourceName) || !defines.Equals(_defines))
            {
                _resourceName = resourceName;
                _defines = defines;
                _model = new GLSLViewModel();
            }
            if( _firstTime && !string.IsNullOrWhiteSpace(_model.VertexShaderCode) && !string.IsNullOrWhiteSpace(_resourceName))
            {
                if(_model.CompileProgram())
                {
                    if (_model.ProgramId != 0)
                    {
                        RHC_UpdateShader(_resourceName, _defines, _model.ProgramId);
                        _model.RecycleCurrentProgram = false;

                        var doc = Rhino.RhinoDoc.ActiveDoc;
                        if (doc != null)
                            doc.Views.Redraw();
                        GLShaderComponentBase.RedrawViewportControl();
                    }
                }
                
            }
            _firstTime = false;
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

        public override void AppendAdditionalMenuItems(System.Windows.Forms.ToolStripDropDown menu)
        {
            base.AppendAdditionalMenuItems(menu);

            var tsi = new System.Windows.Forms.ToolStripMenuItem("&Edit code...", null, (sender, e) =>
            {
                OpenEditor();
            });
            tsi.Font = new System.Drawing.Font(tsi.Font, System.Drawing.FontStyle.Bold);
            menu.Items.Add(tsi);
            menu.Items.Add(new System.Windows.Forms.ToolStripMenuItem("Reset", null, (sender, e) =>
            {
                if( Rhino.UI.Dialogs.ShowMessage("Reset the code to what is built-in?", "reset", Rhino.UI.ShowMessageButton.OKCancel, Rhino.UI.ShowMessageIcon.Question) == Rhino.UI.ShowMessageResult.OK)
                {
                    _model = new GLSLViewModel();
                }
            })
            );
        }

        void OpenEditor()
        {
            if (string.IsNullOrWhiteSpace(_resourceName))
                return;
            if( string.IsNullOrWhiteSpace(_model.VertexShaderCode))
            {
                using (var vertex = new StringWrapper())
                using (var tessctrl = new StringWrapper())
                using (var tesseval = new StringWrapper())
                using (var geometry = new StringWrapper())
                using (var fragment = new StringWrapper())
                {
                    IntPtr _vertex = vertex.NonConstPointer;
                    IntPtr _tessctrl = tessctrl.NonConstPointer;
                    IntPtr _tesseval = tesseval.NonConstPointer;
                    IntPtr _geometry = geometry.NonConstPointer;
                    IntPtr _fragment = fragment.NonConstPointer;
                    RHC_GetShaderSource(_resourceName, _defines, _vertex, _tessctrl, _tesseval, _geometry, _fragment);

                    _model.VertexShaderCode = vertex.ToString();
                    _model.TessellationControlCode = tessctrl.ToString();
                    _model.TessellationEvalualtionCode = tesseval.ToString();
                    _model.GeometryShaderCode = geometry.ToString();
                    _model.FragmentShaderCode = fragment.ToString();
                }
            }

            bool animationEnabled = GLShaderComponentBase.AnimationTimerEnabled;
            Rhino.Display.DisplayPipeline.PreDrawObjects += DisplayPipeline_PreDrawObjects;
            string savedVS = _model.VertexShaderCode;
            string savedGS = _model.GeometryShaderCode;
            string savedTC = _model.TessellationControlCode;
            string savedTE = _model.TessellationEvalualtionCode;
            string savedFS = _model.FragmentShaderCode;
            string savedXfrmFeedbackVertex = _model.TransformFeedbackShaderCode;

            bool containsTessShaders = !string.IsNullOrWhiteSpace(_model.TessellationControlCode) ||
                !string.IsNullOrWhiteSpace(_model.TessellationEvalualtionCode);
            var dlg = new GLSLEditorDialog(_model, containsTessShaders);
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
                GLShaderComponentBase.AnimationTimerEnabled = animationEnabled;
                Rhino.Display.DisplayPipeline.PreDrawObjects -= DisplayPipeline_PreDrawObjects;
            };
            dlg.Title += $" ({_resourceName})";
            dlg.Owner = parent;
            dlg.Show();
        }

        private void DisplayPipeline_PreDrawObjects(object sender, Rhino.Display.DrawEventArgs e)
        {
            if( _model.ProgramId != 0 )
            {
                RHC_UpdateShader(_resourceName, _defines, _model.ProgramId);
                _model.RecycleCurrentProgram = false;
            }
        }

        const string lib = "rhcommon_c";
        //void RHC_GetShaderSource(const RHMONO_STRING* resource_name, const RHMONO_STRING* defines,
        //  ON_wString* vertex, ON_wString* tessctrl, ON_wString* tesseval, ON_wString* geometry, ON_wString* fragment)
        // C:\dev\github\mcneel\rhino\src4\DotNetSDK\rhinocommon\c\rh_displaypipeline.cpp line 2552
        [DllImport(lib, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void RHC_GetShaderSource([MarshalAs(UnmanagedType.LPWStr)]string resourceName, [MarshalAs(UnmanagedType.LPWStr)]string defines, IntPtr vertex, IntPtr tessctrl, IntPtr tesseval, IntPtr geometry, IntPtr fragment);

        //void RHC_UpdateShader(const RHMONO_STRING* resourceName, const RHMONO_STRING* defines, unsigned int programId)
        // C:\dev\github\mcneel\rhino\src4\DotNetSDK\rhinocommon\c\rh_displaypipeline.cpp line 2561
        [DllImport(lib, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void RHC_UpdateShader([MarshalAs(UnmanagedType.LPWStr)]string resourceName, [MarshalAs(UnmanagedType.LPWStr)]string defines, uint programId);
    }
}
