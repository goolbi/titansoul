#if UNITY_EDITOR
using System.IO;
using System.Linq;
using TitanSoul.Bosses.EyeCube;
using TitanSoul.Bosses.FaceCube;
using TitanSoul.Combat;
using TitanSoul.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TitanSoul.EditorTools
{
    public static class FaceCubeBossBuilder
    {
        private const string TexturePath = "Assets/Art/Bosses/FaceCube/FaceCubeBoss_Original.png";
        private const string PrefabPath = "Assets/Prefabs/Bosses/FaceCubeBoss.prefab";

        [MenuItem("TitanSoul/Boss/Rebuild And Place FaceCube Boss")]
        public static void RebuildAndPlace()
        {
            ConfigureTexture();
            BuildPrefab();
            PlaceInCurrentScene();
        }

        private static void ConfigureTexture()
        {
            AssetDatabase.ImportAsset(TexturePath, ImportAssetOptions.ForceSynchronousImport);
            TextureImporter importer = AssetImporter.GetAtPath(TexturePath) as TextureImporter;
            if (importer == null)
                return;

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = 72f;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.alphaIsTransparency = true;
#pragma warning disable CS0618
            importer.spritesheet = new[]
            {
                new SpriteMetaData
                {
                    name = "FaceCubeBoss_Original",
                    rect = new Rect(26f, 609f, 227f, 250f),
                    alignment = (int)SpriteAlignment.Center,
                    pivot = new Vector2(0.5f, 0.5f)
                }
            };
#pragma warning restore CS0618
            importer.SaveAndReimport();
        }

        private static void BuildPrefab()
        {
            Sprite sprite = AssetDatabase.LoadAllAssetsAtPath(TexturePath)
                .OfType<Sprite>()
                .FirstOrDefault(item => item.name == "FaceCubeBoss_Original");
            if (sprite == null)
            {
                Debug.LogError("[TitanSoul] FaceCube sprite is missing.");
                return;
            }

            Directory.CreateDirectory("Assets/Prefabs/Bosses");
            GameObject root = new("FaceCubeBoss");
            root.layer = LayerMask.NameToLayer("Enemy");

            Rigidbody2D body = root.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            body.gravityScale = 0f;
            body.freezeRotation = true;
            BoxCollider2D collider = root.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(3.1f, 3.1f);

            Health health = root.AddComponent<Health>();
            SerializedObject serializedHealth = new(health);
            serializedHealth.FindProperty("maxHealth").intValue = 150;
            serializedHealth.FindProperty("invulnerabilitySeconds").floatValue = 0.05f;
            serializedHealth.ApplyModifiedPropertiesWithoutUndo();

            GameObject visual = new("RollingVisual");
            visual.transform.SetParent(root.transform, false);
            SpriteRenderer renderer = visual.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = 5;

            GameObject shadowObject = new("SlamShadow");
            shadowObject.transform.SetParent(root.transform, false);
            shadowObject.transform.localPosition = new Vector3(0f, -1.5f, 0f);
            SpriteRenderer shadow = shadowObject.AddComponent<SpriteRenderer>();
            shadow.sprite = sprite;
            shadow.color = new Color(0.08f, 0.02f, 0.08f, 0.35f);
            shadow.sortingOrder = 4;
            shadow.enabled = false;

            Transform muzzle = new GameObject("EyeMuzzle").transform;
            muzzle.SetParent(visual.transform, false);
            muzzle.localPosition = new Vector3(0f, 0.45f, 0f);

            GameObject laserObject = new("InstantKillLaser");
            laserObject.transform.SetParent(root.transform, false);
            LineRenderer line = laserObject.AddComponent<LineRenderer>();
            line.positionCount = 2;
            line.useWorldSpace = true;
            line.sortingOrder = 10;
            line.enabled = false;
            line.sharedMaterial = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Line.mat");

            EyeCubeLaser laser = laserObject.AddComponent<EyeCubeLaser>();
            SerializedObject serializedLaser = new(laser);
            serializedLaser.FindProperty("line").objectReferenceValue = line;
            serializedLaser.FindProperty("chargingColor").gradientValue = SolidGradient(new Color(1f, 0.2f, 0.75f, 0.75f));
            serializedLaser.FindProperty("firingColor").gradientValue = SolidGradient(new Color(1f, 0.95f, 1f, 1f));
            serializedLaser.FindProperty("chargingWidth").floatValue = 0.1f;
            serializedLaser.FindProperty("firingWidth").floatValue = 0.75f;
            serializedLaser.FindProperty("damage").intValue = 10;
            serializedLaser.FindProperty("damageInterval").floatValue = 1f;
            serializedLaser.FindProperty("maxDistance").floatValue = 40f;
            serializedLaser.FindProperty("obstacleLayers").intValue = 1 << LayerMask.NameToLayer("Default");
            serializedLaser.FindProperty("targetLayers").intValue = 1 << LayerMask.NameToLayer("Player");
            serializedLaser.ApplyModifiedPropertiesWithoutUndo();

            FaceCubeBoss boss = root.AddComponent<FaceCubeBoss>();
            SerializedObject serializedBoss = new(boss);
            serializedBoss.FindProperty("rollingVisual").objectReferenceValue = visual.transform;
            serializedBoss.FindProperty("laser").objectReferenceValue = laser;
            serializedBoss.FindProperty("eyeMuzzle").objectReferenceValue = muzzle;
            serializedBoss.FindProperty("slamShadow").objectReferenceValue = shadow;
            serializedBoss.FindProperty("slamDamage").intValue = 10;
            serializedBoss.FindProperty("slamTargetLayers").intValue = 1 << LayerMask.NameToLayer("Player");
            serializedBoss.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
            AssetDatabase.SaveAssets();
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
            if (!scene.IsValid() || !scene.isLoaded)
                return;

            foreach (EyeCubeBoss oldBoss in Object.FindObjectsByType<EyeCubeBoss>(FindObjectsSortMode.None))
                Object.DestroyImmediate(oldBoss.gameObject);
            foreach (FaceCubeBoss oldBoss in Object.FindObjectsByType<FaceCubeBoss>(FindObjectsSortMode.None))
                Object.DestroyImmediate(oldBoss.gameObject);

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            GameObject instance = PrefabUtility.InstantiatePrefab(prefab, scene) as GameObject;
            if (instance == null)
                return;

            EyeCubeArena arena = Object.FindFirstObjectByType<EyeCubeArena>();
            instance.transform.position = arena != null && arena.BossSpawn != null
                ? arena.BossSpawn.position
                : new Vector3(0f, 3.5f, 0f);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Selection.activeGameObject = instance;
            Debug.Log("[TitanSoul] FaceCube boss rebuilt and placed in the active scene.");
        }
    }
}
#endif
