using UnityEngine;

namespace UnityComputeShaders
{
    public class OrbitingStars : MonoBehaviour
    {
        public int starCount = 17;
        public ComputeShader shader;

        public GameObject prefab;

        private int kernelHandle;
        private uint threadGroupSizeX;
        private int groupSizeX;

        private Transform[] stars;

        private void Start()
        {
            this.kernelHandle = this.shader.FindKernel("OrbitingStars");
            this.shader.GetKernelThreadGroupSizes(this.kernelHandle, out this.threadGroupSizeX, out _, out _);
            this.groupSizeX = (int)((this.starCount + this.threadGroupSizeX - 1) / this.threadGroupSizeX);

            this.stars = new Transform[this.starCount];
            for (int i = 0; i < this.starCount; i++)
            {
                this.stars[i] = Instantiate(this.prefab, this.transform).transform;
            }
        }

        private void Update()
        {
        
        }
    }
}
