using UnityEngine;

namespace UnityComputeShaders
{
    [ExecuteInEditMode]
    public class BlurHighlight : BaseCompletePP
    {
        private static readonly int RadiusID    = Shader.PropertyToID("radius");
        private static readonly int EdgeWidthID = Shader.PropertyToID("edgeWidth");
        private static readonly int ShadeID     = Shader.PropertyToID("shade");
        private static readonly int CenterID    = Shader.PropertyToID("center");

        [SerializeField, Range(0, 50)]
        private int blurRadius = 20;
        [SerializeField, Range(0f, 100f)]
        private float radius = 10f;
        [SerializeField, Range(0f, 100f)]
        private float softenEdge = 30f;
        [SerializeField, Range(0f, 1f)]
        private float shade = 0.5f;
        [SerializeField]
        private Transform trackedObject;

        private Vector4 center;

        protected override string KernelName => "Highlight";

        private void OnValidate()
        {
            if(!this.init)
            {
                Init();
            }

            SetProperties();
        }

        protected void SetProperties()
        {
            float rad = (this.radius / 100f) * this.textureSize.y;
            this.shader.SetFloat(RadiusID, rad);
            this.shader.SetFloat(EdgeWidthID, rad * this.softenEdge / 100f);
            this.shader.SetFloat(ShadeID, this.shade);
        }

        protected override void DispatchWithSource(ref RenderTexture source, ref RenderTexture destination)
        {
            if (!this.init) return;

            Graphics.Blit(source, this.renderedSource);

            this.shader.Dispatch(this.kernelHandle, this.groupSize.x, this.groupSize.y, 1);

            Graphics.Blit(this.output, destination);
        }

        protected override void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            if (!this.shader)
            {
                Graphics.Blit(source, destination);
                return;
            }

            if (this.trackedObject && this.camera)
            {
                Vector3 pos = this.camera.WorldToScreenPoint(this.trackedObject.position);
                this.center.x = pos.x;
                this.center.y = pos.y;
                this.shader.SetVector(CenterID, this.center);
            }

            CheckResolution(out bool resChange);
            if (resChange)
            {
                SetProperties();
            }

            DispatchWithSource(ref source, ref destination);
        }
    }
}
