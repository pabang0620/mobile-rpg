using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEngine;
namespace Sapphire.EditorTools.ModularTiles{
internal static class ApprovedEnvironmentV2{
 internal enum Material{Grass,Dirt,Water,StoneTop,Cliff,Shadow}
 const string MP="Assets/Sapphire/Art/World/Modular64/Sources/EnvironmentMaterialsV2.png",OP="Assets/Sapphire/Art/World/Modular64/Sources/EnvironmentObjectsV2.png";
 const string MS="06B9C090DC890348C52E89626B066C998EC176156784904ADB82AA015CB88381",OS="5700CE76AB5D55DE0C2B3C4A83FE201A0554B9C093A69D0B9721A3D65CF34FF4";
 static byte[] Bytes(string p,string expected){if(!File.Exists(p))throw new FileNotFoundException("Approved V2 source is required.",p);byte[] b=File.ReadAllBytes(p);using(var h=SHA256.Create()){string a=string.Concat(h.ComputeHash(b).Select(x=>x.ToString("X2")));if(a!=expected)throw new InvalidDataException("Approved V2 source hash mismatch: "+p);}return b;}
 internal static Texture2D Load(Material m){var s=new Texture2D(2,2,TextureFormat.RGBA32,false);if(!s.LoadImage(Bytes(MP,MS))||s.width!=1536||s.height!=1024){Object.DestroyImmediate(s);throw new InvalidDataException("EnvironmentMaterialsV2.png must be 1536x1024.");}int n=(int)m,x=n%3*512,y=n<3?512:0;Color32[] a=s.GetPixels32(),r=new Color32[512*512];for(int row=0;row<512;row++)System.Array.Copy(a,(y+row)*s.width+x,r,row*512,512);Object.DestroyImmediate(s);if(r.Count(c=>c.a>224)<4096)throw new InvalidDataException("Empty V2 material region: "+m);var t=new Texture2D(512,512,TextureFormat.RGBA32,false);t.SetPixels32(r);t.Apply();return t;}
 internal static Color32[] Palette(Material m,int count){Texture2D t=Load(m);try{var p=t.GetPixels32().Where(c=>c.a>224).OrderBy(c=>(c.r*3+c.g*5+c.b*2)/10).ToArray();var r=new Color32[count];for(int i=0;i<count;i++){r[i]=p[(i+1)*p.Length/(count+1)];r[i].a=255;}return r;}finally{Object.DestroyImmediate(t);}}
 internal static Color32[] ObjectPixels(Vector2Int topAnchor,int w,int h){var s=new Texture2D(2,2,TextureFormat.RGBA32,false);if(!s.LoadImage(Bytes(OP,OS))||s.width!=1536||s.height!=1024){Object.DestroyImmediate(s);throw new InvalidDataException("EnvironmentObjectsV2.png must be 1536x1024.");}Color32[] all=s.GetPixels32();int seed=Nearest(all,s.width,s.height,topAnchor.x,s.height-1-topAnchor.y);bool[] keep=Flood(all,s.width,s.height,seed);RectInt bounds=ValidateComponent(keep,s.width,s.height,topAnchor);int margin=2,sw=bounds.width,sh=bounds.height;float scale=Mathf.Min((w-margin*2)/(float)sw,(h-margin*2)/(float)sh);int dw=Mathf.Max(1,Mathf.RoundToInt(sw*scale)),dh=Mathf.Max(1,Mathf.RoundToInt(sh*scale)),ox=(w-dw)/2,oy=margin;var output=new Color32[w*h];for(int y=0;y<dh;y++)for(int x=0;x<dw;x++){int sx=bounds.x+Mathf.Min(sw-1,x*sw/dw),sy=bounds.y+Mathf.Min(sh-1,y*sh/dh),p=sy*s.width+sx;if(!keep[p])continue;Color32 v=all[p];v.a=255;output[(oy+y)*w+ox+x]=v;}Object.DestroyImmediate(s);return output;}
 // Source rectangles use top-origin image coordinates; output is Unity bottom-origin.
 internal static Color32[] ObjectStrip(Vector2Int anchor,RectInt topRect,int width,int height,int sidePadding=4){
  var s=new Texture2D(2,2,TextureFormat.RGBA32,false);
  try{
   if(!s.LoadImage(Bytes(OP,OS))||s.width!=1536||s.height!=1024)throw new InvalidDataException("Invalid approved object sheet.");
   if(topRect.x<0||topRect.y<0||topRect.xMax>s.width||topRect.yMax>s.height||topRect.width<=0||topRect.height<=0||width<=sidePadding*2||height<=0)throw new InvalidDataException("Invalid approved object strip rectangle.");
   var all=s.GetPixels32();var keep=Flood(all,s.width,s.height,Nearest(all,s.width,s.height,anchor.x,s.height-1-anchor.y));ValidateComponent(keep,s.width,s.height,anchor);
   var output=new Color32[width*height];int dw=width-2*sidePadding;
   for(int y=0;y<height;y++)for(int x=0;x<dw;x++){
    int sx=topRect.x+x*topRect.width/dw,sy=s.height-topRect.yMax+y*topRect.height/height,p=sy*s.width+sx;
    if(!keep[p])continue;var c=all[p];c.a=255;output[y*width+sidePadding+x]=c;
   }
   if(!output.Any(c=>c.a==255))throw new InvalidDataException("Empty approved strip at "+topRect);
   return output;
  }finally{Object.DestroyImmediate(s);}
 }
 static RectInt ValidateComponent(bool[] keep,int w,int h,Vector2Int anchor){int x0=w,y0=h,x1=-1,y1=-1,count=0;for(int y=0;y<h;y++)for(int x=0;x<w;x++)if(keep[y*w+x]){count++;x0=Mathf.Min(x0,x);y0=Mathf.Min(y0,y);x1=Mathf.Max(x1,x);y1=Mathf.Max(y1,y);}if(count==0)throw new InvalidDataException("Empty approved object component at "+anchor);if(x0==0||y0==0||x1==w-1||y1==h-1)throw new InvalidDataException("Approved object component touches full-sheet edge at "+anchor);return new RectInt(x0,y0,x1-x0+1,y1-y0+1);}
 static int Nearest(Color32[] p,int w,int h,int ax,int ay){int best=-1,bd=int.MaxValue;for(int y=0;y<h;y++)for(int x=0;x<w;x++)if(p[y*w+x].a>16){int d=(x-ax)*(x-ax)+(y-ay)*(y-ay);if(d<bd){bd=d;best=y*w+x;}}if(best<0)throw new InvalidDataException("Object crop has no alpha.");return best;}
 static bool[] Flood(Color32[] p,int w,int h,int seed){var seen=new bool[p.Length];var q=new Queue<int>();seen[seed]=true;q.Enqueue(seed);while(q.Count>0){int n=q.Dequeue(),x=n%w,y=n/w;for(int dy=-1;dy<=1;dy++)for(int dx=-1;dx<=1;dx++){int xx=x+dx,yy=y+dy,k=yy*w+xx;if(xx<0||yy<0||xx>=w||yy>=h||(dx==0&&dy==0)||seen[k]||p[k].a<=16)continue;seen[k]=true;q.Enqueue(k);}}return seen;}
}}
