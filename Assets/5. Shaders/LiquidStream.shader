// Pour Decisions - LiquidStream (Unlit, URP, Quest 2)
//
// The thin trickle between bottle neck and glass while pouring.
// Absolute minimum: 1 ALU vert (MVP transform only), 0 ALU frag (constant color out).
// Alpha blend required because the stream is translucent — but ZWrite Off and a tiny
// screen footprint keep overdraw negligible. Driven by PourDetector; at most 1 active.
//
// Properties set by PourStream.cs via MaterialPropertyBlock:
//  - _BaseColor  (RGBA)

Shader "PourDecisions/LiquidStream"
{
    Properties
    {
        _BaseColor ("Color", Color) = (1, 1, 1, 0.85)
    }

    SubShader
    {
        Tags
        {
            "RenderType"     = "Transparent"
            "Queue"          = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector"= "True"
            "PreviewType"    = "Plane"
        }
        LOD 100

        Pass
        {
            Name "StreamUnlit"
            Tags { "LightMode" = "UniversalForward" }

            Blend  SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest  LEqual
            Cull   Back

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag

            #pragma multi_compile_instancing
            #pragma multi_compile _ STEREO_INSTANCING_ON STEREO_MULTIVIEW_ON

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);
                return _BaseColor;
            }
            ENDHLSL
        }
    }

    Fallback Off
}
