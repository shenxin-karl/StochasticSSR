Shader "Unlit/Trace" {
    Properties {
    }
    SubShader {
        Tags { "RenderType"="Opaque" "RenderPipeline" = "UniversalPipeline"}
        LOD 100
        ZWrite Off Cull Off
        Pass {
            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            #pragma vertex Vert
            #pragma fragment frag

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 frag (Varyings pin) : SV_Target {
                //float4 col = tex2D(_MainTex, pin.texcoord);
                float4 col = float4(pin.texcoord, 0.0, 1.0);
                return col; 
            }
            ENDHLSL
        }
    }
}
