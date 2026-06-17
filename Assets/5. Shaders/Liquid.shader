// Pour Decisions - Liquid (Unlit, URP, Quest 2)
//
// Goals:
//  - Zero texture samples. Zero lighting. Zero shadows. Zero GI.
//  - Fill is computed in VERTEX by collapsing above-fill verts down to the fill plane.
//    No fragment clip()/discard, preserves early-Z on tile-based GPUs.
//  - Single-pass stereo (multiview) via URP macros.
//  - CBUFFER(UnityPerMaterial) so SRP Batcher can batch all liquid instances.
//  - Fragment is 2 ALU: lerp between body and surface tint.
//
// Mesh requirements:
//  - A closed cylinder/solid sized to fit inside the glass cup.
//  - Vertically subdivided: ~12-16 rings along the up axis. Coarser = visible "stair" when filling.
//  - Local +Y must point up (matches the glass orientation).
//  - Normals are not used by this shader (keep for future, doesn't cost anything).
//
// Properties set by LiquidRenderer.cs via MaterialPropertyBlock:
//  - _FillAmount   (0..1)
//  - _LiquidColor  (RGBA, alpha ignored)
//  Optional, set once in Material inspector:
//  - _FillMinY / _FillMaxY  (local-space Y bounds of the liquid mesh)
//  - _SurfaceColor          (slight darker rim for top surface)
//  - _WobbleVelocity / _WobbleStrength  (horizontal world velocity, tilts the plane)

Shader "PourDecisions/Liquid"
{
    Properties
    {
        _LiquidColor   ("Liquid Color", Color) = (0.8, 0.3, 0.2, 1)
        _SurfaceColor  ("Surface Tint", Color) = (1, 1, 1, 1)
        _FillAmount    ("Fill Amount", Range(0, 1)) = 0.5
        _FillMinY      ("Local Fill Min Y", Float) = -0.05
        _FillMaxY      ("Local Fill Max Y", Float) =  0.05
        _WobbleStrength("Wobble Strength", Range(0, 2)) = 0.0
        _WobbleVelocity("Wobble Velocity XZ", Vector) = (0, 0, 0, 0)
    }

    SubShader
    {
        Tags
        {
            "RenderType"     = "Opaque"
            "Queue"          = "Geometry"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector"= "True"
        }
        LOD 100

        Pass
        {
            Name "LiquidUnlit"
            Tags { "LightMode" = "UniversalForward" }

            ZWrite On
            ZTest  LEqual
            Cull   Back

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag

            // Stereo / instancing for Quest 2 multiview.
            #pragma multi_compile_instancing
            #pragma multi_compile _ STEREO_INSTANCING_ON STEREO_MULTIVIEW_ON

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4  _LiquidColor;
                half4  _SurfaceColor;
                float4 _WobbleVelocity; // xz used
                float  _FillAmount;
                float  _FillMinY;
                float  _FillMaxY;
                float  _WobbleStrength;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                // 0 = below fill (body), 1 = snapped to fill plane (top surface).
                // Branchless lerp in frag — cheaper than an if.
                half   surfaceMask : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                // Object-space fill line. Lerp is 2 ALU.
                float fillOS = lerp(_FillMinY, _FillMaxY, _FillAmount);

                // Transform position to world.
                float3 posWS = TransformObjectToWorld(IN.positionOS.xyz);

                // Object origin in world (for horizontal wobble offset and world-space fill).
                float3 originWS = TransformObjectToWorld(float3(0, 0, 0));

                // World-space fill Y, tilted by horizontal velocity (wobble).
                float fillWS = originWS.y + fillOS;
                float2 tilt  = _WobbleVelocity.xz * _WobbleStrength;
                float2 off   = posWS.xz - originWS.xz;
                fillWS += off.x * tilt.x + off.y * tilt.y;

                // Mark verts that were above the fill plane before clamp.
                half above = (half)step(fillWS, posWS.y);

                // Collapse above-fill verts DOWN to the fill plane (flat top surface).
                // No discard, no clip. Triangles with all 3 verts above become degenerate
                // (zero area) and are culled by the rasterizer for free.
                posWS.y = min(posWS.y, fillWS);

                OUT.positionCS  = TransformWorldToHClip(posWS);
                OUT.surfaceMask = above;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);
                // Branchless: body OR surface tint. 3 ALU.
                half3 c = lerp(_LiquidColor.rgb, _SurfaceColor.rgb, IN.surfaceMask);
                return half4(c, 1.0h);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
