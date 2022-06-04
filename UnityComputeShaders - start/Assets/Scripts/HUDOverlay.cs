using UnityEngine;

namespace UnityComputeShaders
{
    [ExecuteInEditMode]
    public class HUDOverlay : BasePP
    {
        private static readonly int LineWidthID   = Shader.PropertyToID("lineWidth");
        private static readonly int AxisColourID  = Shader.PropertyToID("axisColour");
        private static readonly int SweepColourID = Shader.PropertyToID("sweepColour");
        private static readonly int TimeID        = Shader.PropertyToID("time");

        [SerializeField, Range(0.001f, 0.01f)]
        private float lineWidth   = 0.002f;
        [SerializeField]
        private Color axisColour  = Color.white;
        [SerializeField]
        private Color sweepColour = Color.blue;

        protected override void SetProperties()
        {
            this.shader.SetFloat(LineWidthID, this.lineWidth);
            this.shader.SetVector(AxisColourID, this.axisColour);
            this.shader.SetVector(SweepColourID, this.sweepColour);
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
