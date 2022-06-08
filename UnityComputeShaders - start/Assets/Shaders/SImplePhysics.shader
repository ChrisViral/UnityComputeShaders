Shader "Physics/Simple"
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
        Cull Off

        CGPROGRAM
        #pragma surface surf Standard vertex:vert addshadow nolightmap
        #pragma instancing_options procedural:setup

        #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
        struct Ball
        {
            float3 position;
            float3 velocity;
            float4 colour;
        };

        StructuredBuffer<Ball> ballsBuffer;
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
        float3 _BallPosition;
        float _Radius;

        void setup()
        {
            #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
            Ball ball = ballsBuffer[unity_InstanceID];
            _Colour = ball.colour;
            _BallPosition = ball.position;
            #endif
        }

        void vert(inout appdata_full v, out Input OUT)
        {
            UNITY_INITIALIZE_OUTPUT(Input, OUT);

            #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
            v.vertex.xyz *= _Radius;
            v.vertex.xyz += _BallPosition;
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