using Eto;
using Eto.Forms;

namespace CodeEditor
{
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

        public interface IScriptEditorControlHandler : Control.IHandler
        {
            string Text { get; set; }
            ScriptEditorLanguage Language { get; set; }
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
