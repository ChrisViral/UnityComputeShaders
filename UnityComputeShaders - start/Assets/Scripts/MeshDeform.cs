using UnityEngine;

namespace UnityComputeShaders
{
    public class MeshDeform : MonoBehaviour
    {
        public ComputeShader shader;
        [Range(0.5f, 2.0f)]
        public float radius;

        private int kernelHandle;
        private Mesh mesh;
    
        // Use this for initialization
        private void Start()
        {
    
            if (InitData())
            {
                InitShader();
            }
        }

        private bool InitData()
        {
            this.kernelHandle = this.shader.FindKernel("CSMain");

            MeshFilter mf = GetComponent<MeshFilter>();

            if (mf == null)
            {
                Debug.Log("No MeshFilter found");
                return false;
            }

            InitVertexArrays(mf.mesh);
            InitGPUBuffers();

            this.mesh = mf.mesh;

            return true;
        }

        private void InitShader()
        {
            this.shader.SetFloat("radius", this.radius);

        }
    
        private void InitVertexArrays(Mesh mesh)
        {
        
        }

        private void InitGPUBuffers()
        {
        
        }

        private void GetVerticesFromGPU()
        {
        
        }

        private void Update(){
            if (this.shader)
            {
                this.shader.SetFloat("radius", this.radius);
                float delta = (Mathf.Sin(Time.time) + 1)/ 2;
                this.shader.SetFloat("delta", delta);
                this.shader.Dispatch(this.kernelHandle, 1, 1, 1);
            
                GetVerticesFromGPU();
            }
        }

        private void OnDestroy()
        {
        
        }
    }
}

