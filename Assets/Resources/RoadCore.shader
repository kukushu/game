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
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct Input { float4 positionOS:POSITION; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Output { float4 positionCS:SV_POSITION; };
            CBUFFER_START(UnityPerMaterial)
            float4 _Color;
            CBUFFER_END
            Output vert(Input input) { UNITY_SETUP_INSTANCE_ID(input);Output o;o.positionCS=TransformObjectToHClip(input.positionOS.xyz);return o; }
            half4 frag(Output input):SV_Target { return _Color; }
            ENDHLSL
        }
    }
}
