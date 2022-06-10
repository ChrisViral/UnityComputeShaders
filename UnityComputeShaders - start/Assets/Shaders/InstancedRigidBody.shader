Shader "Physics/InstancedRigidbody"
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
        #pragma surface surf Standard vertex:vert addshadow nolightmap
        #pragma instancing_options procedural:setup

        #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
        struct Rigidbody
        {
            float3 position;
            float4 rotation;
            float3 velocity;
            float3 angularVelocity;
            int particleOffset;
        };

        StructuredBuffer<Rigidbody> rigidbodies;
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
        float4x4 _DisplacementMatrix;

        float4x4 quaternionToMatrix(float4 quaternion)
        {
            float4x4 result = 0;

            float x = quaternion.x, y = quaternion.y, z = quaternion.z, w = quaternion.w;
            float x2 = x + x,  y2 = y + y,  z2 = z + z;
            float xx = x * x2, xy = x * y2, xz = x * z2;
            float yy = y * y2, yz = y * z2, zz = z * z2;
            float wx = w * x2, wy = w * y2, wz = w * z2;

            result[0][0] = 1 - (yy + zz);
            result[0][1] = xy - wz;
            result[0][2] = xz + wy;

            result[1][0] = xy + wz;
            result[1][1] = 1 - (xx + zz);
            result[1][2] = yz - wx;

            result[2][0] = xz - wy;
            result[2][1] = yz + wx;
            result[2][2] = 1 - (xx + yy);

            result[3][3] = 1;

            return result;
        }

        float4x4 getDisplacementMatrix(float3 position, float4 quaternion)
        {
            float4x4 rotation = quaternionToMatrix(quaternion);
            float4x4 translation = float4x4(1, 0, 0, position.x,
                                            0, 1, 0, position.y,
                                            0, 0, 1, position.z,
                                            0, 0, 0, 1);

            return mul(translation, rotation);
        }

        void setup()
        {
            #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
            Rigidbody body      = rigidbodies[unity_InstanceID];
            _DisplacementMatrix = getDisplacementMatrix(body.position, body.rotation);
            #endif
        }

        void vert(inout appdata_full v, out Input OUT)
        {
            UNITY_INITIALIZE_OUTPUT(Input, OUT);

            #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
            v.vertex = mul(_DisplacementMatrix, v.vertex);
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