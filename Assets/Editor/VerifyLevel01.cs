using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.Tilemaps;
using OooAdventure;

[InitializeOnLoad]
public static class VerifyLevel01
{
    const string Result="Library/Level01Verification.txt";
    static Level01Game game;
    static FinnController player;
    static Keyboard keyboard;
    static EnemyController mushroom,guardian;
    static int stage,attacks;
    static double next,deadline,lastTrace;
    static float startX,floor;
    static readonly StringBuilder log=new StringBuilder();
    static VerifyLevel01()
    {
        if (!Environment.GetCommandLineArgs().Contains("-verifyLevel01")) return;

        EditorApplication.playModeStateChanged+=s=>{
            if(s==PlayModeStateChange.EnteredPlayMode && !File.Exists(Result))
            {stage=0;next=EditorApplication.timeSinceStartup+1;deadline=next+90;log.Clear();EditorApplication.update+=Tick;}
            if(s==PlayModeStateChange.ExitingPlayMode)EditorApplication.update-=Tick;
        };
    }
    static void Keys(params Key[] keys) => InputSystem.QueueStateEvent(keyboard,new KeyboardState(keys));
    static void Wait(double seconds,int to){next=EditorApplication.timeSinceStartup+seconds;stage=to;}
    static void Assert(bool valid,string why){if(!valid)throw new Exception(why);}
    static void Tick()
    {
        if(EditorApplication.timeSinceStartup<next || !EditorApplication.isPlaying)return;
        try
        {
            Assert(EditorApplication.timeSinceStartup<deadline,"Timed out at stage "+stage);
            if(stage==0)
            {
                game=Level01Game.Instance;player=game.player;
                Assert(game.CurrentMode==Level01Game.Mode.Playing && player.Health==100 && game.Shards==0,"Initial state incorrect");
                Assert(UnityEngine.Object.FindFirstObjectByType<TilemapCollider2D>().shapeCount>0,"Ground collider missing");
                keyboard=InputSystem.AddDevice<Keyboard>("Level01 verification keyboard");
                foreach(var enemy in UnityEngine.Object.FindObjectsByType<EnemyController>(FindObjectsSortMode.None))
                {enemy.enabled=false;if(enemy.kind==EnemyController.Kind.Mushroom)mushroom=enemy;if(enemy.kind==EnemyController.Kind.Guardian)guardian=enemy;}
                mushroom=UnityEngine.Object.FindObjectsByType<EnemyController>(FindObjectsSortMode.None).Where(e=>e.kind==EnemyController.Kind.Mushroom).OrderBy(e=>e.transform.position.x).First();
                log.AppendLine("PASS: Immediate gameplay, initial health/shards and native ground collider.");Wait(.7,1);
            }
            else if(stage==1)
            {Assert(player.Grounded,"Finn did not land");startX=player.transform.position.x;Keys(Key.D);Wait(.2,2);}
            else if(stage==2)
            {Assert(player.transform.position.x>startX+.5f,"D input did not move Finn");Keys(Key.Space);floor=player.transform.position.y;Wait(.17,3);}
            else if(stage==3)
            {Assert(player.transform.position.y>floor+.5f && !player.Grounded,"Space did not jump");Keys();Wait(.05,4);}
            else if(stage==4){Keys(Key.Space);Wait(.12,5);}
            else if(stage==5)
            {Assert(player.Body.linearVelocityY<11,"Double jump allowed");Keys();log.AppendLine("PASS: Actual keyboard D moves, Space jumps, midair Space blocked.");Wait(1,6);}
            else if(stage==6)
            {Assert(player.Grounded,"Finn did not land after jump");Warp(new Vector2(mushroom.transform.position.x-1.15f,.1f));Keys(Key.Enter);Wait(.25,7);}
            else if(stage==7)
            {Assert(mushroom.IsDead,"Sword did not damage enemy");Keys();Warp(new Vector2(3,.1f));log.AppendLine("PASS: Enter sword attack kills the 35-HP mushroom.");Wait(.2,8);}
            else if(stage==8)
            {var prefab=AssetDatabase.LoadAssetAtPath<IceProjectile>("Assets/Prefabs/Chapter01/Enemies/IceBolt.prefab");UnityEngine.Object.Instantiate(prefab,player.transform.position+new Vector3(1,.6f,0),Quaternion.identity).Launch(Vector2.left);Wait(.35,9);}
            else if(stage==9)
            {Assert(player.Health==80,"Projectile should subtract 20 health, actual="+player.Health);var prefab=AssetDatabase.LoadAssetAtPath<IceProjectile>("Assets/Prefabs/Chapter01/Enemies/IceBolt.prefab");UnityEngine.Object.Instantiate(prefab,player.transform.position+new Vector3(.5f,.6f,0),Quaternion.identity).Launch(Vector2.left);Wait(.25,10);}
            else if(stage==10)
            {Assert(player.Health==80,"Repeated projectile bypassed invulnerability");Assert(Mathf.Abs(game.healthFill.fillAmount-.8f)<.001f,"Health UI incorrect");log.AppendLine("PASS: Real projectile collision, 20 damage, invulnerability and 80% health UI.");Wait(.5,11);}
            else if(stage==11)
            {
                Assert(!player.IsDead,"Traversal died at "+player.transform.position);
                if(EditorApplication.timeSinceStartup-lastTrace>1)
                {lastTrace=EditorApplication.timeSinceStartup;File.AppendAllText("Library/Level01Traversal.txt","x="+player.transform.position.x+" y="+player.transform.position.y+" shards="+game.Shards+" ground="+player.Grounded+"\n");}
                if(player.transform.position.x>=94)
                {
                    Keys();Assert(game.Shards==2,"Traversal must collect both platform shards, actual="+game.Shards);
                    Assert(!game.TryFinish(),"Gate opened without three shards");
                    log.AppendLine("PASS: Keyboard traversal from spawn through all gaps/platforms; two shards collected; gate locked at 2/3.");
                    player.TakeDamage(100,player.transform.position+Vector3.right);Wait(1.1,12);return;
                }
                var p=(Vector2)player.transform.position;
                bool wall=Physics2D.Raycast(p+Vector2.up*.6f,Vector2.right,1.3f,player.groundMask);
                bool floorAhead=Physics2D.Raycast(p+new Vector2(1.1f,.12f),Vector2.down,.7f,player.groundMask);
                if(player.Grounded && (wall || !floorAhead))Keys(Key.D,Key.Space);else Keys(Key.D);
            }
            else if(stage==12)
            {
                Assert(game.CurrentMode==Level01Game.Mode.Dead,"Death panel missing");
                game.secondaryButton.onClick.Invoke();
                Wait(.7,18);
            }
            else if(stage==18)
            {
                game=Level01Game.Instance;player=game.player;
                Assert(game.CurrentMode==Level01Game.Mode.Playing && player.Health==100 && game.Shards==0,"Replay after death did not reset level");
                Assert(Mathf.Abs(player.transform.position.x-startX)<.1f,"Replay did not return to the level spawn");
                Assert(player.GetComponent<BoxCollider2D>().enabled && player.Body.gravityScale==3,"Replay did not restore physics");
                log.AppendLine("PASS: Death panel replay starts a fresh level at the spawn.");
                guardian=UnityEngine.Object.FindObjectsByType<EnemyController>(FindObjectsSortMode.None)
                    .Single(e=>e.kind==EnemyController.Kind.Guardian);
                game.CollectShard();game.CollectShard();
                Warp(new Vector2(94,.1f));Wait(.6,13);
            }
            else if(stage==13)
            {
                Assert(UnityEngine.Object.FindObjectsByType<IceProjectile>(FindObjectsSortMode.None).Length>0,"Guardian did not instantiate a projectile");
                guardian.enabled=false;foreach(var shot in UnityEngine.Object.FindObjectsByType<IceProjectile>(FindObjectsSortMode.None))UnityEngine.Object.Destroy(shot.gameObject);
                Warp(new Vector2(97.8f,.1f));log.AppendLine("PASS: Guardian AI instantiates moving ice projectiles.");attacks=0;Wait(.3,14);
            }
            else if(stage==14){Keys(Key.Enter);attacks++;Wait(.17,15);}
            else if(stage==15)
            {
                Keys();
                if(attacks<4){Wait(.3,14);return;}
                Assert(guardian.IsDead,"Guardian did not die after four sword strikes");Keys(Key.D);Wait(.35,16);
            }
            else if(stage==16)
            {
                Keys();Assert(game.Shards==3,"Guardian shard was not spawned/collected");
                Assert(game.TryFinish() && game.CurrentMode==Level01Game.Mode.Won,"Win state missing");
                log.AppendLine("PASS: Guardian defeated, third shard spawned/collected, 3/3 opens win panel.");
                game.secondaryButton.onClick.Invoke();Wait(.7,17);
            }
            else if(stage==17)
            {
                game=Level01Game.Instance;
                Assert(game.Shards==0 && game.player.Health==100 && game.CurrentMode==Level01Game.Mode.Playing,"Replay did not reset health/shards/gameplay");
                log.AppendLine("PASS: Win-panel replay loads a fresh level with 100 HP and 0 shards.");Finish("PASS\n"+log);
            }
        }
        catch(Exception ex){Finish("ERROR at stage "+stage+": "+ex+"\n"+log);}
    }
    static void Warp(Vector2 pos){player.Body.position=pos;player.transform.position=pos;player.Body.linearVelocity=Vector2.zero;Physics2D.SyncTransforms();}
    static void Finish(string result)
    {
        File.WriteAllText(Result,result);EditorApplication.update-=Tick;
        if(keyboard!=null)InputSystem.RemoveDevice(keyboard);
        EditorApplication.isPlaying=false;
    }
}
