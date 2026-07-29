#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using TitanSoul.Player;

namespace TitanSoul.EditorTools
{
    [InitializeOnLoad]
    public static class TitanSoulAnimationAssetBuilder
    {
        private const string GeneratedRoot = "Assets/Animations/Generated";
        private const string MarkerPath = GeneratedRoot + "/.generated-v13.txt";
        private const string PlayerSheet = "Assets/Art/Player/Original/PlayerFinalMapped.png";
        private const string PlayerShootSheet = "Assets/Art/Player/Original/PlayerShoot_Original.png";
        private const string MagicArrowSheet = "Assets/Art/Projectiles/Generated/PlainArrow_OriginalStyle.png";
        private const string BossBody = "Assets/Art/EyeCube/Source/EyeCube_Body.png";
        private const string BossClosed = "Assets/Art/EyeCube/Source/EyeCube_Closed.png";
        private const string BossOpen = "Assets/Art/EyeCube/Source/EyeCube_Open.png";
        private const string BossLaser = "Assets/Art/EyeCube/Source/EyeCube_Laser.png";

        static TitanSoulAnimationAssetBuilder()
        {
            EditorApplication.delayCall += BuildIfNeeded;
        }

        [MenuItem("TitanSoul/Animation/Rebuild Player and EyeCube")]
        public static void RebuildFromMenu()
        {
            Build(true);
        }

        private static void BuildIfNeeded()
        {
            if (!File.Exists(MarkerPath)
                && File.Exists(PlayerSheet)
                && File.Exists(PlayerShootSheet)
                && File.Exists(MagicArrowSheet)
                && File.Exists(BossBody)
                && File.Exists(BossClosed)
                && File.Exists(BossOpen)
                && File.Exists(BossLaser))
            {
                Build(true);
            }
        }

