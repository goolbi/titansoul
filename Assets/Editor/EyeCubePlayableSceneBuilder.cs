#if UNITY_EDITOR
using System.Linq;
using TitanSoul.Bosses.EyeCube;
using TitanSoul.Player;
using TitanSoul.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TitanSoul.EditorTools
{
    [InitializeOnLoad]
    public static class EyeCubePlayableSceneBuilder
    {
        private const string ScenePath = "Assets/Scenes/SampleScene.unity";
        private const string ArenaPrefabPath = "Assets/Prefabs/Maps/EyeCubeArena.prefab";

        static EyeCubePlayableSceneBuilder()
        {
            EditorApplication.delayCall += Apply;
        }

        [MenuItem("TitanSoul/Scene/Apply Player And Map Only %#m")]
        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            GameObject arenaPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ArenaPrefabPath);
            if (arenaPrefab == null)
            {
                EditorApplication.delayCall += Apply;
                return;
            }

            Scene scene = SceneManager.GetSceneByPath(ScenePath);
            bool openedTemporarily = !scene.IsValid() || !scene.isLoaded;
            if (openedTemporarily)
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);

            bool changed = RemoveBosses(scene);
            EyeCubeArena arena = FindInScene<EyeCubeArena>(scene);
            if (arena == null)
            {
                GameObject instance = PrefabUtility.InstantiatePrefab(arenaPrefab, scene) as GameObject;
                arena = instance != null ? instance.GetComponent<EyeCubeArena>() : null;
                changed |= instance != null;
            }

            PlayerController player = FindInScene<PlayerController>(scene);
            if (player != null && arena != null && arena.PlayerSpawn != null
                && player.transform.position != arena.PlayerSpawn.position)
            {
                player.transform.position = arena.PlayerSpawn.position;
                changed = true;
            }

            Camera mainCamera = FindInScene<Camera>(scene);
            if (mainCamera != null && mainCamera.GetComponent<ArenaCamera2D>() == null)
            {
                mainCamera.gameObject.AddComponent<ArenaCamera2D>();
                changed = true;
            }

            if (changed)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                Debug.Log("[TitanSoul] SampleScene now contains the player and map only; the boss remains a prefab for later.");
            }

            if (openedTemporarily)
                EditorSceneManager.CloseScene(scene, true);
        }

        private static bool RemoveBosses(Scene scene)
        {
            EyeCubeBoss[] bosses = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<EyeCubeBoss>(true))
                .ToArray();
            foreach (EyeCubeBoss boss in bosses)
                Object.DestroyImmediate(boss.gameObject);

            return bosses.Length > 0;
        }

        private static T FindInScene<T>(Scene scene) where T : Component
        {
            return scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<T>(true))
                .FirstOrDefault();
        }
    }
}
#endif
