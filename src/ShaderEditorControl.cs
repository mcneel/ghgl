using System;

namespace ghgl
{
    class ShaderEditorControl : Ed.Eto.Ed
    {
        readonly ShaderType _shaderType;
        readonly GLSLViewModel _model;
        Eto.Forms.UITimer _compileTimer = new Eto.Forms.UITimer();
        bool _updateInTimer = false;

        public ShaderEditorControl(ShaderType type, GLSLViewModel model)
            :base("ghgl")
        {
            MinimapEnabled = true;
            _shaderType = type;
            _model = model;
            SetTextAsync(model.GetCode(type));
            ContentChanged += ShaderEditorControl_ContentChanged;

            MarkErrors();
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
                MarkErrors();
                ShaderCompiled?.Invoke(this, new EventArgs());
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

        private async void ShaderEditorControl_ContentChanged(object sender, Ed.Eto.ContentChangedEventArgs e)
        {
            string text = await GetTextAsync();

            _model.SetCode(_shaderType, text);

            if(_model.GetShader(_shaderType).ShaderId==0)
            {
                _updateInTimer = true;
            }
        }

        async void MarkErrors()
        {
            await ClearDiagnosticsAsync();
            foreach (var error in _model.AllCompileErrors())
            {
                if (error.Shader == null)
                    continue;
                if (error.Shader.ShaderType == _shaderType)
                {
                    //AddErrorIndicator(error.LineNumber, 0);
                    var diagnostics = new System.Collections.Generic.List<(string message, Ed.Eto.DiagnosticSeverity severity, int startLine, int startCharacter, int endLine, int endCharacter)>
                    {
                        (message: "", severity: Ed.Eto.DiagnosticSeverity.Error, error.LineNumber, 0, error.LineNumber, 0)
                    };
                    await AddDiagnosticsAsync(diagnostics);
                }
            }
        }
    }
}
