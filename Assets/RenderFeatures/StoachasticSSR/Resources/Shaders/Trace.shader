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
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

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
                vout.vertex = vin.vertex;
                vout.uv = vin.uv;
                return vout;
            }

            struct PixelOut {
                float4 hitPosAndPdf : SV_TARGET0;
                float  mask         : SV_TARGET1;
            };

            PixelOut frag (VertexOut pin) {
                PixelOut pout;
                pout.hitPosAndPdf = float4(pin.uv, 0.0, 1.0);
                pout.mask = pin.uv.x;
                return pout;  
            }
            ENDHLSL
        }
    }
}
