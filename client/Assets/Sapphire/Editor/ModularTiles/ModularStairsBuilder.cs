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
    /// <summary>Adds the south-facing stair extension to reserved TS02 slots without rebuilding ET/C.</summary>
    public static class ModularStairsBuilder
    {
        const int Cell=64, AtlasSize=1024, MapW=30, MapH=16;
        const string Root="Assets/Sapphire/Art/World/Modular64";
        const string Atlas=Root+"/TS02_Elevation_Cliff_64.png", GroundAtlas=Root+"/TS01_Ground_Path_64.png";
        const string TileDir=Root+"/Tiles/Stairs", Manifest=Root+"/TS02_Stairs_64.csv";
        const string ScenePath="Assets/Sapphire/Scenes/ModularStairsTest.unity";
        static string Verification=>Path.GetFullPath(Path.Combine(Application.dataPath,"../../verification"));
        static readonly Color32[] Rock={new Color32(48,58,70,255),new Color32(59,70,82,255),new Color32(70,82,94,255),new Color32(82,94,106,255),new Color32(95,107,118,255),new Color32(110,121,131,255),new Color32(125,135,144,255),new Color32(140,149,157,255)};
        static readonly Color32[] Grass={new Color32(43,77,25,255),new Color32(57,96,30,255),new Color32(71,116,36,255),new Color32(86,136,43,255),new Color32(101,151,50,255),new Color32(114,162,60,255),new Color32(129,176,70,255),new Color32(144,188,82,255),new Color32(163,201,100,255)};

        sealed class Item { public string Id,Semantic; public int Slot,Width,Segment,Lane; public Color32[] Pixels=new Color32[Cell*Cell]; public Tile Tile; }
        sealed class Placement { public int X,Y,Order; public Tile Tile; public Color32[] Pixels; }
        sealed class ExistingRef { public string Id,Path,TileGuid,SpriteGuid; public long TileLocalId,SpriteLocalId; public Rect Rect; }

        [MenuItem("Sapphire/Modular Tiles/Build Independent Stairs Test")]
        public static void Build()
        {
            CheckSceneCreationAllowed();
            Texture2D oldAtlas=ReadAtlas(Atlas,"Build the elevation stage first."), ground=ReadAtlas(GroundAtlas,"Build the ground stage first."), cliff=ReadOpaque(Root+"/Sources/CliffMaster.png","Provide CliffMaster.png."), grass=ReadMaterial(Root+"/Sources/GrassMaster.png");
            try
            {
                if(oldAtlas.width!=AtlasSize||oldAtlas.height!=AtlasSize||ground.width!=AtlasSize||ground.height!=AtlasSize) throw new InvalidDataException("TS01 and TS02 must be 1024x1024.");
                Color32[] before=oldAtlas.GetPixels32(), groundPixels=ground.GetPixels32();
                List<Item> items=BuildItems(before,groundPixels,cliff,grass);
                ValidateExistingContract(before,items);
                List<ExistingRef> existingRefs=CaptureExistingRefs();
                Color32[] after=(Color32[])before.Clone(); WriteSlots(after,items); Validate(items,before,after,groundPixels);
                Directory.CreateDirectory(TileDir); Directory.CreateDirectory(Verification); WritePng(Atlas,after,AtlasSize,AtlasSize); ImportAtlas();
                AssertExistingRefs(existingRefs); SaveTiles(items); WriteManifest(items); AssetDatabase.SaveAssets(); ReloadCheck(items); AssertExistingRefs(existingRefs); BuildScene(items,before,groundPixels); WriteValidationReport(); AssetDatabase.Refresh();
                Debug.Log("Modular64: S001-S012 south-facing stairs built in TS02 slots 96-107; all ET/C pixels preserved.");
            }
            finally { UnityEngine.Object.DestroyImmediate(oldAtlas); UnityEngine.Object.DestroyImmediate(ground); UnityEngine.Object.DestroyImmediate(cliff); UnityEngine.Object.DestroyImmediate(grass); }
        }

        static Texture2D ReadOpaque(string file,string hint)
        {
            if(!File.Exists(file)) throw new FileNotFoundException(hint,file); var t=new Texture2D(2,2,TextureFormat.RGBA32,false);
            try { if(!t.LoadImage(File.ReadAllBytes(file))) throw new InvalidDataException("Cannot decode "+file); if(t.GetPixels32().Any(p=>p.a!=255)) throw new InvalidDataException(file+" must be opaque."); return t; }
            catch { UnityEngine.Object.DestroyImmediate(t); throw; }
        }

        static Texture2D ReadAtlas(string file,string hint)
        {
            if(!File.Exists(file)) throw new FileNotFoundException(hint,file); var t=new Texture2D(2,2,TextureFormat.RGBA32,false);
            try { if(!t.LoadImage(File.ReadAllBytes(file))) throw new InvalidDataException("Cannot decode "+file); return t; }
            catch { UnityEngine.Object.DestroyImmediate(t); throw; }
        }

        static Texture2D ReadMaterial(string file)
        {
            if(!File.Exists(file)) throw new FileNotFoundException("Provide GrassMaster.png.",file); var t=new Texture2D(2,2,TextureFormat.RGBA32,false);
            try { if(!t.LoadImage(File.ReadAllBytes(file))) throw new InvalidDataException("Cannot decode "+file); if(t.GetPixels32().Any(p=>p.a==0)) throw new InvalidDataException("GrassMaster contains transparent material pixels."); return t; }
            catch { UnityEngine.Object.DestroyImmediate(t); throw; }
        }

        // S001-S003 are one-cell-wide Top/Middle/Bottom. S004-S012 are a legal
        // three-cell-wide staircase: Left/Center/Right for Top, Middle, Bottom.
        static List<Item> BuildItems(Color32[] atlas,Color32[] ground,Texture2D cliff,Texture2D grass)
        {
            var items=new List<Item>();
            for(int segment=0;segment<3;segment++) items.Add(MakeItem("S"+(segment+1).ToString("D3"),"Narrow"+SegmentName(segment),96+segment,1,segment,0,atlas,ground,cliff,grass));
            for(int segment=0;segment<3;segment++) for(int lane=0;lane<3;lane++)
            {
                int n=4+segment*3+lane; items.Add(MakeItem("S"+n.ToString("D3"),"Wide"+SegmentName(segment)+LaneName(lane),96+n-1,3,segment,lane,atlas,ground,cliff,grass));
            }
            StitchVertical(items[0],items[1]); StitchVertical(items[1],items[2]);
            for(int lane=0;lane<3;lane++){StitchVertical(items[3+lane],items[6+lane]);StitchVertical(items[6+lane],items[9+lane]);}
            for(int segment=0;segment<3;segment++) HarmonizeWideRow(items.Skip(3+segment*3).Take(3).ToArray(),segment,atlas,ground);
            ApplyCliffBanks(items,atlas);
            return items;
        }

        static void StitchVertical(Item upper,Item lower){for(int x=0;x<Cell;x++)lower.Pixels[63*Cell+x]=upper.Pixels[x];}
        static void HarmonizeWideRow(Item[] row,int segment,Color32[] atlas,Color32[] ground)
        {
            for(int y=0;y<Cell;y++) for(int join=0;join<2;join++)
            {
                Color32 c=segment==0&&y==63?AtlasPixel(atlas,64,0,63):segment==2&&y==0?AtlasPixel(ground,46,0,63):WideJoin(y,segment);
                row[join].Pixels[y*Cell+63]=c; row[join+1].Pixels[y*Cell]=c;
            }
        }
        // Corners belong to the N/S contracts. Interior west/east pixels match the
        // adjacent C001 east/west bank exactly on every stair row.
        static void ApplyCliffBanks(List<Item> items,Color32[] atlas)
        {
            foreach(Item i in items) for(int y=1;y<Cell-1;y++)
            {
                if(i.Width==1||i.Lane==0)i.Pixels[y*Cell]=AtlasPixel(atlas,64,63,y);
                if(i.Width==1||i.Lane==2)i.Pixels[y*Cell+63]=AtlasPixel(atlas,64,0,y);
            }
        }

        static Item MakeItem(string id,string semantic,int slot,int width,int segment,int lane,Color32[] atlas,Color32[] ground,Texture2D cliff,Texture2D grass)
        {
            var item=new Item{Id=id,Semantic=semantic,Slot=slot,Width=width,Segment=segment,Lane=lane}; Color32[] cs=cliff.GetPixels32(), gs=grass.GetPixels32();
            for(int y=0;y<Cell;y++) for(int x=0;x<Cell;x++)
            {
                if(segment==0&&y==63) { item.Pixels[y*Cell+x]=AtlasPixel(atlas,64,x,63); continue; } // C north == exposed ET south rim
                if(segment==2&&y==0) { item.Pixels[x]=AtlasPixel(ground,46,x,63); continue; } // full grass north contact
                int globalX=lane*Cell+x, runWidth=width*Cell;
                float progress=(segment*Cell+(63-y))/(3f*Cell-1f);
                int side=Math.Min(globalX,runWidth-1-globalX), inset=width==1?10:8; bool bank=side<inset;
                int tread=(int)(progress*12f), stripe=PositiveMod(globalX/9+tread,3);
                if(bank)
                {
                    Color32 sample=cs[PositiveMod(173+y*3+segment*97,cliff.height)*cliff.width+PositiveMod(101+globalX*2,cliff.width)];
                    item.Pixels[y*Cell+x]=Rock[Mathf.Clamp((sample.r+sample.g+sample.b)*Rock.Length/(3*256),0,Rock.Length-1)];
                }
                else if(((segment*Cell+(63-y))%16)<3) item.Pixels[y*Cell+x]=Rock[6+stripe%2];
                else
                {
                    Color32 sample=gs[PositiveMod(233+y*2+segment*83,grass.height)*grass.width+PositiveMod(149+globalX*2,grass.width)];
                    int b=Mathf.Clamp((sample.r*2+sample.g*5+sample.b)*Grass.Length/(8*256),0,Grass.Length-1); item.Pixels[y*Cell+x]=Grass[b];
                }
            }
            // Every legal horizontal join, including corners, is exact.
            if(width==3) for(int y=1;y<Cell-1;y++) item.Pixels[y*Cell+(lane==0?63:0)]=WideJoin(y,segment);
            return item;
        }

        static Color32 WideJoin(int y,int segment)=>Rock[3+PositiveMod((segment*63+63-y)/7,3)];
        static string SegmentName(int i)=>i==0?"Top":i==1?"Middle":"Bottom";
        static string LaneName(int i)=>i==0?"Left":i==1?"Center":"Right";
        static Color32 AtlasPixel(Color32[] pixels,int slot,int x,int y){Rect r=SlotRect(slot);return pixels[((int)r.y+y)*AtlasSize+(int)r.x+x];}
        static Rect SlotRect(int slot)=>new Rect((slot%16)*Cell,(15-slot/16)*Cell,Cell,Cell);

        static void ValidateExistingContract(Color32[] pixels,List<Item> items)
        {
            foreach(int slot in Enumerable.Range(0,47).Concat(Enumerable.Range(64,12))) if(SlotIsZero(pixels,slot)) throw new InvalidDataException("Occupied TS02 slot is blank: "+slot);
            bool[] occupied=items.Select(i=>!SlotIsZero(pixels,i.Slot)).ToArray();
            if(occupied.Any(x=>x)&&!occupied.All(x=>x)) throw new InvalidDataException("TS02 stair range is partially occupied; refusing ambiguous ownership.");
            if(!occupied.Any(x=>x)) return;
            foreach(Item i in items) CompareItemSlot(pixels,i,"Foreign data occupies stair slot");
            if(!File.Exists(Manifest)||File.ReadAllText(Manifest)!=BuildManifest(items)) throw new InvalidDataException("Occupied stair pixels are not accompanied by the owned TS02_Stairs_64.csv manifest.");
            Texture2D ownedAtlas=AssetDatabase.LoadAssetAtPath<Texture2D>(Atlas);
            foreach(Item i in items)
            {
                string path=TileDir+"/"+i.Id+".asset"; Tile tile=AssetDatabase.LoadAssetAtPath<Tile>(path); Sprite sprite=tile==null?null:tile.sprite;
                if(sprite==null||sprite.texture!=ownedAtlas||sprite.rect!=SlotRect(i.Slot)||sprite.pixelsPerUnit!=Cell) throw new InvalidDataException("Occupied stair range is missing its owned Tile/Sprite: "+i.Id);
            }
        }

        static void Validate(List<Item> items,Color32[] before,Color32[] after,Color32[] ground)
        {
            if(items.Count!=12||items.Select(i=>i.Id).Distinct().Count()!=12||items.Select(i=>i.Slot).OrderBy(x=>x).Where((x,i)=>x!=96+i).Any()) throw new InvalidOperationException("S001-S012/slot 96-107 contract violated.");
            if(items.Any(i=>i.Pixels.Any(p=>p.a!=255))) throw new InvalidOperationException("Stairs must be opaque.");
            var changed=new HashSet<int>(Enumerable.Range(96,12));
            for(int slot=0;slot<256;slot++) if(!changed.Contains(slot)) CompareSlot(before,after,slot,"Reserved preservation");
            Item[] narrow=items.Take(3).ToArray(); CheckVertical(narrow[0],narrow[1],"narrow top/middle"); CheckVertical(narrow[1],narrow[2],"narrow middle/bottom");
            for(int segment=0;segment<3;segment++){Item[] row=items.Skip(3+segment*3).Take(3).ToArray();CheckHorizontal(row[0],row[1],"wide left/center");CheckHorizontal(row[1],row[2],"wide center/right");}
            for(int lane=0;lane<3;lane++){CheckVertical(items[3+lane],items[6+lane],"wide top/middle");CheckVertical(items[6+lane],items[9+lane],"wide middle/bottom");}
            foreach(Item i in items.Where(i=>i.Segment==0)) for(int x=0;x<Cell;x++) if(!i.Pixels[63*Cell+x].Equals(AtlasPixel(before,64,x,63))) throw new InvalidOperationException(i.Id+" top rim mismatch.");
            foreach(Item i in items.Where(i=>i.Segment==2)) for(int x=0;x<Cell;x++) if(!i.Pixels[x].Equals(AtlasPixel(ground,46,x,63))) throw new InvalidOperationException(i.Id+" bottom grass mismatch.");
            foreach(Item i in items) for(int y=1;y<Cell-1;y++){if(i.Width==1||i.Lane==0)if(!i.Pixels[y*Cell].Equals(AtlasPixel(before,64,63,y)))throw new InvalidOperationException(i.Id+" west cliff-bank mismatch.");if(i.Width==1||i.Lane==2)if(!i.Pixels[y*Cell+63].Equals(AtlasPixel(before,64,0,y)))throw new InvalidOperationException(i.Id+" east cliff-bank mismatch.");}
        }

        static void WriteValidationReport()=>File.WriteAllText(Path.Combine(Verification,"modular-stairs-validation.txt"),"PASS: S001-S003 = Narrow Top/Middle/Bottom; S004-S012 = Wide Top/Middle/Bottom x Left/Center/Right.\nPASS: TS02 slots 96-107 only; all other pixels, including ET001-ET047 and C001-C012, preserved exactly. Idempotent reruns accept only owned pixels+manifest+assets.\nPASS: legal wide horizontal, all vertical joins, and west/east C001 cliff-bank contacts match RGBA; no placement overlap.\nPASS: diagnostic scene flanks narrow and wide stairs with C001 on west/east sides of all three rows.\nPASS: top edge equals the existing exposed ET/C rim contact; bottom edge equals G047 grass north contact.\nPASS: opaque stair pixels; saved stair and pre-existing ET/C Tile/Sprite identity, texture, rect, and 64 PPU survive atlas reimport.\nLIMITATION: SOUTH-FACING, three-cell descent only; shadow remains a separate TS04 layer.\n");

        static void CheckHorizontal(Item a,Item b,string label){for(int y=0;y<Cell;y++)if(!a.Pixels[y*Cell+63].Equals(b.Pixels[y*Cell]))throw new InvalidOperationException(label+" mismatch.");}
        static void CheckVertical(Item top,Item bottom,string label){for(int x=0;x<Cell;x++)if(!top.Pixels[x].Equals(bottom.Pixels[63*Cell+x]))throw new InvalidOperationException(label+" mismatch.");}
        static void CompareSlot(Color32[] a,Color32[] b,int slot,string label){Rect r=SlotRect(slot);for(int y=0;y<Cell;y++)for(int x=0;x<Cell;x++){int p=((int)r.y+y)*AtlasSize+(int)r.x+x;if(!a[p].Equals(b[p]))throw new InvalidOperationException(label+" changed slot "+slot);}}
        static void CompareItemSlot(Color32[] atlas,Item item,string label){Rect r=SlotRect(item.Slot);for(int y=0;y<Cell;y++)for(int x=0;x<Cell;x++)if(!atlas[((int)r.y+y)*AtlasSize+(int)r.x+x].Equals(item.Pixels[y*Cell+x]))throw new InvalidDataException(label+" "+item.Slot+" ("+item.Id+").");}
        static bool SlotIsZero(Color32[] p,int slot){Rect r=SlotRect(slot);for(int y=0;y<Cell;y++)for(int x=0;x<Cell;x++)if(!p[((int)r.y+y)*AtlasSize+(int)r.x+x].Equals(new Color32()))return false;return true;}
        static void WriteSlots(Color32[] atlas,List<Item> items){foreach(Item i in items){Rect r=SlotRect(i.Slot);for(int y=0;y<Cell;y++)Array.Copy(i.Pixels,y*Cell,atlas,((int)r.y+y)*AtlasSize+(int)r.x,Cell);}}

        static List<ExistingRef> CaptureExistingRefs()
        {
            var result=new List<ExistingRef>();
            foreach(string id in Enumerable.Range(1,47).Select(n=>"ET"+n.ToString("D3")).Concat(Enumerable.Range(1,12).Select(n=>"C"+n.ToString("D3"))))
            {
                string path=Root+"/Tiles/Elevation/"+id+".asset"; Tile tile=AssetDatabase.LoadAssetAtPath<Tile>(path); Sprite sprite=tile==null?null:tile.sprite;
                if(sprite==null) throw new InvalidDataException("Missing pre-existing elevation Tile/Sprite: "+id);
                string tg,sg; long tl,sl;
                if(!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(tile,out tg,out tl)||!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(sprite,out sg,out sl)) throw new InvalidDataException("Cannot capture persistent identity: "+id);
                result.Add(new ExistingRef{Id=id,Path=path,TileGuid=tg,TileLocalId=tl,SpriteGuid=sg,SpriteLocalId=sl,Rect=sprite.rect});
            }
            return result;
        }

        static void AssertExistingRefs(List<ExistingRef> refs)
        {
            Texture2D atlas=AssetDatabase.LoadAssetAtPath<Texture2D>(Atlas);
            foreach(ExistingRef old in refs)
            {
                Tile tile=AssetDatabase.LoadAssetAtPath<Tile>(old.Path); Sprite sprite=tile==null?null:tile.sprite; string tg,sg; long tl,sl;
                if(sprite==null||!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(tile,out tg,out tl)||!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(sprite,out sg,out sl)||tg!=old.TileGuid||tl!=old.TileLocalId||sg!=old.SpriteGuid||sl!=old.SpriteLocalId||sprite.texture!=atlas||sprite.rect!=old.Rect||sprite.pixelsPerUnit!=Cell)
                    throw new InvalidDataException("Atlas reimport changed pre-existing Tile/Sprite identity or import contract: "+old.Id);
            }
        }

        static void ImportAtlas(){AssetDatabase.ImportAsset(Atlas,ImportAssetOptions.ForceSynchronousImport);var i=(TextureImporter)AssetImporter.GetAtPath(Atlas);i.textureType=TextureImporterType.Sprite;i.spriteImportMode=SpriteImportMode.Single;i.spritePixelsPerUnit=Cell;i.filterMode=FilterMode.Point;i.mipmapEnabled=false;i.textureCompression=TextureImporterCompression.Uncompressed;i.alphaIsTransparency=false;i.isReadable=true;i.wrapMode=TextureWrapMode.Clamp;i.maxTextureSize=AtlasSize;i.SaveAndReimport();}
        static void SaveTiles(List<Item> items)
        {
            Texture2D atlas=AssetDatabase.LoadAssetAtPath<Texture2D>(Atlas); foreach(Item item in items){string path=TileDir+"/"+item.Id+".asset";Tile tile=AssetDatabase.LoadAssetAtPath<Tile>(path);if(tile==null){tile=ScriptableObject.CreateInstance<Tile>();AssetDatabase.CreateAsset(tile,path);}Sprite sprite=AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().FirstOrDefault();Sprite made=Sprite.Create(atlas,SlotRect(item.Slot),new Vector2(.5f,.5f),Cell,0,SpriteMeshType.FullRect);made.name=item.Id;if(sprite==null){sprite=made;AssetDatabase.AddObjectToAsset(sprite,tile);}else{EditorUtility.CopySerialized(made,sprite);UnityEngine.Object.DestroyImmediate(made);}tile.name=item.Id;tile.sprite=sprite;tile.colliderType=Tile.ColliderType.None;tile.color=Color.white;tile.transform=Matrix4x4.identity;EditorUtility.SetDirty(sprite);EditorUtility.SetDirty(tile);item.Tile=tile;}
        }
        static string BuildManifest(List<Item> items)
        {
            var s=new StringBuilder("id,semantic,layer,width_cells,segment,lane,atlas_slot,atlas_x,atlas_y_bottom,asset\n");
            foreach(Item i in items)
            {
                Rect r=SlotRect(i.Slot); string lane=i.Width==1?"Full":LaneName(i.Lane);
                s.Append(i.Id).Append(',').Append(i.Semantic).Append(",StairsSouth,").Append(i.Width).Append(',')
                    .Append(SegmentName(i.Segment)).Append(',').Append(lane).Append(',').Append(i.Slot).Append(',')
                    .Append(r.x).Append(',').Append(r.y).Append(',').Append(TileDir).Append('/').Append(i.Id).AppendLine(".asset");
            }
            return s.ToString();
        }
        static void WriteManifest(List<Item> items)=>File.WriteAllText(Manifest,BuildManifest(items));
        static void ReloadCheck(List<Item> items){Texture2D atlas=AssetDatabase.LoadAssetAtPath<Texture2D>(Atlas);foreach(Item i in items){string path=TileDir+"/"+i.Id+".asset";AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);i.Tile=AssetDatabase.LoadAssetAtPath<Tile>(path);Sprite s=i.Tile==null?null:i.Tile.sprite;if(s==null||s.texture!=atlas||s.rect!=SlotRect(i.Slot)||s.pixelsPerUnit!=Cell)throw new InvalidDataException("Sprite texture/rect/64PPU mismatch: "+i.Id);}}

        static void CheckSceneCreationAllowed(){if(Application.isBatchMode)return;for(int i=0;i<SceneManager.sceneCount;i++){string p=SceneManager.GetSceneAt(i).path;if(string.IsNullOrEmpty(p))throw new InvalidOperationException("Save or close untitled scenes before building ModularStairsTest. Nothing changed.");if(p==ScenePath)throw new InvalidOperationException("Close loaded ModularStairsTest before rebuilding. Nothing changed.");}}
        static void BuildScene(List<Item> items,Color32[] elevation,Color32[] ground)
        {
            Tile cliff=AssetDatabase.LoadAssetAtPath<Tile>(Root+"/Tiles/Elevation/C001.asset"), top=AssetDatabase.LoadAssetAtPath<Tile>(Root+"/Tiles/Elevation/ET001.asset"), grass=AssetDatabase.LoadAssetAtPath<Tile>(Root+"/Tiles/G047.asset");
            if(cliff==null||top==null||grass==null) throw new InvalidDataException("Build ground and elevation Tile assets first.");
            var plan=new List<Placement>(); for(int x=1;x<MapW-1;x++){plan.Add(new Placement{X=x,Y=6,Order=0,Tile=grass,Pixels=Extract(ground,46)});plan.Add(new Placement{X=x,Y=10,Order=2,Tile=top,Pixels=Extract(elevation,0)});if(x!=7&&x!=20&&x!=21&&x!=22)plan.Add(new Placement{X=x,Y=9,Order=1,Tile=cliff,Pixels=Extract(elevation,64)});}
            for(int s=0;s<3;s++)plan.Add(new Placement{X=7,Y=9-s,Order=3,Tile=items[s].Tile,Pixels=items[s].Pixels});
            for(int s=0;s<3;s++)for(int lane=0;lane<3;lane++){Item i=items[3+s*3+lane];plan.Add(new Placement{X=20+lane,Y=9-s,Order=3,Tile=i.Tile,Pixels=i.Pixels});}
            for(int s=1;s<3;s++) foreach(int x in new[]{6,8,19,23}) plan.Add(new Placement{X=x,Y=9-s,Order=1,Tile=cliff,Pixels=Extract(elevation,64)});
            foreach(int y in new[]{9,8,7}) foreach(int x in new[]{6,8,19,23}) if(!plan.Any(p=>p.X==x&&p.Y==y&&p.Order==1&&p.Tile==cliff)) throw new InvalidOperationException("Diagnostic scene must flank narrow and wide stairs on every row.");
            if(plan.GroupBy(p=>(p.X,p.Y,p.Order)).Any(g=>g.Count()>1))throw new InvalidOperationException("Diagnostic plan overlaps within a layer.");
            Scene previous=SceneManager.GetActiveScene(),scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,Application.isBatchMode?NewSceneMode.Single:NewSceneMode.Additive);SceneManager.SetActiveScene(scene);
            try{var grid=new GameObject("Modular64 South Stairs Diagnostic",typeof(Grid));SceneManager.MoveGameObjectToScene(grid,scene);Tilemap[] maps={NewMap("Ground contact",grid.transform,0),NewMap("Cliff run",grid.transform,1),NewMap("Elevated top",grid.transform,2),NewMap("Stairs",grid.transform,3)};foreach(Placement p in plan)maps[p.Order].SetTile(new Vector3Int(p.X,p.Y,0),p.Tile);var go=new GameObject("Diagnostic Camera",typeof(Camera));SceneManager.MoveGameObjectToScene(go,scene);go.transform.position=new Vector3(15,8,-10);var c=go.GetComponent<Camera>();c.orthographic=true;c.orthographicSize=8;c.backgroundColor=new Color(.12f,.15f,.19f);c.clearFlags=CameraClearFlags.SolidColor;Directory.CreateDirectory("Assets/Sapphire/Scenes");if(!EditorSceneManager.SaveScene(scene,ScenePath))throw new IOException("Could not save "+ScenePath);WritePreview(plan);}
            finally{if(!Application.isBatchMode){if(previous.IsValid()&&previous.isLoaded)SceneManager.SetActiveScene(previous);EditorSceneManager.CloseScene(scene,true);}}
        }
        static Tilemap NewMap(string name,Transform parent,int order){var go=new GameObject(name,typeof(Tilemap),typeof(TilemapRenderer));go.transform.SetParent(parent,false);go.GetComponent<TilemapRenderer>().sortingOrder=order;return go.GetComponent<Tilemap>();}
        static Color32[] Extract(Color32[] source,int slot){var p=new Color32[Cell*Cell];Rect r=SlotRect(slot);for(int y=0;y<Cell;y++)Array.Copy(source,((int)r.y+y)*AtlasSize+(int)r.x,p,y*Cell,Cell);return p;}
        static void WritePreview(List<Placement> plan){var pixels=Enumerable.Repeat(new Color32(31,38,48,255),MapW*Cell*MapH*Cell).ToArray();foreach(Placement p in plan.OrderBy(x=>x.Order))for(int y=0;y<Cell;y++)for(int x=0;x<Cell;x++){Color32 s=p.Pixels[y*Cell+x];if(s.a!=0)pixels[((p.Y*Cell+y)*MapW*Cell)+p.X*Cell+x]=s;}WritePng(Path.Combine(Verification,"modular-stairs-test.png"),pixels,MapW*Cell,MapH*Cell);}
        static int PositiveMod(int v,int m){int r=v%m;return r<0?r+m:r;}
        static void WritePng(string path,Color32[] pixels,int w,int h){var t=new Texture2D(w,h,TextureFormat.RGBA32,false);t.SetPixels32(pixels);t.Apply();File.WriteAllBytes(path,t.EncodeToPNG());UnityEngine.Object.DestroyImmediate(t);}
    }
}
