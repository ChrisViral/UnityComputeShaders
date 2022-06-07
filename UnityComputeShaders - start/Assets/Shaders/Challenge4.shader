Shader "Flocking/Fish"
{

   Properties
   {
        _Colour("Colour", Color)               = (1, 1, 1, 1)
        _MainTex("Albedo (RGB)", 2D)           = "white" { }
        _NormalMap("Normal Map", 2D)           = "bump" { }
        _MetallicMap("Metallic Map", 2D)       = "white" { }
        _Metallic("Metallic", Range(0, 1))     = 0
        _Glossiness("Smoothness", Range(0, 1)) = 1
    }

   SubShader
   {
        CGPROGRAM
        #include "UnityCG.cginc"

        #pragma surface surf Standard vertex:vert addshadow nolightmap
        #pragma instancing_options procedural:setup

        #define UP float3(0, 1, 0)

        #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
        struct Boid
        {
            float3 position;
            float3 direction;
            float noise;
            float theta;
        };

        StructuredBuffer<Boid> boidsBuffer;
         #endif

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
        float3 _BoidPosition;
        float _FinOffset;
        float4x4 _MovementMatrix;

        float4x4 getMovementMatrix(float3 position, float3 direction)
        {
            float3 zAxis = normalize(direction);
            float3 xAxis = normalize(cross(UP, zAxis));
            float3 yAxis = normalize(cross(zAxis, xAxis));
            return float4x4(xAxis.x, yAxis.x, zAxis.x, position.x,
                            xAxis.y, yAxis.y, zAxis.y, position.y,
                            xAxis.z, yAxis.z, zAxis.z, position.z,
                            0,       0,       0,       1);
        }

        void setup()
        {
            #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
            Boid boid = boidsBuffer[unity_InstanceID];
            //Convert the boid theta value to a value between -1 and 1
            //Hint: use sin and save the value as _FinOffset
            _FinOffset = sin(boid.theta) / 5;
            _MovementMatrix = getMovementMatrix(boid.position, boid.direction);
            #endif
        }

        float inverseLerp(float a, float b, float value)
        {
            return (value - a) / (b - a);
        }

        void vert(inout appdata_full v, out Input OUT)
        {
            UNITY_INITIALIZE_OUTPUT(Input, OUT);

            #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
            if (v.vertex.z < -0.2)
            {
                //If v.vertex.z is less than -0.2 then this is a tail vertex
                //The sin curve between 3π/2 and 2π ramps up from -1 to 0
                //Use this curve plus 1, ie a curve from 0 to 1 to control the strength of the swish
                //Apply the value you calculate as an offset to v.vertex.x

                float t = inverseLerp(-0.2, -0.4, v.vertex.z);
                float strength = 1 - sin(lerp(UNITY_HALF_PI, UNITY_PI, t));
                v.vertex.x += _FinOffset * strength;
            }

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