using UnityEngine;

namespace UnityComputeShaders
{
    [ExecuteInEditMode]
    public class RingHighlight : BasePP
    {
        [Range(0.0f, 100.0f)]
        public float radius = 10;
        [Range(0.0f, 100.0f)]
        public float softenEdge;
        [Range(0.0f, 1.0f)]
        public float shade;
        public Transform trackedObject;

        protected override string KernelName => "Highlight";

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
            float rad = (this.radius / 100.0f) * this.textureSize.y;
            this.shader.SetFloat("radius", rad);
            this.shader.SetFloat("edgeWidth", rad * this.softenEdge / 100.0f);
            this.shader.SetFloat("shade", this.shade);
        }

        protected override void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            if (!this.init || this.shader == null)
            {
                Graphics.Blit(source, destination);
            }
            else
            {
                CheckResolution(out _);
                DispatchWithSource(ref source, ref destination);
            }
        }

    }
}
