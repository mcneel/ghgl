using System;
using Eto.Forms;
using CodeEditor;
using System.ComponentModel;

namespace ghgl
{
    class GLSLEditorDialog : Form
    {
        static GLSLEditorDialog()
        {
            Eto.Platform.Instance.Add<ScriptEditorControl.IScriptEditorControlHandler>(() => new ScriptEditorControlHandlerWin());
        }

        class SimpleCommand : Eto.Forms.Command
        {
            public SimpleCommand(string text, Action action)
            {
                MenuText = text;
                Executed += (s, e) => action();
            }
        }

        class EditorPage : INotifyPropertyChanged
        {
            readonly TabControl _tabControl;
            public EditorPage(TabControl tab)
            {
                _tabControl = tab;
            }
            bool _visible;
            public bool Visible
            {
                get { return _visible; }
                set
                {
                    if(_visible!=value)
                    {
                        _visible = value;
                        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Visible"));
                    }
                }
            }
            public ShaderEditorControl Control { get; set; }
            public CheckCommand CheckCommand { get; set; }

            public event PropertyChangedEventHandler PropertyChanged;
        }

        EditorPage[] _shaderControls;
        TabControl _tabarea;
        ListBox _errorList;

        void ShowTab(ShaderType type)
        {
            var sc = _shaderControls[(int)type];
            if (sc.Control == null)
            {
                var model = DataContext as GLSLViewModel;
                sc.Control = new ShaderEditorControl(type, model);
                sc.Control.ShaderCompiled += OnShadersCompiled;
            }

            int index = -1;
            for (int i=0; i<_tabarea.Pages.Count; i++)
            {
                var ctrl = _tabarea.Pages[i].Content as ShaderEditorControl;
                if (ctrl.ShaderType == type)
                    return;
                if((int)ctrl.ShaderType > (int)type)
                {
                    _tabarea.Pages.Insert(i, new TabPage() { Text=sc.Control.Title, Content=sc.Control});
                    index = i;
                    break;
                }
            }
            if (-1 == index)
            {
                _tabarea.Pages.Add(new TabPage() { Text = sc.Control.Title, Content = sc.Control });
                index = _tabarea.Pages.Count - 1;
            }
            _tabarea.SelectedIndex = index;
            sc.Visible = true;
        }

        void HideTab(ShaderType type)
        {
            var sc = _shaderControls[(int)type];
            if (sc.Control == null)
            {
                sc.Visible = false;
                return;
            }

            for (int i = 0; i < _tabarea.Pages.Count; i++)
            {
                var ctrl = _tabarea.Pages[i].Content as ShaderEditorControl;
                if (ctrl.ShaderType == type)
                {
                    _tabarea.Pages.Remove(_tabarea.Pages[i]);
                    sc.Control = null;
                    sc.Visible = false;
                }
            }
        }

        public bool Canceled { get; set; }

