Shader "Custom/QuadParticle"
{
    Properties
    {
        _MainTex("Texture", 2D) = "white" { }
    }

    SubShader
    {
        Pass
        {
            Tags
            {
                "RenderType"="Transparent"
                "Queue"="Transparent"
                "IgnoreProjector"="True"
            }
            LOD 200
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // Use shader model 3.0 target, to get nicer looking lighting
            #pragma target 5.0

            #include "UnityCG.cginc"

            struct v2f
            {
                float4 position: SV_POSITION;
                float4 colour:   COLOR;
                float2 uv:       TEXCOORD0;
            };

            struct Vertex
            {
                float3 position;
                float2 uv;
                float life;
            };

            sampler2D _MainTex;
            StructuredBuffer<Vertex> vertices;

            v2f vert(uint vertex_id : SV_VertexID, uint instance_id : SV_InstanceID)
            {
                v2f output;
                int id          = (instance_id * 6) + vertex_id;
                Vertex vertex   = vertices[id];
                float value     = vertex.life / 4;
                output.colour   = saturate(fixed4(1 - value + 0.1, value + 0.1, 1, value));
                output.position = UnityWorldToClipPos(float4(vertex.position, 1));
                output.uv       = vertex.uv;
                return output;
            }

            float4 frag(v2f i) : COLOR
            {
                return tex2D(_MainTex, i.uv) * i.colour;
            }
            ENDCG
        }
    }
    FallBack Off
}
