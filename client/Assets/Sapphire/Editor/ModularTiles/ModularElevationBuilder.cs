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
    /// <summary>Builds the deliberately south-facing elevation stage. Stairs are excluded.</summary>
    public static class ModularElevationBuilder
    {
        const int Cell=64, ElevationSize=1024, ShadowSize=512, MapW=30, MapH=24;
        const string Root="Assets/Sapphire/Art/World/Modular64";
        const string ElevationAtlas=Root+"/TS02_Elevation_Cliff_64.png", ShadowAtlas=Root+"/TS04_Shadow_Overlay_64.png";
        const string ElevationTiles=Root+"/Tiles/Elevation", ShadowTiles=Root+"/Tiles/Shadows";
        const string TargetScene="Assets/Sapphire/Scenes/ModularElevationTest.unity";
        static string Verification=>Path.GetFullPath(Path.Combine(Application.dataPath,"../../verification"));
        static readonly int[] Dx={0,1,1,1,0,-1,-1,-1}, Dy={1,1,0,-1,-1,-1,0,1};
        static readonly string[] RunNames={"Straight","EndLeft","EndRight","Isolated"};
        static readonly Color32[] Grass={new Color32(43,77,25,255),new Color32(57,96,30,255),new Color32(71,116,36,255),new Color32(86,136,43,255),new Color32(101,151,50,255),new Color32(114,162,60,255),new Color32(129,176,70,255),new Color32(144,188,82,255),new Color32(163,201,100,255),new Color32(122,137,62,255),new Color32(160,162,87,255),new Color32(191,189,118,255)};
        static readonly Color32[] Rock={new Color32(48,58,70,255),new Color32(59,70,82,255),new Color32(70,82,94,255),new Color32(82,94,106,255),new Color32(95,107,118,255),new Color32(110,121,131,255),new Color32(125,135,144,255),new Color32(140,149,157,255)};

        sealed class Item { public string Id,Semantic,Layer; public int Slot,Variant,Mask; public Color32[] Pixels=new Color32[Cell*Cell]; public Tile Tile; }
        sealed class Placement { public int X,Y,Level; public Item Top,Cliff,Shadow; }

        [MenuItem("Sapphire/Modular Tiles/Build Independent Elevation Test")]
        public static void Build()
        {
            CheckSceneCreationAllowed(); // interactive preflight before writes
            Texture2D source=ReadCliffSource(); List<Item> elevation;
            try { elevation=BuildElevation(source); } finally { UnityEngine.Object.DestroyImmediate(source); }
            List<Item> shadows=BuildShadows(); Validate(elevation,shadows);
            Directory.CreateDirectory(ElevationTiles); Directory.CreateDirectory(ShadowTiles); Directory.CreateDirectory(Verification);
            WriteAtlas(ElevationAtlas,ElevationSize,elevation); WriteAtlas(ShadowAtlas,ShadowSize,shadows);
            ImportAtlas(ElevationAtlas,ElevationSize); ImportAtlas(ShadowAtlas,ShadowSize);
            SaveTiles(elevation,ElevationAtlas,ElevationSize,ElevationTiles); SaveTiles(shadows,ShadowAtlas,ShadowSize,ShadowTiles);
            WriteManifest(Root+"/TS02_Elevation_Cliff_64.csv",elevation,ElevationSize,ElevationTiles);
            WriteManifest(Root+"/TS04_Shadow_Overlay_64.csv",shadows,ShadowSize,ShadowTiles);
            AssetDatabase.SaveAssets(); ReloadCheck(elevation,ElevationAtlas,ElevationSize,ElevationTiles); ReloadCheck(shadows,ShadowAtlas,ShadowSize,ShadowTiles);
            BuildScene(elevation,shadows); AssetDatabase.Refresh();
            Debug.Log("Modular64: ET001-ET047, south-facing C001-C012, SH001-SH008 built; stairs excluded.");
        }

        static Texture2D ReadCliffSource()
        {
            string file=Root+"/Sources/CliffMaster.png";
            if(!File.Exists(file)) throw new FileNotFoundException("Provide opaque CliffMaster.png (the 1254px material source).",file);
            var texture=new Texture2D(2,2,TextureFormat.RGBA32,false);
            try { if(!texture.LoadImage(File.ReadAllBytes(file))) throw new InvalidDataException("Cannot decode "+file); if(texture.GetPixels32().Any(p=>p.a!=255)) throw new InvalidDataException("CliffMaster.png must be opaque."); return texture; }
            catch { UnityEngine.Object.DestroyImmediate(texture); throw; }
        }

        static List<Item> BuildElevation(Texture2D source)
        {
            int[] masks=Enumerable.Range(0,256).Select(NormalizeMask).Distinct().OrderBy(x=>x).ToArray();
            if(masks.Length!=47) throw new InvalidOperationException("Mask normalization must yield 47 masks.");
            var items=masks.Select((m,i)=>MakeTop(m,i)).ToList();
            for(int v=0;v<3;v++) for(int s=0;s<4;s++) items.Add(MakeCliff(s,v,source));
            return items;
        }

        static Item MakeTop(int mask,int ordinal)
        {
            Item item=NewItem("ET"+(ordinal+1).ToString("D3"),"BlobMask"+mask,"ElevatedTop",ordinal,0,mask);
            for(int y=0;y<Cell;y++) for(int x=0;x<Cell;x++)
            {
                if(x==0||x==63||y==0||y==63) { item.Pixels[y*Cell+x]=TopBoundary(mask,x,y); continue; }
                int rim=RimDistance(mask,x,y), n=PositiveMod(Hash(x/4,y/4,19),7);
                item.Pixels[y*Cell+x]=rim<8?Rock[3+n%3]:Grass[2+PositiveMod(Hash(x,y,7),7)];
            }
            return item;
        }

        static int RimDistance(int mask,int x,int y)
        {
            int[] d={63-y,63-x,y,x}; int best=64;
            for(int side=0;side<4;side++) if((mask&(1<<(side*2)))==0) best=Math.Min(best,d[side]);
            for(int c=0;c<4;c++) { int a=c*2,b=(a+2)%8; if((mask&(1<<a))!=0&&(mask&(1<<b))!=0&&(mask&(1<<(a+1)))==0) best=Math.Min(best,Math.Max(d[c],d[(c+1)%4])); }
            return best;
        }

        static Item MakeCliff(int shape,int variant,Texture2D source)
        {
            int ordinal=variant*4+shape; Item item=NewItem("C"+(ordinal+1).ToString("D3"),RunNames[shape],"CliffSouth",64+ordinal,variant+1,-1); Color32[] src=source.GetPixels32();
            for(int y=0;y<Cell;y++) for(int x=0;x<Cell;x++)
            {
                if(y==63||y==0) { item.Pixels[y*Cell+x]=RimContact(x==63?0:x); continue; }
                if(x==0||x==63) { item.Pixels[y*Cell+x]=CliffSideContact(y); continue; }
                int sx=PositiveMod(137+x*48/63+variant*173,source.width), sy=PositiveMod(211+y*48/63+variant*197,source.height); Color32 sample=src[sy*source.width+sx];
                int band=Mathf.Clamp(((sample.r*3+sample.g*5+sample.b*2)/10)*Rock.Length/256+((y/9+variant)%3)-1,0,Rock.Length-1);
                int cap=shape==1?Math.Max(0,13-x):shape==2?Math.Max(0,x-50):shape==3?Math.Max(0,11-Math.Min(x,63-x)):0;
                if(cap>0&&y<48) band=Mathf.Clamp(band+2,0,Rock.Length-1); item.Pixels[y*Cell+x]=Rock[band];
            }
            return item;
        }

        static Color32 TopBoundary(int mask,int x,int y)
        {
            bool north=(mask&1)!=0,east=(mask&4)!=0,south=(mask&16)!=0,west=(mask&64)!=0;
            if((x==0||x==63)&&(y==0||y==63))
            {
                bool connected=(x==0?west:east)&&(y==0?south:north);
                return connected?TopContact(0):RimContact(0);
            }
            if(y==0) return south?TopContact(x):RimContact(x);
            if(y==63) return north?TopContact(x):RimContact(x);
            if(x==0) return west?TopContact(y):RimContact(y);
            return east?TopContact(y):RimContact(y);
        }

        // A geometric corner belongs to two independently joinable edges. Both semantic
        // branches converge on one corner sample so every legal opposing edge remains exact.
        static Color32 TopContact(int along)=>along==0?RimContact(0):Grass[4+PositiveMod(Hash(along,0,43),4)];
        static Color32 RimContact(int along)=>Rock[3+PositiveMod(Hash(along,0,31),3)];
        static Color32 CliffSideContact(int along)=>Rock[3+PositiveMod(Hash(along==63?0:along,0,53),3)];

        static List<Item> BuildShadows()
        {
            var items=new List<Item>();
            for(int h=0;h<2;h++) for(int shape=0;shape<4;shape++)
            {
                int ordinal=h*4+shape, depth=h==0?19:29; Item item=NewItem("SH"+(ordinal+1).ToString("D3"),RunNames[shape]+"H"+(h+1),"Shadow",ordinal,h+1,-1);
                for(int y=0;y<Cell;y++) for(int x=0;x<Cell;x++)
                {
                    if(y>=depth) continue; byte a=y<depth-7?(byte)(h==0?72:112):y<depth-2?(byte)72:(byte)40;
                    if(shape==1&&x<6) a=0; if(shape==2&&x>57) a=0; if(shape==3&&(x<6||x>57)) a=0;
                    item.Pixels[y*Cell+x]=a==0?new Color32():new Color32(18,24,32,a);
                }
                items.Add(item);
            }
            return items;
        }

        static Item NewItem(string id,string semantic,string layer,int slot,int variant,int mask)=>new Item{Id=id,Semantic=semantic,Layer=layer,Slot=slot,Variant=variant,Mask=mask};
        public static int NormalizeMask(int mask) { mask&=255; for(int d=1;d<8;d+=2) if((mask&(1<<(d-1)))==0||(mask&(1<<((d+1)%8)))==0) mask&=~(1<<d); return mask; }
        static int MaskAt(byte[,] h,int x,int y,byte level) { int mask=0; for(int d=0;d<8;d++){int xx=x+Dx[d],yy=y+Dy[d];if(xx>=0&&yy>=0&&xx<MapW&&yy<MapH&&h[xx,yy]==level)mask|=1<<d;} return NormalizeMask(mask); }

        static byte[,] MakeHeights()
        {
            var h=new byte[MapW,MapH];
            for(int x=2;x<=8;x++) for(int y=5;y<=10;y++) if(x<=4||y<=7) h[x,y]=1;
            for(int x=12;x<=21;x++) for(int y=5;y<=11;y++) if(x<=14||x>=19||y<=7) h[x,y]=1;
            for(int x=5;x<=16;x++) for(int y=15;y<=21;y++) if((x+y*3)%11!=0&&!(x>13&&y>19)) h[x,y]=1;
            for(int x=8;x<=13;x++) for(int y=17;y<=20;y++) h[x,y]=2;
            for(int x=24;x<=27;x++) for(int y=16;y<=19;y++) h[x,y]=2; // level 2 directly above ground: two-cell drop
            return h;
        }

        static List<Placement> Plan(byte[,] h,List<Item> elevation,List<Item> shadows)
        {
            var tops=elevation.Where(i=>i.Layer=="ElevatedTop").ToDictionary(i=>i.Mask); Item[] cliffs=elevation.Where(i=>i.Layer=="CliffSouth").ToArray(); var result=new List<Placement>();
            for(byte level=1;level<=2;level++)
            {
                for(int y=0;y<MapH;y++) for(int x=0;x<MapW;x++) if(h[x,y]==level) result.Add(new Placement{X=x,Y=y,Level=level,Top=tops[MaskAt(h,x,y,level)]});
                var usedC=new HashSet<int>(); var usedS=new HashSet<int>();
                for(int y=0;y<MapH;y++) for(int x=0;x<MapW;x++)
                {
                    int below=y==0?0:h[x,y-1], drop=h[x,y]==level?level-below:0;
                    if(drop<=0) continue; int start=x;
                    while(x+1<MapW&&h[x+1,y]==level&&level-(y==0?0:h[x+1,y-1])==drop) x++; int end=x;
                    for(int xx=start;xx<=end;xx++)
                    {
                        int shape=start==end?3:xx==start?1:xx==end?2:0, v=PositiveMod(Hash(xx,y,level),3);
                        for(int row=1;row<=drop;row++)
                        {
                            int cy=y-row;if(cy<0||!usedC.Add(cy*MapW+xx))throw new InvalidOperationException("Diagnostic grid causes cliff self-overwrite.");
                            result.Add(new Placement{X=xx,Y=cy,Level=level,Cliff=cliffs[v*4+shape]});
                        }
                        int sy=y-drop-1;if(sy<0||!usedS.Add(sy*MapW+xx))throw new InvalidOperationException("Diagnostic grid causes shadow self-overwrite.");
                        result.Add(new Placement{X=xx,Y=sy,Level=level,Shadow=shadows[(drop-1)*4+shape]});
                    }
                }
            }
            return result;
        }

        static void Validate(List<Item> elevation,List<Item> shadows)
        {
            Item[] tops=elevation.Where(i=>i.Layer=="ElevatedTop").ToArray(), cliffs=elevation.Where(i=>i.Layer=="CliffSouth").ToArray();
            if(tops.Length!=47||cliffs.Length!=12||shadows.Count!=8||elevation.Count!=59) throw new InvalidOperationException("Expected 47 ET, 12 C, 8 SH.");
            if(elevation.Select(i=>i.Id).Concat(shadows.Select(i=>i.Id)).Distinct().Count()!=67) throw new InvalidOperationException("Duplicate ID.");
            if(elevation.Select(i=>i.Slot).Distinct().Count()!=elevation.Count||shadows.Select(i=>i.Slot).Distinct().Count()!=shadows.Count) throw new InvalidOperationException("Duplicate atlas slot.");
            if(elevation.Any(i=>i.Slot<0||i.Slot>=256)||shadows.Any(i=>i.Slot<0||i.Slot>=64)) throw new InvalidOperationException("Atlas capacity exceeded.");
            if(tops.Select(i=>i.Slot).OrderBy(i=>i).Where((s,i)=>s!=i).Any()||cliffs.Any(i=>i.Slot<64||i.Slot>75)||shadows.Any(i=>i.Slot<0||i.Slot>7)) throw new InvalidOperationException("Slot contract violated.");
            if(elevation.Any(i=>i.Pixels.Any(p=>p.a!=255))) throw new InvalidOperationException("ET/C must be opaque.");
            var allowed=new HashSet<byte>{0,40,72,112}; if(shadows.Any(i=>i.Pixels.Any(p=>!allowed.Contains(p.a)||(p.a==0&&(p.r!=0||p.g!=0||p.b!=0))))) throw new InvalidOperationException("Shadow alpha/RGB contract violated.");
            var lookup=tops.ToDictionary(i=>i.Mask); int comparisons=0;
            for(int bits=0;bits<4096;bits++)
            {
                if((bits&(1<<5))==0||(bits&(1<<6))==0) continue; Func<int,int,bool> has=(x,y)=>x>=0&&x<4&&y>=0&&y<3&&(bits&(1<<(y*4+x)))!=0;
                for(int t=0;t<2;t++) { int ma=0,mb=0; for(int d=0;d<8;d++){int dx=t==0?Dx[d]:Dy[d],dy=t==0?Dy[d]:Dx[d];if(has(1+dx,1+dy))ma|=1<<d;if(has(2+dx,1+dy))mb|=1<<d;} CheckEdge(lookup[NormalizeMask(ma)],lookup[NormalizeMask(mb)],t==0,"ET");comparisons+=Cell; }
            }
            for(int a=0;a<3;a++) for(int b=0;b<3;b++){CheckEdge(cliffs[a*4+1],cliffs[b*4],true,"EndLeft-Straight");CheckEdge(cliffs[a*4],cliffs[b*4],true,"Straight-Straight");CheckEdge(cliffs[a*4],cliffs[b*4+2],true,"Straight-EndRight");}
            foreach(Item upper in cliffs) foreach(Item lower in cliffs) CheckEdge(upper,lower,false,"Cliff vertical stack");
            foreach(Item top in tops.Where(t=>(t.Mask&16)==0)) foreach(Item cliff in cliffs) for(int x=0;x<Cell;x++) if(!top.Pixels[x].Equals(cliff.Pixels[63*Cell+x])) throw new InvalidOperationException("ET south/C north mismatch.");
            foreach(Item top in tops) ValidateTopBoundaryProfile(top);
            for(int h=0;h<2;h++){CheckEdge(shadows[h*4+1],shadows[h*4],true,"shadow left");CheckEdge(shadows[h*4],shadows[h*4],true,"shadow straight");CheckEdge(shadows[h*4],shadows[h*4+2],true,"shadow right");}
            for(int s=0;s<4;s++) for(int v=1;v<3;v++) if(!InteriorDiffers(cliffs[s],cliffs[v*4+s])) throw new InvalidOperationException("Cliff variants identical.");
            CheckReserved(elevation,ElevationSize); CheckReserved(shadows,ShadowSize);
            File.WriteAllText(Path.Combine(Verification,"modular-elevation-validation.txt"),$"PASS: 256 masks normalize to 47; ET001-ET047 use slots 0-46.\nPASS: {comparisons} exhaustive legal ET edge comparisons.\nPASS: C001-C012 = Straight/EndLeft/EndRight/Isolated x3 in slots 64-75; legal joins and ET-south/C-north contacts match.\nPASS: SH001-SH008 H1/H2 overlays; legal joins and alpha 0/40/72/112.\nPASS: unused atlas slots zero RGBA; reload checks Sprite texture/rect/64PPU.\nLIMITATION: SOUTH-FACING cliffs only; no four-direction cliff claim. Stairs excluded.\n");
        }

        static void CheckEdge(Item a,Item b,bool horizontal,string label) { for(int p=0;p<Cell;p++){int ia=horizontal?p*Cell+63:63*Cell+p,ib=horizontal?p*Cell:p;if(!a.Pixels[ia].Equals(b.Pixels[ib]))throw new InvalidOperationException(label+" edge mismatch.");} }
        static void ValidateTopBoundaryProfile(Item item)
        {
            for(int p=1;p<63;p++)
            {
                if(!item.Pixels[p].Equals((item.Mask&16)!=0?TopContact(p):RimContact(p)))throw new InvalidOperationException("ET south boundary profile mismatch: "+item.Id);
                if(!item.Pixels[63*Cell+p].Equals((item.Mask&1)!=0?TopContact(p):RimContact(p)))throw new InvalidOperationException("ET north boundary profile mismatch: "+item.Id);
                if(!item.Pixels[p*Cell].Equals((item.Mask&64)!=0?TopContact(p):RimContact(p)))throw new InvalidOperationException("ET west boundary profile mismatch: "+item.Id);
                if(!item.Pixels[p*Cell+63].Equals((item.Mask&4)!=0?TopContact(p):RimContact(p)))throw new InvalidOperationException("ET east boundary profile mismatch: "+item.Id);
            }
        }
        static bool InteriorDiffers(Item a,Item b) { for(int y=1;y<63;y++)for(int x=1;x<63;x++)if(!a.Pixels[y*Cell+x].Equals(b.Pixels[y*Cell+x]))return true;return false; }
        static Color32[] Assemble(List<Item> items,int size) { var p=new Color32[size*size];foreach(Item i in items){Rect r=SlotRect(i.Slot,size);for(int y=0;y<Cell;y++)Array.Copy(i.Pixels,y*Cell,p,((int)r.y+y)*size+(int)r.x,Cell);}return p; }
        static void CheckReserved(List<Item> items,int size) { Color32[] p=Assemble(items,size);var used=new HashSet<int>(items.Select(i=>i.Slot));int cols=size/Cell;for(int s=0;s<cols*cols;s++)if(!used.Contains(s)){Rect r=SlotRect(s,size);for(int y=0;y<Cell;y++)for(int x=0;x<Cell;x++)if(!p[((int)r.y+y)*size+(int)r.x+x].Equals(new Color32()))throw new InvalidOperationException("Nonzero reserved slot "+s); } }

        static void CheckSceneCreationAllowed()
        {
            if(Application.isBatchMode)return;for(int i=0;i<SceneManager.sceneCount;i++){string p=SceneManager.GetSceneAt(i).path;if(string.IsNullOrEmpty(p))throw new InvalidOperationException("Save or close untitled scenes before building ModularElevationTest. Nothing changed.");if(p==TargetScene)throw new InvalidOperationException("Close loaded ModularElevationTest before rebuilding. Nothing changed.");}
        }

        static void BuildScene(List<Item> elevation,List<Item> shadows)
        {
            List<Placement> plan=Plan(MakeHeights(),elevation,shadows);Scene previous=SceneManager.GetActiveScene();Scene scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,Application.isBatchMode?NewSceneMode.Single:NewSceneMode.Additive);SceneManager.SetActiveScene(scene);
            try
            {
                var grid=new GameObject("Modular64 Elevation Diagnostic - SOUTH-FACING only",typeof(Grid));SceneManager.MoveGameObjectToScene(grid,scene);var maps=new Tilemap[2,3];
                for(int l=0;l<2;l++){maps[l,0]=NewMap("Shadow Level "+(l+1),grid.transform,l*10);maps[l,1]=NewMap("Cliff South Level "+(l+1),grid.transform,l*10+1);maps[l,2]=NewMap("Elevated Top Level "+(l+1),grid.transform,l*10+2);}
                foreach(Placement p in plan){int layer=p.Top!=null?2:p.Cliff!=null?1:0;Tile tile=p.Top!=null?p.Top.Tile:p.Cliff!=null?p.Cliff.Tile:p.Shadow.Tile;maps[p.Level-1,layer].SetTile(new Vector3Int(p.X,p.Y,0),tile);}
                var go=new GameObject("Diagnostic Camera",typeof(Camera));SceneManager.MoveGameObjectToScene(go,scene);go.transform.position=new Vector3(15,12,-10);Camera camera=go.GetComponent<Camera>();camera.orthographic=true;camera.orthographicSize=12.5f;camera.backgroundColor=new Color(.12f,.15f,.19f);camera.clearFlags=CameraClearFlags.SolidColor;
                Directory.CreateDirectory("Assets/Sapphire/Scenes");if(!EditorSceneManager.SaveScene(scene,TargetScene))throw new IOException("Could not save "+TargetScene);WritePreview(plan);
            }
            finally { if(!Application.isBatchMode){if(previous.IsValid()&&previous.isLoaded)SceneManager.SetActiveScene(previous);EditorSceneManager.CloseScene(scene,true);} }
        }

        static Tilemap NewMap(string name,Transform parent,int order){var go=new GameObject(name,typeof(Tilemap),typeof(TilemapRenderer));go.transform.SetParent(parent,false);go.GetComponent<TilemapRenderer>().sortingOrder=order;return go.GetComponent<Tilemap>();}
        static void WritePreview(List<Placement> plan)
        {
            var pixels=Enumerable.Repeat(new Color32(31,38,48,255),MapW*Cell*MapH*Cell).ToArray();
            foreach(Placement p in plan.OrderBy(p=>p.Level).ThenBy(p=>p.Shadow==null?(p.Cliff==null?2:1):0)){Item item=p.Top??p.Cliff??p.Shadow;for(int y=0;y<Cell;y++)for(int x=0;x<Cell;x++){Color32 s=item.Pixels[y*Cell+x];if(s.a==0)continue;int i=(p.Y*Cell+y)*MapW*Cell+p.X*Cell+x,a=s.a;Color32 d=pixels[i];pixels[i]=new Color32((byte)((s.r*a+d.r*(255-a))/255),(byte)((s.g*a+d.g*(255-a))/255),(byte)((s.b*a+d.b*(255-a))/255),255);}}
            WritePng(Path.Combine(Verification,"modular-elevation-test.png"),pixels,MapW*Cell,MapH*Cell);
        }

        static Rect SlotRect(int slot,int size){int c=size/Cell;return new Rect((slot%c)*Cell,(c-1-slot/c)*Cell,Cell,Cell);}
        static int Hash(int x,int y,int salt){unchecked{int h=x*73856093^y*19349663^salt*83492791;return h^(h>>13);}}
        static int PositiveMod(int v,int m){int r=v%m;return r<0?r+m:r;}
        static void WriteAtlas(string path,int size,List<Item> items)=>WritePng(path,Assemble(items,size),size,size);
        static void WritePng(string path,Color32[] pixels,int w,int h){var t=new Texture2D(w,h,TextureFormat.RGBA32,false);t.SetPixels32(pixels);t.Apply();File.WriteAllBytes(path,t.EncodeToPNG());UnityEngine.Object.DestroyImmediate(t);}
        static void ImportAtlas(string path,int size){AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);var i=(TextureImporter)AssetImporter.GetAtPath(path);i.textureType=TextureImporterType.Sprite;i.spriteImportMode=SpriteImportMode.Single;i.spritePixelsPerUnit=Cell;i.filterMode=FilterMode.Point;i.mipmapEnabled=false;i.textureCompression=TextureImporterCompression.Uncompressed;i.alphaIsTransparency=false;i.isReadable=true;i.wrapMode=TextureWrapMode.Clamp;i.maxTextureSize=size;i.SaveAndReimport();}

        static void SaveTiles(List<Item> items,string atlasPath,int size,string dir)
        {
            Texture2D atlas=AssetDatabase.LoadAssetAtPath<Texture2D>(atlasPath);foreach(Item item in items){string path=dir+"/"+item.Id+".asset";Tile tile=AssetDatabase.LoadAssetAtPath<Tile>(path);if(tile==null){tile=ScriptableObject.CreateInstance<Tile>();AssetDatabase.CreateAsset(tile,path);}Sprite sprite=AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().FirstOrDefault();Sprite made=Sprite.Create(atlas,SlotRect(item.Slot,size),new Vector2(.5f,.5f),Cell,0,SpriteMeshType.FullRect);made.name=item.Id;if(sprite==null){sprite=made;AssetDatabase.AddObjectToAsset(sprite,tile);}else{EditorUtility.CopySerialized(made,sprite);UnityEngine.Object.DestroyImmediate(made);}tile.name=item.Id;tile.sprite=sprite;tile.colliderType=Tile.ColliderType.None;tile.color=Color.white;tile.transform=Matrix4x4.identity;EditorUtility.SetDirty(sprite);EditorUtility.SetDirty(tile);item.Tile=tile;}
        }
        static void WriteManifest(string path,List<Item> items,int size,string dir){var csv=new StringBuilder("id,semantic,layer,mask,atlas_slot,atlas_x,atlas_y_bottom,variant,asset\n");foreach(Item i in items.OrderBy(i=>i.Slot)){Rect r=SlotRect(i.Slot,size);csv.AppendLine($"{i.Id},{i.Semantic},{i.Layer},{i.Mask},{i.Slot},{r.x},{r.y},{i.Variant},{dir}/{i.Id}.asset");}File.WriteAllText(path,csv.ToString());}
        static void ReloadCheck(List<Item> items,string atlasPath,int size,string dir){Texture2D atlas=AssetDatabase.LoadAssetAtPath<Texture2D>(atlasPath);foreach(Item i in items){string path=dir+"/"+i.Id+".asset";AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);i.Tile=AssetDatabase.LoadAssetAtPath<Tile>(path);Sprite s=i.Tile==null?null:i.Tile.sprite;if(s==null||s.texture!=atlas||s.rect!=SlotRect(i.Slot,size)||s.pixelsPerUnit!=Cell)throw new InvalidDataException("Sprite texture/rect/64PPU mismatch: "+i.Id);}}
    }
}
