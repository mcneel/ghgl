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

        public ScriptEditorControl() { }
        public ScriptEditorControl(ScriptEditorLanguage language)
        {
            Language = language;
        }

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

        public event EventHandler TextChanged
        {
            add { Handler.TextChanged += value; }
            remove { Handler.TextChanged -= value; }
        }

        public bool AutoCActive { get => Handler.AutoCActive; }
        public int CurrentPosition { get => Handler.CurrentPosition; }
        public void InsertText(int position, string text) { Handler.InsertText(position, text); }
        public int WordStartPosition(int position, bool onlyWordCharacters) { return Handler.WordStartPosition(position, onlyWordCharacters); }
        public string GetTextRange(int position, int length) { return Handler.GetTextRange(position, length); }
        public void AutoCShow(int lenEntered, string list) { Handler.AutoCShow(lenEntered, list); }
        public void ClearErrors() { Handler.ClearErrors(); }
        public void MarkError(int line) { Handler.MarkError(line); }

        public interface IScriptEditorControlHandler : Control.IHandler
        {
            string Text { get; set; }
            ScriptEditorLanguage Language { get; set; }
            event EventHandler<CharAddedEventArgs> CharAdded;
            event EventHandler TextChanged;

            bool AutoCActive { get; }
            int CurrentPosition { get; }
            void InsertText(int position, string text);
            int WordStartPosition(int position, bool onlyWordCharacters);
            string GetTextRange(int position, int length);
            void AutoCShow(int lenEntered, string list);

            void ClearErrors();
            void MarkError(int line);
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
