using UnityEngine;

namespace UnityComputeShaders
{
    [ExecuteInEditMode]
    public class RingHighlight : BasePP
    {
        private static readonly int RadiusID = Shader.PropertyToID("radius");
        private static readonly int EdgeID   = Shader.PropertyToID("edge");
        private static readonly int ShadeID  = Shader.PropertyToID("shade");
        private static readonly int CenterID = Shader.PropertyToID("center");

        [SerializeField, Range(0.01f, 1f)]
        private float radius = 0.1f;
        [SerializeField, Range(0f, 1f)]
        private float smoothing = 0.1f;
        [SerializeField, Range(0f, 1f)]
        private float shade = 0.7f;
        [SerializeField]
        private Transform trackedObject;

        protected override string KernelName => "Highlight";

        protected override void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            if (this.trackedObject)
            {
                Vector2 position = this.Camera.WorldToScreenPoint(this.trackedObject.position);
                this.shader.SetVector(CenterID, position);
            }

            base.OnRenderImage(source, destination);
        }

        protected override void SetProperties()
        {
            // ReSharper disable once LocalVariableHidesMember
            float radius = this.radius * this.textureSize.y;
            this.shader.SetFloat(RadiusID, radius);
            this.shader.SetFloat(EdgeID, radius * this.smoothing);
            this.shader.SetFloat(ShadeID, this.shade);
        }
    }
}
