using System;
using System.ComponentModel;
using Eto.Forms;
using CodeEditor;

namespace ghgl
{

    class GLSLEditorDialog : Dialog<bool>
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

        ShaderEditorControl _vertexShaderControl;
        ShaderEditorControl _fragmentShaderControl;
        ShaderEditorControl _geometryShaderControl;
        ListBox _errorList;

        protected override void OnClosing(CancelEventArgs e)
        {
            if( !e.Cancel )
            {
                var model = DataContext as GLSLViewModel;
                model.VertexShaderCode = _vertexShaderControl.Text;
                model.GeometryShaderCode = _geometryShaderControl.Text;
                model.FragmentShaderCode = _fragmentShaderControl.Text;
            }
            base.OnClosing(e);
        }

        public GLSLEditorDialog(GLSLViewModel model)
        {
            DataContext = model;
            Title = "GLSL Shader";
            Menu = new MenuBar
            {
                Items = {
                    new ButtonMenuItem
                    {
                      Text = "&File",
                      Items = {new SimpleCommand("&Save", SaveGLSL) }
                    },
                }
            };
            Resizable = true;
            Size = new Eto.Drawing.Size(600, 600);

            DefaultButton = new Button() { Text = "OK", Command = new SimpleCommand("OK", () => Close(true)) };
            AbortButton = new Button() { Text = "Cancel", Command = new SimpleCommand("Cancel", () => Close(false)) };
            var button_stack = new StackLayout
            {
                Padding = 5,
                Orientation = Orientation.Horizontal,
                Items = { null, DefaultButton, AbortButton }
            };

            var tabarea = new TabControl();
            _vertexShaderControl = new ShaderEditorControl(ShaderType.Vertex, model);
            _vertexShaderControl.ShaderCompiled += OnShadersCompiled;
            tabarea.Pages.Add(new TabPage() { Text = _vertexShaderControl.Title, Content = _vertexShaderControl });

            _geometryShaderControl = new ShaderEditorControl(ShaderType.Geometry, model);
            _geometryShaderControl.ShaderCompiled += OnShadersCompiled;
            tabarea.Pages.Add(new TabPage() { Text = _geometryShaderControl.Title, Content = _geometryShaderControl });

            _fragmentShaderControl = new ShaderEditorControl(ShaderType.Fragment, model);
            _fragmentShaderControl.ShaderCompiled += OnShadersCompiled;
            tabarea.Pages.Add(new TabPage() { Text = _fragmentShaderControl.Title, Content = _fragmentShaderControl });

            _errorList = new ListBox();
            _errorList.Height = 40;

            Content = new StackLayout
            {
                Padding = new Eto.Drawing.Padding(5),
                Orientation = Orientation.Vertical,
                Spacing = 5,
                Items = {
                    new StackLayoutItem(tabarea, HorizontalAlignment.Stretch, true),
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

    }

}