        private static void Build(bool force)
        {
            try
            {
                Directory.CreateDirectory(GeneratedRoot);
                Directory.CreateDirectory(GeneratedRoot + "/Player");
                Directory.CreateDirectory(GeneratedRoot + "/EyeCube");
                Directory.CreateDirectory(GeneratedRoot + "/Projectiles");
                Directory.CreateDirectory("Assets/Prefabs/Projectiles/Generated");

                SliceSheet(PlayerSheet, 8, 6, "Final8", 24f);
                SliceSheet(PlayerShootSheet, 5, 1, "PlayerShoot", 20f);
                SliceSheet(MagicArrowSheet, 2, 1, "PlainArrow", 16f);
                SliceSheet(BossBody, 4, 3, "Body");
                SliceSheet(BossClosed, 4, 3, "Closed");
                SliceSheet(BossOpen, 4, 3, "Open");
                SliceSheet(BossLaser, 8, 3, "Laser");

                BuildPlayerAssets(force);
                BuildEyeCubeAssets(force);
                BuildMagicArrowAssets(force);

                File.WriteAllText(
                    MarkerPath,
                    "Generated automatically. Use TitanSoul/Animation/Rebuild Player and EyeCube to rebuild.");
                AssetDatabase.ImportAsset(MarkerPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log("[TitanSoul] Player and EyeCube animation assets were generated.");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        private static void SliceSheet(
            string path,
            int columns,
            int rows,
            string prefix,
            float pixelsPerUnit = 100f)
        {
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
                throw new InvalidOperationException($"TextureImporter was not found: {path}");

            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            int width = texture.width;
            int height = texture.height;
            List<SpriteMetaData> sprites = new(columns * rows);

            for (int topRow = 0; topRow < rows; topRow++)
            {
                int yMin = Mathf.RoundToInt(height * (rows - topRow - 1f) / rows);
                int yMax = Mathf.RoundToInt(height * (rows - topRow) / rows);
                for (int column = 0; column < columns; column++)
                {
                    int xMin = Mathf.RoundToInt(width * column / (float)columns);
                    int xMax = Mathf.RoundToInt(width * (column + 1f) / columns);
                    sprites.Add(new SpriteMetaData
                    {
                        name = $"{prefix}_R{topRow}_C{column}",
                        rect = new Rect(xMin, yMin, xMax - xMin, yMax - yMin),
                        alignment = (int)SpriteAlignment.Custom,
                        pivot = new Vector2(0.5f, 0.12f)
                    });
                }
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = pixelsPerUnit;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.alphaIsTransparency = true;
#pragma warning disable CS0618
            importer.spritesheet = sprites.ToArray();
#pragma warning restore CS0618
            importer.SaveAndReimport();
        }

        private static void BuildPlayerAssets(bool force)
        {
            const string folder = GeneratedRoot + "/Player";
            Dictionary<string, Sprite> sprites = LoadSprites(PlayerSheet);

            // Fixed visual and gameplay order:
            // Down, DownRight, Right, UpRight, Up, UpLeft, Left, DownLeft.
            int[] sourceColumns = { 0, 1, 2, 3, 4, 5, 6, 7 };
            string[] directionNames =
            {
                "Down", "DownRight", "Right", "UpRight",
                "Up", "UpLeft", "Left", "DownLeft"
            };
            AnimationClip[] idleClips = new AnimationClip[8];
            AnimationClip[] moveClips = new AnimationClip[8];
            AnimationClip[] armedClips = new AnimationClip[8];

            for (int direction = 0; direction < 8; direction++)
            {
                int column = sourceColumns[direction];
                Sprite idleSprite = sprites[$"Final8_R0_C{column}"];
                Sprite armedSprite = sprites[$"Final8_R5_C{column}"];
                string suffix = directionNames[direction];
                idleClips[direction] = StaticClip(folder, $"Player_Idle_{suffix}", idleSprite, force);
                moveClips[direction] = ColumnClip(
                    folder,
                    $"Player_Move_{suffix}",
                    sprites,
                    "Final8",
                    column,
                    force);
                armedClips[direction] = StaticClip(
                    folder,
                    $"Player_Armed_{suffix}",
                    armedSprite,
                    force);
            }

            StaticClip(folder, "Player_Dash", sprites["Final8_R1_C2"], force);
            StaticClip(folder, "Player_Hurt", sprites["Final8_R0_C0"], force);
            StaticClip(folder, "Player_Dead", sprites["Final8_R0_C0"], force);
            StaticClip(folder, "Player_Recovery", sprites["Final8_R0_C0"], force);

            string controllerPath = folder + "/PlayerAnimator.controller";
            if (force && AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath) != null)
                AssetDatabase.DeleteAsset(controllerPath);
            if (AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath) != null)
                return;

            AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
            controller.AddParameter("Moving", AnimatorControllerParameterType.Bool);
            controller.AddParameter("Armed", AnimatorControllerParameterType.Bool);
            controller.AddParameter("Direction", AnimatorControllerParameterType.Int);
            controller.AddParameter("Dash", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Hurt", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Dead", AnimatorControllerParameterType.Trigger);

            AnimatorStateMachine machine = controller.layers[0].stateMachine;
            AnimatorState[] idleStates = new AnimatorState[8];
            for (int direction = 0; direction < 8; direction++)
            {
                float x = 100f + direction * 170f;
                idleStates[direction] = machine.AddState($"Idle {directionNames[direction]}", new Vector3(x, 50f));
                idleStates[direction].motion = idleClips[direction];
                AnimatorState moveState = machine.AddState($"Move {directionNames[direction]}", new Vector3(x, 180f));
                moveState.motion = moveClips[direction];
                AnimatorState armedState = machine.AddState($"Armed {directionNames[direction]}", new Vector3(x, 310f));
                armedState.motion = armedClips[direction];

                AddDirectTransition(machine, armedState, direction, true, false);
                AddDirectTransition(machine, moveState, direction, false, true);
                AddDirectTransition(machine, idleStates[direction], direction, false, false);
            }
            machine.defaultState = idleStates[0];
            EditorUtility.SetDirty(controller);
        }

        private static void AddDirectTransition(
            AnimatorStateMachine machine,
            AnimatorState state,
            int direction,
            bool armed,
            bool moving)
        {
            AnimatorStateTransition transition = machine.AddAnyStateTransition(state);
            ConfigureTransition(transition);
            transition.canTransitionToSelf = false;
            transition.AddCondition(
                armed ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot,
                0f,
                "Armed");
            transition.AddCondition(
                moving ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot,
                0f,
                "Moving");
            transition.AddCondition(AnimatorConditionMode.Equals, direction, "Direction");
        }

        private static void BuildMagicArrowAssets(bool force)
        {
            const string folder = GeneratedRoot + "/Projectiles";
            Dictionary<string, Sprite> sprites = LoadSprites(MagicArrowSheet);
            Sprite[] flyFrames =
            {
                sprites["PlainArrow_R0_C0"]
            };
            AnimationClip fly = CreateClip(folder, "MagicArrow_Fly", flyFrames, 12f, true, force);
            AnimationClip impact = StaticClip(
                folder,
                "MagicArrow_Impact",
                sprites["PlainArrow_R0_C1"],
                force);

            string controllerPath = folder + "/MagicArrowAnimator.controller";
            if (force && AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath) != null)
                AssetDatabase.DeleteAsset(controllerPath);
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
            if (controller == null)
            {
                controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
                AnimatorStateMachine machine = controller.layers[0].stateMachine;
                AnimatorState flyState = machine.AddState("Fly");
                flyState.motion = fly;
                machine.defaultState = flyState;
                AnimatorState impactState = machine.AddState("Impact");
                impactState.motion = impact;
                controller.AddParameter("Impact", AnimatorControllerParameterType.Trigger);
                AnimatorStateTransition transition = machine.AddAnyStateTransition(impactState);
                ConfigureTransition(transition);
                transition.AddCondition(AnimatorConditionMode.If, 0f, "Impact");
            }

            string prefabPath = "Assets/Prefabs/Projectiles/Generated/MagicArrow.prefab";
            if (force && AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null)
                AssetDatabase.DeleteAsset(prefabPath);
            if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null)
                return;

            GameObject arrow = new("MagicArrow");
            SpriteRenderer renderer = arrow.AddComponent<SpriteRenderer>();
            renderer.sprite = sprites["PlainArrow_R0_C0"];
            renderer.sortingOrder = 5;
            Animator animator = arrow.AddComponent<Animator>();
            animator.runtimeAnimatorController = controller;
            Rigidbody2D rigidbody = arrow.AddComponent<Rigidbody2D>();
            rigidbody.gravityScale = 0f;
            rigidbody.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            CapsuleCollider2D collider = arrow.AddComponent<CapsuleCollider2D>();
            collider.isTrigger = true;
            collider.direction = CapsuleDirection2D.Horizontal;
            collider.size = new Vector2(1.25f, 0.25f);
            PlayerProjectile projectile = arrow.AddComponent<PlayerProjectile>();

            SerializedObject serializedProjectile = new(projectile);
            serializedProjectile.FindProperty("speed").floatValue = 12f;
            serializedProjectile.FindProperty("damage").intValue = 5;
            serializedProjectile.FindProperty("maxDistance").floatValue = 7f;
            int enemyLayer = LayerMask.NameToLayer("Enemy");
            serializedProjectile.FindProperty("targetLayers").intValue =
                enemyLayer >= 0 ? 1 << enemyLayer : ~0;
            serializedProjectile.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(arrow, prefabPath);
            UnityEngine.Object.DestroyImmediate(arrow);
        }

