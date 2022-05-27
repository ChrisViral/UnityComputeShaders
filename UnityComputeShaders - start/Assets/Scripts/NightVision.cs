using UnityEngine;

namespace UnityComputeShaders
{
    [ExecuteInEditMode]
    public class NightVision : BaseCompletePP
    {
        [Range(0.0f, 100.0f)]
        public float radius = 70;
        [Range(0.0f, 1.0f)]
        public float tintStrength = 0.7f;
        [Range(0.0f, 100.0f)]
        public float softenEdge = 3;
        public Color tint = Color.green;
        [Range(50, 500)]
        public int lines = 100;

        private void OnValidate()
        {
            if(!this.init)
                Init();
           
            SetProperties();
        }

        protected void SetProperties()
        {
            float rad = (this.radius / 100.0f) * this.textureSize.y;
            this.shader.SetFloat("radius", rad);
            this.shader.SetFloat("edgeWidth", rad * this.softenEdge / 100.0f);
            this.shader.SetVector("tintColor", this.tint);
            this.shader.SetFloat("tintStrength", this.tintStrength);
            this.shader.SetInt("lines", this.lines);
        }

        protected override void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            this.shader.SetFloat("time", Time.time);
            base.OnRenderImage(source, destination);
        }
    }
}
