Shader "ImageEffect/StarGlow"
{
    Properties
    {
        [HideInInspector] _MainTex("Texture", 2D) = "white" { }
        _BrightnessSettings("(Threshold, Intensity, Attenuation, -)", Vector) = (0.8, 1, 0.95, 0)
    }

    SubShader
    {
        CGINCLUDE
        #include "UnityCG.cginc"

        sampler2D _MainTex;
        float4    _MainTex_ST;
        float4    _MainTex_TexelSize;
        float4    _BrightnessSettings;

        #define THRESHOLD _BrightnessSettings.x
        #define INTENSITY            _BrightnessSettings.y
        #define ATTENUATION          _BrightnessSettings.z

        ENDCG

        Pass
        {
            CGPROGRAM

            #pragma vertex vert_img
            #pragma fragment frag

            fixed4 frag(v2f_img i) : SV_Target
            {
                return tex2D(_MainTex, i.uv);
            }
            ENDCG
        }

        Pass
        {
            CGPROGRAM

            #pragma vertex vert_img
            #pragma fragment frag

            fixed4 frag(v2f_img i) : SV_Target
            {
                fixed4 colour = tex2D(_MainTex, i.uv);
                return max(colour - THRESHOLD, 0) * INTENSITY;
            }
            ENDCG
        }

        Pass
        {
            CGPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            struct v2f
            {
                float4 position: SV_POSITION;
                float2 uv:       TEXCOORD0;
                float power:     TEXCOORD1;
                float2 offset:   TEXCOORD2;
            };

            int _Iteration;
            float2 _Offset;

            v2f vert(appdata_img v)
            {
                v2f output;
                output.position = UnityObjectToClipPos(v.vertex);
                output.uv = v.texcoord;
                output.power = pow(4, _Iteration - 1);
                output.offset = _MainTex_TexelSize.xy * _Offset * output.power;
                return output;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 colour = 0;
                float2 uv = i.uv;
                for (int j = 0; j < 4; j++)
                {
                    colour += saturate(tex2D(_MainTex, uv) * pow(ATTENUATION, i.power * j));
                    uv += i.offset;
                }
                return colour;
            }
            ENDCG
        }

        Pass
        {
            Blend OneMinusDstColor One

            CGPROGRAM

            #pragma vertex vert_img
            #pragma fragment frag

            fixed4 frag(v2f_img i) : SV_Target
            {
                return tex2D(_MainTex, i.uv);
            }
            ENDCG
        }

        Pass
        {
            CGPROGRAM

            #pragma vertex vert_img
            #pragma fragment frag

            sampler2D _CompositeTex;
            float4 _CompositeColour;

            fixed4 frag(v2f_img i) : SV_Target
            {
                fixed4 mainColour = tex2D(_MainTex, i.uv);
                float4 compositeColour = tex2D(_CompositeTex, i.uv);
                compositeColour.rgb = ((compositeColour.r + compositeColour.g + compositeColour.b) / 3) * _CompositeColour;
                return saturate(mainColour + compositeColour);
            }
            ENDCG
        }
    }
}