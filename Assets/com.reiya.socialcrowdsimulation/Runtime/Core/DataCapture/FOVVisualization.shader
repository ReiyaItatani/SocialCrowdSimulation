Shader "Custom/FOVVisualization"
{
    Properties
    {
        _FillColor ("Fill Color", Color) = (0, 1, 1, 0.15)
        _EdgeColor ("Edge Color", Color) = (0, 1, 1, 0.9)
        _EdgeWidth ("Edge Width", Range(0.5, 5.0)) = 1.5
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }

        Pass
        {
            Name "FOVFillAndEdge"
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma geometry geom
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _FillColor;
                float4 _EdgeColor;
                float _EdgeWidth;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 bary : TEXCOORD0;
            };

            struct GeomOut
            {
                float4 positionCS : SV_POSITION;
                float3 bary : TEXCOORD0;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.bary = float3(0, 0, 0);
                return output;
            }

            // Geometry shader assigns barycentric coordinates to each triangle vertex
            [maxvertexcount(3)]
            void geom(triangle Varyings input[3], inout TriangleStream<GeomOut> stream)
            {
                GeomOut o0, o1, o2;

                o0.positionCS = input[0].positionCS;
                o0.bary = float3(1, 0, 0);

                o1.positionCS = input[1].positionCS;
                o1.bary = float3(0, 1, 0);

                o2.positionCS = input[2].positionCS;
                o2.bary = float3(0, 0, 1);

                stream.Append(o0);
                stream.Append(o1);
                stream.Append(o2);
            }

            float4 frag(GeomOut input) : SV_Target
            {
                // Distance to nearest edge using barycentric coordinates
                float3 bary = input.bary;
                float3 deltas = fwidth(bary);
                float3 smoothing = deltas * _EdgeWidth;
                float3 thickness = smoothstep(0.0, smoothing, bary);
                float minEdge = min(thickness.x, min(thickness.y, thickness.z));

                // Blend between edge color (minEdge near 0) and fill color (minEdge near 1)
                float4 color = lerp(_EdgeColor, _FillColor, minEdge);
                return color;
            }
            ENDHLSL
        }
    }

    // Fallback for platforms without geometry shader support
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }

        Pass
        {
            Name "FOVFillFallback"
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _FillColor;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                return output;
            }

            float4 frag(Varyings input) : SV_Target
            {
                return _FillColor;
            }
            ENDHLSL
        }
    }
}