        public GLSLEditorDialog(GLSLViewModel model, bool includeTessellationShaders)
        {
            _tabarea = new TabControl();
            _shaderControls = new EditorPage[(int)ShaderType.Fragment + 1];
            var checkCommand = new CheckCommand[_shaderControls.Length];
            for (int i = 0; i < _shaderControls.Length; i++)
            {
                ShaderType st = (ShaderType)i;
                _shaderControls[i] = new EditorPage(_tabarea);
                checkCommand[i] = new CheckCommand();
                checkCommand[i].DataContext = _shaderControls[i];
                checkCommand[i].MenuText = st.ToString()+" Shader";
                checkCommand[i].BindDataContext<bool>("Checked", "Visible");
                int current = i;
                checkCommand[i].CheckedChanged += (s, e) => {
                    if (checkCommand[current].Checked)
                        ShowTab(st);
                    else
                        HideTab(st);
                };
            }


            DataContext = model;
            Title = "GLSL Shader";

            var builtinsMenu = new ButtonMenuItem { Text = "Insert BuiltIn" };
            foreach(var bi in BuiltIn.GetUniformBuiltIns())
            {
                var menuitem = builtinsMenu.Items.Add(new SimpleCommand(bi.Name, ()=>InsertBuiltIn(bi)));
                menuitem.ToolTip = $"({bi.DataType}) {bi.Description}";
            }
            //builtinsMenu.Items.AddSeparator();
            //foreach (var bi in BuiltIn.GetAttributeBuiltIns())
            //{
            //    builtinsMenu.Items.Add(new SimpleCommand(bi.Name, SaveGLSL));
            //}
            Menu = new MenuBar
            {
                Items = {
                    new ButtonMenuItem
                    {
                        Text = "&File",
                        Items = {new SimpleCommand("&Save", SaveGLSL) }
                    },
                    new ButtonMenuItem
                    {
                        Text = "&Edit",
                        Items = {
                            builtinsMenu
                        }
                    },
                    new ButtonMenuItem
                    {
                        Text = "&View",
                        Items =
                        {
                            checkCommand[(int)ShaderType.Vertex],
                            checkCommand[(int)ShaderType.Geometry],
                            checkCommand[(int)ShaderType.Fragment],
                        }
                    }
                }
            };
            Resizable = true;
            Size = new Eto.Drawing.Size(600, 600);

            var DefaultButton = new Button() { Text = "OK", Command = new SimpleCommand("OK", () => Close()) };
            var AbortButton = new Button() { Text = "Cancel", Command = new SimpleCommand("Cancel", () =>
            {
                Canceled = true;
                Close();
            }) };
            var button_stack = new StackLayout
            {
                Padding = 5,
                Orientation = Orientation.Horizontal,
                Items = { null, DefaultButton, AbortButton }
            };

            // Just show the common shaders by default
            ShowTab(ShaderType.Vertex);
            if (includeTessellationShaders)
            {
                ShowTab(ShaderType.TessellationControl);
                ShowTab(ShaderType.TessellationEval);
            }
            ShowTab(ShaderType.Geometry);
            ShowTab(ShaderType.Fragment);
            //ShowTab(ShaderType.TransformFeedbackVertex);
            _tabarea.SelectedIndex = 0;

            _errorList = new ListBox();
            _errorList.Height = 40;

            Content = new StackLayout
            {
                Padding = new Eto.Drawing.Padding(5),
                Orientation = Orientation.Vertical,
                Spacing = 5,
                Items = {
                    new StackLayoutItem(_tabarea, HorizontalAlignment.Stretch, true),
                    new StackLayoutItem(_errorList, HorizontalAlignment.Stretch),
                    new StackLayoutItem(button_stack, HorizontalAlignment.Stretch)
                },
            };
            GLShaderComponentBase.AnimationTimerEnabled = true;
            OnShadersCompiled(null, EventArgs.Empty);
        }

        private void OnShadersCompiled(object sender, EventArgs e)
        {
            GLSLViewModel model = DataContext as GLSLViewModel;
            if (model != null)
            {
                var errors = model.AllCompileErrors();
                _errorList.Items.Clear();
                if(errors.Length==0)
                {
                    _errorList.Items.Add("Compile output (no errors)");
                    _errorList.TextColor = Eto.Drawing.Colors.Gray;
                }
                else
                {
                    _errorList.TextColor = Eto.Drawing.Colors.Red;
                    foreach(var err in errors)
                        _errorList.Items.Add(err.ToString());
                }
            }
        }

        void SaveGLSL()
        {
            var model = DataContext as GLSLViewModel;
            var saveDlg = new SaveFileDialog();
            saveDlg.Filters.Add(new FileFilter("Text file", new string[] { "txt" }));
            if( saveDlg.ShowDialog(this) == DialogResult.Ok )
            {
                model.SaveAs(saveDlg.FileName);
            }
        }

        ShaderEditorControl ActiveEditorControl()
        {
            return _tabarea.SelectedPage.Content as ShaderEditorControl;
        }

        void InsertBuiltIn(BuiltIn b)
        {
            var shaderCtrl = ActiveEditorControl();
            if( shaderCtrl != null )
            {
                string text = $"uniform {b.DataType} {b.Name};";
                shaderCtrl.InsertText(shaderCtrl.CurrentPosition, text);
            }
        }
    }

}
