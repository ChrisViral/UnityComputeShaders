using UnityEngine;

namespace UnityComputeShaders
{
    [ExecuteInEditMode]
    public class HUDOverlay : BasePP
    {
        public Color axisColor = new(0.8f, 0.8f, 0.8f, 1);
        public Color sweepColor = new(0.1f, 0.3f, 0.1f, 1);

        private void OnValidate()
        {
            if (!this.init)
                Init();

            SetProperties();
        }

        protected void SetProperties()
        {
            this.shader.SetVector("axisColor", this.axisColor);
            this.shader.SetVector("sweepColor", this.sweepColor);
        }

        protected override void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            if (this.shader) this.shader.SetFloat("time", Time.time);
            base.OnRenderImage(source, destination);
        }

    }
}
