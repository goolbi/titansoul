#if UNITY_EDITOR
using System.IO;
using System.Linq;
using TitanSoul.Bosses.EyeCube;
using TitanSoul.Combat;
using TitanSoul.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TitanSoul.EditorTools
{
    [InitializeOnLoad]
    public static class EyeCubeBossBuilder
    {
        private const string BossPrefabPath = "Assets/Prefabs/Bosses/EyeCubeBoss.prefab";
        private const string ProjectilePrefabPath = "Assets/Prefabs/Projectiles/Generated/EyeCubeOrb.prefab";
        private const string ControllerPath = "Assets/Animations/Generated/EyeCube/EyeCubeAnimator.controller";
        private const string BodyTexturePath = "Assets/Art/EyeCube/Source/EyeCube_Body.png";

        static EyeCubeBossBuilder()
        {
            EditorApplication.delayCall += BuildIfNeeded;
        }

        [MenuItem("TitanSoul/Boss/Rebuild EyeCube Boss")]
        public static void Rebuild()
        {
            BuildPrefabs(true);
        }

        [MenuItem("TitanSoul/Boss/Place EyeCube Boss In Current Scene")]
        public static void PlaceBossInCurrentScene()
        {
            BuildPrefabs(false);
            PlaceInCurrentScene();
        }

        private static void BuildIfNeeded()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            if (!DependenciesReady())
            {
                EditorApplication.delayCall += BuildIfNeeded;
                return;
            }

            BuildPrefabs(false);
        }

        private static bool DependenciesReady()
        {
            return AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(ControllerPath) != null
                && AssetDatabase.LoadAllAssetsAtPath(BodyTexturePath).OfType<Sprite>().Any();
        }

        private static void BuildPrefabs(bool force)
        {
            Directory.CreateDirectory("Assets/Prefabs/Bosses");
            Directory.CreateDirectory("Assets/Prefabs/Projectiles/Generated");

            if (force || AssetDatabase.LoadAssetAtPath<GameObject>(ProjectilePrefabPath) == null)
                BuildProjectilePrefab();

            if (force || AssetDatabase.LoadAssetAtPath<GameObject>(BossPrefabPath) == null)
                BuildBossPrefab();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void BuildProjectilePrefab()
        {
            GameObject orb = new("EyeCubeOrb");
            orb.layer = LayerMask.NameToLayer("Enemy");

            SpriteRenderer renderer = orb.AddComponent<SpriteRenderer>();
            renderer.sprite = AssetDatabase.LoadAllAssetsAtPath(BodyTexturePath)
                .OfType<Sprite>()
                .FirstOrDefault();
            renderer.color = new Color(1f, 0.18f, 0.65f, 1f);
            renderer.sortingOrder = 8;
            orb.transform.localScale = Vector3.one * 0.22f;

            Rigidbody2D rigidbody = orb.AddComponent<Rigidbody2D>();
            rigidbody.gravityScale = 0f;
            rigidbody.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            CircleCollider2D collider = orb.AddComponent<CircleCollider2D>();
            collider.isTrigger = true;
            collider.radius = 1.2f;

            EyeCubeProjectile projectile = orb.AddComponent<EyeCubeProjectile>();
            SerializedObject serialized = new(projectile);
            serialized.FindProperty("speed").floatValue = 7f;
            serialized.FindProperty("damage").intValue = 1;
            serialized.FindProperty("lifetime").floatValue = 6f;
            serialized.FindProperty("targetLayers").intValue = 1 << LayerMask.NameToLayer("Player");
            serialized.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(orb, ProjectilePrefabPath);
            Object.DestroyImmediate(orb);
        }

        private static void BuildBossPrefab()
        {
            GameObject root = new("EyeCubeBoss");
            root.layer = LayerMask.NameToLayer("Enemy");

            SpriteRenderer renderer = root.AddComponent<SpriteRenderer>();
            renderer.sprite = AssetDatabase.LoadAllAssetsAtPath(BodyTexturePath)
                .OfType<Sprite>()
                .FirstOrDefault();
            renderer.sortingOrder = 4;

            Animator animator = root.AddComponent<Animator>();
            animator.runtimeAnimatorController =
                AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(ControllerPath);

            Rigidbody2D rigidbody = root.AddComponent<Rigidbody2D>();
            rigidbody.bodyType = RigidbodyType2D.Kinematic;
            rigidbody.gravityScale = 0f;
            rigidbody.freezeRotation = true;
            BoxCollider2D collider = root.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(3.3f, 3.3f);

            Health health = root.AddComponent<Health>();
            SerializedObject serializedHealth = new(health);
            serializedHealth.FindProperty("maxHealth").intValue = 100;
            serializedHealth.FindProperty("invulnerabilitySeconds").floatValue = 0.08f;
            serializedHealth.ApplyModifiedPropertiesWithoutUndo();

            Transform muzzle = new GameObject("EyeMuzzle").transform;
            muzzle.SetParent(root.transform, false);
            muzzle.localPosition = new Vector3(0f, 0.15f, 0f);

            GameObject laserObject = new("Laser");
            laserObject.transform.SetParent(root.transform, false);
            LineRenderer line = laserObject.AddComponent<LineRenderer>();
            line.positionCount = 2;
            line.useWorldSpace = true;
            line.sortingOrder = 7;
            line.enabled = false;
            EyeCubeLaser laser = laserObject.AddComponent<EyeCubeLaser>();

            SerializedObject serializedLaser = new(laser);
            serializedLaser.FindProperty("line").objectReferenceValue = line;
            serializedLaser.FindProperty("chargingColor").gradientValue = SolidGradient(new Color(1f, 0.25f, 0.7f, 0.8f));
            serializedLaser.FindProperty("firingColor").gradientValue = SolidGradient(new Color(1f, 0.05f, 0.25f, 1f));
            serializedLaser.FindProperty("obstacleLayers").intValue = 1 << LayerMask.NameToLayer("Default");
            serializedLaser.FindProperty("targetLayers").intValue = 1 << LayerMask.NameToLayer("Player");
            serializedLaser.ApplyModifiedPropertiesWithoutUndo();

            EyeCubeBoss boss = root.AddComponent<EyeCubeBoss>();
            SerializedObject serializedBoss = new(boss);
            serializedBoss.FindProperty("eyeMuzzle").objectReferenceValue = muzzle;
            serializedBoss.FindProperty("projectilePrefab").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<EyeCubeProjectile>(ProjectilePrefabPath);
            serializedBoss.FindProperty("laser").objectReferenceValue = laser;
            serializedBoss.FindProperty("arenaMin").vector2Value = new Vector2(-11f, -9f);
            serializedBoss.FindProperty("arenaMax").vector2Value = new Vector2(11f, 5.5f);
            serializedBoss.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, BossPrefabPath);
            Object.DestroyImmediate(root);
        }

        private static Gradient SolidGradient(Color color)
        {
            Gradient gradient = new();
            gradient.SetKeys(
                new[] { new GradientColorKey(color, 0f), new GradientColorKey(color, 1f) },
                new[] { new GradientAlphaKey(color.a, 0f), new GradientAlphaKey(color.a, 1f) });
            return gradient;
        }

        private static void PlaceInCurrentScene()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded || string.IsNullOrEmpty(scene.path))
                return;
            if (Object.FindFirstObjectByType<EyeCubeBoss>() != null)
                return;

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(BossPrefabPath);
            if (prefab == null)
                return;

            GameObject instance = PrefabUtility.InstantiatePrefab(prefab, scene) as GameObject;
            if (instance == null)
                return;

            EyeCubeArena arena = Object.FindFirstObjectByType<EyeCubeArena>();
            instance.transform.position = arena != null && arena.BossSpawn != null
                ? arena.BossSpawn.position
                : new Vector3(0f, 3.5f, 0f);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[TitanSoul] EyeCube boss prefab was built and placed in the active scene.");
        }
    }
}
#endif
