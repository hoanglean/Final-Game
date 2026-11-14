using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;
using OooAdventure;

[InitializeOnLoad]
public static class PolishLevel01
{
    static PolishLevel01(){if(!File.Exists("Library/Level01Polish.txt"))EditorApplication.delayCall+=Apply;}
    static void Apply()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling){EditorApplication.delayCall+=Apply;return;}
        var game=UnityEngine.Object.FindFirstObjectByType<Level01Game>();if(game==null)return;
        string path=AssetDatabase.FindAssets("tile_0049 t:Texture2D",new[]{"Assets/Art/Source"}).Select(AssetDatabase.GUIDToAssetPath).First(p=>p.Contains("Small tiles/Thick outline"));
        var sprite=AssetDatabase.LoadAssetAtPath<Sprite>(path);
        game.primaryButton.GetComponent<Image>().sprite=sprite;game.secondaryButton.GetComponent<Image>().sprite=sprite;
        var map=GameObject.Find("GroundTilemap").GetComponent<Tilemap>();
        var top=AssetDatabase.LoadAssetAtPath<Tile>("Assets/Tiles/Level01/Ground/SnowSurface.asset");var fill=AssetDatabase.LoadAssetAtPath<Tile>("Assets/Tiles/Level01/Ground/FrozenStone.asset");
        for(int x=18;x<21;x++){map.SetTile(new Vector3Int(x,2,0),null);map.SetTile(new Vector3Int(x,1,0),top);map.SetTile(new Vector3Int(x,0,0),fill);}
        EditorSceneManager.MarkSceneDirty(game.gameObject.scene);EditorSceneManager.SaveScene(game.gameObject.scene);AssetDatabase.SaveAssets();
        File.WriteAllText("Library/Level01Polish.txt","PASS: Accessible early platform and high-contrast menu buttons saved.");
    }
}
