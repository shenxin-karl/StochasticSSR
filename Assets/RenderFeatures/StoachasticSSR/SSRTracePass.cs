using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering;

public class SSRTracePass : ScriptableRenderPass {
    private RenderTargetHandle _traceMapHandle;
    private RenderTargetHandle _maskMapHandle;
    private Material _material;
    private SSRSettings _settings;

    public SSRTracePass(SSRSettings settings) {
        _settings = settings;
        _traceMapHandle.Init("TrackMap");
        _maskMapHandle.Init("MaskMap");
        _material = new Material(Resources.Load<Shader>("Shaders/Trace"));
    }

    public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData) {
        RenderTextureDescriptor desc = renderingData.cameraData.cameraTargetDescriptor;
        desc.width /= 2;
        desc.height /= 2;
        desc.graphicsFormat = GraphicsFormat.R8G8B8A8_UNorm;
        desc.depthStencilFormat = GraphicsFormat.None;
        cmd.GetTemporaryRT(_traceMapHandle.id, desc);

        desc.graphicsFormat = GraphicsFormat.R8_SNorm;
        cmd.GetTemporaryRT(_maskMapHandle.id, desc);
    }

    public override void OnCameraCleanup(CommandBuffer cmd) {
        cmd.ReleaseTemporaryRT(_traceMapHandle.id);
        cmd.ReleaseTemporaryRT(_traceMapHandle.id);
    }

    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData) {
        CommandBuffer cmd = CommandBufferPool.Get();
        using (new ProfilingScope(cmd, new ProfilingSampler("SSRTracePass"))) {
            RenderTargetIdentifier[] renderTextures = new RenderTargetIdentifier[2];

            int width = renderingData.cameraData.cameraTargetDescriptor.width;
            int height = renderingData.cameraData.cameraTargetDescriptor.height;

            renderTextures[0] = _traceMapHandle.Identifier();
            renderTextures[1] = _maskMapHandle.Identifier();

            Vector2 jitterSample = GenerateRandomOffset();
            _material.SetVector("JitterSizeAndOffset", new Vector4(
                (float)width  / (float)_settings.BlurTexture.width,
                (float)height / (float)_settings.BlurTexture.height,
                jitterSample.x,
                jitterSample.y
            ));

            _material.SetVector("ScreenSize", new Vector4(
                (float)width,
                (float)height,
                1f / (float)width,
                1f / (float)height
            ));

            _material.SetFloat("BRDFBias", _settings.BRDFBias);

            cmd.SetRenderTarget(renderTextures, BuiltinRenderTextureType.None);
            cmd.DrawMesh(RenderingUtils.fullscreenMesh, Matrix4x4.identity, _material);
        }
        context.ExecuteCommandBuffer(cmd);
        cmd.Clear();
        CommandBufferPool.Release(cmd);
    }

    private int _sampleIndex = 0;
    private const int KSampleCount = 64;

    private float GetHaltonValue(int index, int radix)  {
        float result = 0f;
        float fraction = 1f / (float) radix;
        while (index > 0) {
            result += (float) (index % radix) * fraction;
            index /= radix;
            fraction /= (float) radix;
        }
        return result;
    }
    
    private Vector2 GenerateRandomOffset()  {
        Vector2 offset = new Vector2(
            GetHaltonValue(_sampleIndex & 1023, 2),
            GetHaltonValue(_sampleIndex & 1023, 3)
        );
        _sampleIndex = (_sampleIndex + 1) % KSampleCount ;
        return offset;
    }
}
