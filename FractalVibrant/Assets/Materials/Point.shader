Shader "Fractal/Point"
{
    SubShader
    {
        CGPROGRAM
        #pragma surface ConfigureSurface Standard fullforwardshadows addshadow
        #pragma instancing_options assumeuniformscaling procedural:ConfigureProcedural
        #pragma editor_sync_compilation
        #pragma target 4.5

        #include "FractalGPU.hlsl"

        struct Input
        {
            float3 worldPos;
        };

        float _Smoothness;

        void ConfigureSurface (Input input, inout SurfaceOutputStandard surface)
        {
            float4 fractalColor = GetFractalColor();
            surface.Albedo = fractalColor.rgb;
            surface.Smoothness = fractalColor.a;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
