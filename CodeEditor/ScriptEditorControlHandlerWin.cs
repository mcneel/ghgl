using System;
using ScintillaNET;

namespace CodeEditor
{
    class CharAddedEventArgsWin : CharAddedEventArgs
    {
    }

    public class ScriptEditorControlHandlerWin : Eto.Wpf.Forms.WpfFrameworkElement<System.Windows.Forms.Integration.WindowsFormsHost, ScriptEditorControl, ScriptEditorControl.ICallback>, ScriptEditorControl.IScriptEditorControlHandler
    {
        Scintilla _control;
        ScriptEditorLanguage _language = ScriptEditorLanguage.None;
        string[] _keywords0;

        public ScriptEditorControlHandlerWin()
        {
            Control = new System.Windows.Forms.Integration.WindowsFormsHost();
            Control.Child = _control = new ScintillaNET.Scintilla();
            _control.CharAdded += OnCharAdded;
            
            SetupTheme();
        }

        private void OnCharAdded(object sender, ScintillaNET.CharAddedEventArgs e)
        {
            CharAdded?.Invoke(this, new CharAddedEventArgsWin());
            /*
            if (_control.AutoCActive && _keywords0!=null)
                return;
            var currentPos = _control.CurrentPosition;
            var wordStartPos = _control.WordStartPosition(currentPos, true);
            var lenEntered = currentPos - wordStartPos;
            if (lenEntered <= 0)
                return;

            if (lenEntered > 0)
            {
                string word = _control.GetTextRange(wordStartPos, lenEntered);
                string items = "";
                foreach(var kw in _keywords0)
                {
                    int startIndex = 0;
                    bool add = true;
                    foreach(var c in word)
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
                if( items.Length>0 )
                  _control.AutoCShow(lenEntered, items);
            }
            */
        }

        public override void Focus()
        {
            _control.Focus();
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

        public ScriptEditorLanguage Language
        {
            get { return _language; }
            set
            {
                if(_language != value )
                {
                    _language = value;
                    SetupLanguageSpecificStyles();
                }
            }
        }

        public bool AutoCActive { get { return _control.AutoCActive; } }
        public int CurrentPosition { get { return _control.CurrentPosition; } }
        public int WordStartPosition(int position, bool onlyWordCharacters)
        {
            return _control.WordStartPosition(position, onlyWordCharacters);
        }
        public string GetTextRange(int position, int length)
        {
            return _control.GetTextRange(position, length);
        }
        public void AutoCShow(int lenEntered, string list)
        {
            _control.AutoCShow(lenEntered, list);
        }
        public event EventHandler<CodeEditor.CharAddedEventArgs> CharAdded;

        void SetupLanguageSpecificStyles()
        {
            switch (Language)
            {
                case ScriptEditorLanguage.GLSL:
                    SetupGlslStyle();
                    break;
                default:
                    break;
            }
        }

        void SetupGlslStyle()
        {
            _control.Lexer = Lexer.Cpp;

            string kw0 = "attribute layout uniform float int bool vec2 vec3 vec4 " +
                "mat4 in out sampler2D if else return void flat discard";
            _keywords0 = kw0.Split(new char[] { ' ' });
            Array.Sort(_keywords0);

            _control.SetKeywords(0, kw0);
            _control.SetKeywords(1, "length sin cos main texture "+
                "_worldToClip _viewportSize _worldToCamera _cameraToClip");

            _control.Styles[Style.Cpp.Comment].ForeColor = System.Drawing.Color.Gray;
            _control.Styles[Style.Cpp.CommentLine].ForeColor = System.Drawing.Color.Gray;
            _control.Styles[Style.Cpp.CommentDoc].ForeColor = System.Drawing.Color.Gray;
            _control.Styles[Style.Cpp.Number].ForeColor = System.Drawing.Color.Black;
            _control.Styles[Style.Cpp.String].ForeColor = System.Drawing.Color.Red;
            _control.Styles[Style.Cpp.Character].ForeColor = System.Drawing.Color.Black;
            _control.Styles[Style.Cpp.Preprocessor].ForeColor = System.Drawing.Color.Black;
            _control.Styles[Style.Cpp.Operator].ForeColor = System.Drawing.Color.Black;
            _control.Styles[Style.Cpp.Regex].ForeColor = System.Drawing.Color.Black;
            _control.Styles[Style.Cpp.CommentLineDoc].ForeColor = System.Drawing.Color.Black;
            _control.Styles[Style.Cpp.Word].ForeColor = System.Drawing.Color.Blue;
            _control.Styles[Style.Cpp.Word2].ForeColor = System.Drawing.Color.CadetBlue;

        }

        void SetupTheme()
        {
            _control.Styles[Style.Default].Font = "Consolas";
            _control.Styles[Style.Default].Size = 10;
            // Show line numbers
            _control.Margins[0].Width = 16;

            _control.Styles[Style.LineNumber].BackColor = System.Drawing.Color.White;
            _control.Styles[Style.LineNumber].ForeColor = System.Drawing.Color.CadetBlue;

            // Instruct the lexer to calculate folding
            _control.SetProperty("fold", "1");
            _control.SetProperty("fold.compact", "1");

            // Configure a margin to display folding symbols
            _control.Margins[1].Type = MarginType.Symbol;
            _control.Margins[1].Mask = Marker.MaskFolders;
            _control.Margins[1].Sensitive = true;
            _control.Margins[1].Width = 20;
//            control.Margins[1].BackColor = System.Drawing.Color.White;

            // Set colors for all folding markers
            for (int i = 25; i <= 31; i++)
            {
                _control.Markers[i].SetForeColor(System.Drawing.Color.WhiteSmoke);
                _control.Markers[i].SetBackColor(System.Drawing.Color.Gray);
            }

            // Configure folding markers with respective symbols
            _control.Markers[Marker.Folder].Symbol = MarkerSymbol.BoxPlus;
            _control.Markers[Marker.FolderOpen].Symbol = MarkerSymbol.BoxMinus;
            _control.Markers[Marker.FolderEnd].Symbol = MarkerSymbol.BoxPlusConnected;
            _control.Markers[Marker.FolderMidTail].Symbol = MarkerSymbol.TCorner;
            _control.Markers[Marker.FolderOpenMid].Symbol = MarkerSymbol.BoxMinusConnected;
            _control.Markers[Marker.FolderSub].Symbol = MarkerSymbol.VLine;
            _control.Markers[Marker.FolderTail].Symbol = MarkerSymbol.LCorner;

            // Enable automatic folding
            _control.AutomaticFold = (AutomaticFold.Show | AutomaticFold.Click | AutomaticFold.Change);
        }


    }
}
