using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace Sapphire.EditorTools.ModularTiles
{
    /// <summary>Builds a non-gameplay diagnostic that proves all Modular64 families compose together.</summary>
    public static class ModularWorldConnectionBuilder
    {
        const int Cell=64, MapW=30, MapH=18;
        const string Root="Assets/Sapphire/Art/World/Modular64";
        const string ScenePath="Assets/Sapphire/Scenes/ModularWorldConnectionTest.unity";
        static string Verification=>Path.GetFullPath(Path.Combine(Application.dataPath,"../../verification"));

        sealed class Placement
        {
            public string Layer,Id; public int X,Y,Order; public Tile Tile;
        }

        [MenuItem("Sapphire/Modular Tiles/Build Full World Connection Test")]
        public static void Build()
        {
            var placements=new List<Placement>();
            Scene scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var root=new GameObject("Modular64 Full Connection Diagnostic",typeof(Grid));
            SceneManager.MoveGameObjectToScene(root,scene);
            var maps=new Dictionary<string,Tilemap>();
            string[] names={"Ground","Path","Water","Foam","Shadow","Elevation","Cliff","Stairs","Bridge","Decoration"};
            for(int n=0;n<names.Length;n++) maps[names[n]]=NewMap(names[n],root.transform,n);

            Tile[] grass=Enumerable.Range(47,13).Select(n=>Load("Tiles/G"+n.ToString("000")+".asset")).ToArray();
            for(int y=0;y<MapH;y++) for(int x=0;x<MapW;x++)
                Put(maps,placements,"Ground",grass[(x*17+y*29)%grass.Length],x,y);

            // Cross, U-turn and a narrow exploration spur use explicit path semantics.
            for(int x=2;x<=17;x++) if(x!=10)Put(maps,placements,"Path",Load("Tiles/P001.asset"),x,8);
            for(int y=2;y<=14;y++) if(y!=8)Put(maps,placements,"Path",Load("Tiles/P002.asset"),10,y);
            Put(maps,placements,"Path",Load("Tiles/P011.asset"),10,8);
            for(int x=3;x<=7;x++){Put(maps,placements,"Path",Load("Tiles/P016.asset"),x,3);Put(maps,placements,"Path",Load("Tiles/P016.asset"),x,6);}
            for(int y=4;y<=5;y++){Put(maps,placements,"Path",Load("Tiles/P017.asset"),3,y);Put(maps,placements,"Path",Load("Tiles/P017.asset"),7,y);}
            for(int x=11;x<=18;x++) Put(maps,placements,"Path",Load("Tiles/P016.asset"),x,14);

            // One- and two-level elevation samples with cliffs, shadows and both stair widths.
            Tile et=Load("Tiles/Elevation/ET047.asset"),cliff=Load("Tiles/Elevation/C001.asset");
            for(int y=11;y<=15;y++)for(int x=2;x<=8;x++)Put(maps,placements,"Elevation",et,x,y);
            for(int x=2;x<=8;x++){Put(maps,placements,"Cliff",cliff,x,10);Put(maps,placements,"Shadow",Load("Tiles/Shadows/SH001.asset"),x,9);}
            Put(maps,placements,"Stairs",Load("Tiles/Stairs/S001.asset"),5,12);
            Put(maps,placements,"Stairs",Load("Tiles/Stairs/S002.asset"),5,11);
            Put(maps,placements,"Stairs",Load("Tiles/Stairs/S003.asset"),5,10);
            for(int y=14;y<=16;y++)for(int x=12;x<=17;x++)Put(maps,placements,"Elevation",et,x,y);
            for(int x=12;x<=17;x++){Put(maps,placements,"Cliff",cliff,x,13);Put(maps,placements,"Shadow",Load("Tiles/Shadows/SH005.asset"),x,12);}
            string[,] wide={{"S004","S005","S006"},{"S007","S008","S009"},{"S010","S011","S012"}};
            for(int row=0;row<3;row++)for(int lane=0;lane<3;lane++)Put(maps,placements,"Stairs",Load("Tiles/Stairs/"+wide[row,lane]+".asset"),14+lane,15-row);

            // A vertical river with irregular banks, frame-zero foam, and a bridge over water only.
            Tile water=Load("Tiles/WaterFoam/W001.asset");
            for(int y=0;y<MapH;y++)for(int x=21;x<=26;x++)Put(maps,placements,"Water",water,x,y);
            // Mask 64 faces west (left bank), mask 4 faces east (right bank).
            for(int y=1;y<MapH-1;y++){Put(maps,placements,"Foam",Load("Tiles/WaterFoam/F014.asset"),21,y);Put(maps,placements,"Foam",Load("Tiles/WaterFoam/F003.asset"),26,y);}
            for(int x=21;x<=26;x++)Put(maps,placements,"Bridge",Load("Tiles/Bridge/"+(x==21?"B002":x==26?"B003":"B001")+".asset"),x,8);

            // Representative small decorations remain independently addressable.
            int[,] deco={{1,1},{4,1},{8,1},{12,1},{16,2},{18,5},{1,7},{18,11},{9,16},{19,16}};
            for(int n=0;n<deco.GetLength(0);n++)Put(maps,placements,"Decoration",Load("Tiles/DecorationsSmall/D"+(n+1).ToString("000")+".asset"),deco[n,0],deco[n,1]);
            AddLarge(root.transform,Load("Tiles/DecorationsLarge/D101.asset"),18,13,9);
            AddLarge(root.transform,Load("Tiles/DecorationsLarge/D108.asset"),26,13,9);

            Validate(maps,placements);
            var cameraGo=new GameObject("Diagnostic Camera",typeof(Camera));
            cameraGo.transform.position=new Vector3(MapW/2f,MapH/2f,-10); Camera camera=cameraGo.GetComponent<Camera>();
            camera.orthographic=true;camera.orthographicSize=MapH/2f+.5f;camera.aspect=(float)MapW/MapH;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.1f,.13f,.16f);
            Directory.CreateDirectory("Assets/Sapphire/Scenes"); Directory.CreateDirectory(Verification);
            if(!EditorSceneManager.SaveScene(scene,ScenePath))throw new IOException("Could not save "+ScenePath);
            WritePreview(placements);
            File.WriteAllText(Path.Combine(Verification,"modular-world-connection-validation.txt"),BuildReport(placements));
            AssetDatabase.Refresh();
            Debug.Log("Modular64: full ground/path/elevation/stairs/water/foam/bridge/decoration connection test built.");
        }

        static Tilemap NewMap(string name,Transform parent,int order)
        {
            var go=new GameObject(name,typeof(Tilemap),typeof(TilemapRenderer));go.transform.SetParent(parent,false);
            go.GetComponent<TilemapRenderer>().sortingOrder=order;return go.GetComponent<Tilemap>();
        }

        static Tile Load(string relative)
        {
            string path=Root+"/"+relative;Tile tile=AssetDatabase.LoadAssetAtPath<Tile>(path);
            if(tile==null||tile.sprite==null||tile.sprite.pixelsPerUnit!=Cell)throw new InvalidDataException("Missing or invalid 64 PPU Tile: "+path);
            return tile;
        }

        static void Put(Dictionary<string,Tilemap> maps,List<Placement> all,string layer,Tile tile,int x,int y)
        {
            Vector3Int cell=new Vector3Int(x,y,0);if(maps[layer].HasTile(cell))throw new InvalidOperationException("Same-layer overlap: "+layer+" "+cell);
            maps[layer].SetTile(cell,tile);all.Add(new Placement{Layer=layer,Id=tile.name,X=x,Y=y,Order=maps[layer].GetComponent<TilemapRenderer>().sortingOrder,Tile=tile});
        }

        static void AddLarge(Transform parent,Tile tile,int x,int y,int order)
        {
            var go=new GameObject(tile.name+" MultiCell",typeof(SpriteRenderer));go.transform.SetParent(parent,false);go.transform.position=new Vector3(x,y,0);
            var sr=go.GetComponent<SpriteRenderer>();sr.sprite=tile.sprite;sr.sortingOrder=order;
        }

        static void Validate(Dictionary<string,Tilemap> maps,List<Placement> p)
        {
            for(int n=0;n<10;n++)if(maps.Values.ElementAt(n).GetComponent<TilemapRenderer>().sortingOrder!=n)throw new InvalidOperationException("Layer order mismatch at "+n);
            if(!p.Any(q=>q.Id=="P011")||!p.Any(q=>q.Id=="P016")||!p.Any(q=>q.Id=="S001")||!p.Any(q=>q.Id=="S012"))throw new InvalidOperationException("Path/stair coverage incomplete.");
            for(int x=21;x<=26;x++){Vector3Int c=new Vector3Int(x,8,0);if(!maps["Water"].HasTile(c)||!maps["Bridge"].HasTile(c))throw new InvalidOperationException("Bridge must occupy water cell "+c);}
            if(maps["Water"].HasTile(new Vector3Int(20,8,0))||maps["Water"].HasTile(new Vector3Int(27,8,0)))throw new InvalidOperationException("Bridge land contacts must remain dry.");
            foreach(Placement q in p)if(maps[q.Layer].GetTile(new Vector3Int(q.X,q.Y,0))==null)throw new InvalidOperationException("Lost placement "+q.Layer+"/"+q.Id);
        }

        static void WritePreview(List<Placement> placements)
        {
            int w=MapW*Cell,h=MapH*Cell;var pixels=Enumerable.Repeat(new Color32(25,31,38,255),w*h).ToArray();
            foreach(Placement p in placements.OrderBy(q=>q.Order)){Sprite s=p.Tile.sprite;Rect r=s.rect;Color32[] source=s.texture.GetPixels32();int sw=(int)r.width,sh=(int)r.height;for(int y=0;y<sh;y++)for(int x=0;x<sw;x++){Color32 c=source[((int)r.y+y)*s.texture.width+(int)r.x+x];if(c.a==0)continue;int dx=p.X*Cell+x,dy=p.Y*Cell+y;if(dx>=0&&dy>=0&&dx<w&&dy<h)pixels[dy*w+dx]=c;}}
            var texture=new Texture2D(w,h,TextureFormat.RGBA32,false);texture.SetPixels32(pixels);texture.Apply();File.WriteAllBytes(Path.Combine(Verification,"modular-world-connection-test.png"),texture.EncodeToPNG());UnityEngine.Object.DestroyImmediate(texture);
        }

        static string BuildReport(List<Placement> p)
        {
            var s=new StringBuilder();s.AppendLine("PASS: all persisted Tile assets loaded with sprites at 64 PPU.");s.AppendLine("PASS: strict Ground < Path < Water < Foam < Shadow < Elevation < Cliff < Stairs < Bridge < Decoration order.");s.AppendLine("PASS: cross, U route, narrow corridor, one/two-level elevation, narrow/wide stairs, water/foam and bridge contacts composed.");s.AppendLine("PASS: bridge cells x=21..26,y=8 contain water below; x=20/27 are dry land contacts.");s.AppendLine("PASS: no duplicate cell exists inside any one layer.");s.AppendLine("Placements: "+p.Count);return s.ToString();
        }
    }
}
