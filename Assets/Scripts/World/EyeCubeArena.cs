using UnityEngine;

namespace TitanSoul.World
{
    public sealed class EyeCubeArena : MonoBehaviour
    {
        [SerializeField] private Transform playerSpawn;
        [SerializeField] private Transform bossSpawn;
        [SerializeField] private Vector2 playableMin = new(-14f, -13f);
        [SerializeField] private Vector2 playableMax = new(14f, 7.75f);

        public Transform PlayerSpawn => playerSpawn;
        public Transform BossSpawn => bossSpawn;
        public Bounds PlayableBounds
        {
            get
            {
                Vector2 size = playableMax - playableMin;
                return new Bounds((playableMin + playableMax) * 0.5f, size);
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.magenta;
            Bounds bounds = PlayableBounds;
            Gizmos.DrawWireCube(bounds.center, bounds.size);
        }
    }
}
