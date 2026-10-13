Shader "HarborCity/RoadCore"
{
    Properties { _Color("Color", Color) = (1,1,1,1) }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" }
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct Input { float4 positionOS:POSITION; };
            struct Output { float4 positionCS:SV_POSITION; };
            CBUFFER_START(UnityPerMaterial)
            float4 _Color;
            CBUFFER_END
            Output vert(Input input) { Output o;o.positionCS=TransformObjectToHClip(input.positionOS.xyz);return o; }
            half4 frag(Output input):SV_Target { return _Color; }
            ENDHLSL
        }
    }
}
