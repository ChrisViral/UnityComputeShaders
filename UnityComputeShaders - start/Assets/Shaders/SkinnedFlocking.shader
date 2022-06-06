Shader "Flocking/Skinned"
{
   Properties
   {
        _Colour("Colour", Color)               = (1, 1, 1, 1)
        _MainTex("Albedo (RGB)", 2D)           = "white" { }
        _NormalMap("Normal Map", 2D)           = "bump"  { }
        _MetallicMap("Metallic Map", 2D)       = "white" { }
        _Metallic("Metallic", Range(0, 1))     = 0
        _Glossiness("Smoothness", Range(0, 1)) = 1
    }

   SubShader
   {
        CGPROGRAM
        #include "UnityCG.cginc"

        #pragma multi_compile __ FRAME_INTERPOLATION
        #pragma surface surf Standard vertex:vert addshadow nolightmap
        #pragma instancing_options procedural:setup

        #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
        struct Boid
        {
            float3 position;
            float3 direction;
            float noise_offset;
            float frame;
        };

        StructuredBuffer<Boid> boidsBuffer;
        StructuredBuffer<float4> vertexAnimation;
        #endif

        struct appdata_custom
        {
            float4 vertex:   POSITION;
            float3 normal:   NORMAL;
            float4 texcoord: TEXCOORD0;
            float4 tangent:  TANGENT;
            uint id:         SV_VertexID;
            uint inst:       SV_InstanceID;

            UNITY_VERTEX_INPUT_INSTANCE_ID
         };

        struct Input
        {
            float2 uv_MainTex;
            float2 uv_NormalMap;
            float3 worldPos;
        };

        sampler2D _MainTex;
        sampler2D _NormalMap;
        sampler2D _MetallicMap;
        half _Glossiness;
        half _Metallic;
        fixed4 _Colour;
        float4x4 _MovementMatrix;
        int _CurrentFrame;
        int _NextFrame;
        float _FrameInterpolation;
        int frameCount;

        float4x4 getMovementMatrix(float3 position, float3 direction)
        {
            float3 zAxis = normalize(direction);
            float3 xAxis = normalize(cross(float3(0, 1, 0), zAxis));
            float3 yAxis = normalize(cross(zAxis, xAxis));
            return float4x4(xAxis.x, yAxis.x, zAxis.x, position.x,
                            xAxis.y, yAxis.y, zAxis.y, position.y,
                            xAxis.z, yAxis.z, zAxis.z, position.z,
                            0,       0,       0,       1);
        }

        void setup()
        {
            #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
            Boid boid       = boidsBuffer[unity_InstanceID];
            _MovementMatrix = getMovementMatrix(boid.position, boid.direction);
            _CurrentFrame   = boid.frame;
            #ifdef FRAME_INTERPOLATION
            _NextFrame = (_CurrentFrame + 1) % frameCount;
            _FrameInterpolation = frac(boid.frame);
            #endif
            #endif
        }

        void vert(inout appdata_custom v)
        {
            #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
            int offset = v.id * frameCount;
            #ifdef FRAME_INTERPOLATION
            v.vertex = lerp(vertexAnimation[offset + _CurrentFrame], vertexAnimation[offset + _NextFrame], _FrameInterpolation);
            #else
            v.vertex = vertexAnimation[offset + _CurrentFrame];
            #endif
            v.vertex = mul(_MovementMatrix, v.vertex);
            #endif
        }

         void surf(Input IN, inout SurfaceOutputStandard OUT)
         {
            fixed4 colour   = tex2D(_MainTex, IN.uv_MainTex) * _Colour;
            fixed4 metallic = tex2D(_MetallicMap, IN.uv_MainTex);
            OUT.Albedo      = colour.rgb;
            OUT.Alpha       = colour.a;
            OUT.Normal      = UnpackNormal(tex2D(_NormalMap, IN.uv_NormalMap));
            OUT.Metallic    = metallic.r;
            OUT.Smoothness  = _Glossiness * metallic.a;
         }
         ENDCG
   }
}