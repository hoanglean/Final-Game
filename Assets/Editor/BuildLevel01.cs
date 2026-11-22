using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using UnityEngine.UI;
using TMPro;
using OooAdventure;

public static class BuildLevel01
{
    const string ScenePath = "Assets/Scenes/Level01_FrozenForest.unity";
    const string Status = "Library/Level01Build.status.txt";
    static Material spriteMaterial;
    static Dictionary<string, Sprite> seasons, ui;
    static Tilemap ground;
    static Tile snowTop, snowFill, iceTop;
    static readonly Color Navy = new Color(.055f, .1f, .18f);
    static readonly Color Ice = new Color(.5f, .88f, .96f);
    static readonly Color Gold = new Color(1, .78f, .36f);

    [InitializeOnLoadMethod]
    static void Schedule()
    {
        if (!File.Exists(Status) && !File.Exists(ScenePath)) EditorApplication.delayCall += Build;
    }
    [MenuItem("Tools/Ooo Adventure/Create Level 01")]
    public static void Build()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode)
        { EditorApplication.delayCall += Build; return; }
        try
        {
            if (File.Exists(ScenePath)) { File.WriteAllText(Status, "EXISTS: Scene preserved. Open " + ScenePath); return; }
            foreach (var folder in new[] { "Assets/Animations/Level01", "Assets/Materials", "Assets/Prefabs/Shared/Player", "Assets/Prefabs/Chapter01/Enemies", "Assets/Prefabs/Shared/Enemies", "Assets/Prefabs/Chapter01/Items", "Assets/Tiles/Level01/Ground", "Assets/Tiles/Level01/Decor" })
                Directory.CreateDirectory(folder);
            AssetDatabase.Refresh();
            SetLayers(); ApplySlideCombat.ConfigureLayers();
            var shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default") ?? Shader.Find("Sprites/Default");
            spriteMaterial = new Material(shader); AssetDatabase.CreateAsset(spriteMaterial, "Assets/Materials/OooSpriteUnlit.mat");
            var noFriction = new PhysicsMaterial2D("PlayerNoFriction") { friction = 0, bounciness = 0 };
            AssetDatabase.CreateAsset(noFriction, "Assets/Materials/PlayerNoFriction.physicsMaterial2D");
            // These are source atlas regions, not newly drawn replacement art.
            seasons = ImportSeasons(); ui = ImportUi();
            var finnFrames = Slice(PathFor("FinnSprite.png"), 32, 16, new Vector2(13f / 32, 6f / 32));
            var finnAnimator = Controller("Finn", new Dictionary<string, Sprite[]> {
                { "Idle", finnFrames.Take(9).ToArray() }, { "Run", finnFrames.Skip(9).Take(7).ToArray() },
                { "Jump", new[] { finnFrames[15] } }, { "Fall", new[] { finnFrames[19] } },
                { "Attack", finnFrames.Skip(25).Take(3).ToArray() }, { "Hurt", finnFrames.Skip(17).Take(2).ToArray() },
                { "Dead", finnFrames.Skip(20).Take(3).ToArray() } });
            var mushroomAnimator = EnemyAnimator("Mushroom");
            var goblinAnimator = EnemyAnimator("Goblin");
            var eyeAnimator = EnemyAnimator("Flying eye");
            var guardianAnimator = EnemyAnimator("Guardian");
            var projectile = ProjectilePrefab();
            var shard = PickupPrefab("ClockShard", WorldPickup.Kind.ClockShard, ui["14"]);
            var heart = PickupPrefab("WarmPotion", WorldPickup.Kind.Healing, seasons["7_8"]);
            var finn = FinnPrefab(finnFrames[0], finnAnimator, noFriction);
            var mushroom = EnemyPrefab("FrostMushroom", EnemyController.Kind.Mushroom, mushroomAnimator, noFriction);
            var goblin = EnemyPrefab("ForestGoblin", EnemyController.Kind.Goblin, goblinAnimator, noFriction);
            var eye = EnemyPrefab("FlyingEye", EnemyController.Kind.FlyingEye, eyeAnimator, noFriction);
            var guardian = EnemyPrefab("GateGuardian", EnemyController.Kind.Guardian, guardianAnimator, noFriction);
            guardian.GetComponent<EnemyController>().projectilePrefab = projectile.GetComponent<IceProjectile>();
            guardian.GetComponent<EnemyController>().shardPrefab = shard.GetComponent<WorldPickup>();
            EditorUtility.SetDirty(guardian); PrefabUtility.SavePrefabAsset(guardian);

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var game = new GameObject("Level01 Game").AddComponent<Level01Game>();
            var grid = new GameObject("Grid", typeof(Grid));
            ground = MakeTilemap(grid.transform, "GroundTilemap", 0);
            ground.gameObject.layer = LayerMask.NameToLayer("Ground");
            var rb = ground.gameObject.AddComponent<Rigidbody2D>(); rb.bodyType = RigidbodyType2D.Static;
            var composite = ground.gameObject.AddComponent<CompositeCollider2D>(); composite.geometryType = CompositeCollider2D.GeometryType.Polygons;
            var mapCollider = ground.gameObject.AddComponent<TilemapCollider2D>();
            mapCollider.compositeOperation = Collider2D.CompositeOperation.Merge;
            var decoration = MakeTilemap(grid.transform, "DecorationTilemap", -2);
            snowTop = TileAsset("SnowSurface", seasons["7_0"]);
            snowFill = TileAsset("FrozenStone", seasons["8_1"]);
            iceTop = TileAsset("IceSurface", seasons["6_0"]);
            Terrain(-8, 25, 0); Terrain(29, 47, 0); Terrain(51, 75, 0); Terrain(80, 115, 0);
            Platform(10, 4, 2); Platform(18, 3, 3); Platform(26, 2, 1);
            Platform(34, 3, 2); Platform(48, 2, 1); Platform(54, 5, 2);
            Platform(64, 3, 2); Platform(68, 3, 4); Platform(76, 2, 1); Platform(85, 3, 2);
            for (int x = 59; x <= 65; x++) ground.SetTile(new Vector3Int(x, -1, 0), iceTop);
            mapCollider.ProcessTilemapChanges(); composite.GenerateGeometry();
            for (int x = -7; x < 114; x += 3)
                if (!new[] { 25, 26, 27, 28, 47, 48, 49, 50, 75, 76, 77, 78, 79 }.Contains(x))
                    decoration.SetTile(new Vector3Int(x, 0, 0), DecorationTile(x % 2 == 0 ? seasons["8_11"] : seasons["9_3"], x));
            Background();
            var playerObject = Spawn(finn, new Vector2(3, .15f)); playerObject.name = "Finn";
            game.player = playerObject.GetComponent<FinnController>();
            var cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener), typeof(CameraFollow2D));
            cameraObject.tag = "MainCamera";
            var camera = cameraObject.GetComponent<Camera>(); camera.orthographic = true; camera.orthographicSize = 5.5f;
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.63f, .77f, .85f);
            var follow = cameraObject.GetComponent<CameraFollow2D>(); follow.target = playerObject.transform;
            cameraObject.transform.position = new Vector3(7, 3.5f, -10);
            Snow(cameraObject.transform);
            Spawn(shard, new Vector2(12, 3.1f)).name = "Clock Gear 1";
            Spawn(shard, new Vector2(56, 3.1f)).name = "Clock Gear 2";
            Spawn(heart, new Vector2(19.5f, 4.1f)); Spawn(heart, new Vector2(69, 5.1f)); Spawn(heart, new Vector2(88, 1.2f));
            Spawn(mushroom, new Vector2(7, .05f)).GetComponent<EnemyController>().patrolRange = 1.3f;
            Spawn(goblin, new Vector2(32, .05f)).GetComponent<EnemyController>().patrolRange = 1.5f;
            Spawn(mushroom, new Vector2(60, .05f)); Spawn(eye, new Vector2(67, 3.5f));
            Spawn(goblin, new Vector2(90, .05f)).GetComponent<EnemyController>().patrolRange = 1.5f;
            Spawn(guardian, new Vector2(99, .05f));
            Sign(5, "Jake: Ooo đang bị đóng băng! Tìm 3 bánh răng để sửa cổng thời gian.\nNhấn SPACE để nhảy và ENTER để đánh kiếm.");
            Sign(23, "Jake: Đường phía trước đã gãy. Nhảy qua các bệ băng; chạm nước lạnh sẽ mất mạng.");
            Sign(91, "Jake: Kẻ giữ cổng đang giữ bánh răng cuối.\nNé đạn của hắn rồi áp sát, đánh kiếm!");
            foreach (var pit in new[] { new Vector2(25, 29), new Vector2(47, 51), new Vector2(75, 80) }) Hazard(pit.x, pit.y);
            var portalSprite = CropSprite("Assets/Art/Chapter02/Source/Sci-Fi Platformer Tileset [16x16][FREE] - RottingPixels/Sci-Fi Platformer Tileset [16x16][FREE] - RottingPixels/sci-fi-tileset.png", "TimePortal", new Rect(0, 0, 48, 48), 16, new Vector2(.5f, 0));
            var portal = Pickup("Time Gate - E to finish", WorldPickup.Kind.Exit, portalSprite, new Vector2(109, 0));
            portal.GetComponent<BoxCollider2D>().size = new Vector2(2.5f, 3);
            portal.GetComponent<BoxCollider2D>().offset = new Vector2(0, 1.5f);
            portal.visual.GetComponent<SpriteRenderer>().color = new Color(.65f, 1, 1);
            BuildUi(game, finnFrames[0]);
            scene.name = "Level01_FrozenForest";
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene, ScenePath);
            var scenes = EditorBuildSettings.scenes.Where(s => s.path != ScenePath).ToList();
            scenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true)); EditorBuildSettings.scenes = scenes.ToArray();
            AssetDatabase.SaveAssets();
            Selection.activeGameObject = playerObject;
            if (SceneView.lastActiveSceneView != null)
            { SceneView.lastActiveSceneView.in2DMode = true; SceneView.lastActiveSceneView.LookAt(new Vector3(52, 2, 0), Quaternion.identity, 60, true); }
            File.WriteAllText(Status, "PASS: Level01_FrozenForest created. Native Tilemaps+CompositeCollider, Finn, 6 enemies, ranged guardian, 3 clock shards, hazards, camera, parallax, snow, HUD and intro/pause/death/win panels. C# runtime only.");
        }
        catch (Exception ex) { File.WriteAllText(Status, "ERROR: " + ex); Debug.LogException(ex); }
    }
    static void SetLayers()
    {
        var settings = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        var layers = settings.FindProperty("layers");
        int i = 6; foreach (var layer in new[] { "Ground", "Player", "Enemy", "Projectile", "Pickup" })
        { layers.GetArrayElementAtIndex(i++).stringValue = layer; }
        settings.ApplyModifiedPropertiesWithoutUndo();
    }
    static string PathFor(string suffix)
    {
        var result = new[] { "Assets/Art/Chapter01/Source", "Assets/Art/Shared/Source" }
            .SelectMany(folder => Directory.GetFiles(folder, "*.png", SearchOption.AllDirectories))
            .FirstOrDefault(p => p.Replace('\\','/').EndsWith(suffix, StringComparison.OrdinalIgnoreCase));
        if (result == null) throw new FileNotFoundException("Asset missing: " + suffix);
        return result.Replace('\\', '/');
    }
    static TextureImporter Texture(string path, float ppu, SpriteImportMode mode)
    {
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        var t = (TextureImporter)AssetImporter.GetAtPath(path);
        t.textureType = TextureImporterType.Sprite; t.spriteImportMode = mode; t.spritePixelsPerUnit = ppu;
        t.filterMode = FilterMode.Point; t.mipmapEnabled = false; t.alphaIsTransparency = true;
        t.textureCompression = TextureImporterCompression.Uncompressed; t.npotScale = TextureImporterNPOTScale.None;
        t.SaveAndReimport(); return t;
    }
    static void Rectangles(TextureImporter importer, List<SpriteRect> rectangles)
    {
        var factory = new SpriteDataProviderFactories(); factory.Init();
        var provider = factory.GetSpriteEditorDataProviderFromObject(importer); provider.InitSpriteEditorDataProvider();
        provider.SetSpriteRects(rectangles.ToArray());
        var names = provider.GetDataProvider<ISpriteNameFileIdDataProvider>();
        names.SetNameFileIdPairs(rectangles.Select(r => new SpriteNameFileIdPair(r.name, r.spriteID)));
        provider.Apply(); importer.SaveAndReimport();
    }
    static Sprite[] Slice(string path, int size, float ppu, Vector2 pivot)
    {
        var importer = Texture(path, ppu, SpriteImportMode.Multiple);
        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path); var rects = new List<SpriteRect>();
        for (int x = 0; x < texture.width / size; x++) rects.Add(new SpriteRect {
            name = "frame_" + x.ToString("D3"), rect = new Rect(x * size, 0, size, size),
            alignment = SpriteAlignment.Custom, pivot = pivot, spriteID = GUID.Generate() });
        Rectangles(importer, rects);
        return AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().OrderBy(s => s.name).ToArray();
    }
    static Dictionary<string, Sprite> ImportSeasons()
    {
        string path = PathFor("four-seasons-tileset.png");
        var importer = Texture(path, 16, SpriteImportMode.Multiple); var rects = new List<SpriteRect>();
        for (int y = 0; y < 16; y++) for (int x = 0; x < 11; x++)
            rects.Add(new SpriteRect { name = x + "_" + y, rect = new Rect(x*16, 256-(y+1)*16,16,16),
                pivot = new Vector2(.5f,.5f), alignment = SpriteAlignment.Center, spriteID = GUID.Generate() });
        rects.Add(new SpriteRect { name = "WinterTree", rect = new Rect(128, 256-112, 16, 48), pivot = new Vector2(.5f,0), alignment = SpriteAlignment.BottomCenter, spriteID = GUID.Generate() });
        rects.Add(new SpriteRect { name = "TallTree", rect = new Rect(0, 256-96, 16, 48), pivot = new Vector2(.5f,0), alignment = SpriteAlignment.BottomCenter, spriteID = GUID.Generate() });
        Rectangles(importer,rects); return AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().ToDictionary(s=>s.name);
    }
    static Dictionary<string, Sprite> ImportUi()
    {
        var result = new Dictionary<string,Sprite>();
        foreach (var id in new[] {12,14,49,50,53,63,64,76,81,85,93,96,104})
        {
            string path = PathFor("kenney_ui-pack-pixel-adventure/Tiles/Small tiles/Thick outline/tile_" + id.ToString("D4") + ".png");
            var t = Texture(path,16,SpriteImportMode.Single);
            if (id == 12 || id == 49) { t.spriteBorder = new Vector4(4,4,4,4); t.SaveAndReimport(); }
            result[id.ToString()] = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
        return result;
    }
    static Sprite CropSprite(string path, string name, Rect topRect, float ppu, Vector2 pivot)
    {
        var importer = Texture(path, ppu, SpriteImportMode.Multiple); var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        Rectangles(importer, new List<SpriteRect> { new SpriteRect { name = name,
            rect = new Rect(topRect.x, texture.height-topRect.y-topRect.height,topRect.width,topRect.height),
            alignment = SpriteAlignment.Custom, pivot=pivot,spriteID=GUID.Generate() } });
        return AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().Single();
    }
    static AnimatorController EnemyAnimator(string name)
    {
        bool wizard = name == "Guardian", flying = name == "Flying eye";
        string prefix = wizard ? "EVil Wizard 2/EVil Wizard 2/Sprites/" : flying ? "Enemies/Flying eye/" : "Monsters_Creatures_Fantasy/Monsters_Creatures_Fantasy/" + name + "/";
        var frames = new Dictionary<string,Sprite[]>();
        foreach (var state in new[] { "Idle", "Run", "Attack", "Hurt", "Dead" })
        {
            string file = state == "Hurt" ? (wizard ? "Take hit" : "Take Hit") : state == "Dead" ? "Death" : state;
            if (wizard && state == "Attack") file = "Attack1";
            if (flying && (state == "Idle" || state == "Run")) file = "Flight";
            frames[state] = Slice(PathFor(prefix + file + ".png"), wizard ? 250 : 150, wizard ? 40 : 32,
                wizard ? new Vector2(.55f,83f/250) : new Vector2(.5f,49f/150));
        }
        return Controller(name.Replace(" ",""), frames);
    }
    static AnimatorController Controller(string name, Dictionary<string,Sprite[]> clips)
    {
        string folder = CharacterAnimationFolder(name);
        Directory.CreateDirectory(folder+"/Clips"); AssetDatabase.Refresh();
        var controller = AnimatorController.CreateAnimatorControllerAtPath(folder+"/"+name+".controller");
        int index=0;
        foreach (var item in clips)
        {
            bool loop=item.Key=="Idle" || item.Key=="Run";
            float fps=item.Key=="Idle" ? 7 : item.Key=="Run" ? 12 : 10;
            var clip=new AnimationClip { name=name+"_"+item.Key,frameRate=fps };
            var keys=item.Value.Select((s,i)=>new ObjectReferenceKeyframe { time=i/fps,value=s }).ToList();
            keys.Add(new ObjectReferenceKeyframe { time=item.Value.Length/fps, value=item.Value.Last() });
            AnimationUtility.SetObjectReferenceCurve(clip,EditorCurveBinding.PPtrCurve("Visual",typeof(SpriteRenderer),"m_Sprite"),keys.ToArray());
            var settings=AnimationUtility.GetAnimationClipSettings(clip);settings.loopTime=loop;AnimationUtility.SetAnimationClipSettings(clip,settings);
            AssetDatabase.CreateAsset(clip,folder+"/Clips/"+clip.name+".anim");
            if (name == "Finn") ApplySlideCombat.ConfigureClip(clip, item.Key == "Attack");
            var state=controller.layers[0].stateMachine.AddState(item.Key,new Vector3(index%3*230,index/3*100));state.motion=clip;
            if(index++==0)controller.layers[0].stateMachine.defaultState=state;
        }
        ConfigureCharacterAnimator(controller,name=="Finn");
        return controller;
    }
    public static string CharacterAnimationFolder(string name)
    {
        if (name == "Flyingeye") return "Assets/Animations/Shared/Enemies/FlyingEye";
        return "Assets/Animations/Level01/" + (name == "Finn" ? "Player/Finn" : "Enemies/" + name);
    }
    public static void ConfigureCharacterAnimator(AnimatorController controller,bool player)
    {
        controller.parameters=new[] {
            new AnimatorControllerParameter {name="Run",type=AnimatorControllerParameterType.Bool},
            new AnimatorControllerParameter {name="Die",type=AnimatorControllerParameterType.Trigger},
            new AnimatorControllerParameter {name="Atk",type=AnimatorControllerParameterType.Trigger},
            new AnimatorControllerParameter {name="Hurt",type=AnimatorControllerParameterType.Trigger}
        };
        if(player)
        {
            controller.AddParameter(new AnimatorControllerParameter {name="Grounded",type=AnimatorControllerParameterType.Bool,defaultBool=true});
            controller.AddParameter("VerticalSpeed",AnimatorControllerParameterType.Float);
        }
        var machine=controller.layers[0].stateMachine;
        var states=machine.states.ToDictionary(s=>s.state.name,s=>s.state);
        foreach(var state in states.Values) foreach(var transition in state.transitions) state.RemoveTransition(transition);
        foreach(var transition in machine.anyStateTransitions) machine.RemoveAnyStateTransition(transition);
        var positions=new Dictionary<string,Vector3> {
            {"Idle",new Vector3(260,100)}, {"Run",new Vector3(620,100)},
            {"Jump",new Vector3(260,-100)}, {"Fall",new Vector3(620,-100)},
            {"Attack",new Vector3(260,340)}, {"Hurt",new Vector3(620,340)}, {"Dead",new Vector3(1000,340)}
        };
        var children=machine.states;
        for(int i=0;i<children.Length;i++) {children[i].position=positions[children[i].state.name];children[i].state.writeDefaultValues=false;}
        machine.states=children;machine.defaultState=states["Idle"];
        machine.entryPosition=new Vector3(-30,100);machine.anyStatePosition=new Vector3(620,550);machine.exitPosition=new Vector3(1240,550);
        var dead=machine.AddAnyStateTransition(states["Dead"]);Instant(dead);dead.AddCondition(AnimatorConditionMode.If,0,"Die");
        foreach(string action in new[]{"Hurt","Attack"})
        {
            var transition=machine.AddAnyStateTransition(states[action]);Instant(transition);
            transition.AddCondition(AnimatorConditionMode.If,0,action=="Attack" ? "Atk" : "Hurt");
        }
        foreach(string from in new[]{"Idle","Run","Attack","Hurt"}.Concat(player ? new[]{"Jump","Fall"} : new string[0]))
            foreach(string to in player ? new[]{"Idle","Run","Jump","Fall"} : new[]{"Idle","Run"})
            {
                if(from==to) continue;
                var transition=states[from].AddTransition(states[to]);Instant(transition);
                if(from=="Attack" || from=="Hurt") {transition.hasExitTime=true;transition.exitTime=1;}
                if(to=="Idle" || to=="Run")
                {
                    transition.AddCondition(to=="Idle" ? AnimatorConditionMode.IfNot : AnimatorConditionMode.If,0,"Run");
                    if(player) transition.AddCondition(AnimatorConditionMode.If,0,"Grounded");
                }
                else
                {
                    transition.AddCondition(AnimatorConditionMode.IfNot,0,"Grounded");
                    transition.AddCondition(to=="Jump" ? AnimatorConditionMode.Greater : AnimatorConditionMode.Less,.1f,"VerticalSpeed");
                }
            }
        EditorUtility.SetDirty(controller);EditorUtility.SetDirty(machine);
    }
    static void Instant(AnimatorStateTransition transition)
    {transition.duration=0;transition.hasFixedDuration=true;transition.hasExitTime=false;transition.canTransitionToSelf=false;}
    static GameObject FinnPrefab(Sprite sprite, RuntimeAnimatorController controller, PhysicsMaterial2D material)
    {
        var go=new GameObject("Finn");go.layer=LayerMask.NameToLayer("Player");
        var rb=go.AddComponent<Rigidbody2D>();rb.gravityScale=3;rb.constraints=RigidbodyConstraints2D.FreezeRotation;rb.collisionDetectionMode=CollisionDetectionMode2D.Continuous;rb.interpolation=RigidbodyInterpolation2D.Interpolate;
        var c=go.AddComponent<BoxCollider2D>();c.size=new Vector2(.65f,1.15f);c.offset=new Vector2(0,.575f);c.sharedMaterial=material;
        var visual=Visual(go.transform,"Visual",sprite,5);var animator=go.AddComponent<Animator>();animator.runtimeAnimatorController=controller;
        var p=go.AddComponent<FinnController>();p.animator=animator;p.image=visual;p.groundMask=1<<LayerMask.NameToLayer("Ground");p.enemyMask=1<<LayerMask.NameToLayer("Enemy");
        p.attackPoint=new GameObject("AttackPoint").transform;p.attackPoint.SetParent(go.transform,false);p.attackPoint.localPosition=new Vector3(.8f,.65f,0);
        ApplySlideCombat.ConfigureFinn(p);
        return SavePrefab(go, "Assets/Prefabs/Shared/Player", "Finn");
    }
    static GameObject EnemyPrefab(string name, EnemyController.Kind kind, AnimatorController controller, PhysicsMaterial2D material)
    {
        var go=new GameObject(name);go.layer=LayerMask.NameToLayer("Enemy");
        var rb=go.AddComponent<Rigidbody2D>();rb.gravityScale=kind==EnemyController.Kind.FlyingEye?0:3;rb.constraints=RigidbodyConstraints2D.FreezeRotation;rb.interpolation=RigidbodyInterpolation2D.Interpolate;
        var c=go.AddComponent<CapsuleCollider2D>();c.size=kind==EnemyController.Kind.Guardian?new Vector2(1,2.2f):new Vector2(.8f,1.1f);c.offset=Vector2.up*c.size.y/2;c.sharedMaterial=material;
        var first=(Sprite)AnimationUtility.GetObjectReferenceCurve(controller.animationClips.First(),EditorCurveBinding.PPtrCurve("Visual",typeof(SpriteRenderer),"m_Sprite"))[0].value;
        var visual=Visual(go.transform,"Visual",first,4);var animator=go.AddComponent<Animator>();animator.runtimeAnimatorController=controller;
        var e=go.AddComponent<EnemyController>();e.kind=kind;e.animator=animator;e.image=visual;e.groundMask=1<<LayerMask.NameToLayer("Ground");
        if (kind == EnemyController.Kind.Guardian)
        {
            e.maxHealth = 140;
            e.shotOrigin = new GameObject("Pos").transform;
            e.shotOrigin.SetParent(go.transform, false);
            e.shotOrigin.localPosition = new Vector3(.8f, 1, 0);
        }
        else
        {
            var range = new GameObject("Attack Range") { tag = "Enemy", layer = LayerMask.NameToLayer("Player") };
            range.transform.SetParent(go.transform, false);
            var trigger = range.AddComponent<CapsuleCollider2D>();
            trigger.isTrigger = true;
            trigger.size = new Vector2(3f, 1.4f);
            trigger.offset = c.offset;
            trigger.direction = CapsuleDirection2D.Horizontal;
            e.attackRange = trigger;
        }
        if(kind==EnemyController.Kind.Mushroom){e.maxHealth=35;e.speed=1.1f;}
        if(kind==EnemyController.Kind.FlyingEye)e.maxHealth=35;
        ApplySlideCombat.ConfigureEnemy(e);
        string folder = kind == EnemyController.Kind.FlyingEye ? "Assets/Prefabs/Shared/Enemies" : "Assets/Prefabs/Chapter01/Enemies";
        return SavePrefab(go, folder, name);
    }
    static GameObject ProjectilePrefab()
    {
        var go=new GameObject("IceBolt");go.layer=LayerMask.NameToLayer("Projectile");
        var v=Visual(go.transform,"Visual",ui["81"],6);v.transform.localScale=Vector3.one*.55f;v.color=Ice;
        var c=go.AddComponent<CircleCollider2D>();c.radius=.18f;c.isTrigger=true;
        var rb=go.AddComponent<Rigidbody2D>();rb.gravityScale=0;rb.collisionDetectionMode=CollisionDetectionMode2D.Continuous;
        go.AddComponent<IceProjectile>();return SavePrefab(go, "Assets/Prefabs/Chapter01/Enemies", "IceBolt");
    }
    static GameObject PickupPrefab(string name,WorldPickup.Kind kind,Sprite sprite)
    { var go=Pickup(name,kind,sprite,Vector2.zero).gameObject;return SavePrefab(go, "Assets/Prefabs/Chapter01/Items", name); }
    static WorldPickup Pickup(string name,WorldPickup.Kind kind,Sprite sprite,Vector2 position)
    {
        var go=new GameObject(name);go.transform.position=position;go.layer=LayerMask.NameToLayer("Pickup");
        var item=go.AddComponent<WorldPickup>();item.kind=kind;item.visual=Visual(go.transform,"Visual",sprite,3).transform;
        var c=go.AddComponent<BoxCollider2D>();c.isTrigger=true;c.size=new Vector2(.8f,1.1f);return item;
    }
    static void Sign(float x,string text)
    {var p=Pickup("Sign "+x,WorldPickup.Kind.Sign,seasons["4_3"],new Vector2(x,.4f));p.message=text;}
    static void Hazard(float start,float end)
    {
        var go=Pickup("Freezing Water",WorldPickup.Kind.Hazard,seasons["4_9"],new Vector2((start+end)*.5f,-2));
        go.visual.localScale=new Vector3(end-start,2,1);go.GetComponent<BoxCollider2D>().size=new Vector2(end-start,2);
    }
    static SpriteRenderer Visual(Transform parent,string name,Sprite sprite,int order)
    {var g=new GameObject(name);g.transform.SetParent(parent,false);var s=g.AddComponent<SpriteRenderer>();s.sprite=sprite;s.sharedMaterial=spriteMaterial;s.sortingOrder=order;return s;}
    static GameObject SavePrefab(GameObject go, string folder, string name)
    {var saved=PrefabUtility.SaveAsPrefabAsset(go,folder + "/" + name + ".prefab");UnityEngine.Object.DestroyImmediate(go);return saved;}
    static GameObject Spawn(GameObject prefab,Vector2 p)
    {var go=(GameObject)PrefabUtility.InstantiatePrefab(prefab);go.transform.position=p;return go;}
    static Tilemap MakeTilemap(Transform parent,string name,int order)
    {var go=new GameObject(name,typeof(Tilemap),typeof(TilemapRenderer));go.transform.SetParent(parent,false);var r=go.GetComponent<TilemapRenderer>();r.sortingOrder=order;r.sharedMaterial=spriteMaterial;return go.GetComponent<Tilemap>();}
    static Tile TileAsset(string name,Sprite sprite)
    {var t=ScriptableObject.CreateInstance<Tile>();t.sprite=sprite;t.colliderType=Tile.ColliderType.Grid;AssetDatabase.CreateAsset(t,"Assets/Tiles/Level01/Ground/"+name+".asset");return t;}
    static Tile DecorationTile(Sprite sprite,int x)
    {var t=ScriptableObject.CreateInstance<Tile>();t.sprite=sprite;t.colliderType=Tile.ColliderType.None;AssetDatabase.CreateAsset(t,"Assets/Tiles/Level01/Decor/Decoration_"+x+".asset");return t;}
    static void Terrain(int from,int to,int surface)
    {for(int x=from;x<to;x++)for(int y=-4;y<surface;y++)ground.SetTile(new Vector3Int(x,y,0),y==surface-1?snowTop:snowFill);}
    static void Platform(int x,int width,int surface)
    {for(int i=0;i<width;i++) {ground.SetTile(new Vector3Int(x+i,surface-1,0),snowTop);if(width>2)ground.SetTile(new Vector3Int(x+i,surface-2,0),snowFill);}}
    static void Background()
    {
        var root=new GameObject("Background - Parallax Forest");root.AddComponent<ParallaxLayer>().factor=.22f;
        var random=new System.Random(12);
        for(int i=-7;i<70;i++)
        {
            var s=Visual(root.transform,"Distant fir "+i,seasons[i%2==0?"TallTree":"WinterTree"],-20);
            s.transform.localPosition=new Vector3(i*3,-.3f+((float)random.NextDouble()- .5f),0);
            s.transform.localScale=new Vector3(3.5f,3+(float)random.NextDouble()*2,1);
            s.color=new Color(.42f,.60f,.71f,.45f);
        }
        var near=new GameObject("Background - Near Trees");near.AddComponent<ParallaxLayer>().factor=.07f;
        foreach(float x in new[]{-3,8,16,32,43,54,71,82,94,113})
        {
            var s=Visual(near.transform,"Frosted tree",seasons["TallTree"],-6);s.transform.localPosition=new Vector3(x,-.2f,0);
            s.transform.localScale=new Vector3(1.6f,1.8f,1);s.color=new Color(.66f,.84f,.92f);
        }
    }
    static void Snow(Transform camera)
    {
        var go=new GameObject("Snowfall");go.transform.SetParent(camera,false);go.transform.localPosition=new Vector3(0,7,12);
        var p=go.AddComponent<ParticleSystem>();p.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
        var main=p.main;main.startLifetime=9;main.startSpeed=0;main.startSize=new ParticleSystem.MinMaxCurve(.025f,.065f);main.startColor=new Color(1,1,1,.7f);main.simulationSpace=ParticleSystemSimulationSpace.World;main.maxParticles=200;
        var emission=p.emission;emission.rateOverTime=15;
        var shape=p.shape;shape.shapeType=ParticleSystemShapeType.Box;shape.scale=new Vector3(26,1,0);
        var velocity=p.velocityOverLifetime;velocity.enabled=true;velocity.space=ParticleSystemSimulationSpace.World;velocity.x=-.35f;velocity.y=-1.4f;velocity.z=0;
        var material=new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));AssetDatabase.CreateAsset(material,"Assets/Materials/Snow.mat");
        var renderer=p.GetComponent<ParticleSystemRenderer>();renderer.sharedMaterial=material;renderer.sortingOrder=9;p.Play();
    }
    static RectTransform Rect(Transform parent,string name,Vector2 anchor,Vector2 pivot,Vector2 pos,Vector2 size)
    {var go=new GameObject(name,typeof(RectTransform));var r=go.GetComponent<RectTransform>();r.SetParent(parent,false);r.anchorMin=r.anchorMax=anchor;r.pivot=pivot;r.anchoredPosition=pos;r.sizeDelta=size;return r;}
    static Image Panel(Transform parent,string name,Vector2 anchor,Vector2 pivot,Vector2 pos,Vector2 size,Color color,Sprite sprite=null)
    {var r=Rect(parent,name,anchor,pivot,pos,size);var im=r.gameObject.AddComponent<Image>();im.color=color;im.sprite=sprite;im.raycastTarget=false;if(sprite!=null&&sprite.border!=Vector4.zero)im.type=Image.Type.Sliced;return im;}
    public static TMP_Text Label(Transform parent,string name,string value,int size,Vector2 anchor,Vector2 pivot,Vector2 pos,Vector2 bounds,Color color,TextAnchor align=TextAnchor.MiddleLeft)
    {
        var label = Rect(parent,name,anchor,pivot,pos,bounds).gameObject.AddComponent<TextMeshProUGUI>();
        label.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
        label.fontSize = size;
        label.text = value;
        label.color = color;
        label.alignment = (TextAlignmentOptions)((1 << ((int)align % 3)) | (256 << ((int)align / 3)));
        label.raycastTarget = false;
        return label;
    }
    static Button Button(Transform parent,string name,string text,float y,out TMP_Text label)
    {var p=Panel(parent,name,new Vector2(.5f,.5f),new Vector2(.5f,.5f),new Vector2(0,y),new Vector2(440,60),Ice,ui["12"]);p.raycastTarget=true;var b=p.gameObject.AddComponent<Button>();b.targetGraphic=p;var colors=b.colors;colors.highlightedColor=new Color(.8f,1,1);colors.pressedColor=new Color(.5f,.75f,.85f);b.colors=colors;SwapSprites(b);label=Label(p.transform,"Text",text,20,new Vector2(.5f,.5f),new Vector2(.5f,.5f),Vector2.zero,new Vector2(420,56),Navy,TextAnchor.MiddleCenter);label.fontStyle=FontStyles.Bold;return b;}
    static void Layout(HorizontalOrVerticalLayoutGroup group, int top, float spacing)
    {
        group.padding.top = top;
        group.spacing = spacing;
        group.childAlignment = TextAnchor.UpperCenter;
        group.childControlWidth = group.childControlHeight = false;
        group.childForceExpandWidth = group.childForceExpandHeight = false;
    }
    static void SwapSprites(Button button)
    {
        var image = (Image)button.targetGraphic;
        string folder = Path.GetDirectoryName(AssetDatabase.GetAssetPath(image.sprite)).Replace('\\', '/');
        var pressed = AssetDatabase.LoadAllAssetsAtPath(folder + "/tile_0013.png").OfType<Sprite>().Single();
        button.transition = Selectable.Transition.SpriteSwap;
        button.spriteState = new SpriteState { pressedSprite = pressed, disabledSprite = pressed };
    }
    public static void BuildPauseMenu(Level01Game game)
    {
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(PathFor("kenney_ui-pack-pixel-adventure/Tiles/Small tiles/Thick outline/tile_0012.png"));
        var board = Panel(game.overlay.transform,"Pause Menu",new Vector2(.5f,.5f),new Vector2(.5f,.5f),Vector2.zero,new Vector2(520,330),Color.white,sprite);
        board.raycastTarget = true;
        Panel(board.transform,"Inner",new Vector2(.5f,.5f),new Vector2(.5f,.5f),Vector2.zero,new Vector2(490,300),new Color(.055f,.1f,.18f));
        var content = Rect(board.transform,"Content",Vector2.one*.5f,Vector2.one*.5f,Vector2.zero,new Vector2(520,330));
        var stack = content.gameObject.AddComponent<VerticalLayoutGroup>();
        Layout(stack, 36, 43);
        var title = Label(content,"Pause Title","Pause",52,new Vector2(.5f,1),new Vector2(.5f,1),new Vector2(0,-36),new Vector2(440,76),new Color(1,.9f,.68f),TextAnchor.MiddleCenter);
        title.fontStyle = FontStyles.Bold;
        var actions = Rect(content,"Buttons",Vector2.one*.5f,Vector2.one*.5f,Vector2.zero,new Vector2(400,112));
        Layout(actions.gameObject.AddComponent<HorizontalLayoutGroup>(), 0, 64);
        game.homeButton = PauseIconButton(actions,"Home",-88,AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Shared/UI/Pause/Home.png"),sprite);
        game.resumeButton = PauseIconButton(actions,"Play",88,AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Shared/UI/Pause/Play.png"),sprite);
        game.homeButton.interactable = false;
        game.pausePanel = board.gameObject;
        game.pausePanel.SetActive(false);
    }
    static Button PauseIconButton(Transform parent,string name,float x,Sprite iconSprite,Sprite sprite)
    {
        var image = Panel(parent,name,new Vector2(.5f,.5f),new Vector2(.5f,.5f),new Vector2(x,-48),new Vector2(112,112),Color.white,sprite);
        image.raycastTarget = true;
        var button = image.gameObject.AddComponent<Button>();button.targetGraphic=image;
        var colors = button.colors;colors.normalColor=Color.white;colors.highlightedColor=new Color(.75f,.93f,1);colors.pressedColor=new Color(.4f,.65f,.8f);colors.disabledColor=new Color(.65f,.65f,.65f);button.colors=colors;SwapSprites(button);
        var icon = Rect(image.transform,"Icon",new Vector2(.5f,.5f),new Vector2(.5f,.5f),Vector2.zero,new Vector2(80,80)).gameObject.AddComponent<Image>();
        icon.sprite=iconSprite;icon.preserveAspect=true;icon.color=new Color(1,.94f,.79f);icon.raycastTarget=false;
        return button;
    }
    public static void CreateSharedUi(Level01Game game, Sprite portrait)
    {
        ui = new Dictionary<string, Sprite>();
        foreach (int id in new[] {12,93})
            ui[id.ToString()] = AssetDatabase.LoadAssetAtPath<Sprite>(PathFor("kenney_ui-pack-pixel-adventure/Tiles/Small tiles/Thick outline/tile_"+id.ToString("D4")+".png"));
        BuildUi(game,portrait);
        var canvas=game.overlay.GetComponentInParent<Canvas>();
        foreach(var child in canvas.GetComponentsInChildren<Transform>(true)) child.gameObject.layer=LayerMask.NameToLayer("UI");
    }
    static void BuildUi(Level01Game game,Sprite portrait)
    {
        var canvas=new GameObject("Canvas",typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));canvas.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;
        var scaler=canvas.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1600,900);scaler.matchWidthOrHeight=.5f;
        var events=new GameObject("EventSystem",typeof(EventSystem),typeof(InputSystemUIInputModule));
        var module=events.GetComponent<InputSystemUIInputModule>();module.AssignDefaultActions();
        var health=Panel(canvas.transform,"Finn Health",new Vector2(0,1),new Vector2(0,1),new Vector2(28,-24),new Vector2(326,94),new Color(.045f,.09f,.16f,.94f),ui["12"]);
        var avatar=Panel(health.transform,"Avatar Mask",new Vector2(0,.5f),new Vector2(0,.5f),new Vector2(10,0),new Vector2(68,68),Color.white);
        avatar.gameObject.AddComponent<Mask>().showMaskGraphic=false;
        var face=Panel(avatar.transform,"Finn Portrait",new Vector2(.5f,.5f),new Vector2(.5f,.5f),Vector2.zero,new Vector2(68,68),Color.white,portrait);face.preserveAspect=true;
        Label(health.transform,"Finn Name","FINN",16,new Vector2(0,1),new Vector2(0,1),new Vector2(84,-12),new Vector2(200,24),Ice);
        Panel(health.transform,"Health Track",new Vector2(0,0),new Vector2(0,0),new Vector2(84,20),new Vector2(218,24),new Color(.1f,.15f,.22f));
        var fill=Panel(health.transform,"Health Fill",new Vector2(0,0),new Vector2(0,0),new Vector2(87,23),new Vector2(212,18),new Color(.98f,.4f,.47f),ui["93"]);
        fill.type=Image.Type.Filled;fill.fillMethod=Image.FillMethod.Horizontal;fill.fillOrigin=0;game.healthFill=fill;
        game.healthText=Label(health.transform,"Health Value","100 / 100",13,new Vector2(0,0),new Vector2(0,0),new Vector2(87,23),new Vector2(212,18),Color.white,TextAnchor.MiddleCenter);
        var objective=Panel(canvas.transform,"Objective",new Vector2(1,1),new Vector2(1,1),new Vector2(-28,-24),new Vector2(310,94),new Color(.045f,.09f,.16f,.94f),ui["12"]);
        Label(objective.transform,"Clock Label","BÁNH RĂNG ĐỒNG",15,new Vector2(0,1),new Vector2(0,1),new Vector2(20,-12),new Vector2(220,24),Ice);
        game.shardText=Label(objective.transform,"Count","0 / 3",26,new Vector2(1,1),new Vector2(1,1),new Vector2(-16,-10),new Vector2(85,34),Gold,TextAnchor.MiddleRight);
        game.objectiveText=Label(objective.transform,"Objective Text","",16,new Vector2(0,0),new Vector2(0,0),new Vector2(20,13),new Vector2(270,30),Color.white);
        var controls=Panel(canvas.transform,"Controls",new Vector2(0,0),new Vector2(0,0),new Vector2(28,22),new Vector2(850,42),new Color(.045f,.09f,.16f,.88f),ui["12"]);
        Label(controls.transform,"Keys","A D / ← →  Di chuyển     SPACE  Nhảy     ENTER  Đánh     E  Tương tác     ESC  Tạm dừng",15,new Vector2(.5f,.5f),new Vector2(.5f,.5f),Vector2.zero,new Vector2(820,40),Color.white);
        game.notificationText=Label(canvas.transform,"Notification","",20,new Vector2(.5f,0),new Vector2(.5f,0),new Vector2(0,115),new Vector2(1100,64),Navy,TextAnchor.MiddleCenter);
        game.notificationText.fontStyle=FontStyles.Bold;
        game.promptText=Label(canvas.transform,"Interaction Prompt","",19,new Vector2(.5f,0),new Vector2(.5f,0),new Vector2(0,78),new Vector2(700,30),Navy,TextAnchor.MiddleCenter);
        var shade=Panel(canvas.transform,"Menu Overlay",new Vector2(.5f,.5f),new Vector2(.5f,.5f),Vector2.zero,new Vector2(5000,5000),new Color(.025f,.055f,.11f,.82f));shade.raycastTarget=true;game.overlay=shade.gameObject;
        var board=Panel(shade.transform,"Menu Board",new Vector2(.5f,.5f),new Vector2(.5f,.5f),Vector2.zero,new Vector2(800,640),new Color(.075f,.15f,.23f),ui["12"]);
        game.resultPanel=board.gameObject;
        Layout(board.gameObject.AddComponent<VerticalLayoutGroup>(), 36, 10);
        Label(board.transform,"Eyebrow","WHAT IF · ADVENTURE TIME",18,new Vector2(.5f,1),new Vector2(.5f,1),new Vector2(0,-36),new Vector2(650,30),Gold,TextAnchor.MiddleCenter);
        game.panelTitle=Label(board.transform,"Title","",36,new Vector2(.5f,1),new Vector2(.5f,1),new Vector2(0,-86),new Vector2(720,55),Ice,TextAnchor.MiddleCenter);game.panelTitle.fontStyle=FontStyles.Bold;
        game.panelBody=Label(board.transform,"Body","",22,new Vector2(.5f,1),new Vector2(.5f,1),new Vector2(0,-165),new Vector2(710,270),Color.white,TextAnchor.UpperCenter);
        game.primaryButton=Button(board.transform,"Primary","",-190,out game.primaryText);
        game.secondaryButton=Button(board.transform,"Secondary","",-260,out game.secondaryText);
        BuildPauseMenu(game);
        game.overlay.SetActive(false);
    }
}
