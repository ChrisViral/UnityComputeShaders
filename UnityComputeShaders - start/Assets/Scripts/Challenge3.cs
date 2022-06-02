using UnityEngine;
using UnityEngine.Serialization;

namespace UnityComputeShaders
{
    [ExecuteInEditMode]
    public class Challenge3 : BasePP
    {
        private static readonly int TintHeightID   = Shader.PropertyToID("tintHeight");
        private static readonly int EdgeWidthID    = Shader.PropertyToID("edgeWidth");
        private static readonly int ShadeID        = Shader.PropertyToID("shade");
        private static readonly int TintStrengthID = Shader.PropertyToID("tintStrength");
        private static readonly int TintColourID   = Shader.PropertyToID("tintColor");

        [SerializeField, Range(0f, 1f)]
        private float height = 0.3f;
        [SerializeField, Range(0f, 100f)]
        private float softenEdge;
        [SerializeField, Range(0f, 1f)]
        private float shade;
        [SerializeField, Range(0f, 1f)]
        private float tintStrength;
        [SerializeField, FormerlySerializedAs("tintColor")]
        private Color tintColour = Color.white;

        private Vector4 center;

        private void OnValidate()
        {
            if (!this.init)
            {
                Init();
            }

            SetProperties();
        }

        protected void SetProperties()
        {
            float tintHeight = this.height * this.textureSize.y;
            this.shader.SetFloat(TintHeightID, tintHeight);
            this.shader.SetFloat(EdgeWidthID, (tintHeight * this.softenEdge) / 100f);
            this.shader.SetFloat(ShadeID, this.shade);
            this.shader.SetFloat(TintStrengthID, this.tintStrength);
            this.shader.SetVector(TintColourID, this.tintColour);
        }

        protected override void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            if (!this.init || !this.shader)
            {
                Graphics.Blit(source, destination);
            }
            else
            {
                CheckResolutionChanged();
                DispatchWithSource(source, destination);
            }
        }

    }
}
