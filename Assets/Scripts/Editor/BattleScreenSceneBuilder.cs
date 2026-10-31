using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.IO;

namespace MoveCute.UIEditor
{
    public static class BattleScreenSceneBuilder
    {
        public const string ScenePath = "Assets/Scenes/BattleUIScreen.unity";
        public const string PrefabPath = "Assets/Prefabs/UI/BattleScreen.prefab";

        [MenuItem("MoveCute/UI/Upgrade Cached Views")]
        public static void UpgradeRuntimeViews()
        {
            if (EditorApplication.isPlaying) throw new System.InvalidOperationException("Stop Play first.");
            var root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                root.GetComponent<BattleScreenUI>().UpgradeViewComponents();
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        public static void ConfigureRewardDimmingAndDeck()
        {
            if (EditorApplication.isPlaying) throw new System.InvalidOperationException("Stop Play first.");
            var root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                var ui = root.GetComponent<BattleScreenUI>();
                ui.rewardOverlay.GetComponent<UnityEngine.UI.Image>().color = Color.clear;
                var confirmation = ui.rewardOverlay.transform.Find("ConfirmLeave");
                if (confirmation != null) Object.DestroyImmediate(confirmation.gameObject);
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            var scene = SceneManager.GetActiveScene();
            if (scene.path != ScenePath) scene = EditorSceneManager.OpenScene(ScenePath);
            var manager = Object.FindFirstObjectByType<BattleManager>();
            manager.startingDeck.Clear();
            foreach (var name in new[] { "Sit", "Jump", "Retreat", "Dash" })
            {
                var card = AssetDatabase.LoadAssetAtPath<CardData>("Assets/Data/Cards/Card_" + name + ".asset");
                if (card == null) throw new System.InvalidOperationException("Missing basic card: " + name);
                for (int i = 0; i < 5; i++) manager.startingDeck.Add(card);
            }
            EditorUtility.SetDirty(manager);
            EditorSceneManager.SaveScene(scene);
        }

        public static void UpgradeBrowserAndRewards()
        {
            if (EditorApplication.isPlaying) throw new System.InvalidOperationException("Stop Play first.");
            var root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                var ui = root.GetComponent<BattleScreenUI>();
                ui.ConfigureCardBrowserScroll();
                ui.AddPanelSupports();
                ui.UpdateRewardControls();
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        public static void AddRewardScreen()
        {
            if (EditorApplication.isPlaying) throw new System.InvalidOperationException("Stop Play first.");
            var root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                var ui = root.GetComponent<BattleScreenUI>();
                ui.BuildRewardLayout();
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        public static void UpdateInformationLayout()
        {
            if (EditorApplication.isPlaying) throw new System.InvalidOperationException("Stop Play first.");
            var root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                var enemy = root.GetComponent<BattleScreenUI>().enemyPrefab.transform;
                var name = enemy.Find("Name");
                if (name != null) Object.DestroyImmediate(name.gameObject);
                SetRect(enemy.Find("Art"), .10f, .035f, .80f, .72f);
                SetRect(enemy.Find("Bar"), .06f, -16f / 700f, .88f, .045f);
                SetRect(enemy.Find("Morale"), .06f, -16f / 700f, .88f, .045f);
                SetRect(enemy.Find("Statuses"), .06f, -.10f, .88f, .06f);
                var tooltip = root.transform.Find("Tooltip");
                if (tooltip != null) Object.DestroyImmediate(tooltip.gameObject);
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        static void SetRect(Transform target, float x, float y, float width, float height)
        {
            var rt = (RectTransform)target;
            rt.anchorMin = new Vector2(x, y);
            rt.anchorMax = new Vector2(x + width, y + height);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        public static void RebuildRequestedScreen()
        {
            if (EditorApplication.isPlaying) throw new System.InvalidOperationException("Stop Play first.");
            var scene = EditorSceneManager.OpenScene(ScenePath);
            var old = Object.FindFirstObjectByType<BattleScreenUI>();
            if (old != null) Object.DestroyImmediate(old.gameObject);
            BuildScreen(scene);
        }

        [MenuItem("MoveCute/UI/Create Battle Screen Scene")]
        public static void CreateScene()
        {
            if (EditorApplication.isPlaying) throw new System.InvalidOperationException("Stop Play mode first.");
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null &&
                AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null)
            {
                EditorSceneManager.OpenScene(ScenePath);
                return;
            }
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null)
            {
                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
                AssetDatabase.CopyAsset("Assets/Scenes/SampleScene.unity", ScenePath);
            }
            var scene = EditorSceneManager.OpenScene(ScenePath);
            var old = Object.FindFirstObjectByType<TempBattleUI>();
            if (old != null) Object.DestroyImmediate(old.gameObject);
            BuildScreen(scene);
        }

        static void BuildScreen(Scene scene)
        {
            var go = new GameObject("BattleScreen", typeof(RectTransform));
            var ui = go.AddComponent<BattleScreenUI>();
            ui.BuildLayout(Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"));
            PrepareArtwork(ui);
            var prefab = PrefabUtility.SaveAsPrefabAssetAndConnect(go, PrefabPath, InteractionMode.AutomatedAction);
            var manager = Object.FindFirstObjectByType<BattleManager>();
            manager.presenter = ui;
            manager.input = ui;
            manager.showDebugGui = false;
            EditorUtility.SetDirty(manager);
            EditorSceneManager.SaveScene(scene);
            Selection.activeGameObject = go;
            Debug.Log("Battle UI scene created: " + ScenePath);
        }

        static void PrepareArtwork(BattleScreenUI ui)
        {
            const string folder = "Assets/Art/UI";
            Directory.CreateDirectory(folder);
            AssetDatabase.Refresh();
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(folder + "/BasicCardAtlas.png");
            if (texture != null)
            {
                string[] names = { "Sit", "Jump", "Retreat", "Dash" };
                for (int i = 0; i < 4; i++)
                {
                    string path = folder + "/Card" + names[i] + ".asset";
                    var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                    if (sprite == null)
                    {
                        float w = texture.width / 2f, h = texture.height / 2f;
                        sprite = Sprite.Create(texture, new Rect((i % 2) * w, (1 - i / 2) * h, w, h), Vector2.one * .5f, 100);
                        sprite.name = names[i];
                        AssetDatabase.CreateAsset(sprite, path);
                    }
                    ui.basicCardArt[i] = sprite;
                }
            }
            string[] directions = { "Left", "Up", "Right", "Down" };
            float[] angles = { 180, 90, 0, 270 };
            for (int i = 0; i < 4; i++)
            {
                string path = folder + "/Direction" + directions[i] + ".png";
                if (!File.Exists(path))
                {
                    var icon = new Texture2D(128, 128, TextureFormat.RGBA32, false);
                    for (int y = 0; y < 128; y++)
                        for (int x = 0; x < 128; x++)
                        {
                            var p = new Vector2((x - 63.5f) / 64, (y - 63.5f) / 64);
                            float angle = -angles[i] * Mathf.Deg2Rad;
                            var q = new Vector2(p.x * Mathf.Cos(angle) - p.y * Mathf.Sin(angle), p.x * Mathf.Sin(angle) + p.y * Mathf.Cos(angle));
                            bool arrow = q.x > -.48f && q.x < .1f && Mathf.Abs(q.y) < .09f ||
                                q.x >= .05f && q.x <= .53f && Mathf.Abs(q.y) <= (.53f - q.x) * .7f;
                            icon.SetPixel(x, y, p.magnitude > .96f ? Color.clear : p.magnitude > .86f || arrow ? Color.black : Color.white);
                        }
                    icon.Apply();
                    File.WriteAllBytes(path, icon.EncodeToPNG());
                    Object.DestroyImmediate(icon);
                    AssetDatabase.ImportAsset(path);
                    var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                    importer.textureType = TextureImporterType.Sprite;
                    importer.mipmapEnabled = false;
                    importer.alphaIsTransparency = true;
                    importer.SaveAndReimport();
                }
                ui.directionIcons[i] = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            }
            const string materialPath = "Assets/Resources/UI/CardGrayscale.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (!material)
            {
                material = new Material(Shader.Find("MoveCute/UI/CardGrayscale"));
                AssetDatabase.CreateAsset(material, materialPath);
            }
            ui.grayscaleMaterial = material;
            AssetDatabase.SaveAssets();
        }
    }
}
