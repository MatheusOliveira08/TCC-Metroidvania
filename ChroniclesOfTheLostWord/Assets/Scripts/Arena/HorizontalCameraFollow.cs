using UnityEngine;

namespace TerraSilente.Arena
{
    [DisallowMultipleComponent]
    public class HorizontalCameraFollow : MonoBehaviour
    {
        [SerializeField] private Transform target;

        private void LateUpdate()
        {
            FollowTarget();
        }

        public void FollowTarget()
        {
            if (target == null)
            {
                return;
            }

            var currentPosition = transform.position;
            transform.position = new Vector3(target.position.x, currentPosition.y, currentPosition.z);
        }
    }
}
