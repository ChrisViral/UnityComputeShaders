using System.Collections.Generic;
using UnityEngine;

namespace UnityComputeShaders
{
    public class VoxelizeMesh : MonoBehaviour
    {
        private const float RAYCAST_DISTANCE = 100f;

        [SerializeField]
        public Mesh mesh;
        [SerializeField, Range(2, 20)]
        public int verticalCount = 4;
        [SerializeField, Range(0, 31)]
        public int layer = 9;

        public float ParticleSize { get; private set; }

        public List<Vector3> Positions { get; } = new();

        public void Voxelize()
        {
            GameObject temp = new("Temp")
            {
                layer = this.layer
            };
            // ReSharper disable once LocalVariableHidesMember
            MeshCollider collider = temp.AddComponent<MeshCollider>();
            collider.sharedMesh   = this.mesh;

            float radius      = this.mesh.bounds.extents.y / this.verticalCount;
            this.ParticleSize = radius * 2f;

            LayerMask mask   = 1 << this.layer;
            Vector3 min      = this.mesh.bounds.min;
            Vector3 max      = this.mesh.bounds.max;
            Vector3Int count = new((int)(this.mesh.bounds.extents.x / radius),
                                   (int)(this.mesh.bounds.extents.y / radius),
                                   (int)(this.mesh.bounds.extents.z / radius));

            if ((count.x % 2) == 0)
            {
                min.x += this.mesh.bounds.extents.x - (count.x * radius);
            }

            for (float y = min.y + radius; y < max.y; y += this.ParticleSize)
            {
                for (float x = min.x; x < max.x; x += this.ParticleSize)
                {
                    Vector3 origin = temp.transform.position + new Vector3(x, y, min.z);

                    if (!Physics.Raycast(origin, Vector3.forward, out RaycastHit hit, RAYCAST_DISTANCE, mask)) continue;

                    Vector3 front = hit.point;
                    origin.z     += max.z * 2f;
                    if (!Physics.Raycast(origin, Vector3.back, out hit, RAYCAST_DISTANCE, mask)) continue;

                    Vector3 back = hit.point;
                    int n        = Mathf.CeilToInt(front.z / this.ParticleSize);
                    front.z      = n * this.ParticleSize;
                    for (float z = front.z; z < back.z; z += this.ParticleSize)
                    {
                        float distance = back.z - front.z;
                        if (distance < radius / 2f) break;

                        this.Positions.Add(new(front.x, front.y, z));
                    }
                }
            }

            Destroy(temp);
        }
    }
}
