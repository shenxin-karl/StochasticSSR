Shader "Unlit/Trace" {
    Properties {
    }
    SubShader {
        Tags { "RenderType"="Opaque" "RenderPipeline" = "UniversalPipeline" }
        Name "TracePass"
        LOD     100
        ZWrite  Off 
        Cull    Off
        ZTest   Off 
        ZClip   Off 
        Blend   Off
        Pass {
            HLSLPROGRAM
            #include_with_pragmas "Assets/Settings/EnableHlslDebug.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareNormalsTexture.hlsl"

            #pragma vertex vert
            #pragma fragment frag

            struct VertexIn {
                float4 vertex   : POSITION;
                float2 uv       : TEXCOORD0;
            };

            struct VertexOut {
                float4 vertex : SV_POSITION;
                float2 uv     : TEXCOORD0;
            };

            VertexOut vert(VertexIn vin) {
                VertexOut vout;
                vout.vertex = float4(vin.vertex.xyz, 1.0);
                vout.uv = vin.uv;
                return vout;
            }

            struct PixelOut {
                float4 hitPosAndPdf : SV_TARGET0;
                float  mask         : SV_TARGET1;
            };

            SamplerState gPointClampSampler;

            Texture2D<float> _HizMap;
            float4x4         _MatInvViewProj;

            float GetNdcDepth(float2 uv) {
                float depth = _HizMap.SampleLevel(gPointClampSampler, uv, 0);
                return depth;
            }

            float3 GetViewNormal(float2 uv) {
                float3 worldNormal = SampleSceneNormals(uv);
	            float3 viewNormal = normalize(mul((float3x3)UNITY_MATRIX_V, worldNormal));
                return viewNormal;
            }

            float GetRoughness(float2 uv) {
                float roughness = 1.0 - _CameraNormalsTexture.SampleLevel(sampler_CameraNormalsTexture, uv, 0).a;
                return roughness;
            }

            float3 GetScreenPos(float2 uv, float ndcDepth) {
                float3 screenPos = float3(uv * 2.0 - 1.0, ndcDepth);
                return screenPos;
            }

            float3 GetWorldPos(float3 screenPos) {
                float4 ndcPos = float4(screenPos, 1.0);
                float4 worldPos = mul(UNITY_MATRIX_I_VP, ndcPos);
                worldPos.xyz /= worldPos.w;
                return worldPos.xyz;
            }

            float3 GetViewDir(float3 viewPos) {
                return normalize(viewPos);
            }

            float3 GetViewPos(float3 screenPos) {
                float4 ndcPos = float4(screenPos, 1.0);
                float4 viewPos = mul(UNITY_MATRIX_I_P, ndcPos);
                viewPos.xyz /= viewPos.w;
                return viewPos;
            }

            PixelOut frag (VertexOut pin) {
                float3 viewNormal = GetViewNormal(pin.uv);
                float3 roughness = GetRoughness(pin.uv);
                float ndcDepth = GetNdcDepth(pin.uv);
                float3 screenPos = GetScreenPos(pin.uv, ndcDepth);
                float3 worldPos = GetWorldPos(screenPos);
                float3 viewDir = GetViewDir(worldPos);


                PixelOut pout;
                pout.hitPosAndPdf = float4(worldPos, 1.0);
                pout.mask = roughness; 
                return pout; 
            }
            ENDHLSL
        }
    }
}
