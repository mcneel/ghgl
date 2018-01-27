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

            control.SetKeywords(0, "attribute layout uniform float int bool vec2 vec3 vec4 mat4 in out sampler2D if else return void flat discard");
            control.SetKeywords(1, "length sin cos main texture _worldToClip _viewportSize _worldToCamera _cameraToClip");


            // Instruct the lexer to calculate folding
            control.SetProperty("fold", "1");
            control.SetProperty("fold.compact", "1");

            // Configure a margin to display folding symbols
            control.Margins[2].Type = MarginType.Symbol;
            control.Margins[2].Mask = Marker.MaskFolders;
            control.Margins[2].Sensitive = true;
            control.Margins[2].Width = 20;

            // Set colors for all folding markers
            for (int i = 25; i <= 31; i++)
            {
                control.Markers[i].SetForeColor(System.Drawing.SystemColors.ControlLightLight);
                control.Markers[i].SetBackColor(System.Drawing.SystemColors.ControlDark);
            }

            // Configure folding markers with respective symbols
            control.Markers[Marker.Folder].Symbol = MarkerSymbol.BoxPlus;
            control.Markers[Marker.FolderOpen].Symbol = MarkerSymbol.BoxMinus;
            control.Markers[Marker.FolderEnd].Symbol = MarkerSymbol.BoxPlusConnected;
            control.Markers[Marker.FolderMidTail].Symbol = MarkerSymbol.TCorner;
            control.Markers[Marker.FolderOpenMid].Symbol = MarkerSymbol.BoxMinusConnected;
            control.Markers[Marker.FolderSub].Symbol = MarkerSymbol.VLine;
            control.Markers[Marker.FolderTail].Symbol = MarkerSymbol.LCorner;

            // Enable automatic folding
            control.AutomaticFold = (AutomaticFold.Show | AutomaticFold.Click | AutomaticFold.Change);
        }


    }
}
