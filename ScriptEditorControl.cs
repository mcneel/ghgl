using Eto;
using Eto.Forms;

namespace ghgl
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

        public interface IScriptEditorControlHandler : Control.IHandler
        {
            string Text { get; set; }
        }
    }
}
