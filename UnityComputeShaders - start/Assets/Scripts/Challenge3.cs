using UnityEngine;

namespace UnityComputeShaders
{
    [ExecuteInEditMode]
    public class Challenge3 : BasePP
    {
        private static readonly int EdgeID         = Shader.PropertyToID("edge");
        private static readonly int ShadeID        = Shader.PropertyToID("shade");
        private static readonly int TintHeightID   = Shader.PropertyToID("tintHeight");
        private static readonly int TintStrengthID = Shader.PropertyToID("tintStrength");
        private static readonly int TintColourID   = Shader.PropertyToID("tintColour");

        [SerializeField, Range(0f, 1f)]
        private float height = 0.3f;
        [SerializeField, Range(0f, 1f)]
        private float softenEdge;
        [SerializeField, Range(0f, 1f)]
        private float shade;
        [SerializeField, Range(0f, 1f)]
        private float tintStrength;
        [SerializeField]
        private Color tintColour = Color.white;

        protected override void SetProperties()
        {
            this.shader.SetFloat(TintHeightID, this.height);
            this.shader.SetFloat(EdgeID, this.height * this.softenEdge);
            this.shader.SetFloat(ShadeID, this.shade);
            this.shader.SetFloat(TintStrengthID, this.tintStrength);
            this.shader.SetVector(TintColourID, this.tintColour);
        }
    }
}
