using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Sapphire.World;

namespace Sapphire
{
    [Serializable] public sealed class ActorView { public int Id,Hp,MaxHp; public string Kind,Name; public float X,Y,FacingX,FacingY,Telegraph,TelegraphRadius,Flash; public bool Dead,Boss; }
    [Serializable] public sealed class ProjectileView { public float X,Y,DirectionX,DirectionY; public bool Hostile; }
    [Serializable] public sealed class EffectView { public float X,Y,Age; public string Kind; public int Amount; }
    [Serializable] public sealed class ItemView { public string Id,Name,Description,Slot; public int Upgrade,Price; public bool Equipped; }
    [Serializable] public sealed class QuestView { public string Name,Description,Status; public int Progress,Target; }

    public sealed class GameApp : MonoBehaviour
    {
        public static GameApp Instance { get; private set; }
        public string Mode="Title",ZoneName="",Objective="",Notice="",SaveStatus="";
        public int Level,Hp,MaxHp,Mp,MaxMp,Gold,Xp,NextXp,HpPotions,MpPotions,QuestIndex;
        public bool Paused,Auto,Dead,CanContinue;
        public float PlayerX,PlayerY,FacingX,FacingY,AttackProgress,DodgeRemaining,ShieldRemaining,MusicVolume=.55f,SfxVolume=.7f;
        public float[] Cooldowns={0,0,0,0},MaxCooldowns={3,7,5,10};
        public bool[] SkillUnlocked={true,false,false,false};
        public readonly List<ActorView> Actors=new List<ActorView>();
        public readonly List<ProjectileView> Projectiles=new List<ProjectileView>();
        public readonly List<EffectView> Effects=new List<EffectView>();
        public readonly List<ItemView> Inventory=new List<ItemView>();
        public readonly List<QuestView> Quests=new List<QuestView>();
        public string[] DestinationNames={"달빛 숲 I","달빛 숲 II","달빛 폐허 던전"};
        public TileWorld Exploration { get; private set; }
        public bool IsExploring { get { return Exploration!=null && (Mode=="Town"||Mode=="Explore"); } }

