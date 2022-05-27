using UnityEngine;

namespace UnityComputeShaders
{
    [ExecuteInEditMode]
    public class GaussianBlurHighlight : BaseCompletePP
    {
        [Range(0, 50)]
        public int blurRadius = 20;
        [Range(0.0f, 100.0f)]
        public float radius = 10;
        [Range(0.0f, 100.0f)]
        public float softenEdge = 30;
        [Range(0.0f, 1.0f)]
        public float shade = 0.5f;
        public Transform trackedObject;

        protected override string KernelName => "Highlight";

        private Vector4 center;
        private ComputeBuffer weightsBuffer;

        private RenderTexture horzOutput;
        private int kernelHorzPassID;

        protected override void Init()
        {
            this.kernelHorzPassID = this.shader.FindKernel("HorzPass");
            base.Init();

        }

        private float[] SetWeightsArray(int radius, float sigma)
        {
            int total = radius * 2 + 1;
            float[] weights = new float[total];
            float sum = 0.0f;
            float c = 1 / Mathf.Sqrt(2 * Mathf.PI * sigma * sigma);

            for (int n=0; n<radius; n++)
            {
                float weight = c * Mathf.Exp(-0.5f * n * n / (sigma * sigma));
                weights[radius + n] = weight;
                weights[radius - n] = weight;
                if (n != 0)
                    sum += weight * 2.0f;
                else
                    sum += weight;
            }
            // normalize kernels
            for (int i=0; i<total; i++) weights[i] /= sum;

            return weights;
        }

        private void UpdateWeightsBuffer()
        {
            this.weightsBuffer?.Dispose();

            float sigma = this.blurRadius / 1.5f;

            this.weightsBuffer = new(this.blurRadius * 2 + 1, sizeof(float));
            float[] blurWeights = SetWeightsArray(this.blurRadius, sigma);
            this.weightsBuffer.SetData(blurWeights);

            this.shader.SetBuffer(this.kernelHorzPassID, "weights", this.weightsBuffer);
            this.shader.SetBuffer(this.kernelHandle, "weights", this.weightsBuffer);
        }

        protected override void CreateTextures()
        {
            base.CreateTextures();
            this.shader.SetTexture(this.kernelHorzPassID, "source", this.renderedSource);

            CreateTexture(out this.horzOutput);

            this.shader.SetTexture(this.kernelHorzPassID, "horzOutput", this.horzOutput);
            this.shader.SetTexture(this.kernelHandle, "horzOutput", this.horzOutput);
        }

        private void OnValidate()
        {
            if(!this.init)
                Init();

            SetProperties();

            UpdateWeightsBuffer();
        }

        protected void SetProperties()
        {
            float rad = (this.radius / 100.0f) * this.textureSize.y;
            this.shader.SetFloat("radius", rad);
            this.shader.SetFloat("edgeWidth", rad * this.softenEdge / 100.0f);
            this.shader.SetInt("blurRadius", this.blurRadius);
            this.shader.SetFloat("shade", this.shade);
        }

        protected override void DispatchWithSource(ref RenderTexture source, ref RenderTexture destination)
        {
            if (!this.init) return;

            Graphics.Blit(source, this.renderedSource);

            this.shader.Dispatch(this.kernelHorzPassID, this.groupSize.x, this.groupSize.y, 1);
            this.shader.Dispatch(this.kernelHandle, this.groupSize.x, this.groupSize.y, 1);

            Graphics.Blit(this.output, destination);
        }

        protected override void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            if (this.shader == null)
            {
                Graphics.Blit(source, destination);
            }
            else
            {
                if (this.trackedObject && this.camera)
                {
                    Vector3 pos = this.camera.WorldToScreenPoint(this.trackedObject.position);
                    this.center.x = pos.x;
                    this.center.y = pos.y;
                    this.shader.SetVector("center", this.center);
                }
                bool resChange = false;
                CheckResolution(out resChange);
                if (resChange) SetProperties();
                DispatchWithSource(ref source, ref destination);
            }
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            UpdateWeightsBuffer();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            this.weightsBuffer?.Dispose();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            this.weightsBuffer?.Dispose();
        }
    }
}
