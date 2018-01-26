using System;
using Eto.Forms;

namespace ghgl
{

    class GLSLEditorDialog : Dialog<bool>
    {
        class SimpleCommand : Eto.Forms.Command
        {
            public SimpleCommand(string text, Action action)
            {
                MenuText = text;
                Executed += (s, e) => action();
            }
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
            var ta = new TextArea();
            ta.BindDataContext(c => c.Text, (GLSLViewModel m) => m.VertexShaderCode);
            var font = new Eto.Drawing.Font("Courier New", 12);
            ta.Font = font;
            tabarea.Pages.Add(new TabPage() { Text = "Vertex Shader", Content = ta });
            // Add these back in after we get the basics working
            /*
            ta = new TextArea();
            ta.BindDataContext(c => c.Text, (GLSLViewModel m) => m.TessellationControlCode);
            tabarea.Pages.Add(new TabPage() { Text = "Tessellation Control", Content = ta });
            ta = new TextArea();
            ta.BindDataContext(c => c.Text, (GLSLViewModel m) => m.TessellationEvalualtionCode);
            tabarea.Pages.Add(new TabPage() { Text = "Tessellation Eval", Content = ta });
            */
            ta = new TextArea();
            ta.BindDataContext(c => c.Text, (GLSLViewModel m) => m.GeometryShaderCode);
            ta.Font = font;
            tabarea.Pages.Add(new TabPage() { Text = "Geometry Shader", Content = ta });
            ta = new TextArea();
            ta.BindDataContext(c => c.Text, (GLSLViewModel m) => m.FragmentShaderCode);
            ta.Font = font;
            tabarea.Pages.Add(new TabPage() { Text = "Fragment Shader", Content = ta });

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
