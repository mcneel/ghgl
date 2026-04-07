using System;
using System.Linq;
using System.Threading.Tasks;

namespace ghgl
{
    class ShaderEditorControl : Ed.Eto.Ed
    {
        readonly ShaderType _shaderType;
        readonly GLSLViewModel _model;
        Eto.Forms.UITimer _compileTimer = new Eto.Forms.UITimer();
        bool _updateInTimer = false;

        public ShaderEditorControl(ShaderType type, GLSLViewModel model)
        {
            _ = SetLanguageAsync("glsl");
            _ = SetMinimapAsync(true);
            _shaderType = type;
            _model = model;
            SetTextAsync(model.GetCode(type));
            ContentChanged += ShaderEditorControl_ContentChanged;

            _ = MarkErrors();
            _compileTimer.Elapsed += CompileTimerTick;
            _compileTimer.Interval = 1; //every second
            _compileTimer.Start();
        }

        private void CompileTimerTick(object sender, EventArgs e)
        {
            if (_updateInTimer)
            {
                _updateInTimer = false;
                GLBuiltInShader.ActivateGL();
                _model.CompileProgram();
                GLShaderComponentBase.AnimationTimerEnabled = true;
                ShaderCompiled?.Invoke(this, new EventArgs());
                _ = MarkErrors();
            }
        }

        public event EventHandler ShaderCompiled;

        public ShaderType ShaderType { get => _shaderType; }

        public string Title
        {
            get
            {
                switch (_shaderType)
                {
                    case ShaderType.Vertex: return "Vertex";
                    case ShaderType.Geometry: return "Geometry";
                    case ShaderType.TessellationControl: return "Tessellation Ctrl";
                    case ShaderType.TessellationEval: return "Tessellation Eval";
                    case ShaderType.Fragment: return "Fragment";
                    case ShaderType.TransformFeedbackVertex: return "Transform Feedback Vertex";
                }
                return "";
            }
        }

        private async void ShaderEditorControl_ContentChanged(object sender, Ed.Core.ContentChangedArgs e)
        {
            string text = await GetTextAsync();

            _model.SetCode(_shaderType, text);

            if(_model.GetShader(_shaderType).ShaderId==0)
            {
                _updateInTimer = true;
            }
        }

        async Task MarkErrors()
        {
            // NOTE:
            // disabled this since compiled errors currently do not have line numbers
            // and there is not point sending diags to editor control without position.

            var diags = new System.Collections.Generic.List<(string message, Ed.Core.Models.DiagnosticSeverity severity, int startLine, int startCharacter, int endLine, int endCharacter)>();

            /*
            foreach (CompileError error in _model.AllCompileErrors().Where(e => e.Shader is not null))
            {
                if (error.Shader.ShaderType == _shaderType)
                {
                    diags.Add((message: "", severity: Ed.Core.Models.DiagnosticSeverity.Error, error.LineNumber, 0, error.LineNumber, 0));
                }
            }
            */

            await SetDiagnosticsAsync(diags);
        }
    }
}
