using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering;

public class SSRTracePass : ScriptableRenderPass {
    private Material _material;
    private RenderTargetHandle _traceMapHandle;
    private RenderTargetHandle _maskMapHandle;

    public SSRTracePass() {
        _material = new Material(Resources.Load<Shader>("Shaders/Trace"));
        _traceMapHandle.Init("TraceMap");
        _maskMapHandle.Init("MaskMap");
    }

    public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData) {
        RenderTextureDescriptor desc = renderingData.cameraData.cameraTargetDescriptor;
        desc.width /= 2;
        desc.height /= 2;
        desc.graphicsFormat = GraphicsFormat.R8G8B8A8_UNorm;
        cmd.GetTemporaryRT(_traceMapHandle.id, desc);
        desc.graphicsFormat = GraphicsFormat.R8_SNorm;
        cmd.GetTemporaryRT(_maskMapHandle.id, desc);
    }

    public override void OnCameraCleanup(CommandBuffer cmd) {
        cmd.ReleaseTemporaryRT(_traceMapHandle.id);
        cmd.ReleaseTemporaryRT(_maskMapHandle.id);
    }

    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData) {

    }
}
