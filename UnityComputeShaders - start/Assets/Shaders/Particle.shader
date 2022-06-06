Shader "Custom/Particle"
{
    Properties
    {
        _PointSize("Point size", Float) = 5
    }

    SubShader
    {
        Pass
        {
            Tags { "RenderType" = "Opaque" }
            LOD 200
            Blend SrcAlpha One

            CGPROGRAM
            // Physically based Standard lighting model, and enable shadows on all light types
            #pragma vertex vert
            #pragma fragment frag
            // Use shader model 3.0 target, to get nicer looking lighting
            #pragma target 5.0

            #include "UnityCG.cginc"

            struct Particle
            {
                float3 position;
                float3 velocity;
                float life;
            };

            struct v2f
            {
                float4 position: SV_POSITION;
                fixed4 colour:   COLOR;
                float life:      LIFE;
                float size:      PSIZE;
            };

            uniform float _PointSize;
            StructuredBuffer<Particle> particles;

            v2f vert(uint vertex_id : SV_VertexID, uint instance_id : SV_InstanceID)
            {
                v2f output;
                Particle particle = particles[instance_id];
                float value       = particle.life / 4;
                output.colour     = saturate(fixed4(1 - value + 0.1, value + 0.1, 1, value));
                output.position   = UnityObjectToClipPos(float4(particle.position, 1));
                output.size       = _PointSize;
                return output;
            }

            float4 frag(v2f i) : COLOR
            {
                return i.colour;
            }
            ENDCG
        }
    }
    FallBack Off
}
