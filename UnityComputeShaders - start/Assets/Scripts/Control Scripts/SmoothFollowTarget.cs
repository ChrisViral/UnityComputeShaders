using UnityEngine;

namespace UnityComputeShaders
{
    public class SmoothFollowTarget : MonoBehaviour
    {
        public GameObject target;
        public Vector2 limitsX = new(float.NegativeInfinity, float.PositiveInfinity);
        private Vector3? offset;

        private void LateUpdate()
        {
            if (!this.target)
            {
                this.target = GameObject.FindGameObjectWithTag("Player");
                if (!this.target)
                {
                    return;
                }
            }

            Vector3 transformPosition = this.transform.position;
            Transform targetTransform = this.target.transform;
            Vector3 targetPosition = targetTransform.position;
            this.offset ??= transformPosition - targetPosition;

            Vector3 position = targetPosition + this.offset.Value;
            position.x = Mathf.Clamp(position.x, this.limitsX.x, this.limitsX.y);
            this.transform.position = Vector3.Lerp(transformPosition, position, Time.deltaTime * 5f);
            this.transform.LookAt(targetTransform);
        }
    }
}

