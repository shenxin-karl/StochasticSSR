using TMPro;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class StochasticSSR : ScriptableRendererFeature {
    private GenerateHizMapPass _generateHizMapPass;
    private PrefilterPass _prefilterPass;
    private SSRTracePass _tracePass;
    private SSRSettings _settings;

    public override void Create() {
        if (_settings == null) {
            _settings = CreateInstance<SSRSettings>(); 
        }

        if (_settings.BlurTexture == null) {
            _settings.BlurTexture = Resources.Load<Texture2D>("Textures/BlueNoise");
        }
        if (_settings.BRDFTexture == null) {
            _settings.BRDFTexture = Resources.Load<Texture2D>("Textures/IBL_BRDF_LUT");
        } 

        _generateHizMapPass = new GenerateHizMapPass(_settings);
        _generateHizMapPass.renderPassEvent = RenderPassEvent.AfterRenderingTransparents;

        _prefilterPass = new PrefilterPass(_settings);
        _prefilterPass.renderPassEvent = (RenderPassEvent)_settings.PassEvent;

        _tracePass = new SSRTracePass(_settings);
        _tracePass.renderPassEvent = _prefilterPass.renderPassEvent + 1;
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData) {
        renderer.EnqueuePass(_generateHizMapPass);
        renderer.EnqueuePass(_prefilterPass);
        renderer.EnqueuePass(_tracePass);
    }
}


