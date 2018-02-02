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

        ScriptEditorControl _vertexShaderControl;
        ScriptEditorControl _fragmentShaderControl;
        ScriptEditorControl _geometryShaderControl;

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
            Size = new Eto.Drawing.Size(600, 500);

            DefaultButton = new Button() { Text = "OK", Command = new SimpleCommand("OK", () => Close(true)) };
            AbortButton = new Button() { Text = "Cancel", Command = new SimpleCommand("Cancel", () => Close(false)) };
            var button_stack = new StackLayout
            {
                Padding = 5,
                Orientation = Orientation.Horizontal,
                Items = { null, DefaultButton, AbortButton }
            };

            var tabarea = new TabControl();
            _vertexShaderControl = new ScriptEditorControl();
            _vertexShaderControl.Text = model.VertexShaderCode;
            _vertexShaderControl.Language = ScriptEditorLanguage.GLSL;
            _vertexShaderControl.CharAdded += (s,e)=>ShaderControlCharAdded(_vertexShaderControl);
            tabarea.Pages.Add(new TabPage() { Text = "Vertex Shader", Content = _vertexShaderControl });
            _geometryShaderControl = new ScriptEditorControl();
            _geometryShaderControl.Text = model.GeometryShaderCode;
            _geometryShaderControl.Language = ScriptEditorLanguage.GLSL;
            _geometryShaderControl.CharAdded += (s,e)=>ShaderControlCharAdded(_geometryShaderControl);
            tabarea.Pages.Add(new TabPage() { Text = "Geometry Shader", Content = _geometryShaderControl });
            _fragmentShaderControl = new ScriptEditorControl();
            _fragmentShaderControl.Text = model.FragmentShaderCode;
            _fragmentShaderControl.Language = ScriptEditorLanguage.GLSL;
            _fragmentShaderControl.CharAdded += (s,e)=>ShaderControlCharAdded(_fragmentShaderControl);
            tabarea.Pages.Add(new TabPage() { Text = "Fragment Shader", Content = _fragmentShaderControl });

            Content = new StackLayout
            {
                Padding = new Eto.Drawing.Padding(5),
                Orientation = Orientation.Vertical,
                Spacing = 5,
                Items = {
            new StackLayoutItem(tabarea, HorizontalAlignment.Stretch, true),
            new StackLayoutItem(button_stack, HorizontalAlignment.Stretch)
          },
            };

        }

        static string[] _keywords;
        static string[] _builtins;
        private void ShaderControlCharAdded(ScriptEditorControl ctrl)
        {
            if (ctrl == null || ctrl.AutoCActive)
                return;

            int currentPos = ctrl.CurrentPosition;
            int wordStartPos = ctrl.WordStartPosition(currentPos, true);
            var lenEntered = currentPos - wordStartPos;
            if (lenEntered <= 0)
                return;

            if (lenEntered > 0)
            {
                string word = ctrl.GetTextRange(wordStartPos, lenEntered);
                string items = "";
                if (_keywords == null)
                {
                    string kw0 = "attribute layout uniform float int bool vec2 vec3 vec4 " +
                        "mat4 in out sampler2D if else return void flat discard";
                    _keywords = kw0.Split(new char[] { ' ' });
                    Array.Sort(_keywords);
                }
                if (_builtins == null)
                {
                    var bis = BuiltIn.GetUniformBuiltIns();
                    _builtins = new string[bis.Count];
                    for (int i = 0; i < bis.Count; i++)
                        _builtins[i] = bis[i].Name;
                    Array.Sort(_builtins);
                }
                string[] list = _keywords;
                if (word.StartsWith("_"))
                    list = _builtins;
                foreach (var kw in list)
                {
                    int startIndex = 0;
                    bool add = true;
                    foreach (var c in word)
                    {
                        startIndex = kw.IndexOf(c, startIndex);
                        if (startIndex < 0)
                        {
                            add = false;
                            break;
                        }
                    }
                    if (add)
                        items += kw + " ";
                }
                items = items.Trim();
                if (items.Length > 0)
                    ctrl.AutoCShow(lenEntered, items);
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