        CampaignService campaign; CombatWorld combat; Vec2 move; float accumulator,exploreRepeat; bool exploreNeutral=true; int dungeonRoom; string savePath;
        void Awake()
        {
            Instance=this; Application.targetFrameRate=60;
            savePath=Path.Combine(Application.persistentDataPath,"sapphire-profile.json");
            campaign=new CampaignService(new FileProfileStore(savePath));
            CanContinue=File.Exists(savePath)||File.Exists(savePath+".bak");
            SaveStatus=CanContinue?"저장된 여정 있음":"새 여정 준비"; Sync();
        }
        void Update()
        {
            if(IsExploring&&!Paused){UpdateExploration();Sync();return;}
            if(combat==null||Paused||Mode=="Title"||Mode=="Result")return;
            accumulator=Mathf.Min(accumulator+Time.unscaledDeltaTime,5*CombatWorld.Dt);
            while(accumulator>=CombatWorld.Dt){combat.Auto=Auto;combat.Step(move);ProcessEvents();accumulator-=CombatWorld.Dt;}
            for(int i=Effects.Count-1;i>=0;i--){Effects[i].Age+=Time.unscaledDeltaTime;if(Effects[i].Age>1)Effects.RemoveAt(i);}Sync();
        }
        void UpdateExploration()
        {
            exploreRepeat-=Time.unscaledDeltaTime;
            if(Mathf.Abs(move.X)<.25f&&Mathf.Abs(move.Y)<.25f){exploreNeutral=true;return;}
            if(!exploreNeutral&&exploreRepeat>0)return;
            Direction direction=Mathf.Abs(move.X)>Mathf.Abs(move.Y)?(move.X<0?Direction.Left:Direction.Right):(move.Y<0?Direction.Down:Direction.Up);
            var result=Exploration.Move(direction);exploreNeutral=false;exploreRepeat=result==MoveResult.Blocked?.12f:.18f;
            if(result==MoveResult.Warped)Notice=Exploration.CurrentMapId=="house"?"별빛 장로의 집":"새로운 지역에 도착했습니다";
        }
        void StartWorld(string mode,string zone)
        {
            if(mode=="Town"){combat=null;Exploration=new TileWorld();Mode="Town";Paused=false;Auto=false;move=new Vec2();exploreNeutral=true;Notice="사파이어 마을에 도착했습니다";campaign.RecordTravel("town");Sync();return;}
            Exploration=null;var s=campaign.Stats;combat=new CombatWorld(zone,s.MaxHp,s.MaxMp,s.Attack,s.Defense,campaign.Profile.Level);Mode=mode;Paused=false;Auto=false;move=new Vec2();dungeonRoom=mode=="Dungeon"?1:0;
            if(mode!="Dungeon")campaign.RecordTravel(mode=="Town"?"town":zone);
            Notice=mode=="Town"?"광장에 도착했습니다":"전투 지역에 진입했습니다";Sync();
        }
        public void NewGame(){if(campaign.NewGame()){CanContinue=true;SaveStatus="새 프로필 저장 완료";StartWorld("Town","town");}else Fail();}
        public void ContinueGame(){if(campaign.Load()){CanContinue=true;SaveStatus=string.IsNullOrEmpty(campaign.LoadWarning)?"저장 불러오기 완료":campaign.LoadWarning;StartWorld("Town","town");}else Fail();}
        public void Move(float x,float y){move=new Vec2(Mathf.Clamp(x,-1,1),Mathf.Clamp(y,-1,1));}
        public void Attack(){if(combat!=null&&!Paused)Report(combat.Attack());}
        public void Cast(int slot){if(combat!=null&&!Paused)Report(combat.Cast(slot));}
        public void Dodge(){if(combat!=null&&!Paused)Report(combat.Dodge());}
        public void UsePotion(bool hp)
        {
            if(combat==null||Paused||combat.Player.Dead)return;
            if(hp&&combat.Player.Hp>=combat.Player.MaxHp){Notice="체력이 가득 찼습니다";return;}
            if(!hp&&combat.Player.Mp>=combat.Player.MaxMp){Notice="마나가 가득 찼습니다";return;}
            if(campaign.ConsumePotion(hp)){combat.Restore(hp?60:0,hp?0:45);Notice=hp?"체력 물약 사용":"마나 물약 사용";}else Fail();Sync();
        }
        public void ToggleAuto(){if(combat==null||Mode=="Town"||Mode=="Dungeon"&&dungeonRoom>=3)return;Auto=!Auto;Notice=Auto?"자동 전투 시작":"자동 전투 종료";}
        public void Interact(){if(IsExploring){string dialogue=Exploration.Interact();Notice=string.IsNullOrEmpty(dialogue)?"조사했지만 특별한 것은 없습니다":dialogue;}}
        public void Travel(int destination)
        {
            if(campaign==null||campaign.Profile==null)return;
            if(destination==2){string id=Guid.NewGuid().ToString("N");if(!campaign.BeginDungeon(id)){Fail();return;}StartWorld("Dungeon","dungeon1");Notice="달빛 폐허 원정 시작";}
            else if(destination==0){Exploration=new TileWorld();Exploration.Enter("route",new TileCoord(1,4),Direction.Right);combat=null;Mode="Explore";Paused=false;move=new Vec2();Notice="달빛 숲길에 도착했습니다";Sync();}
            else StartWorld("Field","field2");
        }
        public void ReturnTown(){if(Mode=="Dungeon"&&campaign.InDungeon)campaign.FinishDungeon(false);StartWorld("Town","town");}
        public void Respawn(){if(Mode=="Dungeon"){campaign.FinishDungeon(false);Mode="Result";Objective="원정 실패";Notice="임시 전리품을 잃었습니다";Auto=false;}else StartWorld("Field",campaign.Profile.CheckpointZone=="field2"?"field2":"field1");}
        public void SetPaused(bool value){Paused=value;accumulator=0;if(value)move=new Vec2();}
        public void Equip(string id){if(campaign.Equip(id)){ApplyStats();Notice="장비를 장착했습니다";}else Fail();Sync();}
        public void Upgrade(string id){if(campaign.Upgrade(id)){ApplyStats();Notice="강화에 성공했습니다";}else Fail();Sync();}
        public void Sell(string id){if(campaign.Sell(id))Notice="장비를 판매했습니다";else Fail();Sync();}
        public void BuyPotion(bool hp){if(campaign.BuyPotion(hp))Notice="물약을 구매했습니다";else Fail();Sync();}
        public void AcceptQuest(){if(campaign.AcceptQuest())Notice="의뢰를 수락했습니다";else Fail();Sync();}
        public void TurnInQuest(){if(campaign.TurnInQuest()){ApplyStats();Notice="의뢰 보상을 받았습니다";}else Fail();Sync();}
        public void SaveNow(){if(campaign!=null&&campaign.Profile!=null&&campaign.Save()){CanContinue=true;SaveStatus="저장 완료  "+DateTime.Now.ToString("HH:mm:ss");}else Fail();}
        public void SetVolumes(float music,float sfx){if(campaign.SetVolumes(music,sfx)){MusicVolume=music;SfxVolume=sfx;Notice="음량 설정 저장";}else Fail();}
        void ProcessEvents()
        {
            foreach(var e in combat.DrainEvents())
            {
                Effects.Add(new EffectView{X=e.Position.X-10,Y=e.Position.Y-5.5f,Age=0,Kind=e.Kind,Amount=e.Amount});
                if(e.Kind=="Killed")
                {
                    if(!campaign.RecordKill(e.EnemyKind,e.Id)){Fail();continue;}Notice=e.EnemyKind+" 처치 · "+e.Xp+" XP · "+e.Gold+" G";
                    if(combat.Cleared)AdvanceClear();
                }
                else if(e.Kind=="PlayerDied"){Auto=false;Notice="쓰러졌습니다";}
            }
        }
        void AdvanceClear()
        {
            if(Mode!="Dungeon")return;
            if(dungeonRoom<3){dungeonRoom++;combat.EnterZone(dungeonRoom==2?"dungeon2":"boss");Notice=dungeonRoom==2?"정예 수문장의 방":"균열 수호자의 방";}
            else if(campaign.FinishDungeon(true)){Mode="Result";Objective="원정 성공";Notice="전리품과 경험치가 확정되었습니다";Auto=false;}
            else Fail();
        }
        void ApplyStats(){if(combat==null)return;var s=campaign.Stats;combat.SetStats(s.MaxHp,s.MaxMp,s.Attack,s.Defense,campaign.Profile.Level);}
        void Report(CommandResult result){if(result!=CommandResult.Accepted)Notice=result==CommandResult.Cooldown?"재사용 대기 중":result==CommandResult.InsufficientResource?"마나가 부족합니다":result==CommandResult.Locked?"아직 배우지 못했습니다":"지금은 사용할 수 없습니다";}
        void Fail(){Notice=campaign==null?"서비스 준비 실패":campaign.LastError;SaveStatus="저장 오류: "+Notice;}
        void Sync()
        {
            if(campaign==null||campaign.Profile==null){Level=1;MaxHp=Hp=120;MaxMp=Mp=100;return;}
            var p=campaign.Profile;var s=campaign.Stats;Level=p.Level;Gold=p.Gold;Xp=p.Xp;NextXp=campaign.NextXp;HpPotions=p.HpPotions;MpPotions=p.MpPotions;QuestIndex=p.QuestIndex;MusicVolume=p.MusicVolume;SfxVolume=p.SfxVolume;
            MaxHp=s.MaxHp;MaxMp=s.MaxMp;if(IsExploring){Hp=MaxHp;Mp=MaxMp;Dead=false;PlayerX=Exploration.PlayerPosition.X;PlayerY=Exploration.PlayerPosition.Y;FacingX=Exploration.PlayerFacing==Direction.Left?-1:Exploration.PlayerFacing==Direction.Right?1:0;FacingY=Exploration.PlayerFacing==Direction.Down?-1:Exploration.PlayerFacing==Direction.Up?1:0;Actors.Clear();Projectiles.Clear();}
            else if(combat!=null){Hp=combat.Player.Hp;Mp=combat.Player.Mp;Dead=combat.Player.Dead;PlayerX=combat.Player.Position.X-10;PlayerY=combat.Player.Position.Y-5.5f;FacingX=combat.Player.Facing.X;FacingY=combat.Player.Facing.Y;AttackProgress=combat.AttackProgress;DodgeRemaining=combat.DodgeRemaining;ShieldRemaining=combat.ShieldRemaining;Cooldowns=combat.Cooldowns;MaxCooldowns=combat.MaxCooldowns;Actors.Clear();foreach(var a in combat.Enemies)Actors.Add(new ActorView{Id=a.Id,Kind=a.Kind,Name=a.Name,X=a.Position.X-10,Y=a.Position.Y-5.5f,FacingX=a.Facing.X,FacingY=a.Facing.Y,Hp=a.Hp,MaxHp=a.MaxHp,Dead=a.Dead,Boss=a.Boss,Telegraph=a.Telegraph,TelegraphRadius=a.TelegraphRadius,Flash=a.Flash});Projectiles.Clear();foreach(var b in combat.Projectiles)Projectiles.Add(new ProjectileView{X=b.Position.X-10,Y=b.Position.Y-5.5f,DirectionX=b.Direction.X,DirectionY=b.Direction.Y,Hostile=b.Hostile});}
            SkillUnlocked=new[]{p.Level>=1,p.Level>=2,p.Level>=3,p.Level>=4};Inventory.Clear();foreach(var x in p.Inventory){var d=CampaignCatalog.Item(x.DefinitionId);Inventory.Add(new ItemView{Id=x.InstanceId,Name=d.Name,Description=d.Slot+" · 공격 "+d.Attack+" 방어 "+d.Defense+" HP "+d.Hp,Slot=d.Slot,Upgrade=x.Upgrade,Equipped=x.Equipped,Price=d.Price/2+x.Upgrade*10});}Quests.Clear();for(int i=0;i<p.Quests.Count;i++){var q=p.Quests[i];var d=CampaignCatalog.Quests[i];Quests.Add(new QuestView{Name=d.Name,Description=d.Description,Progress=q.Progress,Target=d.Target,Status=q.Claimed?"완료":q.Accepted?"진행 중":"미수락"});}
            ZoneName=IsExploring?(Exploration.CurrentMapId=="town"?"사파이어 마을":Exploration.CurrentMapId=="house"?"별빛 장로의 집":"달빛 숲길"):Mode=="Field"?(combat.ZoneId=="field2"?"달빛 숲 깊은 곳":"달빛 숲길"):Mode=="Dungeon"?(dungeonRoom==3?"균열 수호자의 방":dungeonRoom==2?"정예 수문장의 방":"달빛 폐허 입구"):"";
            Objective=p.QuestIndex>=p.Quests.Count?"CHAPTER 01 완료":CampaignCatalog.Quests[p.QuestIndex].Description;
        }
    }
}
