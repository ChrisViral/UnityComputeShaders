using UnityEngine;

namespace UnityComputeShaders
{
    public class StarGlow : MonoBehaviour
    {
        private static readonly int CompositeTexID       = Shader.PropertyToID("_CompositeTex");
        private static readonly int CompositeColorID     = Shader.PropertyToID("_CompositeColour");
        private static readonly int BrightnessSettingsID = Shader.PropertyToID("_BrightnessSettings");
        private static readonly int IterationID          = Shader.PropertyToID("_Iteration");
        private static readonly int OffsetID             = Shader.PropertyToID("_Offset");

        [SerializeField, Range(0f, 1f)]
        private float threshold   = 1f;
        [SerializeField, Range(0f, 10f)]
        private float intensity   = 1f;
        [SerializeField, Range(1, 20)]
        private int divide        = 3;
        [SerializeField, Range(1, 5)]
        private int iteration     = 5;
        [SerializeField, Range(0f, 1f)]
        private float attenuation = 1f;
        [SerializeField, Range(0f, 360f)]
        private float angleOfStreak;
        [SerializeField, Range(1, 16)]
        private int numOfStreaks  = 4;
        [SerializeField]
        private Material material;
        [SerializeField]
        private Color color = Color.white;

        private void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            RenderTexture brightness = RenderTexture.GetTemporary(source.width / this.divide, source.height / this.divide, source.depth, source.format);
            RenderTexture blurred1   = RenderTexture.GetTemporary(brightness.descriptor);
            RenderTexture blurred2   = RenderTexture.GetTemporary(brightness.descriptor);
            RenderTexture composite  = RenderTexture.GetTemporary(brightness.descriptor);

            this.material.SetVector(BrightnessSettingsID, new(this.threshold, this.intensity, this.attenuation));
            Graphics.Blit(source, brightness, this.material, 1);

            float angle = 360f / this.numOfStreaks;
            for (int i = 1; i <= this.numOfStreaks; i++)
            {
                Vector2 offset = (Quaternion.AngleAxis((angle * i) + this.angleOfStreak, Vector3.forward) * Vector2.down).normalized;
                this.material.SetVector(OffsetID, offset);
                this.material.SetInt(IterationID, 1);
                Graphics.Blit(brightness, blurred1, this.material, 2);

                for (int j = 2; j <= this.iteration; j++)
                {
                    this.material.SetInt(IterationID, j);
                    Graphics.Blit(blurred1, blurred2, this.material, 2);

                    (blurred1, blurred2) = (blurred2, blurred1);
                }

                Graphics.Blit(blurred1, composite, this.material, 3);
            }

            this.material.SetColor(CompositeColorID, this.color);
            this.material.SetTexture(CompositeTexID, composite);
            Graphics.Blit(source, destination, this.material, 4);

            RenderTexture.ReleaseTemporary(brightness);
            RenderTexture.ReleaseTemporary(blurred1);
            RenderTexture.ReleaseTemporary(blurred2);
            RenderTexture.ReleaseTemporary(composite);
        }
    }
}