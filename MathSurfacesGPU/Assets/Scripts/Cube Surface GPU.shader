Shader "Graph/Cube Surface GPU"
{
    Properties
    {
        _Smoothness ("Smoothness", Range(0, 1)) = 0.5
    }

    SubShader
    {
        CGPROGRAM
        #pragma surface ConfigureSurface Standard fullforwardshadows addshadow
        #pragma instancing_options assumeuniformscaling procedural:ConfigureProcedural
        #pragma editor_sync_compilation
        #pragma target 4.5

        #include "CubeGPU.hlsl"

        struct Input
        {
            float3 worldPos;
        };

        float _Smoothness;

        void ConfigureSurface(Input input, inout SurfaceOutputStandard surface)
        {
            surface.Smoothness = _Smoothness;
            surface.Albedo.rgb = saturate(input.worldPos * 0.5 + 0.5);
        }

        ENDCG
    }

    Fallback "Diffuse"
}