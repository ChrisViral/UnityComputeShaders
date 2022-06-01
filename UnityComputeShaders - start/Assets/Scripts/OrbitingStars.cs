using UnityEngine;

namespace UnityComputeShaders
{
    public class OrbitingStars : MonoBehaviour
    {
        private const string STARS_KERNEL = "OrbitingStars";
        private const int STRIDE          = sizeof(float) * 3;

        private static readonly int BufferID = Shader.PropertyToID("buffer");
        private static readonly int TimeID   = Shader.PropertyToID("time");


        [SerializeField]
        private int starCount = 17;
        [SerializeField]
        private ComputeShader shader;
        [SerializeField]
        private GameObject prefab;

        private int starsHandle;
        private uint threadSize;
        private int groupSize;
        private Transform[] stars;
        private ComputeBuffer buffer;
        private Vector3[] data;

        private void Start()
        {
            this.starsHandle = this.shader.FindKernel(STARS_KERNEL);
            this.shader.GetKernelThreadGroupSizes(this.starsHandle, out this.threadSize, out _, out _);
            this.groupSize = (int)((this.starCount + this.threadSize - 1) / this.threadSize);

            this.buffer = new(this.starCount, STRIDE);
            this.shader.SetBuffer(this.starsHandle, BufferID, this.buffer);

            this.data  = new Vector3[this.starCount];
            this.stars = new Transform[this.starCount];
            for (int i = 0; i < this.starCount; i++)
            {
                this.stars[i] = Instantiate(this.prefab, this.transform).transform;
            }
        }

        private void OnDestroy()
        {
            this.buffer.Dispose();
        }

        private void Update()
        {
            this.shader.SetFloat(TimeID, Time.time);
            this.shader.Dispatch(this.starsHandle, this.groupSize, 1, 1);
            this.buffer.GetData(this.data);
            for (int i = 0; i < this.starCount; i++)
            {
                this.stars[i].position = this.data[i];
            }
        }
    }
}
