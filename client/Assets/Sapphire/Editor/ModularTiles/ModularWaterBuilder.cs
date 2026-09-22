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
    /// <summary>Builds opaque water and four independently swappable frames of shoreline foam.</summary>
    public static class ModularWaterBuilder
    {
        const int Cell=64, AtlasSize=1024, MapSize=30;
        const string Root="Assets/Sapphire/Art/World/Modular64";
        const string Atlas=Root+"/TS03_Water_Foam_64.png", TileRoot=Root+"/Tiles/WaterFoam";
        const string TargetScene="Assets/Sapphire/Scenes/ModularWaterTest.unity";
        static string Verification=>Path.GetFullPath(Path.Combine(Application.dataPath,"../../verification"));
        static readonly int[] FrameStarts={16,64,112,160};
        static readonly int[] Dx={0,1,1,1,0,-1,-1,-1}, Dy={1,1,0,-1,-1,-1,0,1};
        static readonly Color32[] WaterPalette={new Color32(18,70,94,255),new Color32(22,82,106,255),new Color32(27,94,119,255),new Color32(32,107,132,255),new Color32(41,121,144,255),new Color32(53,137,157,255),new Color32(70,153,169,255),new Color32(91,170,180,255)};
        sealed class Item { public string Id,Semantic,Layer; public int Mask,Slot,Variant,Frame; public Color32[] Pixels=new Color32[Cell*Cell]; public Tile Tile; }

        [MenuItem("Sapphire/Modular Tiles/Build Independent Water + Foam Test")]
        public static void Build()
        {
            CheckSceneCreationAllowed();
            Texture2D source=ReadSource(); List<Item> items;
            try { items=BuildItems(source); } finally { UnityEngine.Object.DestroyImmediate(source); }
            Directory.CreateDirectory(TileRoot); Directory.CreateDirectory(Verification);
            Validate(items); WriteAtlas(items); ImportAtlas(); SaveTiles(items); WriteManifest(items);
            AssetDatabase.SaveAssets(); ReloadCheck(items); BuildScene(items); AssetDatabase.Refresh();
            Debug.Log("Modular64: W001-W012 and F001-F188 (47 shoreline masks x 4 frames at 0.2s) built. AnimatedTile is intentionally not required; CSV records frame timing.");
        }

        static Texture2D ReadSource()
        {
            string path=Root+"/Sources/WaterMaster.png";
            if(!File.Exists(path)) throw new FileNotFoundException("Provide WaterMaster.png material source.",path);
            var t=new Texture2D(2,2,TextureFormat.RGBA32,false);
            try { if(!t.LoadImage(File.ReadAllBytes(path))) throw new InvalidDataException("Cannot decode "+path); if(t.width<3||t.height<3) throw new InvalidDataException("WaterMaster.png must be at least 3x3."); if(t.GetPixels32().Any(p=>p.a==0)) throw new InvalidDataException("WaterMaster.png contains fully transparent pixels with unusable RGB."); return t; }
            catch { UnityEngine.Object.DestroyImmediate(t); throw; }
        }

        static List<Item> BuildItems(Texture2D source)
        {
            var result=new List<Item>(); Color32[] src=source.GetPixels32();
            for(int v=0;v<12;v++)
            {
                var item=new Item{Id="W"+(v+1).ToString("D3"),Semantic="SeamlessVariant",Layer="Water",Mask=255,Slot=v,Variant=v+1,Frame=-1};
                for(int y=0;y<Cell;y++) for(int x=0;x<Cell;x++)
                {
                    // All variants share the exact perimeter; only the protected interior changes.
                    if(x==0||x==63||y==0||y==63) item.Pixels[y*Cell+x]=WaterContact(x==0||x==63?y:x);
                    else { int sx=PositiveMod(x+v*83,source.width),sy=PositiveMod(y+v*127,source.height); Color32 c=src[sy*source.width+sx]; int n=PositiveMod((c.r*3+c.g*5+c.b*2)/10+Hash(x/4,y/4,v),WaterPalette.Length); item.Pixels[y*Cell+x]=WaterPalette[n]; }
                }
                result.Add(item);
            }
            int[] masks=Enumerable.Range(0,256).Select(NormalizeMask).Distinct().OrderBy(x=>x).ToArray();
            if(masks.Length!=47) throw new InvalidOperationException("Mask normalization must produce 47 shoreline shapes.");
            for(int frame=0;frame<4;frame++) for(int ordinal=0;ordinal<47;ordinal++)
            {
                int index=frame*47+ordinal; var item=new Item{Id="F"+(index+1).ToString("D3"),Semantic="BlobMask"+masks[ordinal],Layer="Foam",Mask=masks[ordinal],Slot=FrameStarts[frame]+ordinal,Variant=0,Frame=frame};
                for(int y=0;y<Cell;y++) for(int x=0;x<Cell;x++) item.Pixels[y*Cell+x]=FoamPixel(masks[ordinal],x,y,frame);
                result.Add(item);
            }
            return result;
        }

        // Mask denotes land occupancy. Foam is a transparent water-side overlay around that geometry.
        static Color32 FoamPixel(int mask,int x,int y,int frame)
        {
            // DistanceToLand intentionally has no neighbouring tile's mask available.
            // Never let its out-of-cell probes decide a serialized seam pixel: legal
            // neighbours can have different masks and would otherwise disagree there.
            // The visible foam begins one pixel inside the cell and remains contiguous
            // over the opaque water below, while every legal shared RGBA edge is exact.
            if(x==0||x==63||y==0||y==63) return new Color32();
            int d=DistanceToLand(mask,x,y); if(d<0||d>8) return new Color32();
            bool contact=d<=2; // invariant shoreline contact across all animation frames
            // Edge phase cannot depend on this tile's mask: legal neighbours often have
            // different masks but must serialize byte-identical RGBA at their shared edge.
            int phase=(x==0||x==63)?Hash(y/3,0,frame*29):(y==0||y==63)?Hash(x/3,0,frame*29):Hash(x/3,y/3,frame*29+mask);
            bool moving=d<=5&&PositiveMod(phase,7)<(d==3?5:3);
            if(!contact&&!moving) return new Color32();
            byte a=contact?(byte)(d==0?255:190):(byte)120;
            return new Color32(220,246,245,a);
        }

        static int DistanceToLand(int mask,int x,int y)
        {
            if(LandVisible(mask,x,y)) return -1;
            for(int r=1;r<=8;r++) for(int oy=-r;oy<=r;oy++) for(int ox=-r;ox<=r;ox++)
                if(Math.Max(Math.Abs(ox),Math.Abs(oy))==r&&LandVisible(mask,x+ox,y+oy)) return r-1;
            return 99;
        }

        static bool LandVisible(int mask,int x,int y)
        {
            // The current cell is water. The mask describes land in its eight
            // neighbouring cells, so only probes that leave this 64px cell may
            // encounter land. Treating the interior as land produced a square
            // foam frame for every mask and exposed the tile grid.
            bool north=y>=Cell,south=y<0,east=x>=Cell,west=x<0;
            if(!north&&!south&&!east&&!west)return false;
            // At an out-of-cell corner, either touching cardinal neighbour extends
            // its bank through the probe. This keeps a straight bank continuous
            // across tile rows instead of drawing a bracket around every cell.
            if(north&&east)return (mask&((1<<0)|(1<<1)|(1<<2)))!=0;
            if(south&&east)return (mask&((1<<2)|(1<<3)|(1<<4)))!=0;
            if(south&&west)return (mask&((1<<4)|(1<<5)|(1<<6)))!=0;
            if(north&&west)return (mask&((1<<6)|(1<<7)|(1<<0)))!=0;
            if(north)return (mask&(1<<0))!=0;
            if(east)return (mask&(1<<2))!=0;
            if(south)return (mask&(1<<4))!=0;
            return (mask&(1<<6))!=0;
        }

        public static int NormalizeMask(int mask) { mask&=255; for(int d=1;d<8;d+=2) if((mask&(1<<(d-1)))==0||(mask&(1<<((d+1)%8)))==0) mask&=~(1<<d); return mask; }
        static Color32 WaterContact(int p)=>WaterPalette[2+PositiveMod(Hash(p==63?0:p,0,71),3)];
        static int Hash(int x,int y,int salt){unchecked{int h=x*73856093^y*19349663^salt*83492791;return h^(h>>13);}}
        static int PositiveMod(int v,int m){int r=v%m;return r<0?r+m:r;}

        static void Validate(List<Item> items)
        {
            Item[] water=items.Where(i=>i.Layer=="Water").ToArray(),foam=items.Where(i=>i.Layer=="Foam").ToArray();
            if(water.Length!=12||foam.Length!=188||items.Select(i=>i.Id).Distinct().Count()!=200) throw new InvalidOperationException("Expected 12 unique water and 188 unique foam tiles.");
            if(items.Select(i=>i.Slot).Distinct().Count()!=200||items.Any(i=>i.Slot<0||i.Slot>=256)) throw new InvalidOperationException("Atlas slot contract violated.");
            for(int f=0;f<4;f++) if(foam.Count(i=>i.Frame==f)!=47||foam.Where(i=>i.Frame==f).Select(i=>i.Slot).OrderBy(x=>x).Where((s,n)=>s!=FrameStarts[f]+n).Any()) throw new InvalidOperationException("Foam frame block contract violated.");
            if(water.Any(i=>i.Pixels.Any(p=>p.a!=255))) throw new InvalidOperationException("Water must be opaque.");
            var allowed=new HashSet<byte>{0,120,190,255}; if(foam.Any(i=>i.Pixels.Any(p=>!allowed.Contains(p.a)||(p.a==0&&(p.r!=0||p.g!=0||p.b!=0))))) throw new InvalidOperationException("Foam alpha/RGB contract violated.");
            foreach(Item a in water) foreach(Item b in water) { CheckEdge(a,b,true,"water horizontal"); CheckEdge(a,b,false,"water vertical"); }
            int comparisons=0;
            for(int frame=0;frame<4;frame++)
            {
                var lookup=foam.Where(i=>i.Frame==frame).ToDictionary(i=>i.Mask);
                for(int bits=0;bits<4096;bits++)
                {
                    Func<int,int,bool> has=(x,y)=>x>=0&&x<4&&y>=0&&y<3&&(bits&(1<<(y*4+x)))!=0;
                    if(!has(1,1)||!has(2,1)) continue;
                    int ma=0,mb=0; for(int d=0;d<8;d++){if(has(1+Dx[d],1+Dy[d]))ma|=1<<d;if(has(2+Dx[d],1+Dy[d]))mb|=1<<d;} CheckEdge(lookup[NormalizeMask(ma)],lookup[NormalizeMask(mb)],true,"foam H frame "+frame); comparisons+=Cell;
                }
                for(int bits=0;bits<4096;bits++)
                {
                    Func<int,int,bool> has=(x,y)=>x>=0&&x<3&&y>=0&&y<4&&(bits&(1<<(y*3+x)))!=0;
                    if(!has(1,1)||!has(1,2)) continue;
                    int bottom=0,top=0; for(int d=0;d<8;d++){if(has(1+Dx[d],1+Dy[d]))bottom|=1<<d;if(has(1+Dx[d],2+Dy[d]))top|=1<<d;}
                    CheckEdge(lookup[NormalizeMask(bottom)],lookup[NormalizeMask(top)],false,"foam V frame "+frame); comparisons+=Cell;
                }
            }
            for(int ordinal=0;ordinal<47;ordinal++) for(int y=0;y<Cell;y++) for(int x=0;x<Cell;x++) if(DistanceToLand(foam[ordinal].Mask,x,y)<=2)
            { byte a=foam[ordinal].Pixels[y*Cell+x].a; for(int f=1;f<4;f++) if(foam[f*47+ordinal].Pixels[y*Cell+x].a!=a) throw new InvalidOperationException("Animated foam moved its shoreline contact."); }
            Color32[] atlas=Assemble(items); var used=new HashSet<int>(items.Select(i=>i.Slot)); for(int s=0;s<256;s++) if(!used.Contains(s)){Rect r=SlotRect(s);for(int y=0;y<Cell;y++)for(int x=0;x<Cell;x++)if(!atlas[((int)r.y+y)*AtlasSize+(int)r.x+x].Equals(new Color32()))throw new InvalidOperationException("Reserved slot nonzero: "+s);}
            File.WriteAllText(Path.Combine(Verification,"modular-water-validation.txt"),$"PASS: 256 masks normalize to 47; W001-W012 perimeter-compatible.\nPASS: F001-F188 = 47 shoreline masks x 4 frames; frame duration 0.2 seconds.\nPASS: {comparisons} legal per-frame RGBA edge pixel comparisons.\nPASS: foam alpha limited to 0/120/190/255, transparent pixels zero RGB, shoreline contact invariant.\nPASS: all 56 reserved slots zero RGBA; reload checks Sprite texture/rect/64PPU.\nNOTE: four Tile assets per shape are provided instead of optional AnimatedTile; CSV frame/frame_seconds columns define playback.\n");
        }

        static void CheckEdge(Item a,Item b,bool horizontal,string label){for(int p=0;p<Cell;p++){int ia=horizontal?p*Cell+63:63*Cell+p,ib=horizontal?p*Cell:p;if(!a.Pixels[ia].Equals(b.Pixels[ib]))throw new InvalidOperationException(label+" edge mismatch: "+a.Id+" / "+b.Id);}}
        static Rect SlotRect(int slot)=>new Rect((slot%16)*Cell,(15-slot/16)*Cell,Cell,Cell);
        static Color32[] Assemble(List<Item> items){var p=new Color32[AtlasSize*AtlasSize];foreach(Item i in items){Rect r=SlotRect(i.Slot);for(int y=0;y<Cell;y++)Array.Copy(i.Pixels,y*Cell,p,((int)r.y+y)*AtlasSize+(int)r.x,Cell);}return p;}
        static void WriteAtlas(List<Item> items){WritePng(Atlas,Assemble(items),AtlasSize,AtlasSize);}
        static void WritePng(string path,Color32[] p,int w,int h){var t=new Texture2D(w,h,TextureFormat.RGBA32,false);t.SetPixels32(p);t.Apply();File.WriteAllBytes(path,t.EncodeToPNG());UnityEngine.Object.DestroyImmediate(t);}
        static void ImportAtlas(){AssetDatabase.ImportAsset(Atlas,ImportAssetOptions.ForceSynchronousImport);var i=(TextureImporter)AssetImporter.GetAtPath(Atlas);i.textureType=TextureImporterType.Sprite;i.spriteImportMode=SpriteImportMode.Single;i.spritePixelsPerUnit=Cell;i.filterMode=FilterMode.Point;i.mipmapEnabled=false;i.textureCompression=TextureImporterCompression.Uncompressed;i.alphaIsTransparency=false;i.isReadable=true;i.wrapMode=TextureWrapMode.Clamp;i.maxTextureSize=AtlasSize;i.SaveAndReimport();}
        static void SaveTiles(List<Item> items)
        {
            Texture2D atlas=AssetDatabase.LoadAssetAtPath<Texture2D>(Atlas); foreach(Item item in items){string path=TileRoot+"/"+item.Id+".asset";Tile tile=AssetDatabase.LoadAssetAtPath<Tile>(path);if(tile==null){tile=ScriptableObject.CreateInstance<Tile>();AssetDatabase.CreateAsset(tile,path);}Sprite sprite=AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().FirstOrDefault(),made=Sprite.Create(atlas,SlotRect(item.Slot),new Vector2(.5f,.5f),Cell,0,SpriteMeshType.FullRect);made.name=item.Id;if(sprite==null){sprite=made;AssetDatabase.AddObjectToAsset(sprite,tile);}else{EditorUtility.CopySerialized(made,sprite);UnityEngine.Object.DestroyImmediate(made);}tile.name=item.Id;tile.sprite=sprite;tile.colliderType=Tile.ColliderType.None;tile.color=Color.white;tile.transform=Matrix4x4.identity;EditorUtility.SetDirty(sprite);EditorUtility.SetDirty(tile);item.Tile=tile;}
        }
        static void WriteManifest(List<Item> items){var s=new StringBuilder("id,semantic,layer,normalized_mask,atlas_slot,atlas_x,atlas_y_bottom,variant,frame,frame_seconds,asset\n");foreach(Item i in items.OrderBy(i=>i.Slot)){Rect r=SlotRect(i.Slot);s.AppendLine($"{i.Id},{i.Semantic},{i.Layer},{i.Mask},{i.Slot},{r.x},{r.y},{i.Variant},{i.Frame},{(i.Frame<0?0f:.2f):0.0},{TileRoot}/{i.Id}.asset");}File.WriteAllText(Root+"/TS03_Water_Foam_64.csv",s.ToString());}
        static void ReloadCheck(List<Item> items){Texture2D atlas=AssetDatabase.LoadAssetAtPath<Texture2D>(Atlas);foreach(Item i in items){string path=TileRoot+"/"+i.Id+".asset";AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);i.Tile=AssetDatabase.LoadAssetAtPath<Tile>(path);Sprite s=i.Tile==null?null:i.Tile.sprite;if(s==null||s.texture!=atlas||s.rect!=SlotRect(i.Slot)||s.pixelsPerUnit!=Cell)throw new InvalidDataException("Sprite texture/rect/64PPU mismatch: "+i.Id);}}

        static void CheckSceneCreationAllowed(){if(Application.isBatchMode)return;for(int i=0;i<SceneManager.sceneCount;i++){string p=SceneManager.GetSceneAt(i).path;if(string.IsNullOrEmpty(p))throw new InvalidOperationException("Save or close untitled scenes before building ModularWaterTest. Nothing changed.");if(p==TargetScene)throw new InvalidOperationException("Close loaded ModularWaterTest before rebuilding. Nothing changed.");}}
        static bool[,] IslandMap()
        {
            var a=new bool[MapSize,MapSize];
            for(int x=2;x<13;x++)for(int y=3;y<12;y++)if((x-7)*(x-7)+(y-7)*(y-7)<29)a[x,y]=true;
            for(int x=17;x<28;x++)for(int y=3;y<12;y++)if(x<20||x>25||y<6)a[x,y]=true;
            for(int x=3;x<14;x++)for(int y=17;y<27;y++)if(x==5||y==20||(x>8&&y>22))a[x,y]=true;
            for(int x=18;x<27;x++)for(int y=18;y<27;y++)if((x+y*3)%7!=0)a[x,y]=true;
            return a;
        }
        static int MaskAt(bool[,] land,int x,int y){int m=0;for(int d=0;d<8;d++){int xx=x+Dx[d],yy=y+Dy[d];if(xx>=0&&yy>=0&&xx<MapSize&&yy<MapSize&&land[xx,yy])m|=1<<d;}return NormalizeMask(m);}
        static void BuildScene(List<Item> items)
        {
            var water=items.Where(i=>i.Layer=="Water").ToArray(); var foam=items.Where(i=>i.Layer=="Foam").ToDictionary(i=>(i.Frame,i.Mask)); bool[,] land=IslandMap(); Scene previous=SceneManager.GetActiveScene();Scene scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,Application.isBatchMode?NewSceneMode.Single:NewSceneMode.Additive);SceneManager.SetActiveScene(scene);
            try
            {
                var masks=Enumerable.Range(0,256).Select(NormalizeMask).Distinct().OrderBy(x=>x).ToArray(); var ground=masks.Select((m,n)=>new{Mask=m,Tile=AssetDatabase.LoadAssetAtPath<Tile>(Root+"/Tiles/G"+(n+1).ToString("D3")+".asset")}).ToDictionary(x=>x.Mask,x=>x.Tile);
                if(ground.Values.Any(t=>t==null)) throw new FileNotFoundException("Build TS01 first; ModularWaterTest displays its G001-G047 island geometry.");
                var grid=new GameObject("Modular64 Water + Ground Islands + Foam Diagnostic (quadrants show frames 0-3)",typeof(Grid));SceneManager.MoveGameObjectToScene(grid,scene);Tilemap waterMap=NewMap("Opaque Water Variants",grid.transform,0);Tilemap groundMap=NewMap("TS01 Ground Islands",grid.transform,1);Tilemap[] frames=Enumerable.Range(0,4).Select(f=>NewMap("Foam Frame "+f+" (0.2 sec)",grid.transform,f+2)).ToArray();
                for(int y=0;y<MapSize;y++)for(int x=0;x<MapSize;x++){waterMap.SetTile(new Vector3Int(x,y,0),water[PositiveMod(Hash(x,y,11),12)].Tile);if(land[x,y]){int mask=MaskAt(land,x,y),frame=(x<15?0:1)+(y>=15?2:0);groundMap.SetTile(new Vector3Int(x,y,0),ground[mask]);frames[frame].SetTile(new Vector3Int(x,y,0),foam[(frame,mask)].Tile);}}
                var camGo=new GameObject("Diagnostic Camera",typeof(Camera));SceneManager.MoveGameObjectToScene(camGo,scene);camGo.transform.position=new Vector3(15,15,-10);Camera cam=camGo.GetComponent<Camera>();cam.orthographic=true;cam.orthographicSize=15.5f;cam.backgroundColor=new Color(.08f,.16f,.2f);cam.clearFlags=CameraClearFlags.SolidColor;
                Directory.CreateDirectory("Assets/Sapphire/Scenes");if(!EditorSceneManager.SaveScene(scene,TargetScene))throw new IOException("Could not save "+TargetScene);WritePreview(items,land);
            }
            finally {if(!Application.isBatchMode){if(previous.IsValid()&&previous.isLoaded)SceneManager.SetActiveScene(previous);EditorSceneManager.CloseScene(scene,true);}}
        }
        static Tilemap NewMap(string name,Transform parent,int order){var go=new GameObject(name,typeof(Tilemap),typeof(TilemapRenderer));go.transform.SetParent(parent,false);go.GetComponent<TilemapRenderer>().sortingOrder=order;return go.GetComponent<Tilemap>();}
        static void WritePreview(List<Item> items,bool[,] land)
        {
            Item[] water=items.Where(i=>i.Layer=="Water").ToArray();var foam=items.Where(i=>i.Layer=="Foam").ToDictionary(i=>(i.Frame,i.Mask));var p=new Color32[MapSize*Cell*MapSize*Cell];
            int[] masks=Enumerable.Range(0,256).Select(NormalizeMask).Distinct().OrderBy(x=>x).ToArray();
            var ground=masks.Select((m,n)=>new{Mask=m,Pixels=ReadTilePixels(Root+"/Tiles/G"+(n+1).ToString("D3")+".asset")}).ToDictionary(x=>x.Mask,x=>x.Pixels);
            for(int y=0;y<MapSize;y++)for(int x=0;x<MapSize;x++){Item w=water[PositiveMod(Hash(x,y,11),12)];Blit(p,w.Pixels,x,y);if(land[x,y]){int mask=MaskAt(land,x,y),f=(x<15?0:1)+(y>=15?2:0);Blit(p,ground[mask],x,y);Blit(p,foam[(f,mask)].Pixels,x,y);}}
            WritePng(Path.Combine(Verification,"modular-water-test.png"),p,MapSize*Cell,MapSize*Cell);
        }
        static Color32[] ReadTilePixels(string path)
        {
            Tile tile=AssetDatabase.LoadAssetAtPath<Tile>(path); Sprite sprite=tile==null?null:tile.sprite;
            if(sprite==null||sprite.texture==null||sprite.rect.width!=Cell||sprite.rect.height!=Cell) throw new InvalidDataException("Missing or invalid TS01 ground sprite: "+path);
            Color32[] atlas=sprite.texture.GetPixels32(), result=new Color32[Cell*Cell]; int ox=Mathf.RoundToInt(sprite.rect.x),oy=Mathf.RoundToInt(sprite.rect.y),width=sprite.texture.width;
            for(int y=0;y<Cell;y++) Array.Copy(atlas,(oy+y)*width+ox,result,y*Cell,Cell);
            return result;
        }
        static void Blit(Color32[] dst,Color32[] src,int cx,int cy){int width=MapSize*Cell;for(int y=0;y<Cell;y++)for(int x=0;x<Cell;x++){Color32 s=src[y*Cell+x];if(s.a==0)continue;int i=(cy*Cell+y)*width+cx*Cell+x,a=s.a;Color32 d=dst[i];dst[i]=new Color32((byte)((s.r*a+d.r*(255-a))/255),(byte)((s.g*a+d.g*(255-a))/255),(byte)((s.b*a+d.b*(255-a))/255),255);}}
    }
}