        private static void BuildEyeCubeAssets(bool force)
        {
            const string folder = GeneratedRoot + "/EyeCube";
            Dictionary<string, Sprite> body = LoadSprites(BossBody);
            Dictionary<string, Sprite> closed = LoadSprites(BossClosed);
            Dictionary<string, Sprite> open = LoadSprites(BossOpen);
            Dictionary<string, Sprite> laser = LoadSprites(BossLaser);

            AnimationClip sleep = RowClip(folder, "EyeCube_Sleep", closed, "Closed", 0, 4, 6f, true, force);
            AnimationClip opening = RowClip(folder, "EyeCube_Open", open, "Open", 0, 4, 10f, false, force);
            AnimationClip moving = RowClip(folder, "EyeCube_Move", body, "Body", 0, 4, 8f, true, force);
            AnimationClip shooting = RowClip(folder, "EyeCube_Shoot", open, "Open", 1, 4, 12f, false, force);
            AnimationClip laserClip = RowClip(folder, "EyeCube_Laser", laser, "Laser", 2, 8, 12f, false, force);
            AnimationClip hurt = RowClip(folder, "EyeCube_Hurt", body, "Body", 1, 4, 14f, false, force);
            AnimationClip dead = RowClip(folder, "EyeCube_Dead", closed, "Closed", 2, 4, 10f, false, force);

            string controllerPath = folder + "/EyeCubeAnimator.controller";
            if (force && AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath) != null)
                AssetDatabase.DeleteAsset(controllerPath);
            if (AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath) != null)
                return;

            AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
            controller.AddParameter("State", AnimatorControllerParameterType.Int);
            controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
            controller.AddParameter("Hurt", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Dead", AnimatorControllerParameterType.Trigger);

            AnimatorStateMachine machine = controller.layers[0].stateMachine;
            AnimatorState[] states =
            {
                AddState(machine, "Sleep", sleep, 100f),
                AddState(machine, "Open", opening, 200f),
                AddState(machine, "Move", moving, 300f),
                AddState(machine, "Shoot", shooting, 400f),
                AddState(machine, "Laser", laserClip, 500f)
            };
            machine.defaultState = states[0];

            for (int index = 0; index < states.Length; index++)
            {
                AnimatorStateTransition transition = machine.AddAnyStateTransition(states[index]);
                ConfigureTransition(transition);
                transition.canTransitionToSelf = false;
                transition.AddCondition(AnimatorConditionMode.Equals, index, "State");
            }

            AnimatorState hurtState = AddState(machine, "Hurt", hurt, 600f);
            AnimatorState deadState = AddState(machine, "Dead", dead, 700f);
            AnimatorStateTransition hurtTransition = machine.AddAnyStateTransition(hurtState);
            ConfigureTransition(hurtTransition);
            hurtTransition.AddCondition(AnimatorConditionMode.If, 0f, "Hurt");
            AnimatorStateTransition deadTransition = machine.AddAnyStateTransition(deadState);
            ConfigureTransition(deadTransition);
            deadTransition.AddCondition(AnimatorConditionMode.If, 0f, "Dead");

            EditorUtility.SetDirty(controller);
        }

        private static BlendTree DirectionalTree(
            AnimatorController controller,
            string name,
            string horizontalParameter,
            string verticalParameter,
            AnimationClip down,
            AnimationClip right,
            AnimationClip up,
            AnimationClip left,
            AnimationClip upLeft,
            AnimationClip downLeft,
            AnimationClip downRight,
            AnimationClip upRight)
        {
            BlendTree tree = new()
            {
                name = name,
                blendType = BlendTreeType.SimpleDirectional2D,
                blendParameter = horizontalParameter,
                blendParameterY = verticalParameter,
                useAutomaticThresholds = false
            };
            tree.AddChild(down, Vector2.down);
            tree.AddChild(right, Vector2.right);
            tree.AddChild(up, Vector2.up);
            tree.AddChild(left, Vector2.left);
            tree.AddChild(upLeft, new Vector2(-1f, 1f).normalized);
            tree.AddChild(downLeft, new Vector2(-1f, -1f).normalized);
            tree.AddChild(downRight, new Vector2(1f, -1f).normalized);
            tree.AddChild(upRight, new Vector2(1f, 1f).normalized);
            AssetDatabase.AddObjectToAsset(tree, controller);
            return tree;
        }

