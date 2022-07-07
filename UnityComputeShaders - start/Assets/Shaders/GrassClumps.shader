Shader "Custom/GrassClumps"
{
    Properties
    {
        _Colour("Colour", Color)               = (1, 1, 1, 1)
        _MainTex("Albedo (RGB)", 2D)           = "white" { }
        _Glossiness("Smoothness", Range(0, 1)) = 0.5
        _Metallic("Metallic", Range(0, 1))     = 0
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200
        Cull Off

        CGPROGRAM
        // Physically based Standard lighting model, and enable shadows on all light types
        #pragma surface surf Standard vertex:vert addshadow fullforwardshadows
        #pragma instancing_options procedural:setup

        #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
        struct GrassClump
        {
            float3 position;
            float lean;
            float noise;
        };

        StructuredBuffer<GrassClump> clumps;
        #endif

        struct Input
        {
            float2 uv_MainTex;
        };

        sampler2D _MainTex;
        half _Glossiness;
        half _Metallic;
        fixed4 _Colour;
        float _Scale;
        float4x4 _Matrix;
        float3 _Position;

        float4x4 getPositionMatrix(float3 position, float theta)
        {
            float c = cos(theta);
            float s = sin(theta);
            return float4x4(
                c, -s, 0, position.x,
                s,  c, 0, position.y,
                0,  0, 1, position.z,
                0,  0, 0, 1
            );
        }

        void setup()
        {
            #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
            GrassClump clump = clumps[unity_InstanceID];
            _Position = clump.position;
            _Matrix = getPositionMatrix(clump.position, clump.lean);
            #endif
        }

        void vert(inout appdata_full v, out Input IN)
        {
            UNITY_INITIALIZE_OUTPUT(Input, IN);

            #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
            v.vertex.xyz *= _Scale;
            float4 rotated = mul(_Matrix, v.vertex);
            v.vertex.xyz += _Position;
            v.vertex = lerp(v.vertex, rotated, v.texcoord.y);
            #endif
        }

        void surf(Input IN, inout SurfaceOutputStandard OUT)
        {
            // Albedo comes from a texture tinted by color
            fixed4 colour = tex2D(_MainTex, IN.uv_MainTex) * _Colour;
            OUT.Albedo    = colour.rgb;
            OUT.Alpha     = colour.a;

            // Metallic and smoothness come from slider variables
            OUT.Metallic   = _Metallic;
            OUT.Smoothness = _Glossiness;

            clip(colour.a - 0.4);
        }
        ENDCG
    }

    FallBack "Diffuse"
}
