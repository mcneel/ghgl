using System;
using Eto;
using Eto.Forms;

namespace CodeEditor
{
    public abstract class CharAddedEventArgs : EventArgs
    {
    }

    [Handler(typeof(IScriptEditorControlHandler))]
    public class ScriptEditorControl : Control
    {
        new IScriptEditorControlHandler Handler { get { return (IScriptEditorControlHandler)base.Handler; } }

        public string Text
        {
            get => Handler.Text;
            set => Handler.Text = value;
        }

        public ScriptEditorLanguage Language
        {
            get => Handler.Language;
            set => Handler.Language = value;
        }

        public event EventHandler<CharAddedEventArgs> CharAdded
        {
            add { Handler.CharAdded += value; }
            remove { Handler.CharAdded -= value; }
        }

        public bool AutoCActive { get => Handler.AutoCActive; }
        public int CurrentPosition { get => Handler.CurrentPosition; }
        public int WordStartPosition(int position, bool onlyWordCharacters) { return Handler.WordStartPosition(position, onlyWordCharacters); }
        public string GetTextRange(int position, int length) { return Handler.GetTextRange(position, length); }
        public void AutoCShow(int lenEntered, string list) { Handler.AutoCShow(lenEntered, list); }

        public interface IScriptEditorControlHandler : Control.IHandler
        {
            string Text { get; set; }
            ScriptEditorLanguage Language { get; set; }
            event EventHandler<CharAddedEventArgs> CharAdded;

            bool AutoCActive { get; }
            int CurrentPosition { get; }
            int WordStartPosition(int position, bool onlyWordCharacters);
            string GetTextRange(int position, int length);
            void AutoCShow(int lenEntered, string list);
        }
    }

    public enum ScriptEditorLanguage
    {
        None,
        GLSL,
        VBScript,
        VBNET,
        CSharp,
        Python
    }
}
