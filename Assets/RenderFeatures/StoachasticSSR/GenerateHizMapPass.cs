using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering;

class GenerateHizMapPass : ScriptableRenderPass {
    private RenderTargetHandle _hizMapHandle;
    private readonly ComputeShader _copyMapShader;
    private readonly ComputeShader _generateShader;
    private readonly LocalKeyword _useReverseDepth;
    private SSRSettings _settings;

    public GenerateHizMapPass(SSRSettings settings) {
        _settings = settings;
        _hizMapHandle.Init("HizMap");
        _copyMapShader = Resources.Load<ComputeShader>("Shaders/CopyDepthMap");
        _generateShader = Resources.Load<ComputeShader>("Shaders/GenerateMipMap");
        _useReverseDepth = new LocalKeyword(_generateShader, "UNITY_REVERSED_Z");
    }

    public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData) {
        RenderTextureDescriptor desc = renderingData.cameraData.cameraTargetDescriptor;
        desc.useMipMap = true;
        desc.enableRandomWrite = true;
        desc.graphicsFormat = GraphicsFormat.R16_UNorm;
        cmd.GetTemporaryRT(_hizMapHandle.id, desc);
    }
     
    public override void OnCameraCleanup(CommandBuffer cmd) {
        cmd.ReleaseTemporaryRT(_hizMapHandle.id);
    }

    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData) {
        CommandBuffer cmd = CommandBufferPool.Get();
        using (new ProfilingScope(cmd, new ProfilingSampler("GenerateHizMapPass"))) {
            CopyDepthMap(cmd, ref renderingData);
            GenerateHizMipMap(cmd, ref renderingData);
        }
        context.ExecuteCommandBuffer(cmd);
        CommandBufferPool.Release(cmd);
        cmd.Clear();
    }

    private void CopyDepthMap(CommandBuffer cmd, ref RenderingData renderingData) {
        int kernel = _copyMapShader.FindKernel("CSMain");
        RenderTargetIdentifier cameraDepthTarget = renderingData.cameraData.renderer.cameraDepthTarget;
        RenderTextureDescriptor desc = renderingData.cameraData.cameraTargetDescriptor;
        cmd.SetComputeTextureParam(_copyMapShader, kernel, "_SourceTex", cameraDepthTarget);
        cmd.SetComputeTextureParam(_copyMapShader, kernel, "_ResultTex", _hizMapHandle.Identifier());
        int x = Mathf.CeilToInt(desc.width / 8f);
        int y = Mathf.CeilToInt(desc.height / 8f);
        cmd.DispatchCompute(_copyMapShader, kernel, x, y, 1);
    }

    private void GenerateHizMipMap(CommandBuffer cmd, ref RenderingData renderingData) {
        int width = renderingData.cameraData.cameraTargetDescriptor.width;
        int height = renderingData.cameraData.cameraTargetDescriptor.height;
        int kernel = _generateShader.FindKernel("CSMain");
        int mipMapCount = GetMipMapCount(width, height);
        cmd.SetKeyword(_generateShader, _useReverseDepth, SystemInfo.usesReversedZBuffer);
        RenderTargetHandle[] mipMaps = new RenderTargetHandle[mipMapCount];
        for (int i = 1; i < mipMaps.Length-1; ++i) {
            width = Math.Max(width / 2, 1);
            height = Math.Max(height / 2, 1);
            RenderTextureDescriptor desc = renderingData.cameraData.cameraTargetDescriptor;
            desc.graphicsFormat = GraphicsFormat.R16_UNorm;
            desc.width = width;
            desc.height = height;
            mipMaps[i].Init($"TempDepthMipMap_{i}");
            cmd.GetTemporaryRT(mipMaps[i].id, desc);
        }

        width = renderingData.cameraData.cameraTargetDescriptor.width;
        height = renderingData.cameraData.cameraTargetDescriptor.height;
        RenderTargetIdentifier hizMapIdentifier = _hizMapHandle.Identifier();
        RenderTargetIdentifier cameraDepthTarget = renderingData.cameraData.renderer.cameraDepthTarget;
        for (int i = 1; i < mipMapCount; ++i) {
            width = Math.Max(width / 2, 1);
            height = Math.Max(height / 2, 1);
            RenderTargetIdentifier sourceIdentifier = (i == 1) ? cameraDepthTarget : mipMaps[i-1].Identifier();
            cmd.SetComputeTextureParam(_generateShader, kernel, "_SourceTex", sourceIdentifier);
            cmd.SetComputeTextureParam(_generateShader, kernel, "_ResultTex", hizMapIdentifier, i);
            int x = Mathf.CeilToInt(width / 8f);
            int y = Mathf.CeilToInt(height / 8f);
            cmd.DispatchCompute(_generateShader, kernel, x, y, 1);
            if (i < mipMapCount - 1) {
                cmd.CopyTexture(hizMapIdentifier, 0, i, mipMaps[i].Identifier(), 0, 0);
            }
        }

        for (int i = 1; i < mipMaps.Length-1; ++i) {
            cmd.ReleaseTemporaryRT(mipMaps[i].id);
        }
    }

    int GetMipMapCount(int width, int height) {
        int levels = 1;
        while (width > 1 || height > 1) {
            if (width > 1) width /= 2;
            if (height > 1) height /= 2;
            levels++;
        }
        return levels;
    }
}