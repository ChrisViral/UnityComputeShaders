using System.Collections.Generic;
using UnityEngine;

namespace UnityComputeShaders
{
    public class VoxelizeMesh : MonoBehaviour
    {
        [SerializeField]
        public Mesh mesh;
        [SerializeField, Range(2, 20)]
        public int yParticleCount = 4;
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

            float radius      = this.mesh.bounds.extents.y / this.yParticleCount;
            this.ParticleSize = radius / 2f;

            Vector3 min              = this.mesh.bounds.min;
            Vector3 max              = this.mesh.bounds.max;
            Vector3 count            = this.mesh.bounds.extents / radius;
            Vector3Int particleCount = new((int)count.x, (int)count.y, (int)count.z);
            LayerMask mask           = 1 << this.layer;

            if ((particleCount.x % 2) == 0)
            {
                min.x += (this.mesh.bounds.extents.x - (particleCount.x * radius));
            }

            float zOffset = 0f;
            if (((particleCount.z % 2) == 0))
            {
                zOffset += (this.mesh.bounds.extents.z - (particleCount.z * radius));
            }

            for (float y = min.y + radius; y < max.y; y += this.ParticleSize)
            {
                for (float x = min.x; x < max.x; x += this.ParticleSize)
                {
                    Vector3 offset = new(x, y, min.z);
                    Vector3 origin = temp.transform.position + offset;

                    if (!Physics.Raycast(origin, Vector3.forward, out RaycastHit hit, 100f, mask)) continue;

                    Vector3 front = hit.point;
                    origin.z += max.z * 2f;

                    if (!Physics.Raycast(origin, Vector3.back, out hit, 100f, mask)) continue;

                    Vector3 back = hit.point;
                    int n = Mathf.CeilToInt(front.z / this.ParticleSize);

                    for (float z = front.z; z < back.z; z += this.ParticleSize)
                    {
                        float distance = back.z - z;
                        if (distance < radius / 2f) break;

                        this.Positions.Add(new(front.x, front.y, z + zOffset));
                    }
                }
            }
        }
    }
}
