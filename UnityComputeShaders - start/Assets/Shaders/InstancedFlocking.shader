Shader "Flocking/Instanced"
{

   Properties
   {
        _Colour("Colour", Color)               = (1, 1, 1, 1)
        _MainTex("Albedo (RGB)", 2D)           = "white" { }
        _NormalMap("Normal Map", 2D)           = "bump"  { }
        _MetallicGlossMap("Metallic", 2D)      = "white" { }
        _Metallic("Metallic", Range(0, 1))     = 0
        _Glossiness("Smoothness", Range(0, 1)) = 1
    }

   SubShader
   {
        CGPROGRAM

        #pragma surface surf Standard vertex:vert addshadow nolightmap
        #pragma instancing_options procedural:setup

        #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
        struct Boid
        {
            float3 position;
            float3 direction;
            float noise;
        };

        StructuredBuffer<Boid> boidsBuffer;
         #endif

        struct Input
        {
            float2 uv_MainTex;
            float2 uv_NormalMap;
            float3 worldPos;
        };

        half _Glossiness;
        half _Metallic;
        fixed4 _Colour;
        sampler2D _MainTex;
        sampler2D _NormalMap;
        sampler2D _MetallicGlossMap;
        float4x4 _MovementMatrix;
        float3 _BoidPosition;

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
            #endif
        }

        void vert(inout appdata_full v, out Input OUT)
        {
            UNITY_INITIALIZE_OUTPUT(Input, OUT);

            #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
            v.vertex = mul(_MovementMatrix, v.vertex);
            #endif
        }

         void surf(Input IN, inout SurfaceOutputStandard OUT)
         {
            fixed4 colour   = tex2D(_MainTex, IN.uv_MainTex) * _Colour;
            fixed4 metallic = tex2D(_MetallicGlossMap, IN.uv_MainTex);
            OUT.Albedo      = colour.rgb;
            OUT.Alpha       = colour.a;
            OUT.Normal      = UnpackNormal(tex2D(_NormalMap, IN.uv_NormalMap));
            OUT.Metallic    = metallic.r;
            OUT.Smoothness  = _Glossiness * metallic.a;
         }
         ENDCG
   }
}