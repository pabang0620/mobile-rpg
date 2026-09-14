using System.Collections.Generic;
using UnityEngine;
using Sapphire.World;

namespace Sapphire
{
    /// <summary>Visual-only view pool. Entity HP, telegraphs and positions are application snapshots.</summary>
    public sealed class WorldRenderer : MonoBehaviour
    {
        GameApp app; GamePresenter ui;
        SpriteRenderer backdrop, player, shield;
        PixelTileMapRenderer tileMap; PixelCameraFollow cameraFollow;
        readonly List<SpriteRenderer> npcViews=new List<SpriteRenderer>();
        readonly List<SpriteRenderer> gridLines = new List<SpriteRenderer>();
        readonly Dictionary<int, SpriteRenderer> actors = new Dictionary<int, SpriteRenderer>();
        readonly Dictionary<int, SpriteRenderer> warnings = new Dictionary<int, SpriteRenderer>();
        readonly Dictionary<int, SpriteRenderer> health = new Dictionary<int, SpriteRenderer>();
        readonly List<SpriteRenderer> bullets = new List<SpriteRenderer>();
        readonly List<TextMesh> numbers = new List<TextMesh>();
        Sprite dot, ring, square; string zone;
        public void Initialize(GameApp game, GamePresenter presenter)
        {
            app=game; ui=presenter;
            square=Sprite.Create(Texture2D.whiteTexture,new Rect(0,0,2,2),new Vector2(.5f,.5f),2);
            dot=Circle(false); ring=Circle(true);
            backdrop=Make("World background",null,-1000,Color.white);
            player=Make("Sapphire mage",ui.ArtSprite("MageSprite","MageIdle"),10,Color.white);
            var mapObject=new GameObject("Exploration tile world");mapObject.transform.SetParent(transform);tileMap=mapObject.AddComponent<PixelTileMapRenderer>();mapObject.SetActive(false);
            cameraFollow=ui.CaptureCamera.gameObject.AddComponent<PixelCameraFollow>();cameraFollow.enabled=false;
            shield=Make("Absorption shield",ring,9,new Color(.3f,.8f,1,.7f));
            for(int x=-10;x<=10;x++){var line=Make("Grid X "+x,square,-900,new Color(.3f,.65f,.85f,.10f));line.transform.position=new Vector3(x,0,0);Size(line,.012f,11);gridLines.Add(line);}
            for(int y=-5;y<=5;y++){var line=Make("Grid Y "+y,square,-900,new Color(.3f,.65f,.85f,.10f));line.transform.position=new Vector3(0,y+.5f,0);Size(line,20,.012f);gridLines.Add(line);}
        }
        Sprite Circle(bool outline)
        {
            var tex=new Texture2D(64,64,TextureFormat.RGBA32,false); tex.filterMode=FilterMode.Bilinear;
            var p=new Color[4096]; for(int y=0;y<64;y++)for(int x=0;x<64;x++){float d=Vector2.Distance(new Vector2(x,y),new Vector2(31.5f,31.5f));p[y*64+x]=new Color(1,1,1,outline?Mathf.Clamp01(2-Mathf.Abs(d-28)):Mathf.Clamp01(31-d));}
            tex.SetPixels(p);tex.Apply();return Sprite.Create(tex,new Rect(0,0,64,64),new Vector2(.5f,.5f),64);
        }
        SpriteRenderer Make(string name,Sprite sprite,int order,Color color)
        {
            var o=new GameObject(name);o.transform.SetParent(transform);var r=o.AddComponent<SpriteRenderer>();r.sprite=sprite;r.sortingOrder=order;r.color=color;return r;
        }
        void Size(SpriteRenderer r,float width,float height) { if(r.sprite != null) r.transform.localScale=new Vector3(width/r.sprite.bounds.size.x,height/r.sprite.bounds.size.y,1); }
        void LateUpdate()
        {
            if(app==null) return;
            if(app.IsExploring){RenderExploration();return;}
            if(tileMap)tileMap.gameObject.SetActive(false);if(cameraFollow){cameraFollow.enabled=false;ui.CaptureCamera.transform.position=new Vector3(0,0,-20);}
            foreach(var n in npcViews)n.enabled=false;foreach(var line in gridLines)line.enabled=true;backdrop.enabled=true;
            bool playing=app.Mode!="Title";
            player.enabled=playing;shield.enabled=playing && app.ShieldRemaining>0;
            if(zone!=app.Mode+app.ZoneName){zone=app.Mode+app.ZoneName;backdrop.sprite=app.Mode=="Title"?ui.ArtSprite("TitleBackground","SapphireTown"):app.Mode=="Town"?ui.ArtSprite("TopdownTown","MoonshardField"):app.Mode=="Dungeon"||app.Mode=="Result"?ui.ArtSprite("TopdownDungeon","MoonshardField"):ui.ArtSprite("TopdownField","MoonshardField");Size(backdrop,20,11.25f);}
            float bob=app.Paused?0:Mathf.Sin(Time.time*7)*.018f;
            if(Mathf.Abs(app.FacingY)>=Mathf.Abs(app.FacingX))player.sprite=app.FacingY>=0?ui.AtlasSprite("MageSDDirectional",1,1,2,2):ui.AtlasSprite("MageSDDirectional",0,0,2,2);
            else player.sprite=app.FacingX<0?ui.AtlasSprite("MageSDDirectional",1,0,2,2):ui.AtlasSprite("MageSDDirectional",0,1,2,2);
            if(player.sprite==null)player.sprite=ui.ArtSprite("MageIdle");
            player.transform.position=new Vector3(app.PlayerX,app.PlayerY+.38f+bob,0); Size(player,.9f,1.25f);player.flipX=false;player.sortingOrder=100-Mathf.RoundToInt(app.PlayerY*10);
            shield.transform.position=new Vector3(app.PlayerX,app.PlayerY+.23f,0);Size(shield,1.3f,1.3f);shield.sortingOrder=player.sortingOrder+1;
            foreach(var r in actors.Values)r.enabled=false;foreach(var r in warnings.Values)r.enabled=false;foreach(var r in health.Values)r.enabled=false;
            if(playing && app.Actors!=null)foreach(var a in app.Actors)
            {
                if(a.Dead)continue;SpriteRenderer r;
                if(!actors.TryGetValue(a.Id,out r)){r=Make("Actor "+a.Id,null,0,Color.white);actors.Add(a.Id,r);}
                string kind=(a.Kind??"").ToLowerInvariant();
                r.sprite=a.Boss?ui.ArtSprite("GuardianSprite","Goblin"):kind.Contains("spirit")?ui.ArtSprite("SpiritSprite","Goblin"):kind.Contains("wolf")?ui.ArtSprite("WolfSprite","Goblin"):kind.Contains("elite")?ui.ArtSprite("EliteSprite","GoblinMonsterFrame","Goblin"):ui.ArtSprite("GoblinSprite","GoblinPixelArtIdle","Goblin");
                r.enabled=true;r.transform.position=new Vector3(a.X,a.Y+.32f,0);r.flipX=a.FacingX>0;r.sortingOrder=100-Mathf.RoundToInt(a.Y*10);Size(r,a.Boss?1.6f:.84f,a.Boss?1.8f:.94f);r.color=a.Flash>0?new Color(1,.6f,.6f):Color.white;
                SpriteRenderer hp;if(!health.TryGetValue(a.Id,out hp)){hp=Make("Enemy health",square,500,new Color(.95f,.3f,.38f));health.Add(a.Id,hp);}hp.enabled=a.Hp<a.MaxHp;hp.transform.position=new Vector3(a.X,a.Y+(a.Boss?1.6f:1),0);Size(hp,.65f*Mathf.Clamp01((float)a.Hp/Mathf.Max(1,a.MaxHp)),.045f);
                if(a.Telegraph>0){SpriteRenderer w;if(!warnings.TryGetValue(a.Id,out w)){w=Make("Attack warning",ring,-400,new Color(1,.3f,.27f,.85f));warnings.Add(a.Id,w);}w.enabled=true;w.transform.position=new Vector3(a.X,a.Y,0);Size(w,a.TelegraphRadius*2,a.TelegraphRadius*2);w.color=new Color(1,.25f,.2f,.5f+Mathf.PingPong(Time.unscaledTime*2,.4f));}
            }
            int index=0;if(playing && app.Projectiles!=null)foreach(var b in app.Projectiles){if(index==bullets.Count)bullets.Add(Make("Magic projectile",dot,600,Color.white));var r=bullets[index++];r.enabled=true;r.sprite=ui.AtlasSprite("MagicMissile",7,0,42,1)??dot;r.transform.position=new Vector3(b.X,b.Y+.15f,0);r.transform.rotation=Quaternion.Euler(0,0,Mathf.Atan2(b.DirectionY,b.DirectionX)*Mathf.Rad2Deg);Size(r,.34f,.34f);r.color=b.Hostile?new Color(1,.4f,.2f):new Color(.35f,.9f,1);}
            for(;index<bullets.Count;index++)bullets[index].enabled=false;
            index=0;if(playing && app.Effects!=null)foreach(var e in app.Effects){if(e.Amount==0)continue;if(index==numbers.Count){var go=new GameObject("Combat text");go.transform.SetParent(transform);var t=go.AddComponent<TextMesh>();t.anchor=TextAnchor.MiddleCenter;t.fontSize=42;t.characterSize=.075f;t.font=ui.UiFont;if(ui.UiFont)t.GetComponent<MeshRenderer>().sharedMaterial=ui.UiFont.material;t.GetComponent<MeshRenderer>().sortingOrder=1000;numbers.Add(t);}var text=numbers[index++];text.gameObject.SetActive(true);text.text=e.Amount.ToString();text.color=e.Kind=="heal"?new Color(.4f,1,.7f):new Color(1,.9f,.65f,Mathf.Clamp01(1-e.Age));text.transform.position=new Vector3(e.X,e.Y+1+e.Age*.6f,0);}
            for(;index<numbers.Count;index++)numbers[index].gameObject.SetActive(false);
        }
        void RenderExploration()
        {
            backdrop.enabled=false;shield.enabled=false;foreach(var line in gridLines)line.enabled=false;
            foreach(var r in actors.Values)r.enabled=false;foreach(var r in warnings.Values)r.enabled=false;foreach(var r in health.Values)r.enabled=false;foreach(var r in bullets)r.enabled=false;foreach(var t in numbers)t.gameObject.SetActive(false);
            var world=app.Exploration;var map=world.CurrentMap;string key="explore/"+world.CurrentMapId;
            if(zone!=key)
            {
                zone=key;var visual=new ExplorationTile[map.Width,map.Height];
                for(int x=0;x<map.Width;x++)for(int y=0;y<map.Height;y++)visual[x,y]=Visual(map.Get(new TileCoord(x,y)));
                tileMap.gameObject.SetActive(true);tileMap.Configure(map.Width,map.Height,1,new Vector2(-.5f,-.5f));tileMap.SetTiles(visual);
                cameraFollow.enabled=true;cameraFollow.Configure(player.transform,tileMap.WorldBounds,true);
            }
            tileMap.gameObject.SetActive(true);player.enabled=true;
            Vector3 target=new Vector3(app.PlayerX,app.PlayerY+.28f,0);bool moving=Vector3.Distance(player.transform.position,target)>.025f;
            player.transform.position=Vector3.MoveTowards(player.transform.position,target,Time.unscaledDeltaTime*7f);
            int row=Mathf.Abs(app.FacingY)>=Mathf.Abs(app.FacingX)?(app.FacingY>=0?3:0):(app.FacingX<0?1:2);int col=moving?(Mathf.FloorToInt(Time.unscaledTime*10)&1)*2:1;
            player.sprite=ui.AtlasSprite("MageWalk4x3-v2",col,row,3,4);if(player.sprite==null)player.sprite=PixelSpriteLibrary.Walker((ExplorationFacing)row,moving?1:0);
            Size(player,.86f,1.08f);player.sortingOrder=500-Mathf.RoundToInt(player.transform.position.y*10);
            int i=0;foreach(var npc in world.CurrentNpcs)
            {
                if(i==npcViews.Count)npcViews.Add(Make("Village NPC",null,20,Color.white));var r=npcViews[i++];r.enabled=true;r.sprite=PixelSpriteLibrary.Walker(ToFacing(npc.Facing),0);r.transform.position=new Vector3(npc.Position.X,npc.Position.Y+.18f,0);Size(r,.82f,1.02f);r.sortingOrder=500-npc.Position.Y*10;
            }
            for(;i<npcViews.Count;i++)npcViews[i].enabled=false;
            cameraFollow.SetBounds(tileMap.WorldBounds);
        }
        static ExplorationFacing ToFacing(Direction d){return d==Direction.Up?ExplorationFacing.Up:d==Direction.Left?ExplorationFacing.Left:d==Direction.Right?ExplorationFacing.Right:ExplorationFacing.Down;}
        static ExplorationTile Visual(TileKind tile)
        {
            switch(tile){case TileKind.Path:return ExplorationTile.Path;case TileKind.Wall:return ExplorationTile.Wall;case TileKind.Tree:return ExplorationTile.Tree;case TileKind.Water:return ExplorationTile.Water;case TileKind.Floor:return ExplorationTile.Sand;case TileKind.Door:return ExplorationTile.Door;default:return ExplorationTile.Grass;}
        }
    }
}
