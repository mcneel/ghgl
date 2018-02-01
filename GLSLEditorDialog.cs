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
            tabarea.Pages.Add(new TabPage() { Text = "Vertex Shader", Content = _vertexShaderControl });
            _geometryShaderControl = new ScriptEditorControl();
            _geometryShaderControl.Text = model.GeometryShaderCode;
            _geometryShaderControl.Language = ScriptEditorLanguage.GLSL;
            tabarea.Pages.Add(new TabPage() { Text = "Geometry Shader", Content = _geometryShaderControl });
            _fragmentShaderControl = new ScriptEditorControl();
            _fragmentShaderControl.Text = model.FragmentShaderCode;
            _fragmentShaderControl.Language = ScriptEditorLanguage.GLSL;
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

        void SaveGLSL()
        { }

    }

}
