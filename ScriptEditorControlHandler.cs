using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ScintillaNET;

namespace ghgl
{
    public class ScriptEditorControlHandler : Eto.Wpf.Forms.WpfFrameworkElement<System.Windows.Forms.Integration.WindowsFormsHost, ScriptEditorControl, ScriptEditorControl.ICallback>, ScriptEditorControl.IScriptEditorControlHandler
    {
        ScintillaNET.Scintilla _control;
        public ScriptEditorControlHandler()
        {
            Control = new System.Windows.Forms.Integration.WindowsFormsHost();
            Control.Child = _control = new ScintillaNET.Scintilla();
            SetupScintilla(_control);
        }

        public override Eto.Drawing.Color BackgroundColor
        {
            get => Eto.Drawing.Colors.Transparent;
            set
            {
                throw new NotImplementedException();
            }
        }

        public string Text
        {
            get { return _control.Text; }
            set { _control.Text = value; }
        }

        static void SetupScintilla(Scintilla control)
        {
            // Show line numbers
            control.Margins[0].Width = 16;

            // Configure the CPP (C#) lexer styles
            control.Styles[Style.Cpp.Comment].ForeColor = System.Drawing.Color.Gray;
            control.Styles[Style.Cpp.CommentLine].ForeColor = System.Drawing.Color.Gray;
            control.Styles[Style.Cpp.CommentDoc].ForeColor = System.Drawing.Color.Gray;
            control.Styles[Style.Cpp.Number].ForeColor = System.Drawing.Color.Black;
            control.Styles[Style.Cpp.String].ForeColor = System.Drawing.Color.Red;
            control.Styles[Style.Cpp.Character].ForeColor = System.Drawing.Color.Black;
            control.Styles[Style.Cpp.Preprocessor].ForeColor = System.Drawing.Color.Black;
            control.Styles[Style.Cpp.Operator].ForeColor = System.Drawing.Color.Black;
            control.Styles[Style.Cpp.Regex].ForeColor = System.Drawing.Color.Black;
            control.Styles[Style.Cpp.CommentLineDoc].ForeColor = System.Drawing.Color.Black;
            control.Styles[Style.Cpp.Word].ForeColor = System.Drawing.Color.Blue;
            control.Styles[Style.Cpp.Word2].ForeColor = System.Drawing.Color.CadetBlue;

            control.Lexer = Lexer.Cpp;

            control.SetKeywords(0, "attribute layout uniform float int bool vec2 vec3 vec4 if else");
            control.SetKeywords(1, "length sin cos main");

        }


    }
}
