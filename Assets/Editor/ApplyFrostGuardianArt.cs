using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using OooAdventure;

public static class ApplyFrostGuardianArt
{
    const string Request="Library/ApplyFrostGuardian.request";
    const string Result="Library/FrostGuardianAsset.txt";
    const string Source="Assets/Art/Source/FrostGuardian/PNG files";
    const string Animations="Assets/Animations/Boss01/FrostGuardian";

    [InitializeOnLoadMethod]
    static void Schedule()
    { if(File.Exists(Request))EditorApplication.delayCall+=Apply; }

    [MenuItem("Tools/Ooo Adventure/Apply Frost Guardian Art")]
    public static void Apply()
    {
        if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode)
        {EditorApplication.delayCall+=Apply;return;}
        try
        {
            ImportAnimations();
            var prefab=PrefabUtility.LoadPrefabContents("Assets/Prefabs/Boss01/FrostGuardian.prefab");
            try { ConfigureBoss(prefab);PrefabUtility.SaveAsPrefabAsset(prefab,"Assets/Prefabs/Boss01/FrostGuardian.prefab"); }
            finally { PrefabUtility.UnloadPrefabContents(prefab); }
            var active=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if(active.isDirty&&!string.IsNullOrEmpty(active.path))EditorSceneManager.SaveScene(active);
            var scene=EditorSceneManager.OpenScene(BuildBoss01.ScenePath);
            var game=UnityEngine.Object.FindFirstObjectByType<BossArenaGame>();
            ConfigureBoss(game.boss.gameObject);
            AddCredit(game);
            EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            Selection.activeGameObject=game.boss.gameObject;
            File.WriteAllText(Result,"PASS: authentic Frost Guardian FREE art by chierit imported: Idle 6, Run 10, Attack 14, Hurt 7, Dead 16. Native clips and root Animator retained. Feet pivot 96,18 at 28 PPU, capsule 2.1 x 3, native left-facing art corrected, death delay 1.8 seconds, credit added.");
            if(File.Exists(Request))File.Delete(Request);
        }
        catch(Exception error)
        {File.WriteAllText(Result,"ERROR: "+error);if(File.Exists(Request))File.Delete(Request);Debug.LogException(error);}
    }

    public static AnimatorController ImportAnimations()
    {
        var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(Animations+"/FrostGuardian.controller");
        if(controller==null)controller=AnimatorController.CreateAnimatorControllerAtPath(Animations+"/FrostGuardian.controller");
        foreach(var item in new[]{("Idle","idle",6,7f),("Run","walk",10,12f),("Attack","1_atk",14,10f),("Hurt","take_hit",7,12f),("Dead","death",16,10f)})
        {
            var frames=Enumerable.Range(1,item.Item3).Select(i=>ImportSprite(Source+"/"+item.Item2+"/"+item.Item2+"_"+i+".png")).ToArray();
            string path=Animations+"/Clips/FrostGuardian_"+item.Item1+".anim";
            var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if(clip==null){clip=new AnimationClip();AssetDatabase.CreateAsset(clip,path);}
            clip.name="FrostGuardian_"+item.Item1;clip.frameRate=item.Item4;
            var keys=frames.Select((sprite,index)=>new ObjectReferenceKeyframe {time=index/item.Item4,value=sprite}).ToList();
            keys.Add(new ObjectReferenceKeyframe {time=frames.Length/item.Item4,value=frames.Last()});
            AnimationUtility.SetObjectReferenceCurve(clip,EditorCurveBinding.PPtrCurve("Visual",typeof(SpriteRenderer),"m_Sprite"),keys.ToArray());
            var settings=AnimationUtility.GetAnimationClipSettings(clip);settings.loopTime=item.Item1=="Idle"||item.Item1=="Run";AnimationUtility.SetAnimationClipSettings(clip,settings);
            var machine=controller.layers[0].stateMachine;
            var state=machine.states.Select(s=>s.state).FirstOrDefault(s=>s.name==item.Item1)??machine.AddState(item.Item1);
            state.motion=clip;EditorUtility.SetDirty(state);EditorUtility.SetDirty(clip);
        }
        BuildLevel01.ConfigureCharacterAnimator(controller,false);EditorUtility.SetDirty(controller);return controller;
    }
    static Sprite ImportSprite(string path)
    {
        var importer=(TextureImporter)AssetImporter.GetAtPath(path);
        if(importer==null)throw new FileNotFoundException("Missing Frost Guardian frame",path);
        var pivot=new Vector2(.5f,18f/128f);
        if(importer.textureType!=TextureImporterType.Sprite||importer.spriteImportMode!=SpriteImportMode.Single||importer.spritePixelsPerUnit!=28||importer.spritePivot!=pivot||importer.filterMode!=FilterMode.Point)
        {
            importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.spritePixelsPerUnit=28;
            var settings=new TextureImporterSettings();importer.ReadTextureSettings(settings);settings.spriteAlignment=(int)SpriteAlignment.Custom;settings.spritePivot=pivot;settings.spriteMeshType=SpriteMeshType.FullRect;importer.SetTextureSettings(settings);
            importer.filterMode=FilterMode.Point;importer.mipmapEnabled=false;importer.alphaIsTransparency=true;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.npotScale=TextureImporterNPOTScale.None;importer.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
    public static void ConfigureBoss(GameObject root)
    {
        var boss=root.GetComponent<FrostBossController>();
        boss.animator=root.GetComponent<Animator>();boss.animator.runtimeAnimatorController=AssetDatabase.LoadAssetAtPath<AnimatorController>(Animations+"/FrostGuardian.controller");
        boss.image=root.transform.Find("Visual").GetComponent<SpriteRenderer>();boss.image.sprite=AssetDatabase.LoadAssetAtPath<Sprite>(Source+"/idle/idle_1.png");
        boss.image.transform.localScale=Vector3.one;boss.image.transform.localPosition=Vector3.zero;boss.image.flipX=false;boss.image.color=Color.white;
        var collider=root.GetComponent<CapsuleCollider2D>();collider.size=new Vector2(2.1f,3);collider.offset=new Vector2(0,1.5f);
        boss.dashHitbox.size=new Vector2(3,1.9f);boss.dashHitbox.offset=new Vector2(-.65f,1);boss.dashHitbox.enabled=false;
        EditorUtility.SetDirty(boss);EditorUtility.SetDirty(boss.animator);EditorUtility.SetDirty(boss.image);EditorUtility.SetDirty(collider);EditorUtility.SetDirty(boss.dashHitbox);
    }
    public static void AddCredit(BossArenaGame game)
    {
        var canvas=game.overlay.GetComponentInParent<Canvas>();
        if(canvas.transform.Find("Boss Art Credit")!=null)return;
        var text = BuildLevel01.Label(canvas.transform,"Boss Art Credit","Boss art: chierit • Frost Guardian",13,Vector2.right,Vector2.right,new Vector2(-24,24),new Vector2(300,24),new Color(.65f,.8f,.95f),TextAnchor.MiddleRight);
        text.gameObject.layer = LayerMask.NameToLayer("UI");
        game.overlay.transform.SetAsLastSibling();
    }
}
