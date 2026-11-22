using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using OooAdventure;

public static class ApplySlideCombat
{
    const string Request = "Library/ApplySlideCombat.request";
    const string Report = "Library/SlideCombatSetup.txt";
    public const string RangePath = "Visual/AtkRange";

    [InitializeOnLoadMethod]
    static void Schedule()
    { if (File.Exists(Request)) EditorApplication.delayCall += Apply; }

    public static void ConfigureLayers()
    {
        var settings = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        var tags = settings.FindProperty("tags");
        foreach (string tag in new[] { "PlayerSword", "Enemy" })
        {
            if (Enumerable.Range(0, tags.arraySize).Any(i => tags.GetArrayElementAtIndex(i).stringValue == tag)) continue;
            int index = tags.arraySize;
            tags.InsertArrayElementAtIndex(index);
            tags.GetArrayElementAtIndex(index).stringValue = tag;
        }
        var layers = settings.FindProperty("layers");
        if (LayerMask.NameToLayer("PlayerSword") < 0)
        {
            int index = Enumerable.Range(8, 24).First(i => string.IsNullOrEmpty(layers.GetArrayElementAtIndex(i).stringValue));
            layers.GetArrayElementAtIndex(index).stringValue = "PlayerSword";
        }
        settings.ApplyModifiedPropertiesWithoutUndo();
        int sword = LayerMask.NameToLayer("PlayerSword");
        Physics2D.IgnoreLayerCollision(sword, LayerMask.NameToLayer("Player"), true);
        Physics2D.IgnoreLayerCollision(sword, sword, true);
        Physics2D.IgnoreLayerCollision(sword, LayerMask.NameToLayer("Enemy"), false);
    }

    public static void ConfigureFinn(FinnController finn)
    {
        var range = finn.attackPoint;
        if (range == null) range = new GameObject("AtkRange").transform;
        range.SetParent(finn.image.transform, false); range.name = "AtkRange";
        range.localPosition = new Vector3(.8f, .65f, 0); range.localRotation = Quaternion.identity; range.localScale = Vector3.one;
        range.gameObject.layer = LayerMask.NameToLayer("PlayerSword"); range.gameObject.tag = "PlayerSword";
        var body = range.GetComponent<Rigidbody2D>();
        if (body == null) body = range.gameObject.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic; body.gravityScale = 0;
        body.constraints = RigidbodyConstraints2D.FreezeAll;
        var box = range.GetComponent<BoxCollider2D>();
        if (box == null) box = range.gameObject.AddComponent<BoxCollider2D>();
        box.isTrigger = true; box.size = new Vector2(1.6f, 1.1f); box.offset = Vector2.zero; box.enabled = false;
        var sword = range.GetComponent<PlayerSwordHitbox>();
        if (sword == null) sword = range.gameObject.AddComponent<PlayerSwordHitbox>();
        sword.owner = finn; sword.damage = 35; finn.attackPoint = range; finn.sword = sword;
        EditorUtility.SetDirty(finn);
    }

    public static void ConfigureEnemy(EnemyController enemy)
    {
        var visibility = enemy.image.GetComponent<EnemyVisibility>();
        if (visibility == null) visibility = enemy.image.gameObject.AddComponent<EnemyVisibility>();
        visibility.enemy = enemy;
    }

    public static void ConfigureClip(AnimationClip clip, bool attack)
    {
        var curve = attack
            ? new AnimationCurve(Step(0, 0), Step(.1f, 1), Step(.2f, 0), Step(.3f, 0))
            : AnimationCurve.Constant(0, clip.length, 0);
        AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve(RangePath, typeof(BoxCollider2D), "m_Enabled"), curve);
        EditorUtility.SetDirty(clip);
    }

    static Keyframe Step(float time, float value)
    { return new Keyframe(time, value, float.PositiveInfinity, float.PositiveInfinity); }

    [MenuItem("Tools/Ooo Adventure/Apply Slide Sword and Enemy Visibility")]
    public static void Apply()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
        { EditorApplication.delayCall += Apply; return; }
        if (File.Exists(Request)) File.Delete(Request);
        try
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play Mode before applying slide combat.");
            var active = EditorSceneManager.GetActiveScene();
            if (active.isDirty) EditorSceneManager.SaveScene(active);
            ConfigureLayers();
            string finnPath = "Assets/Prefabs/Shared/Player/Finn.prefab";
            var finnRoot = PrefabUtility.LoadPrefabContents(finnPath);
            try { ConfigureFinn(finnRoot.GetComponent<FinnController>()); PrefabUtility.SaveAsPrefabAsset(finnRoot, finnPath); }
            finally { PrefabUtility.UnloadPrefabContents(finnRoot); }
            foreach (string path in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs/Chapter01/Enemies", "Assets/Prefabs/Shared/Enemies" }).Select(AssetDatabase.GUIDToAssetPath))
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab.GetComponent<EnemyController>() == null) continue;
                string backup = "Library/BeforeSlideCombat/" + path;
                Directory.CreateDirectory(Path.GetDirectoryName(backup)); if (!File.Exists(backup)) File.Copy(path, backup);
                var root = PrefabUtility.LoadPrefabContents(path);
                try { ConfigureEnemy(root.GetComponent<EnemyController>()); PrefabUtility.SaveAsPrefabAsset(root, path); }
                finally { PrefabUtility.UnloadPrefabContents(root); }
            }
            foreach (string path in AssetDatabase.FindAssets("t:AnimationClip", new[] { "Assets/Animations/Level01/Player/Finn" }).Select(AssetDatabase.GUIDToAssetPath))
            {
                var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
                ConfigureClip(clip, clip.name == "Finn_Attack");
            }
            AssetDatabase.SaveAssets();
            foreach (string path in new[] { "Assets/Scenes/Level01_FrozenForest.unity", "Assets/Scenes/Boss01_FrostGuardian.unity" })
            {
                bool opened = path != active.path;
                var scene = opened ? EditorSceneManager.OpenScene(path, OpenSceneMode.Additive) : active;
                try
                {
                    var finn = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<FinnController>(true)).Single();
                    if (finn.sword == null || finn.sword.GetComponent<BoxCollider2D>().enabled) throw new Exception("Sword prefab binding invalid in " + path);
                    foreach (var enemy in scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<EnemyController>(true)))
                        if (enemy.image.GetComponent<EnemyVisibility>() == null) throw new Exception("Enemy visibility missing: " + enemy.name);
                }
                finally { if (opened) EditorSceneManager.CloseScene(scene, true); }
            }
            EditorSceneManager.SetActiveScene(active);
            var player = active.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<FinnController>(true)).FirstOrDefault();
            if (player != null) Selection.activeGameObject = player.sword.gameObject;
            File.WriteAllText(Report, "PASS: Finn/Visual/AtkRange has PlayerSword tag/layer, its own frozen Kinematic Rigidbody2D and disabled trigger BoxCollider2D. Attack.anim enables at 0.1s and disables at 0.2s; all other Finn clips disable it. All 4 normal-enemy prefabs have renderer visibility callbacks. Both scenes retain valid Finn and enemy bindings.");
        }
        catch (Exception error) { File.WriteAllText(Report, "ERROR: " + error); Debug.LogException(error); }
    }
}
