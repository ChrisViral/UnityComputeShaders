using System;
using UnityEngine;

namespace UnityComputeShaders
{
    [RequireComponent(typeof(MeshFilter))]
    public class MeshDeform : MonoBehaviour
    {
        private struct Vertex
        {
            public Vector3 position;
            public Vector3 normal;
        }

        private const string KERNEL = "CSMain";
        private const int STRIDE    = sizeof(float) * 6;

        private static readonly int VerticesID = Shader.PropertyToID("vertices");
        private static readonly int InitialID  = Shader.PropertyToID("initial");
        private static readonly int RadiusID   = Shader.PropertyToID("radius");
        private static readonly int DeltaID    = Shader.PropertyToID("delta");

        [SerializeField]
        private ComputeShader shader;
        [SerializeField, Range(0.5f, 2f)]
        private float radius;

        private int kernelHandle;
        private MeshFilter meshFilter;
        private Mesh mesh;
        private Vertex[] vertices;
        private Vertex[] initial;
        private ComputeBuffer verticesBuffer;
        private ComputeBuffer initialBuffer;
        private Vector3[] tempVertices;
        private Vector3[] tempNormals;

        private void Start()
        {
            InitShader();
            InitData();
        }

        private void OnDestroy()
        {
            this.verticesBuffer.Dispose();
            this.initialBuffer.Dispose();
        }

        private void Update()
        {
            Dispatch();
        }

        private void InitShader()
        {
            this.kernelHandle = this.shader.FindKernel(KERNEL);
            this.shader.SetFloat(RadiusID, this.radius);
        }

        private void InitData()
        {
            this.meshFilter = GetComponent<MeshFilter>();
            this.mesh = this.meshFilter.mesh;
            InitVertexArrays();
            InitGPUBuffers();
        }

        private void InitVertexArrays()
        {
            this.vertices = new Vertex[this.mesh.vertices.Length];
            this.initial  = new Vertex[this.mesh.vertices.Length];
            this.tempVertices = new Vector3[this.vertices.Length];
            this.tempNormals  = new Vector3[this.vertices.Length];

            for (int i = 0; i < this.vertices.Length; i++)
            {
                this.vertices[i] = this.initial[i] = new()
                {
                    position = this.mesh.vertices[i],
                    normal   = this.mesh.normals[i]
                };
            }
        }

        private void InitGPUBuffers()
        {
            this.verticesBuffer = new(this.vertices.Length, STRIDE);
            this.initialBuffer  = new(this.vertices.Length, STRIDE);

            this.verticesBuffer.SetData(this.vertices);
            this.initialBuffer.SetData(this.initial);

            this.shader.SetBuffer(this.kernelHandle, VerticesID, this.verticesBuffer);
            this.shader.SetBuffer(this.kernelHandle, InitialID, this.initialBuffer);
        }

        private void GetVerticesFromGPU()
        {
            this.verticesBuffer.GetData(this.vertices);
            for (int i = 0; i < this.vertices.Length; i++)
            {
                Vertex vertex = this.vertices[i];
                this.tempVertices[i] = vertex.position;
                this.tempNormals[i]  = vertex.normal;
            }

            this.mesh.vertices = this.tempVertices;
            this.mesh.normals  = this.tempNormals;
        }

        private void Dispatch()
        {
            this.shader.SetFloat(RadiusID, this.radius);
            this.shader.SetFloat(DeltaID, (Mathf.Sin(Time.time) + 1f) / 2f);
            this.shader.Dispatch(this.kernelHandle, this.vertices.Length, 1, 1);
            GetVerticesFromGPU();
        }
    }
}

