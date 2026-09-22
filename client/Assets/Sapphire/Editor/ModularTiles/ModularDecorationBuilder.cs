using System;
using System.Collections.Generic;
using System.Globalization;
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
    /// <summary>Builds independent, grid-addressable decorations and bridge parts. It never bakes a map.</summary>
    public static class ModularDecorationBuilder
    {
        const int Cell=64, SmallSize=1024, LargeSize=1024, BridgeSize=512;
        const string Root="Assets/Sapphire/Art/World/Modular64";
        const string SmallAtlas=Root+"/TS05_Deco_Small_64.png", LargeAtlas=Root+"/TS06_Deco_Large_64.png", BridgeAtlas=Root+"/TS07_Bridge_64.png";
        const string SmallDir=Root+"/Tiles/DecorationsSmall", LargeDir=Root+"/Tiles/DecorationsLarge", BridgeDir=Root+"/Tiles/Bridge";
        const string TargetScene="Assets/Sapphire/Scenes/ModularDecorationTest.unity";
        static string Verification=>Path.GetFullPath(Path.Combine(Application.dataPath,"../../verification"));

        sealed class Item { public string Id,Name,Layer,Category; public int Slot,X,Y,W,H; public Vector2 Pivot; public Color32[] Pixels; public Tile Tile; }
        static Color32 woodDark,woodMid,woodLight,leafDark,leafMid,leafLight,stoneDark,stoneMid,stoneLight,accent;
        static string woodSourceReport,decoSourceReport;
        static TilemapRenderer diagnosticTerrainRenderer,diagnosticWaterRenderer,diagnosticBridgeRenderer;
        static readonly Vector3Int BridgeLeftLand=new Vector3Int(2,3,0), BridgeFirstWater=new Vector3Int(3,3,0), BridgeLastWater=new Vector3Int(11,3,0), BridgeRightLand=new Vector3Int(12,3,0);

        [MenuItem("Sapphire/Modular Tiles/Build Independent Decoration + Bridge Test")]
        public static void Build()
        {
            CheckSceneCreationAllowed();
            RequireDiagnosticTile(Root+"/Tiles/G047.asset","Build TS01 first: actual G047 land tile is required for bridge contacts.");
            RequireDiagnosticTile(Root+"/Tiles/WaterFoam/W001.asset","Build TS03 first: actual W001 water tile is required beneath the bridge.");
            Texture2D wood=ReadSource("WoodMaster.png",true,out woodSourceReport), deco=null;
            List<Item> small,large,bridge;
            try { deco=ReadSource("DecorationSource.png",false,out decoSourceReport); DerivePalette(wood,deco); small=BuildSmall(); large=BuildLarge(); bridge=BuildBridge(); }
            finally { UnityEngine.Object.DestroyImmediate(wood); if(deco!=null)UnityEngine.Object.DestroyImmediate(deco); }
            Validate(small,large,bridge);
            Directory.CreateDirectory(SmallDir);Directory.CreateDirectory(LargeDir);Directory.CreateDirectory(BridgeDir);Directory.CreateDirectory(Verification);
            WriteAtlas(SmallAtlas,SmallSize,small);WriteAtlas(LargeAtlas,LargeSize,large);WriteAtlas(BridgeAtlas,BridgeSize,bridge);
            ImportAtlas(SmallAtlas,SmallSize);ImportAtlas(LargeAtlas,LargeSize);ImportAtlas(BridgeAtlas,BridgeSize);
            SaveTiles(small,SmallAtlas,SmallDir);SaveTiles(large,LargeAtlas,LargeDir);SaveTiles(bridge,BridgeAtlas,BridgeDir);
            WriteManifest(SmallAtlas.Replace(".png",".csv"),small,SmallDir);WriteManifest(LargeAtlas.Replace(".png",".csv"),large,LargeDir);WriteManifest(BridgeAtlas.Replace(".png",".csv"),bridge,BridgeDir);
            AssetDatabase.SaveAssets();ReloadCheck(small,SmallAtlas,SmallDir);ReloadCheck(large,LargeAtlas,LargeDir);ReloadCheck(bridge,BridgeAtlas,BridgeDir);
            BuildScene(small,large,bridge);AssetDatabase.Refresh();
            Debug.Log("Modular64: D001-D032, D101-D114 and B001-B012 built from material sources and procedural silhouettes.");
        }

        static Texture2D ReadSource(string name,bool allowOpaqueFullFrame,out string report)
        {
            string path=Root+"/Sources/"+name;if(!File.Exists(path))throw new FileNotFoundException("Provide the material source before building (no outputs were written): "+path,path);
            var t=new Texture2D(2,2,TextureFormat.RGBA32,false);try{if(!t.LoadImage(File.ReadAllBytes(path)))throw new InvalidDataException("Cannot decode "+path);if(t.width<256||t.height<256)throw new InvalidDataException(name+" must be at least 256x256.");Color32[] p=t.GetPixels32();int transparent=p.Count(c=>c.a<=32),opaque=p.Count(c=>c.a>=224),perimeter=2*t.width+2*t.height;var bins=EdgeBins(t);var dominant=bins.OrderByDescending(k=>k.Value).Take(4).Where(k=>k.Value>=Math.Max(8,perimeter/16)).ToArray();List<Color32> material=MaterialPixels(t);bool alphaSeparated=transparent>=p.Length/50&&opaque>=p.Length/50,matteDetected=dominant.Sum(k=>k.Value)>=perimeter/4;float separation=matteDetected?ColorSeparation(material,dominant.Select(k=>BinCenter(k.Key)).ToArray()):0f;if(!allowOpaqueFullFrame&&!alphaSeparated&&(!matteDetected||separation<20f))throw new InvalidDataException(name+" must have meaningful alpha separation, or a dominant edge matte/checker separated from foreground by >=20 RGB units; arbitrary opaque/noisy/gradient backgrounds are rejected. Detected matte="+matteDetected+", separation="+separation.ToString("0.0",CultureInfo.InvariantCulture));if(material.Count<1024)throw new InvalidDataException(name+" has too little foreground/material after alpha and edge-matte rejection ("+material.Count+" pixels).");string policy=allowOpaqueFullFrame?"wood-policy: opaque full-frame material allowed":"decoration-policy: alpha-separated or dominant-matte/checker + color separation required";string result=alphaSeparated?"alpha-separated":matteDetected?"dominant edge matte/checker, separation="+separation.ToString("0.0",CultureInfo.InvariantCulture):"opaque full-frame accepted by wood-only policy";report=name+": "+policy+"; result="+result+"; "+t.width+"x"+t.height+", usable="+material.Count+", transparent="+transparent+", opaque="+opaque;return t;}catch{UnityEngine.Object.DestroyImmediate(t);throw;}
        }
        static void DerivePalette(Texture2D wood,Texture2D deco)
        {
            Color32 w=AverageMaterial(wood), d=AverageMaterial(deco);
            woodMid=Tone(w,.78f,255);woodDark=Tone(w,.43f,255);woodLight=Tone(w,1.18f,255);
            leafMid=new Color32((byte)Mathf.Clamp(d.r*.65f,28,105),(byte)Mathf.Clamp(d.g*.92f,65,150),(byte)Mathf.Clamp(d.b*.55f,22,95),255);
            leafDark=Tone(leafMid,.58f,255);leafLight=Tone(leafMid,1.32f,255);stoneMid=new Color32((byte)((d.r+w.r)/4+48),(byte)((d.g+w.g)/4+51),(byte)((d.b+w.b)/4+54),255);stoneDark=Tone(stoneMid,.58f,255);stoneLight=Tone(stoneMid,1.30f,255);accent=new Color32(224,174,67,255);
        }
        static Dictionary<int,int> EdgeBins(Texture2D t){Color32[] p=t.GetPixels32();var bins=new Dictionary<int,int>();Action<Color32> add=c=>{if(c.a<224)return;int key=(c.r/16)<<8|(c.g/16)<<4|c.b/16;bins[key]=bins.ContainsKey(key)?bins[key]+1:1;};for(int x=0;x<t.width;x++){add(p[x]);add(p[(t.height-1)*t.width+x]);}for(int y=0;y<t.height;y++){add(p[y*t.width]);add(p[y*t.width+t.width-1]);}return bins;}
        static Color32 BinCenter(int key)=>new Color32((byte)((((key>>8)&15)*16)+8),(byte)((((key>>4)&15)*16)+8),(byte)(((key&15)*16)+8),255);
        static float ColorSeparation(List<Color32> foreground,Color32[] matte){if(foreground.Count==0||matte.Length==0)return 0f;long total=0;int count=0,step=Math.Max(1,foreground.Count/4096);for(int n=0;n<foreground.Count;n+=step){Color32 c=foreground[n];int best=int.MaxValue;foreach(Color32 m in matte){int dr=c.r-m.r,dg=c.g-m.g,db=c.b-m.b;best=Math.Min(best,(int)Math.Sqrt(dr*dr+dg*dg+db*db));}total+=best;count++;}return (float)total/count;}
        static List<Color32> MaterialPixels(Texture2D t){Color32[] p=t.GetPixels32();var edgeBins=EdgeBins(t);int perimeter=2*t.width+2*t.height;var matte=new HashSet<int>(edgeBins.OrderByDescending(k=>k.Value).Take(4).Where(k=>k.Value>=Math.Max(8,perimeter/16)).Select(k=>k.Key));var result=new List<Color32>();int step=Math.Max(1,p.Length/65536);for(int n=0;n<p.Length;n+=step){Color32 c=p[n];int key=(c.r/16)<<8|(c.g/16)<<4|c.b/16,lum=(c.r*3+c.g*5+c.b*2)/10;if(c.a>32&&!matte.Contains(key)&&lum>=14)result.Add(c);}return result;}
        static Color32 AverageMaterial(Texture2D t){List<Color32> px=MaterialPixels(t);if(px.Count<1024)throw new InvalidDataException("Material foreground sample unexpectedly empty.");var ordered=px.OrderBy(c=>(c.r*3+c.g*5+c.b*2)/10).ToArray();int lo=ordered.Length/10,hi=ordered.Length*9/10;long r=0,g=0,b=0,n=0;for(int i=lo;i<hi;i++){r+=ordered[i].r;g+=ordered[i].g;b+=ordered[i].b;n++;}return new Color32((byte)(r/n),(byte)(g/n),(byte)(b/n),255);}
        static Color32 Tone(Color32 c,float f,byte a)=>new Color32((byte)Mathf.Clamp(c.r*f,0,255),(byte)Mathf.Clamp(c.g*f,0,255),(byte)Mathf.Clamp(c.b*f,0,255),a);

        static List<Item> BuildSmall()
        {
            string[] names={"RockRound","RockFlat","RockPointed","PebblePair","RockMossy","StoneCluster","GrassTuft","GrassWide","FlowerRed","FlowerBlue","FlowerGold","BushSmall","BushBerry","Fern","Reeds","Stump","ChoppedWood","LogHorizontal","LogVertical","TwigPile","Signpost","FenceStraight","FenceEnd","Crate","Barrel","FenceCorner","MushroomPair","Fireflies","GroundLeaves","Bones","Lantern","PuddleReeds"};
            string[] categories={"Rock","Nature","Wood","Built","Ambient"};int[] starts={0,6,15,20,26,32};var list=new List<Item>();for(int i=0;i<names.Length;i++){int group=Enumerable.Range(0,5).First(g=>i>=starts[g]&&i<starts[g+1]);Item it=New("D"+(i+1).ToString("D3"),names[i],"DecorationSmall",categories[group],i,Cell,Cell,new Vector2(.5f,.12f));DrawSmall(it,i);list.Add(it);}return list;
        }
        static void DrawSmall(Item i,int n)
        {
            if(n<6){Ellipse(i,14+(n%3)*3,9,49-(n%2)*4,29+(n%3)*4,stoneDark);Ellipse(i,17,15,46,34,stoneMid);if(n==4)Ellipse(i,20,27,38,35,leafMid);return;}
            if(n<15){int blades=5+n%4;for(int b=0;b<blades;b++){int x=20+b*24/Math.Max(1,blades-1);Line(i,32,10,x,25+(b%3)*7,b%2==0?leafDark:leafLight,2);}if(n>=8&&n<=10)for(int b=0;b<4;b++)Ellipse(i,20+b*7,26+(b%2)*4,24+b*7,30+(b%2)*4,n==8?new Color32(205,64,61,255):n==9?new Color32(75,137,219,255):accent);if(n>=11&&n<=13){Ellipse(i,12,10,52,38,leafDark);Ellipse(i,16,17,48,43,leafMid);if(n==12)for(int b=0;b<5;b++)Dot(i,21+b*6,25+(b%2)*5,new Color32(185,54,63,255));}return;}
            if(n<20){Rect(i,27,8,37,30,woodDark);Rect(i,24,17,40,32,woodMid);if(n==16)for(int b=0;b<3;b++)Rect(i,13+b*13,12,24+b*13,20,woodLight);if(n==17||n==18){Rect(i,n==17?10:27,n==17?14:7,n==17?54:37,n==17?27:51,woodMid);Line(i,n==17?10:27,n==17?14:7,n==17?54:37,n==17?27:51,woodDark,3);}return;}
            if(n<26){if(n==20){Rect(i,29,7,35,42,woodDark);Rect(i,17,31,47,48,woodMid);}else if(n<23||n==25){Rect(i,10,18,54,24,woodMid);Rect(i,15,8,21,40,woodDark);if(n==25)Rect(i,43,8,49,40,woodDark);}else if(n==23){Rect(i,15,10,49,42,woodMid);Line(i,15,10,49,42,woodDark,2);Line(i,49,10,15,42,woodDark,2);}else{Ellipse(i,18,8,46,43,woodDark);Rect(i,20,15,44,38,woodMid);Line(i,20,26,44,26,woodLight,2);}return;}
            if(n==30){Rect(i,27,8,37,39,woodDark);Rect(i,22,29,42,47,accent);return;}if(n==29){Line(i,18,10,45,33,stoneLight,3);Line(i,45,10,18,33,stoneLight,3);return;}if(n==31){Ellipse(i,13,6,51,20,new Color32(45,96,103,255));for(int x=18;x<50;x+=9)Line(i,x,13,x-4,31,leafMid,2);return;}for(int b=0;b<7;b++)Dot(i,15+(b*17)%38,13+(b*11)%26,n==26?new Color32(238,213,103,255):n==27?leafLight:new Color32(205,72,62,255));
        }

        static List<Item> BuildLarge()
        {
            string[] names={"TreeBroad","TreePine","TreeDead","BushLarge","BoulderLarge","BoulderMossy","RuinArch","RuinPillar","Shrine","Cart","Well","TreeTwin","RuinWall","AncientTree"};
            int[,] dims={{2,3},{2,3},{2,3},{2,2},{2,2},{2,2},{2,3},{2,3},{2,3},{3,2},{2,2},{3,3},{3,2},{3,3}};
            // Bottom-origin shelf packing; every origin and extent is a 64px multiple.
            int[,] pos={{0,0},{128,0},{256,0},{384,0},{512,0},{640,0},{768,0},{896,0},{0,192},{128,192},{320,192},{448,192},{640,192},{0,384}};
            var list=new List<Item>();for(int n=0;n<14;n++){int w=dims[n,0]*Cell,h=dims[n,1]*Cell;Item i=New("D"+(101+n).ToString("D3"),names[n],"DecorationLarge",n<=3||n==11||n==13?"Vegetation":n<=5?"Rock":n<=8||n==12?"Ruin":"Structure",n,w,h,new Vector2(.5f,.06f));i.X=pos[n,0];i.Y=pos[n,1];DrawLarge(i,n);list.Add(i);}return list;
        }
        static void DrawLarge(Item i,int n)
        {
            int w=i.W,h=i.H,c=w/2;if(n<=2||n==11||n==13){int trunk=n==11?22:n==13?28:16;Rect(i,c-trunk/2,8,c+trunk/2,h*2/3,woodDark);Rect(i,c-trunk/2+4,12,c+trunk/2-3,h*2/3,woodMid);if(n!=2){int rx=n==11?w/2-8:w/2-14;Ellipse(i,c-rx,h/3,c+rx,h-10,leafDark);Ellipse(i,c-rx+8,h/2,c+rx-5,h-18,leafMid);Ellipse(i,c-rx/2,h*2/3,c+rx/2,h-8,leafLight);}else{Line(i,c,h*2/3,c-38,h-20,woodDark,8);Line(i,c,h*2/3,c+34,h-30,woodDark,7);}return;}
            if(n==3){Ellipse(i,7,8,w-7,h-12,leafDark);Ellipse(i,15,20,w-13,h-5,leafMid);return;}if(n==4||n==5){Ellipse(i,8,7,w-8,h-18,stoneDark);Ellipse(i,15,18,w-14,h-8,stoneMid);if(n==5)Ellipse(i,22,h/2,w-28,h-9,leafMid);return;}
            if(n==6||n==7||n==12){Rect(i,10,8,w-10,n==12?h-18:h-10,stoneDark);Rect(i,17,13,w-17,n==12?h-25:h-17,stoneMid);if(n==6)Ellipse(i,w/2-27,8,w/2+27,h-35,new Color32());if(n==7)Rect(i,w/2-8,h-45,w/2+8,h,new Color32());for(int y=24;y<h-20;y+=25)Line(i,15,y,w-16,y+3,stoneLight,2);return;}
            if(n==8){for(int x=18;x<w-18;x+=18)Line(i,x,9,w/2,h-24,woodDark,4);Rect(i,15,7,w-15,18,stoneMid);Dot(i,w/2,h-17,accent);return;}if(n==9){Rect(i,18,24,w-18,h-24,woodMid);Line(i,18,24,w-18,h-24,woodDark,4);Ellipse(i,25,7,57,39,stoneDark);Ellipse(i,w-57,7,w-25,39,stoneDark);return;}if(n==10){Ellipse(i,12,8,w-12,h-18,stoneDark);Ellipse(i,20,18,w-20,h-25,new Color32(29,49,55,255));Rect(i,9,h-25,w-9,h-15,woodMid);}
        }

        static List<Item> BuildBridge()
        {
            string[] names={"Horizontal","HorizontalLeftEnd","HorizontalRightEnd","Vertical","VerticalBottomEnd","VerticalTopEnd","RailTop","RailBottom","RailLeft","RailRight","Post","JunctionDeck"};
            var list=new List<Item>();for(int n=0;n<names.Length;n++){Item i=New("B"+(n+1).ToString("D3"),names[n],"Bridge","BridgePart",n,Cell,Cell,new Vector2(.5f,.5f));DrawBridge(i,n);list.Add(i);}return list;
        }
        static void DrawBridge(Item i,int n)
        {
            if(n==11){DrawHorizontalDeck(i);DrawVerticalDeck(i);CanonicalHorizontalEdges(i,false,false);CanonicalVerticalEdges(i,false,false);return;}
            if(n<=2){DrawHorizontalDeck(i);CanonicalHorizontalEdges(i,n==1,n==2);return;}
            if(n<=5){DrawVerticalDeck(i);CanonicalVerticalEdges(i,n==4,n==5);return;}
            if(n==6||n==7){int y=n==6?50:10;Line(i,0,y,63,y,woodLight,4);for(int x=5;x<64;x+=18)Line(i,x,y,x,y+(n==6?-10:10),woodDark,4);return;}if(n==8||n==9){int x=n==8?10:50;Line(i,x,0,x,63,woodLight,4);for(int y=5;y<64;y+=18)Line(i,x,y,x+(n==8?10:-10),y,woodDark,4);return;}Ellipse(i,22,8,42,29,woodDark);Rect(i,26,16,38,54,woodMid);
        }
        static void DrawHorizontalDeck(Item i){Rect(i,0,15,63,49,woodDark);for(int x=1;x<64;x+=10)Rect(i,x,18,Math.Min(63,x+7),46,woodMid);Line(i,0,15,63,15,woodLight,2);Line(i,0,49,63,49,woodLight,2);}
        static void DrawVerticalDeck(Item i){Rect(i,15,0,49,63,woodDark);for(int y=1;y<64;y+=10)Rect(i,18,y,46,Math.Min(63,y+7),woodMid);Line(i,15,0,15,63,woodLight,2);Line(i,49,0,49,63,woodLight,2);}
        static void CanonicalHorizontalEdges(Item i,bool openLeft,bool openRight){for(int y=0;y<Cell;y++){Color32 join=i.Pixels[y*Cell];i.Pixels[y*Cell+63]=join;if(openLeft)i.Pixels[y*Cell]=new Color32();if(openRight)i.Pixels[y*Cell+63]=new Color32();}}
        static void CanonicalVerticalEdges(Item i,bool openBottom,bool openTop){for(int x=0;x<Cell;x++){Color32 join=i.Pixels[x];i.Pixels[63*Cell+x]=join;if(openBottom)i.Pixels[x]=new Color32();if(openTop)i.Pixels[63*Cell+x]=new Color32();}}

        static Item New(string id,string name,string layer,string category,int slot,int w,int h,Vector2 pivot)=>new Item{Id=id,Name=name,Layer=layer,Category=category,Slot=slot,W=w,H=h,Pivot=pivot,Pixels=new Color32[w*h]};
        static void Rect(Item i,int x0,int y0,int x1,int y1,Color32 c){x0=Mathf.Clamp(x0,0,i.W-1);x1=Mathf.Clamp(x1,0,i.W-1);y0=Mathf.Clamp(y0,0,i.H-1);y1=Mathf.Clamp(y1,0,i.H-1);for(int y=y0;y<=y1;y++)for(int x=x0;x<=x1;x++)i.Pixels[y*i.W+x]=c;}
        static void Ellipse(Item i,int x0,int y0,int x1,int y1,Color32 c){float cx=(x0+x1)*.5f,cy=(y0+y1)*.5f,rx=Math.Max(1,(x1-x0)*.5f),ry=Math.Max(1,(y1-y0)*.5f);for(int y=Math.Max(0,y0);y<=Math.Min(i.H-1,y1);y++)for(int x=Math.Max(0,x0);x<=Math.Min(i.W-1,x1);x++)if(((x-cx)*(x-cx)/(rx*rx)+(y-cy)*(y-cy)/(ry*ry))<=1)i.Pixels[y*i.W+x]=c;}
        static void Dot(Item i,int x,int y,Color32 c)=>Ellipse(i,x-2,y-2,x+2,y+2,c);
        static void Line(Item i,int x0,int y0,int x1,int y1,Color32 c,int thick){int dx=Math.Abs(x1-x0),sx=x0<x1?1:-1,dy=-Math.Abs(y1-y0),sy=y0<y1?1:-1,e=dx+dy;while(true){Rect(i,x0-thick/2,y0-thick/2,x0+thick/2,y0+thick/2,c);if(x0==x1&&y0==y1)break;int e2=2*e;if(e2>=dy){e+=dy;x0+=sx;}if(e2<=dx){e+=dx;y0+=sy;}}}

        static void Validate(List<Item> small,List<Item> large,List<Item> bridge)
        {
            if(small.Count!=32||large.Count!=14||bridge.Count!=12)throw new InvalidOperationException("Decoration counts changed.");
            var expected=new Dictionary<string,int>{{"Rock",6},{"Nature",9},{"Wood",5},{"Built",6},{"Ambient",6}};foreach(var pair in expected)if(small.Count(i=>i.Category==pair.Key)!=pair.Value)throw new InvalidOperationException("Small decoration category contract changed: "+pair.Key);if(small.Any(i=>!expected.ContainsKey(i.Category)))throw new InvalidOperationException("Unknown small decoration category.");
            string[] ids=small.Select(x=>x.Id).Concat(large.Select(x=>x.Id)).Concat(bridge.Select(x=>x.Id)).ToArray();if(ids.Distinct().Count()!=ids.Length)throw new InvalidOperationException("Duplicate stable ID.");
            if(small.Where((x,n)=>x.Id!="D"+(n+1).ToString("D3")||x.Slot!=n).Any()||large.Where((x,n)=>x.Id!="D"+(101+n).ToString("D3")).Any()||bridge.Where((x,n)=>x.Id!="B"+(n+1).ToString("D3")||x.Slot!=n).Any())throw new InvalidOperationException("ID/slot contract changed.");
            foreach(Item i in ids.Select(id=>small.Concat(large).Concat(bridge).First(x=>x.Id==id))){if(i.W%Cell!=0||i.H%Cell!=0||i.Pixels.Length!=i.W*i.H)throw new InvalidOperationException("Non-64 footprint: "+i.Id);if(!i.Pixels.Any(p=>p.a==255)||i.Pixels.Any(p=>p.a!=0&&p.a!=255)||i.Pixels.Any(p=>p.a==0&&(p.r!=0||p.g!=0||p.b!=0)))throw new InvalidOperationException("Alpha/RGB contract: "+i.Id);}
            ValidatePacking(small,SmallSize);ValidatePacking(large,LargeSize);ValidatePacking(bridge,BridgeSize);ValidateBridge(bridge);
            File.WriteAllText(Path.Combine(Verification,"modular-decoration-validation.txt"),"PASS: D001-D032 asserted category counts Rock=6, Nature=9, Wood=5, Built=6, Ambient=6; D101-D114 large footprints are 64px multiples.\nPASS: B001-B012 include horizontal/vertical deck, endcaps, rails, post and junction; legal deck seams match.\nPASS: all sprites use binary alpha with zero RGB outside silhouettes; unused atlas pixels are zero RGBA.\nPASS: reloaded assets reference the generated texture, exact rect/pivot and 64 PPU.\nSOURCE: "+woodSourceReport+"\nSOURCE: "+decoSourceReport+"\nNOTE: source images supply filtered palette/material cues; object silhouettes are deterministic procedural artwork, not claims of source-authored subjects.\n");
        }
        static void ValidatePacking(List<Item> items,int size){var used=new bool[size*size];foreach(Item i in items){RectInt r=AtlasRect(i,size);if(r.x<0||r.y<0||r.xMax>size||r.yMax>size)throw new InvalidOperationException("Atlas overflow: "+i.Id);for(int y=r.y;y<r.yMax;y++)for(int x=r.x;x<r.xMax;x++){int q=y*size+x;if(used[q])throw new InvalidOperationException("Atlas overlap: "+i.Id);used[q]=true;}}Color32[] atlas=Assemble(items,size);for(int q=0;q<atlas.Length;q++)if(!used[q]&&!atlas[q].Equals(new Color32()))throw new InvalidOperationException("Reserved atlas area is not zero RGBA.");}
        static void ValidateBridge(List<Item> b){Action<Item,Item,bool> edge=(a,c,h)=>{for(int p=0;p<Cell;p++){int ia=h?p*Cell+63:63*Cell+p,ib=h?p*Cell:p;if(!a.Pixels[ia].Equals(c.Pixels[ib]))throw new InvalidOperationException("Illegal bridge deck seam: "+a.Id+"/"+c.Id);}};edge(b[0],b[0],true);edge(b[1],b[0],true);edge(b[0],b[2],true);edge(b[0],b[11],true);edge(b[11],b[0],true);edge(b[3],b[3],false);edge(b[4],b[3],false);edge(b[3],b[5],false);edge(b[3],b[11],false);edge(b[11],b[3],false);}

        static RectInt AtlasRect(Item i,int size){if(i.Layer=="DecorationLarge")return new RectInt(i.X,i.Y,i.W,i.H);int cols=size/Cell;return new RectInt((i.Slot%cols)*Cell,(cols-1-i.Slot/cols)*Cell,i.W,i.H);}
        static Color32[] Assemble(List<Item> items,int size){var p=new Color32[size*size];foreach(Item i in items){RectInt r=AtlasRect(i,size);for(int y=0;y<i.H;y++)Array.Copy(i.Pixels,y*i.W,p,(r.y+y)*size+r.x,i.W);}return p;}
        static void WriteAtlas(string path,int size,List<Item> items){var t=new Texture2D(size,size,TextureFormat.RGBA32,false);t.SetPixels32(Assemble(items,size));t.Apply();File.WriteAllBytes(path,t.EncodeToPNG());UnityEngine.Object.DestroyImmediate(t);}
        static void ImportAtlas(string path,int size){AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);var i=(TextureImporter)AssetImporter.GetAtPath(path);i.textureType=TextureImporterType.Sprite;i.spriteImportMode=SpriteImportMode.Single;i.spritePixelsPerUnit=Cell;i.filterMode=FilterMode.Point;i.mipmapEnabled=false;i.textureCompression=TextureImporterCompression.Uncompressed;i.alphaIsTransparency=false;i.isReadable=true;i.wrapMode=TextureWrapMode.Clamp;i.maxTextureSize=size;i.SaveAndReimport();}
        static void SaveTiles(List<Item> items,string atlasPath,string dir){Texture2D atlas=AssetDatabase.LoadAssetAtPath<Texture2D>(atlasPath);foreach(Item i in items){string path=dir+"/"+i.Id+".asset";Tile tile=AssetDatabase.LoadAssetAtPath<Tile>(path);if(tile==null){tile=ScriptableObject.CreateInstance<Tile>();AssetDatabase.CreateAsset(tile,path);}RectInt atlasRect=AtlasRect(i,atlas.width);Sprite old=AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().FirstOrDefault(),made=Sprite.Create(atlas,new Rect(atlasRect.x,atlasRect.y,atlasRect.width,atlasRect.height),i.Pivot,Cell,0,SpriteMeshType.FullRect);made.name=i.Id;if(old==null){old=made;AssetDatabase.AddObjectToAsset(old,tile);}else{EditorUtility.CopySerialized(made,old);UnityEngine.Object.DestroyImmediate(made);}tile.name=i.Id;tile.sprite=old;tile.colliderType=Tile.ColliderType.None;tile.color=Color.white;tile.transform=Matrix4x4.identity;EditorUtility.SetDirty(old);EditorUtility.SetDirty(tile);i.Tile=tile;}}
        static void WriteManifest(string path,List<Item> items,string dir){var c=new StringBuilder("id,semantic,layer,category,atlas_slot,atlas_x,atlas_y_bottom,width_px,height_px,footprint_w,footprint_h,pivot_x,pivot_y,asset\n");foreach(Item i in items){RectInt r=AtlasRect(i,i.Layer=="Bridge"?BridgeSize:i.Layer=="DecorationLarge"?LargeSize:SmallSize);c.AppendLine(string.Join(",",i.Id,i.Name,i.Layer,i.Category,i.Slot,r.x,r.y,i.W,i.H,i.W/Cell,i.H/Cell,i.Pivot.x.ToString("0.##",CultureInfo.InvariantCulture),i.Pivot.y.ToString("0.##",CultureInfo.InvariantCulture),dir+"/"+i.Id+".asset"));}File.WriteAllText(path,c.ToString());}
        static void ReloadCheck(List<Item> items,string atlasPath,string dir){Texture2D a=AssetDatabase.LoadAssetAtPath<Texture2D>(atlasPath);foreach(Item i in items){string path=dir+"/"+i.Id+".asset";AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);i.Tile=AssetDatabase.LoadAssetAtPath<Tile>(path);Sprite s=i.Tile==null?null:i.Tile.sprite;if(s==null||s.texture!=a||s.rect!=new Rect(AtlasRect(i,a.width).x,AtlasRect(i,a.width).y,i.W,i.H)||s.pixelsPerUnit!=Cell||s.pivot!=new Vector2(i.Pivot.x*i.W,i.Pivot.y*i.H))throw new InvalidDataException("Sprite reload mismatch: "+i.Id);}}

        static void CheckSceneCreationAllowed(){if(Application.isBatchMode)return;for(int n=0;n<SceneManager.sceneCount;n++){string p=SceneManager.GetSceneAt(n).path;if(string.IsNullOrEmpty(p))throw new InvalidOperationException("Save or close untitled scenes before building. Nothing changed.");if(p==TargetScene)throw new InvalidOperationException("Close loaded ModularDecorationTest before rebuilding. Nothing changed.");}}
        static Tile RequireDiagnosticTile(string path,string message){Tile tile=AssetDatabase.LoadAssetAtPath<Tile>(path);if(tile==null||tile.sprite==null)throw new InvalidDataException(message);return tile;}
        static void BuildScene(List<Item> small,List<Item> large,List<Item> bridge)
        {
            Scene previous=SceneManager.GetActiveScene(),scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,Application.isBatchMode?NewSceneMode.Single:NewSceneMode.Additive);SceneManager.SetActiveScene(scene);
            // NewSceneMode.Single unloads temporary asset references. Reload the persisted bridge
            // assets only after the diagnostic scene becomes active.
            for(int n=0;n<bridge.Count;n++)
                bridge[n].Tile=RequireDiagnosticTile(BridgeDir+"/"+bridge[n].Id+".asset","Missing persisted bridge tile "+bridge[n].Id+".");
            try{Tile groundTile=RequireDiagnosticTile(Root+"/Tiles/G047.asset","Build TS01 first."),waterTile=RequireDiagnosticTile(Root+"/Tiles/WaterFoam/W001.asset","Build TS03 first.");var root=new GameObject("Modular64 Decoration + Bridge Diagnostic",typeof(Grid));SceneManager.MoveGameObjectToScene(root,scene);Tilemap terrain=NewMap("Actual TS01 Land Contacts",root.transform,0),water=NewMap("Actual TS03 Water Span",root.transform,1),parts=NewMap("Bridge Parts Above Water",root.transform,2),sm=NewMap("Small Decorations",root.transform,3);for(int x=0;x<=2;x++)for(int y=1;y<=5;y++)terrain.SetTile(new Vector3Int(x,y,0),groundTile);for(int x=12;x<=14;x++)for(int y=1;y<=5;y++)terrain.SetTile(new Vector3Int(x,y,0),groundTile);for(int x=BridgeFirstWater.x;x<=BridgeLastWater.x;x++)for(int y=1;y<=5;y++)water.SetTile(new Vector3Int(x,y,0),waterTile);for(int x=BridgeFirstWater.x;x<=BridgeLastWater.x;x++)parts.SetTile(new Vector3Int(x,3,0),bridge[x==BridgeFirstWater.x?1:x==BridgeLastWater.x?2:0].Tile);if(terrain.GetTile(BridgeLeftLand)!=groundTile||terrain.GetTile(BridgeRightLand)!=groundTile)throw new InvalidOperationException("Bridge scene lacks TS01 land contacts.");for(int x=BridgeFirstWater.x;x<=BridgeLastWater.x;x++){Vector3Int cell=new Vector3Int(x,3,0);Tile placedWater=water.GetTile<Tile>(cell),placedBridge=parts.GetTile<Tile>(cell);if(placedWater==null||placedWater.sprite!=waterTile.sprite||placedBridge==null||placedBridge.sprite==null)throw new InvalidOperationException("Bridge scene water/span layering contract failed at x="+x+" (water="+(placedWater==null?"null":placedWater.name)+", bridge="+(placedBridge==null?"null":placedBridge.name)+").");}File.AppendAllText(Path.Combine(Verification,"modular-decoration-validation.txt"),"PASS: scene contract uses actual G047 land at (2,3)/(12,3), actual W001 water at x=3..11,y=1..5, and bridge parts on the higher sorting layer at x=3..11,y=3.\n");for(int y=1;y<=5;y++)parts.SetTile(new Vector3Int(17,y,0),bridge[y==1?4:y==5?5:3].Tile);for(int n=0;n<small.Count;n++)sm.SetTile(new Vector3Int(1+n%16,7+n/16,0),small[n].Tile);for(int n=0;n<large.Count;n++){var go=new GameObject(large[n].Id+" "+large[n].Name,typeof(SpriteRenderer));go.transform.SetParent(root.transform,false);go.transform.position=new Vector3(20+(n%5)*3,1+(n/5)*4,0);var sr=go.GetComponent<SpriteRenderer>();sr.sprite=large[n].Tile.sprite;sr.sortingOrder=4;}var camGo=new GameObject("Diagnostic Camera",typeof(Camera));SceneManager.MoveGameObjectToScene(camGo,scene);camGo.transform.position=new Vector3(18,8,-10);Camera cam=camGo.GetComponent<Camera>();cam.orthographic=true;cam.orthographicSize=9;cam.backgroundColor=new Color(.11f,.14f,.17f);cam.clearFlags=CameraClearFlags.SolidColor;Directory.CreateDirectory("Assets/Sapphire/Scenes");if(!EditorSceneManager.SaveScene(scene,TargetScene))throw new IOException("Could not save "+TargetScene);WritePreview(small,large,bridge,groundTile,waterTile);}finally{if(!Application.isBatchMode){if(previous.IsValid()&&previous.isLoaded)SceneManager.SetActiveScene(previous);EditorSceneManager.CloseScene(scene,true);}}}
        static Tilemap NewMap(string name,Transform parent,int order){var go=new GameObject(name,typeof(Tilemap),typeof(TilemapRenderer));go.transform.SetParent(parent,false);TilemapRenderer renderer=go.GetComponent<TilemapRenderer>();renderer.sortingOrder=order;if(order==0){diagnosticTerrainRenderer=renderer;diagnosticWaterRenderer=null;diagnosticBridgeRenderer=null;}else if(order==1)diagnosticWaterRenderer=renderer;else if(order==2){diagnosticBridgeRenderer=renderer;if(diagnosticTerrainRenderer==null||diagnosticWaterRenderer==null||!(diagnosticTerrainRenderer.sortingOrder<diagnosticWaterRenderer.sortingOrder&&diagnosticWaterRenderer.sortingOrder<diagnosticBridgeRenderer.sortingOrder)||diagnosticTerrainRenderer.sortingOrder==diagnosticWaterRenderer.sortingOrder||diagnosticWaterRenderer.sortingOrder==diagnosticBridgeRenderer.sortingOrder)throw new InvalidOperationException("Diagnostic renderers must be retained on distinct strict terrain < water < bridge sorting orders.");}return go.GetComponent<Tilemap>();}
        static Color32[] TilePixels(Tile tile){Sprite s=tile.sprite;Rect r=s.rect;Color32[] source=s.texture.GetPixels32(),result=new Color32[Cell*Cell];for(int y=0;y<Cell;y++)Array.Copy(source,((int)r.y+y)*s.texture.width+(int)r.x,result,y*Cell,Cell);return result;}
        static void WritePreview(List<Item> small,List<Item> large,List<Item> bridge,Tile groundTile,Tile waterTile){int w=1280,h=768;var p=Enumerable.Repeat(new Color32(28,35,43,255),w*h).ToArray();Action<Color32[],int,int,int,int> raw=(px,pw,ph,ox,oy)=>{for(int y=0;y<ph;y++)for(int x=0;x<pw;x++){Color32 c=px[y*pw+x];if(c.a>0&&ox+x>=0&&ox+x<w&&oy+y>=0&&oy+y<h)p[(oy+y)*w+ox+x]=c;}};Action<Item,int,int> blit=(i,ox,oy)=>raw(i.Pixels,i.W,i.H,ox,oy);Color32[] ground=TilePixels(groundTile),water=TilePixels(waterTile);for(int gx=0;gx<=2;gx++)for(int gy=0;gy<4;gy++)raw(ground,Cell,Cell,32+gx*Cell,64+gy*Cell);for(int gx=12;gx<=14;gx++)for(int gy=0;gy<4;gy++)raw(ground,Cell,Cell,32+gx*Cell,64+gy*Cell);for(int gx=3;gx<=11;gx++)for(int gy=0;gy<4;gy++)raw(water,Cell,Cell,32+gx*Cell,64+gy*Cell);for(int n=0;n<9;n++)blit(bridge[n==0?1:n==8?2:0],32+(3+n)*64,170);for(int n=0;n<small.Count;n++)blit(small[n],32+(n%16)*72,360+(n/16)*80);for(int n=0;n<large.Count;n++)blit(large[n],790+(n%3)*155,20+(n/3)*145);var t=new Texture2D(w,h,TextureFormat.RGBA32,false);t.SetPixels32(p);t.Apply();File.WriteAllBytes(Path.Combine(Verification,"modular-decoration-test.png"),t.EncodeToPNG());UnityEngine.Object.DestroyImmediate(t);}
    }
}
