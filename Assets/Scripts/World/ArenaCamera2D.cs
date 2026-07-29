using UnityEngine;

namespace TitanSoul.World
{
    [RequireComponent(typeof(Camera))]
    public sealed class ArenaCamera2D : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private EyeCubeArena arena;
        [SerializeField, Min(0f)] private float followSmoothTime = 0.15f;
        [SerializeField, Min(1f)] private float orthographicSize = 7f;

        private Camera cameraComponent;
        private Vector3 velocity;

        private void Awake()
        {
            cameraComponent = GetComponent<Camera>();
            cameraComponent.orthographic = true;
            cameraComponent.orthographicSize = orthographicSize;

            if (target == null)
            {
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                    target = player.transform;
            }
            if (arena == null)
                arena = FindFirstObjectByType<EyeCubeArena>();
        }

        private void LateUpdate()
        {
            if (target == null || arena == null)
                return;

            Vector3 desired = new(target.position.x, target.position.y, transform.position.z);
            desired = ClampToArena(desired, arena.PlayableBounds);
            transform.position = Vector3.SmoothDamp(
                transform.position,
                desired,
                ref velocity,
                followSmoothTime);
        }

        private Vector3 ClampToArena(Vector3 position, Bounds bounds)
        {
            float halfHeight = cameraComponent.orthographicSize;
            float halfWidth = halfHeight * cameraComponent.aspect;
            float minX = bounds.min.x + halfWidth;
            float maxX = bounds.max.x - halfWidth;
            float minY = bounds.min.y + halfHeight;
            float maxY = bounds.max.y - halfHeight;

            position.x = minX <= maxX
                ? Mathf.Clamp(position.x, minX, maxX)
                : bounds.center.x;
            position.y = minY <= maxY
                ? Mathf.Clamp(position.y, minY, maxY)
                : bounds.center.y;
            return position;
        }
    }
}
