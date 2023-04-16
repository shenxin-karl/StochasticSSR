using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering;

public class PrefilterPass : ScriptableRenderPass {
    private RenderTargetHandle _prefilterMapHandle;
    private readonly SSRSettings _settings;
    private readonly ComputeShader _shader;
    
    public PrefilterPass(SSRSettings settings) {
        _settings = settings;
        _prefilterMapHandle.Init("PrefilterMap");
        _shader = Resources.Load<ComputeShader>("Shaders/Prefilter");
    }

    public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData) {
        RenderTextureDescriptor desc = renderingData.cameraData.cameraTargetDescriptor;
        desc.graphicsFormat = GraphicsFormat.R16G16B16A16_SFloat;
        desc.mipCount = _settings.NumPrefilter;
        desc.useMipMap = true;
        desc.enableRandomWrite = true;
        desc.autoGenerateMips = false;
        desc.width /= 2;
        desc.height /= 2;
        cmd.GetTemporaryRT(_prefilterMapHandle.id, desc);
    }

    public override void OnCameraCleanup(CommandBuffer cmd) {
        cmd.ReleaseTemporaryRT(_prefilterMapHandle.id);
    }

    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData) {
        CommandBuffer cmd = CommandBufferPool.Get();
        using (new ProfilingScope(cmd, new ProfilingSampler("SSRPrefilterPass"))) {
            int width = renderingData.cameraData.cameraTargetDescriptor.width;
            int height = renderingData.cameraData.cameraTargetDescriptor.height;
            int kernel = _shader.FindKernel("CSMain");

            RenderTargetHandle tempHandle = new RenderTargetHandle();
            if (_settings.NumPrefilter > 1) {
                tempHandle.Init("PrefilterTempTexture");
                RenderTextureDescriptor desc = renderingData.cameraData.cameraTargetDescriptor;
                desc.graphicsFormat = GraphicsFormat.R16G16B16A16_SFloat;
                desc.enableRandomWrite = false;
                desc.useMipMap = true;
                desc.autoGenerateMips = false;
                desc.mipCount = _settings.NumPrefilter - 1;
                desc.width /= 2;
                desc.height /= 2;
                cmd.GetTemporaryRT(tempHandle.id, desc);
            }
            
            RenderTargetIdentifier cameraColorTarget = renderingData.cameraData.renderer.cameraColorTarget;
            RenderTargetIdentifier prefilterIdentifier = _prefilterMapHandle.Identifier();
            RenderTargetIdentifier tempIdentifier = tempHandle.Identifier();
            for (int i = 0; i < _settings.NumPrefilter; ++i) {
                width = Math.Max(width / 2, 1);
                height = Math.Max(height / 2, 1);
                if (i == 0) {
                    Vector4 texDimes = new Vector4 {
                        x = renderingData.cameraData.cameraTargetDescriptor.width,
                        y = renderingData.cameraData.cameraTargetDescriptor.height
                    };
                    texDimes.z = 1f / texDimes.x;
                    texDimes.w = 1f / texDimes.y;
                    cmd.SetComputeVectorParam(_shader, "_TexDimes", texDimes);
                    cmd.SetComputeTextureParam(_shader, kernel, "_SourceTex", cameraColorTarget);
                    cmd.SetComputeIntParam(_shader, "_SampleMipLevel", 0);
                } else {
                    Vector4 texDimes = new Vector4 {
                        x = width * 2,
                        y = height * 2,
                    };
                    texDimes.z = 1f / texDimes.x;
                    texDimes.w = 1f / texDimes.y;
                    cmd.SetComputeVectorParam(_shader, "_TexDimes", texDimes);
                    cmd.SetComputeIntParam(_shader, "_SampleMipLevel", i-1);
                    cmd.SetComputeTextureParam(_shader, kernel, "_SourceTex", tempIdentifier);  
                }
                cmd.SetComputeTextureParam(_shader, kernel, "_ResultTex", prefilterIdentifier, i);
                int x = Mathf.CeilToInt(width / 8f);
                int y = Mathf.CeilToInt(height / 8f);
                cmd.DispatchCompute(_shader, kernel, x, y, 1);
                
                if (i != _settings.NumPrefilter - 1) {
                    cmd.CopyTexture(
                        prefilterIdentifier, 0, i, 
                        tempIdentifier, 0, i
                    );
                }
            }

            if (_settings.NumPrefilter > 1) {
                cmd.ReleaseTemporaryRT(tempHandle.id);
            }
        }
        context.ExecuteCommandBuffer(cmd);
        cmd.Clear();
        CommandBufferPool.Release(cmd);
    }
}
