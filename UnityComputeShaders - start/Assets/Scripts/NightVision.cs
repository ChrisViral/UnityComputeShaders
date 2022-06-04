using UnityEngine;

namespace UnityComputeShaders
{
    [ExecuteInEditMode]
    public class NightVision : BasePP
    {
        private static readonly int RadiusID       = Shader.PropertyToID("radius");
        private static readonly int EdgeID         = Shader.PropertyToID("edge");
        private static readonly int TintColourID   = Shader.PropertyToID("tintColour");
        private static readonly int TintStrengthID = Shader.PropertyToID("tintStrength");
        private static readonly int LinesID        = Shader.PropertyToID("lines");
        private static readonly int LineStrengthID = Shader.PropertyToID("lineStrength");
        private static readonly int LineSpeedID    = Shader.PropertyToID("lineSpeed");
        private static readonly int NoiseFactorID  = Shader.PropertyToID("noiseFactor");
        private static readonly int TimeID         = Shader.PropertyToID("time");

        [SerializeField, Range(0f, 1f)]
        private float radius = 0.7f;
        [SerializeField, Range(0f, 1f)]
        private float tintStrength = 0.7f;
        [SerializeField, Range(0f, 1f)]
        private float smoothing = 0.3f;
        [SerializeField]
        private Color tintColour = Color.green;
        [SerializeField, Range(20, 500)]
        private int lines = 100;
        [SerializeField, Range(0f, 1f)]
        private float lineStrength = 0.7f;
        [SerializeField, Range(0.1f, 10f)]
        private float lineSpeed = 3f;
        [SerializeField, Range(0.25f, 4f)]
        private float noiseFactor = 1.5f;

        protected override void SetProperties()
        {
            // ReSharper disable once LocalVariableHidesMember
            float radius = this.radius * this.textureSize.y;
            this.shader.SetFloat(RadiusID, radius);
            this.shader.SetFloat(EdgeID, radius * this.smoothing);
            this.shader.SetVector(TintColourID, this.tintColour);
            this.shader.SetFloat(TintStrengthID, this.tintStrength);
            this.shader.SetInt(LinesID, this.lines);
            this.shader.SetFloat(LineStrengthID, this.lineStrength);
            this.shader.SetFloat(LineSpeedID, this.lineSpeed);
            this.shader.SetFloat(NoiseFactorID, this.noiseFactor);
        }

        protected override void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            if (this.shader)
            {
                this.shader.SetFloat(TimeID, Time.time);
            }

            base.OnRenderImage(source, destination);
        }
    }
}