        private static AnimatorState AddState(
            AnimatorStateMachine machine,
            string name,
            AnimationClip clip,
            float x)
        {
            AnimatorState state = machine.AddState(name, new Vector3(x, 200f));
            state.motion = clip;
            return state;
        }

        private static void ConfigureTransition(AnimatorStateTransition transition)
        {
            transition.hasExitTime = false;
            transition.duration = 0.03f;
        }

        private static Dictionary<string, Sprite> LoadSprites(string path)
        {
            return AssetDatabase.LoadAllAssetsAtPath(path)
                .OfType<Sprite>()
                .ToDictionary(sprite => sprite.name, sprite => sprite);
        }

        private static AnimationClip StaticClip(
            string folder,
            string name,
            Sprite sprite,
            bool force)
        {
            return CreateClip(folder, name, new[] { sprite }, 1f, true, force);
        }

        private static AnimationClip RowClip(
            string folder,
            string name,
            Dictionary<string, Sprite> sprites,
            string prefix,
            int row,
            int columns,
            float frameRate,
            bool loop,
            bool force)
        {
            Sprite[] frames = Enumerable.Range(0, columns)
                .Select(column => sprites[$"{prefix}_R{row}_C{column}"])
                .ToArray();
            return CreateClip(folder, name, frames, frameRate, loop, force);
        }

        private static AnimationClip PairClip(
            string folder,
            string name,
            Sprite idle,
            Sprite step,
            bool force)
        {
            return CreateClip(
                folder,
                name,
                new[] { idle, step },
                6f,
                true,
                force);
        }

        private static AnimationClip ColumnClip(
            string folder,
            string name,
            Dictionary<string, Sprite> sprites,
            string prefix,
            int column,
            bool force)
        {
            Sprite[] frames = Enumerable.Range(1, 4)
                .Select(row => sprites[$"{prefix}_R{row}_C{column}"])
                .ToArray();
            return CreateClip(folder, name, frames, 8f, true, force);
        }

        private static AnimationClip CopyClipReference(
            string folder,
            string name,
            AnimationClip source,
            bool force)
        {
            string path = $"{folder}/{name}.anim";
            AnimationClip existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (existing != null && !force)
                return existing;
            if (existing != null)
                AssetDatabase.DeleteAsset(path);

            AnimationClip copy = UnityEngine.Object.Instantiate(source);
            copy.name = name;
            AssetDatabase.CreateAsset(copy, path);
            return copy;
        }

        private static AnimationClip CreateClip(
            string folder,
            string name,
            IReadOnlyList<Sprite> sprites,
            float frameRate,
            bool loop,
            bool force)
        {
            string path = $"{folder}/{name}.anim";
            AnimationClip existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (existing != null && !force)
                return existing;
            if (existing != null)
                AssetDatabase.DeleteAsset(path);

            AnimationClip clip = new() { name = name, frameRate = frameRate };
            EditorCurveBinding binding = new()
            {
                type = typeof(SpriteRenderer),
                path = string.Empty,
                propertyName = "m_Sprite"
            };
            ObjectReferenceKeyframe[] keys = new ObjectReferenceKeyframe[sprites.Count];
            for (int index = 0; index < sprites.Count; index++)
            {
                keys[index] = new ObjectReferenceKeyframe
                {
                    time = index / frameRate,
                    value = sprites[index]
                };
            }
            AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);
            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = loop;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            AssetDatabase.CreateAsset(clip, path);
            return clip;
        }
    }
}
#endif
