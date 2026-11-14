using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEditor.Tilemaps;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using UnityEngine.UI;
using TMPro;
using OooAdventure;

public static class BuildBoss01
{
    public const string ScenePath = "Assets/Scenes/Boss01_FrostGuardian.unity";
    const string Status = "Library/Boss01Build.txt";
    const string Prefabs = "Assets/Prefabs/Boss01";
    static Material material;
    static readonly Color Ice = new Color(.5f,.9f,1);

    [InitializeOnLoadMethod]
    static void Schedule()
    {
        if (!File.Exists(Status) && !File.Exists(ScenePath)) EditorApplication.delayCall += Build;
    }
    [MenuItem("Tools/Ooo Adventure/Create Boss 01")]
    public static void Build()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode)
        { EditorApplication.delayCall += Build; return; }
        if (File.Exists(ScenePath)) return;
        try
        {
            foreach (string folder in new[]{Prefabs,"Assets/Animations/Boss01/FrostGuardian/Clips","Assets/Tiles/Boss01/Decor","Assets/Palettes/Boss01"}) Folder(folder);
            // Save an open edited scene before switching; never overwrite Level 1.
            foreach (var scene in Enumerable.Range(0,SceneManager.sceneCount).Select(SceneManager.GetSceneAt))
                if (scene.isDirty && !string.IsNullOrEmpty(scene.path)) EditorSceneManager.SaveScene(scene);
            var arena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            material = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/OooSpriteUnlit.mat");
            var game = new GameObject("Boss01 Game").AddComponent<BossArenaGame>();
            var finn = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Level01/Finn.prefab"));
            finn.transform.position = new Vector3(-7, .04f, 0); game.player = finn.GetComponent<FinnController>();

            var cave = ImportCave();
            var mapRoot = new GameObject("Map_Boss01");
            var grid = new GameObject("Grid",typeof(Grid));grid.transform.SetParent(mapRoot.transform,false);
            var ground = Map(grid.transform,"GroundTilemap","Ground",0);
            var background = Map(grid.transform,"BackgroundTilemap","Background",-10);
            var decor = Map(grid.transform,"DecorationTilemap","Decor",0);
            var top = AssetDatabase.LoadAssetAtPath<Tile>("Assets/Tiles/Level01/Ground/SnowSurface.asset");
            var stone = AssetDatabase.LoadAssetAtPath<Tile>("Assets/Tiles/Level01/Ground/FrozenStone.asset");
            var wall = CaveTile(cave["BossCave_Wall"],"CaveWall");
            var wallAlt = CaveTile(cave["BossCave_Back"],"CaveBackground");
            var crystal = CaveTile(cave["BossCave_Crystal"],"IceCrystal");
            for (int x=-14;x<=13;x++) for(int y=-3;y<=11;y++)
            {
                if (y<0) ground.SetTile(new Vector3Int(x,y,0),y==-1?top:stone);
                else if(x<-12 || x>=12) ground.SetTile(new Vector3Int(x,y,0),stone);
                else background.SetTile(new Vector3Int(x,y,0),stone);
            }
            background.color=new Color(.17f,.27f,.42f);
            foreach(int x in new[]{-10,-5,4,9})
            {
                for(int y=0;y<9;y++)
                {
                    var cell=new Vector3Int(x,y,0);decor.SetTile(cell,stone);
                    decor.SetTileFlags(cell,TileFlags.None);decor.SetColor(cell,new Color(.42f,.64f,.84f));
                }
                decor.SetTile(new Vector3Int(x,9,0),top);
                decor.SetTile(new Vector3Int(x-1,8,0),top);decor.SetTile(new Vector3Int(x+1,8,0),top);
            }
            foreach(int x in new[]{-11,-4,3,10}) decor.SetTile(new Vector3Int(x,0,0),crystal);
            var rb = ground.gameObject.AddComponent<Rigidbody2D>();rb.bodyType=RigidbodyType2D.Static;
            var composite=ground.gameObject.AddComponent<CompositeCollider2D>();composite.geometryType=CompositeCollider2D.GeometryType.Polygons;
            var tc=ground.gameObject.AddComponent<TilemapCollider2D>();tc.compositeOperation=Collider2D.CompositeOperation.Merge;
            tc.ProcessTilemapChanges();composite.GenerateGeometry();
            if(composite.shapeCount==0)throw new Exception("Arena has no collision geometry.");
            MakePalette(new[]{top,stone,wall,wallAlt,crystal});

            var cameraObject=new GameObject("Main Camera",typeof(Camera),typeof(AudioListener),typeof(CameraFollow2D));
            cameraObject.tag="MainCamera";var camera=cameraObject.GetComponent<Camera>();camera.orthographic=true;
            camera.backgroundColor=new Color(.035f,.07f,.13f);camera.clearFlags=CameraClearFlags.SolidColor;
            var follow=cameraObject.GetComponent<CameraFollow2D>();follow.target=finn.transform;follow.fitWholeArena=true;
            follow.left=-12.6f;follow.right=12.6f;follow.bottom=-2.2f;follow.top=10;follow.arenaPadding=.15f;follow.Snap();
            var projectile=MakeProjectile();var pillar=MakePillar(cave["BossCave_Crystal"]);
            var bossPrefab=MakeBoss(projectile,pillar);
            var boss=(GameObject)PrefabUtility.InstantiatePrefab(bossPrefab);boss.transform.position=new Vector3(6,.04f,0);
            game.boss=boss.GetComponent<FrostBossController>();
            foreach(float x in new[]{-10f,10f})
            {
                var potion=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Level01/WarmPotion.prefab"));
                potion.name="Warm Potion "+x;potion.transform.position=new Vector3(x,.75f,0);
            }
            BuildLevel01.CreateSharedUi(game,game.player.image.sprite);BossUi(game);ApplyFrostGuardianArt.AddCredit(game);
            EditorSceneManager.SaveScene(arena,ScenePath);
            var scenes=EditorBuildSettings.scenes.ToList();
            if(!scenes.Any(s=>s.path==ScenePath)) scenes.Add(new EditorBuildSettingsScene(ScenePath,true));
            EditorBuildSettings.scenes=scenes.ToArray();AssetDatabase.SaveAssets();
            Selection.activeGameObject=boss;
            File.WriteAllText(Status,"PASS: native arena Tilemaps + CompositeCollider, palette, Finn, FrostGuardian prefab and editable Animator, 3 attacks, 2 phases, boss HUD, shared Pause and retry. Scene: "+ScenePath);
        }
        catch(Exception error){File.WriteAllText(Status,"ERROR: "+error);Debug.LogException(error);}
    }
    static void Folder(string path)
    {
        if(AssetDatabase.IsValidFolder(path))return;
        string parent=Path.GetDirectoryName(path).Replace('\\','/');Folder(parent);AssetDatabase.CreateFolder(parent,Path.GetFileName(path));
    }
    static Sprite Ui(int id)
    {return AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Source/kenney_ui-pack-pixel-adventure/Tiles/Small tiles/Thick outline/tile_"+id.ToString("D4")+".png");}
    static Tilemap Map(Transform parent,string name,string layer,int order)
    {
        var go=new GameObject(name,typeof(Tilemap),typeof(TilemapRenderer));go.transform.SetParent(parent,false);go.layer=LayerMask.NameToLayer(layer);
        var renderer=go.GetComponent<TilemapRenderer>();renderer.sharedMaterial=material;renderer.sortingLayerName=layer;renderer.sortingOrder=order;
        return go.GetComponent<Tilemap>();
    }
    static Dictionary<string,Sprite> ImportCave()
    {
        string path=Directory.GetFiles("Assets/Art/Source","cave-platformer-tileset-16x16.png",SearchOption.AllDirectories).Single().Replace('\\','/');
        var importer=(TextureImporter)AssetImporter.GetAtPath(path);
        var factories=new SpriteDataProviderFactories();factories.Init();
        var provider=factories.GetSpriteEditorDataProviderFromObject(importer);provider.InitSpriteEditorDataProvider();
        var rects=provider.GetSpriteRects().ToList();
        foreach(var item in new[]{("Wall",3,0),("Back",6,0),("Crystal",2,10)})
        {
            string name="BossCave_"+item.Item1;
            if(!rects.Any(r=>r.name==name))rects.Add(new SpriteRect {name=name,rect=new Rect(item.Item2*16,176-(item.Item3+1)*16,16,16),alignment=SpriteAlignment.Center,pivot=new Vector2(.5f,.5f),spriteID=GUID.Generate()});
        }
        provider.SetSpriteRects(rects.ToArray());
        provider.GetDataProvider<ISpriteNameFileIdDataProvider>().SetNameFileIdPairs(rects.Select(r=>new SpriteNameFileIdPair(r.name,r.spriteID)));
        provider.Apply();importer.SaveAndReimport();
        return AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().ToDictionary(s=>s.name);
    }
    static Tile CaveTile(Sprite sprite,string name)
    {
        string path="Assets/Tiles/Boss01/Decor/"+name+".asset";
        var tile=AssetDatabase.LoadAssetAtPath<Tile>(path);
        if(tile==null){tile=ScriptableObject.CreateInstance<Tile>();tile.sprite=sprite;tile.colliderType=Tile.ColliderType.None;AssetDatabase.CreateAsset(tile,path);}
        return tile;
    }
    static void MakePalette(Tile[] tiles)
    {
        string path="Assets/Palettes/Boss01/FrostArena.prefab";
        if(File.Exists(path))return;
        var palette=GridPaletteUtility.CreateNewPalette("Assets/Palettes/Boss01","FrostArena",GridLayout.CellLayout.Rectangle,GridPalette.CellSizing.Manual,Vector3.one,GridLayout.CellSwizzle.XYZ);
        var root=PrefabUtility.LoadPrefabContents(AssetDatabase.GetAssetPath(palette));
        try
        {
            var map=root.GetComponentInChildren<Tilemap>();map.GetComponent<TilemapRenderer>().sharedMaterial=material;
            for(int i=0;i<tiles.Length;i++)map.SetTile(new Vector3Int(i,0,0),tiles[i]);
            PrefabUtility.SaveAsPrefabAsset(root,path);
        }
        finally{PrefabUtility.UnloadPrefabContents(root);}
    }
    static SpriteRenderer Sprite(Transform parent,string name,UnityEngine.Sprite sprite,Vector3 pos,Vector3 scale,Color color,int order=0)
    {
        var go=new GameObject(name,typeof(SpriteRenderer));go.transform.SetParent(parent,false);go.transform.localPosition=pos;go.transform.localScale=scale;
        var image=go.GetComponent<SpriteRenderer>();image.sprite=sprite;image.color=color;image.sharedMaterial=material;image.sortingLayerName="Entities";image.sortingOrder=order;return image;
    }
    static IceProjectile MakeProjectile()
    {
        var go=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Level01/IceBolt.prefab"));go.name="FrostBolt";
        var shot=go.GetComponent<IceProjectile>();shot.speed=6.5f;shot.damage=14;
        var renderer=go.GetComponentInChildren<SpriteRenderer>();renderer.color=Ice;go.transform.localScale=Vector3.one*1.3f;
        ApplyIceProjectileArt.ImportAnimations();ApplyIceProjectileArt.ConfigureProjectile(go);
        var prefab=PrefabUtility.SaveAsPrefabAsset(go,Prefabs+"/FrostBolt.prefab");UnityEngine.Object.DestroyImmediate(go);return prefab.GetComponent<IceProjectile>();
    }
    static IcePillarHazard MakePillar(UnityEngine.Sprite crystal)
    {
        var root=new GameObject("IcePillar");root.layer=LayerMask.NameToLayer("Projectile");
        var rb=root.AddComponent<Rigidbody2D>();rb.bodyType=RigidbodyType2D.Kinematic;rb.gravityScale=0;
        var collider=root.AddComponent<BoxCollider2D>();collider.isTrigger=true;collider.size=new Vector2(1.15f,2.8f);collider.offset=new Vector2(0,1.4f);collider.enabled=false;
        root.AddComponent<DamageZone>().damage=18;
        var hazard=root.AddComponent<IcePillarHazard>();hazard.damageCollider=collider;
        hazard.warning=Sprite(root.transform,"Warning - leave red area",Ui(81),new Vector3(0,.08f,0),new Vector3(1.9f,.22f,1),new Color(1,.2f,.3f,.85f),10).gameObject;
        hazard.ice=Sprite(root.transform,"Ice",crystal,new Vector3(0,1.4f,0),new Vector3(1.3f,3.1f,1),new Color(.65f,.95f,1),9).gameObject;hazard.ice.SetActive(false);
        var prefab=PrefabUtility.SaveAsPrefabAsset(root,Prefabs+"/IcePillar.prefab");UnityEngine.Object.DestroyImmediate(root);return prefab.GetComponent<IcePillarHazard>();
    }
    static AnimatorController BossAnimator()
    { return ApplyFrostGuardianArt.ImportAnimations(); }
    static GameObject MakeBoss(IceProjectile projectile,IcePillarHazard pillar)
    {
        var root=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Level01/GateGuardian.prefab"));root.name="FrostGuardian";
        UnityEngine.Object.DestroyImmediate(root.GetComponent<EnemyController>());
        var visibility=root.GetComponentInChildren<EnemyVisibility>();
        if(visibility!=null)UnityEngine.Object.DestroyImmediate(visibility);
        var boss=root.AddComponent<FrostBossController>();boss.animator=root.GetComponent<Animator>();boss.animator.runtimeAnimatorController=BossAnimator();
        boss.image=root.GetComponentInChildren<SpriteRenderer>();boss.image.transform.localScale=Vector3.one*1.4f;boss.image.color=new Color(.7f,.91f,1);
        var collider=root.GetComponent<CapsuleCollider2D>();collider.size=new Vector2(1.4f,3.1f);collider.offset=new Vector2(0,1.55f);
        root.GetComponent<Rigidbody2D>().collisionDetectionMode=CollisionDetectionMode2D.Continuous;
        boss.projectilePrefab=projectile;boss.pillarPrefab=pillar;
        Sprite(root.transform,"Frost Aura",Ui(81),new Vector3(0,1.5f,0),new Vector3(4,4.5f,1),new Color(.2f,.65f,1,.18f),-1);
        var hit=new GameObject("Dash Hitbox");hit.transform.SetParent(root.transform,false);hit.layer=LayerMask.NameToLayer("Projectile");
        boss.dashHitbox=hit.AddComponent<BoxCollider2D>();boss.dashHitbox.isTrigger=true;boss.dashHitbox.size=new Vector2(2.1f,1.7f);boss.dashHitbox.offset=new Vector2(0,.9f);boss.dashHitbox.enabled=false;
        hit.AddComponent<DamageZone>().damage=22;
        ApplyFrostGuardianArt.ConfigureBoss(root);
        var prefab=PrefabUtility.SaveAsPrefabAsset(root,Prefabs+"/FrostGuardian.prefab");UnityEngine.Object.DestroyImmediate(root);return prefab;
    }
    static Image Panel(Transform parent,string name,Vector2 anchor,Vector2 pivot,Vector2 position,Vector2 size,Color color,UnityEngine.Sprite sprite=null)
    {
        var go=new GameObject(name,typeof(RectTransform),typeof(Image));go.layer=LayerMask.NameToLayer("UI");
        var rect=go.GetComponent<RectTransform>();rect.SetParent(parent,false);rect.anchorMin=rect.anchorMax=anchor;rect.pivot=pivot;rect.anchoredPosition=position;rect.sizeDelta=size;
        var image=go.GetComponent<Image>();image.color=color;image.sprite=sprite;image.raycastTarget=false;if(sprite!=null&&sprite.border!=Vector4.zero)image.type=Image.Type.Sliced;return image;
    }
    static TMP_Text Label(Transform parent,string name,string text,int size,Vector2 position,Vector2 bounds,Color color)
    {
        var center = Vector2.one * .5f;
        return BuildLevel01.Label(parent,name,text,size,center,center,position,bounds,color,TextAnchor.MiddleCenter);
    }
    static void BossUi(BossArenaGame game)
    {
        var canvas=game.overlay.GetComponentInParent<Canvas>();
        var defaultFill=AssetDatabase.GetBuiltinExtraResource<UnityEngine.Sprite>("UI/Skin/UISprite.psd");
        game.healthFill.sprite=defaultFill;game.healthText.color=new Color(.025f,.07f,.14f);game.healthText.fontStyle=FontStyles.Bold;
        var objective=game.objectiveText.transform.parent.gameObject;objective.SetActive(false);
        var controls=canvas.transform.Find("Controls");controls.GetComponent<RectTransform>().sizeDelta=new Vector2(810,42);
        controls.GetComponentInChildren<TMP_Text>().text="A D / ← →  Di chuyển      SPACE  Nhảy      ENTER / J  Đánh      ESC  Tạm dừng";
        var bar=Panel(canvas.transform,"Boss Health",new Vector2(.5f,1),new Vector2(.5f,1),new Vector2(0,-140),new Vector2(660,106),new Color(.035f,.07f,.14f,.96f),Ui(12));
        Label(bar.transform,"Name","KẺ GIỮ BĂNG",25,new Vector2(0,26),new Vector2(570,36),Ice).fontStyle=FontStyles.Bold;
        Panel(bar.transform,"Track",new Vector2(.5f,.5f),new Vector2(.5f,.5f),new Vector2(0,-5),new Vector2(600,20),new Color(.1f,.17f,.25f));
        var fill=Panel(bar.transform,"Fill",new Vector2(.5f,.5f),new Vector2(.5f,.5f),new Vector2(0,-5),new Vector2(594,14),Ice,defaultFill);
        fill.type=Image.Type.Filled;fill.fillMethod=Image.FillMethod.Horizontal;fill.fillOrigin=0;game.bossHealthFill=fill;
        game.bossHealthText=Label(bar.transform,"Health","630 / 630",16,new Vector2(0,-5),new Vector2(560,22),new Color(.025f,.07f,.14f));game.bossHealthText.fontStyle=FontStyles.Bold;
        game.bossPhaseText=Label(bar.transform,"Phase","GIAI ĐOẠN 1",14,new Vector2(0,-31),new Vector2(560,24),new Color(.8f,.88f,1));
        game.notificationText.color=new Color(1,.94f,.8f);game.notificationText.fontSize=22;
        var notice=game.notificationText.GetComponent<RectTransform>();notice.anchorMin=notice.anchorMax=new Vector2(.5f,1);notice.pivot=new Vector2(.5f,1);notice.anchoredPosition=new Vector2(0,-260);notice.sizeDelta=new Vector2(1100,58);
        var shadow=game.notificationText.gameObject.AddComponent<Shadow>();shadow.effectColor=new Color(.01f,.02f,.04f,1);shadow.effectDistance=new Vector2(2,-2);
        // The same PNG Home/Play buttons from Level 1 stay above the boss HUD.
        game.overlay.transform.SetAsLastSibling();
        var eyebrow=game.resultPanel.transform.Find("Eyebrow");if(eyebrow!=null)eyebrow.gameObject.SetActive(false);
    }
}
