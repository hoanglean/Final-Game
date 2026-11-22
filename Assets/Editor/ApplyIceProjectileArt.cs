using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using OooAdventure;

public static class ApplyIceProjectileArt
{
    const string Request="Library/ApplyIceProjectile.request";
    const string Status="Library/IceProjectileAsset.txt";
    const string Source="Assets/Art/Chapter01/Source/PinoGames/IceProjectile/Ice projectile";
    const string Folder="Assets/Animations/Boss01/FrostBolt";
    [InitializeOnLoadMethod]
    static void Schedule(){if(File.Exists(Request))EditorApplication.delayCall+=Apply;}

    [MenuItem("Tools/Ooo Adventure/Apply Ice Projectile Art")]
    public static void Apply()
    {
        if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode){EditorApplication.delayCall+=Apply;return;}
        try
        {
            ImportAnimations();
            const string path="Assets/Prefabs/Chapter01/Bosses/FrostBolt.prefab";
            var root=PrefabUtility.LoadPrefabContents(path);
            try{ConfigureProjectile(root);PrefabUtility.SaveAsPrefabAsset(root,path);}
            finally{PrefabUtility.UnloadPrefabContents(root);}
            AssetDatabase.SaveAssets();File.WriteAllText(Status,"PASS: PinoGames F0-F4 imported as Point sprites. FrostBolt uses native Spawn/Fly/Impact Animator; flying frames track the ice tip, F3/F4 shatter on collision. 14 damage, speed 6.5, collider radius .16, PPU 32.");
            if(File.Exists(Request))File.Delete(Request);
        }
        catch(Exception error){File.WriteAllText(Status,"ERROR: "+error);if(File.Exists(Request))File.Delete(Request);Debug.LogException(error);}
    }
    static void MakeFolder(string path)
    {
        if(AssetDatabase.IsValidFolder(path))return;
        string parent=Path.GetDirectoryName(path).Replace('\\','/');MakeFolder(parent);AssetDatabase.CreateFolder(parent,Path.GetFileName(path));
    }
    public static AnimatorController ImportAnimations()
    {
        MakeFolder(Folder+"/Clips");
        var frames=Enumerable.Range(0,5).Select(ImportSprite).ToArray();
        string path=Folder+"/FrostBolt.controller";
        var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(path)??AnimatorController.CreateAnimatorControllerAtPath(path);
        controller.parameters=new[]{new AnimatorControllerParameter {name="Hit",type=AnimatorControllerParameterType.Trigger}};
        var machine=controller.layers[0].stateMachine;
        foreach(var item in new[]{("Spawn",new[]{0,1},16f,false),("Fly",new[]{1,2},12f,true),("Impact",new[]{3,4},12f,false)})
        {
            string clipPath=Folder+"/Clips/FrostBolt_"+item.Item1+".anim";
            var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
            if(clip==null){clip=new AnimationClip();AssetDatabase.CreateAsset(clip,clipPath);}
            clip.name="FrostBolt_"+item.Item1;clip.frameRate=item.Item3;
            var keys=item.Item2.Select((index,i)=>new ObjectReferenceKeyframe {time=i/item.Item3,value=frames[index]}).ToList();
            keys.Add(new ObjectReferenceKeyframe {time=item.Item2.Length/item.Item3,value=frames[item.Item2.Last()]});
            AnimationUtility.SetObjectReferenceCurve(clip,EditorCurveBinding.PPtrCurve("Visual",typeof(SpriteRenderer),"m_Sprite"),keys.ToArray());
            var settings=AnimationUtility.GetAnimationClipSettings(clip);settings.loopTime=item.Item4;AnimationUtility.SetAnimationClipSettings(clip,settings);
            var state=machine.states.Select(s=>s.state).FirstOrDefault(s=>s.name==item.Item1)??machine.AddState(item.Item1);state.motion=clip;state.writeDefaultValues=false;
            foreach(var transition in state.transitions)state.RemoveTransition(transition);
            EditorUtility.SetDirty(clip);EditorUtility.SetDirty(state);
        }
        foreach(var transition in machine.anyStateTransitions)machine.RemoveAnyStateTransition(transition);
        var states=machine.states.ToDictionary(s=>s.state.name,s=>s.state);machine.defaultState=states["Spawn"];
        var children=machine.states;for(int i=0;i<children.Length;i++)children[i].position=new Vector3(220+i*260,120);machine.states=children;
        machine.entryPosition=new Vector3(-30,120);machine.anyStatePosition=new Vector3(740,-50);
        var fly=states["Spawn"].AddTransition(states["Fly"]);fly.duration=0;fly.hasExitTime=true;fly.exitTime=1;
        var hit=machine.AddAnyStateTransition(states["Impact"]);hit.duration=0;hit.hasExitTime=false;hit.canTransitionToSelf=false;hit.AddCondition(AnimatorConditionMode.If,0,"Hit");
        EditorUtility.SetDirty(machine);EditorUtility.SetDirty(controller);return controller;
    }
    static Sprite ImportSprite(int frame)
    {
        string path=Source+"/F"+frame+".png";
        var importer=(TextureImporter)AssetImporter.GetAtPath(path);
        if(importer==null)throw new FileNotFoundException("Missing ice projectile frame",path);
        // Align the moving tip in F0-F2; the impact frames use the burst centre.
        var pixels=new[]{new Vector2(16,31),new Vector2(28,31),new Vector2(49,30),new Vector2(33,31),new Vector2(33,31)};
        var pivot=pixels[frame]/64;
        if(importer.textureType!=TextureImporterType.Sprite||importer.spritePixelsPerUnit!=32||importer.filterMode!=FilterMode.Point||importer.spritePivot!=pivot)
        {
            importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.spritePixelsPerUnit=32;
            var settings=new TextureImporterSettings();importer.ReadTextureSettings(settings);settings.spriteAlignment=(int)SpriteAlignment.Custom;settings.spritePivot=pivot;settings.spriteMeshType=SpriteMeshType.FullRect;importer.SetTextureSettings(settings);
            importer.filterMode=FilterMode.Point;importer.mipmapEnabled=false;importer.alphaIsTransparency=true;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.npotScale=TextureImporterNPOTScale.None;importer.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
    public static void ConfigureProjectile(GameObject root)
    {
        var animator=root.GetComponent<Animator>();
        if(animator==null)animator=root.AddComponent<Animator>();
        animator.runtimeAnimatorController=AssetDatabase.LoadAssetAtPath<AnimatorController>(Folder+"/FrostBolt.controller")??ImportAnimations();
        animator.applyRootMotion=false;animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
        var visual=root.transform.Find("Visual");visual.localScale=Vector3.one;visual.localPosition=Vector3.zero;visual.localRotation=Quaternion.identity;root.transform.localScale=Vector3.one;
        var image=visual.GetComponent<SpriteRenderer>();image.sprite=AssetDatabase.LoadAssetAtPath<Sprite>(Source+"/F1.png");image.color=Color.white;image.flipX=false;image.sortingLayerName="Entities";image.sortingOrder=6;
        var shot=root.GetComponent<IceProjectile>();shot.animator=animator;shot.impactDuration=.22f;shot.damage=14;shot.speed=6.5f;
        var collider=root.GetComponent<CircleCollider2D>();collider.radius=.16f;collider.offset=Vector2.zero;collider.isTrigger=true;
        root.GetComponent<Rigidbody2D>().collisionDetectionMode=CollisionDetectionMode2D.Continuous;
    }
}
