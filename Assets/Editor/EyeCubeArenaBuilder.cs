#if UNITY_EDITOR
using System.IO;
using TitanSoul.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TitanSoul.EditorTools
{
    [InitializeOnLoad]
    public static class EyeCubeArenaBuilder
    {
        private const string BackgroundPath =
            "Assets/Art/Maps/EyeCube/EyeCubeArena_Background.png";
        private const string PrefabPath =
            "Assets/Prefabs/Maps/EyeCubeArena.prefab";

        static EyeCubeArenaBuilder()
        {
            EditorApplication.delayCall += BuildIfNeeded;
            EditorApplication.delayCall += PlaceAndSaveIfNeeded;
        }

        [MenuItem("TitanSoul/Map/Rebuild EyeCube Arena Prefab")]
        public static void Rebuild()
        {
            BuildPrefab();
        }

        [MenuItem("TitanSoul/Map/Place EyeCube Arena In Current Scene")]
        public static void PlaceInCurrentScene()
        {
            BuildPrefab();
            if (Object.FindFirstObjectByType<EyeCubeArena>() != null)
            {
                Debug.Log("[TitanSoul] An EyeCubeArena already exists in the current scene.");
                return;
            }

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            GameObject instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            if (instance == null)
                return;

            Undo.RegisterCreatedObjectUndo(instance, "Place EyeCube Arena");
            Selection.activeGameObject = instance;

            Camera mainCamera = Camera.main;
            if (mainCamera != null && mainCamera.GetComponent<ArenaCamera2D>() == null)
                Undo.AddComponent<ArenaCamera2D>(mainCamera.gameObject);

            EditorUtility.SetDirty(instance);
            Debug.Log("[TitanSoul] EyeCubeArena was placed in the current scene.");
        }

        private static void BuildIfNeeded()
        {
            if (File.Exists(BackgroundPath) && !File.Exists(PrefabPath))
                BuildPrefab();
        }

        private static void PlaceAndSaveIfNeeded()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded || string.IsNullOrEmpty(scene.path))
                return;
            if (Object.FindFirstObjectByType<EyeCubeArena>() != null)
                return;

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null)
                return;

            GameObject instance = PrefabUtility.InstantiatePrefab(prefab, scene) as GameObject;
            if (instance == null)
                return;

            Camera mainCamera = Camera.main;
            if (mainCamera != null && mainCamera.GetComponent<ArenaCamera2D>() == null)
                mainCamera.gameObject.AddComponent<ArenaCamera2D>();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Selection.activeGameObject = instance;
            Debug.Log($"[TitanSoul] EyeCubeArena placed and saved in {scene.path}.");
        }

        private static void BuildPrefab()
        {
            ConfigureTexture();
            Sprite background = AssetDatabase.LoadAssetAtPath<Sprite>(BackgroundPath);
            if (background == null)
            {
                Debug.LogError($"[TitanSoul] Arena background was not imported: {BackgroundPath}");
                return;
            }

            Directory.CreateDirectory("Assets/Prefabs/Maps");
            GameObject root = new("EyeCubeArena");
            SpriteRenderer renderer = root.AddComponent<SpriteRenderer>();
            renderer.sprite = background;
            renderer.sortingOrder = -100;

            EyeCubeArena arena = root.AddComponent<EyeCubeArena>();
            CreateWall(root.transform, "Wall_Left", new Vector2(-14.3f, -2.625f), new Vector2(0.6f, 21.35f));
            CreateWall(root.transform, "Wall_Right", new Vector2(14.3f, -2.625f), new Vector2(0.6f, 21.35f));
            CreateWall(root.transform, "Wall_Bottom", new Vector2(0f, -13.3f), new Vector2(29.2f, 0.6f));
            CreateWall(root.transform, "Wall_Top", new Vector2(0f, 8.05f), new Vector2(29.2f, 0.6f));

            Transform playerSpawn = CreateMarker(root.transform, "PlayerSpawn", new Vector2(0f, -9.5f));
            Transform bossSpawn = CreateMarker(root.transform, "BossSpawn", new Vector2(0f, 3.5f));

            SerializedObject serializedArena = new(arena);
            serializedArena.FindProperty("playerSpawn").objectReferenceValue = playerSpawn;
            serializedArena.FindProperty("bossSpawn").objectReferenceValue = bossSpawn;
            serializedArena.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[TitanSoul] EyeCubeArena prefab generated at 50 x 35 world units.");
        }

        private static void ConfigureTexture()
        {
            AssetDatabase.ImportAsset(BackgroundPath, ImportAssetOptions.ForceSynchronousImport);
            TextureImporter importer = AssetImporter.GetAtPath(BackgroundPath) as TextureImporter;
            if (importer == null)
                return;

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 40f;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();
        }

        private static void CreateWall(
            Transform parent,
            string name,
            Vector2 position,
            Vector2 size)
        {
            GameObject wall = new(name);
            wall.transform.SetParent(parent, false);
            wall.transform.localPosition = position;
            BoxCollider2D collider = wall.AddComponent<BoxCollider2D>();
            collider.size = size;
        }

        private static Transform CreateMarker(
            Transform parent,
            string name,
            Vector2 position)
        {
            GameObject marker = new(name);
            marker.transform.SetParent(parent, false);
            marker.transform.localPosition = position;
            return marker.transform;
        }
    }
}
#endif
