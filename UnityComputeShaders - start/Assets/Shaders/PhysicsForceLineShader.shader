    Shader "Physics/ParticleForceLineShader"
    {
        Properties
        {
            _Colour("Colour", Color)                   = (1, 1, 1, 1)
            _LineLength("Velocity Line Scaler", Float) = 50
        }

        SubShader
        {
            Tags { "RenderType"="Opaque" }
            LOD 100

            Pass
            {
                CGPROGRAM
                #include "UnityCG.cginc"
                #pragma vertex vert
                #pragma fragment frag
                #pragma multi_compile_instancing
                #pragma instancing_options procedural:setup



                struct Particle
                {
                    float3 position;
                    float3 velocity;
                    float3 force;
                    float3 localPosition;
                    float3 offsetPosition;
                };

                struct appdata
                {
                    float4 vertex : POSITION;
                    UNITY_VERTEX_INPUT_INSTANCE_ID
                };

                struct v2f
                {
                    float4 vertex : SV_POSITION;
                };

                float4 _Colour;
                float _LineLength;

                StructuredBuffer<Particle> particles;

                void setup()
                {
                }

                v2f vert(appdata v)
                {
                    v2f OUT;

                    UNITY_SETUP_INSTANCE_ID(v);
                    #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
                    int instance_id   = UNITY_GET_INSTANCE_ID(v);
                    Particle particle = particles[instance_id];
                    float3 position   = particle.position;
                    float3 endPoint   = particle.velocity * _LineLength * v.vertex;
                    OUT.vertex        = UnityObjectToClipPos(position + endPoint);
                    #else
                    OUT.vertex = UnityObjectToClipPos(v.vertex);
                    #endif
                    return OUT;
                }

                fixed4 frag(v2f i) : SV_Target
                {
                    return _Colour;
                }
                ENDCG
            }
        }
    }
