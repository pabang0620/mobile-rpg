using System;
using System.Collections.Generic;

namespace Sapphire
{
    public sealed class CampaignService
    {
        readonly IProfileStore store;
        ProfileData profile;
        string runId;
        readonly List<string> pendingKinds = new List<string>();
        readonly HashSet<string> pendingEvents = new HashSet<string>();
        public ProfileData Profile { get { return profile; } }
        public string LastError { get; private set; }
        public string LoadWarning { get; private set; }
        public bool InDungeon { get { return runId != null; } }
        public int PendingKills { get { return pendingKinds.Count; } }
        public int NextXp { get { return profile.Level >= 5 ? 0 : CampaignCatalog.LevelXp[profile.Level-1]; } }
        public CampaignService(IProfileStore store) { this.store = store; }
        public bool NewGame()
        {
            var p = new ProfileData { ProfileId = Guid.NewGuid().ToString("N") };
            foreach(var q in CampaignCatalog.Quests) p.Quests.Add(new QuestState { Id=q.Id });
            foreach(var id in new[]{"item.staff_01","item.robe_01","item.charm_01"}) p.Inventory.Add(new EquipmentData { InstanceId=Guid.NewGuid().ToString("N"),DefinitionId=id,Equipped=true });
            string error; if(!store.TrySave(p,out error)) return Reject(error);
            profile=p; ClearRun(); LastError=""; return true;
        }
        public bool Load()
        {
            ProfileData p; string warning; if(!store.TryLoad(out p,out warning)) return Reject(warning);
            profile=p; LoadWarning=warning; ClearRun(); LastError=""; return true;
        }
        public PlayerStats Stats
        {
            get { var s = new PlayerStats { MaxHp=120+12*(profile.Level-1),MaxMp=100,Attack=18+3*(profile.Level-1),Defense=4 };
                foreach(var item in profile.Inventory) if(item.Equipped) { var d=CampaignCatalog.Item(item.DefinitionId); if(d==null) continue; s.MaxHp+=d.Hp; s.Attack+=d.Attack+(d.Slot=="Weapon"?item.Upgrade*2:0); s.Defense+=d.Defense+(d.Slot!="Weapon"?item.Upgrade:0); } return s; }
        }
        bool Reject(string error) { LastError=error; return false; }
        bool Commit(string id,long revision,Func<ProfileData,string> mutate)
        {
            if(profile==null) return Reject("NoProfile");
            if(profile.RecentTransactions.Contains(id)) { LastError=""; return true; }
            if(profile.Revision!=revision) return Reject("StaleRevision");
            var draft=profile.Copy(); string error=mutate(draft); if(error!=null) return Reject(error);
            draft.Revision++; draft.RecentTransactions.Add(id); if(draft.RecentTransactions.Count>256) draft.RecentTransactions.RemoveAt(0);
            if(!store.TrySave(draft,out error)) return Reject("SaveFailed: "+error);
            profile=draft; LastError=""; return true;
        }
        bool Change(Func<ProfileData,string> mutate) { return Commit(Guid.NewGuid().ToString("N"),profile==null?-1:profile.Revision,mutate); }
        public bool Save() { return Change(p=>null); }
        public bool SetVolumes(float music,float sfx) { return Change(p=>{p.MusicVolume=Math.Max(0,Math.Min(1,music));p.SfxVolume=Math.Max(0,Math.Min(1,sfx));return null;}); }
        public bool AcceptQuest() { return Change(p=>{if(p.QuestIndex>=p.Quests.Count)return "ChapterComplete";var q=p.Quests[p.QuestIndex];if(q.Accepted)return "AlreadyAccepted";q.Accepted=true;if(p.QuestIndex==4&&p.BossDefeated)q.Progress=1;if(p.QuestIndex==5&&p.CheckpointZone=="town")q.Progress=1;return null;}); }
        public bool TurnInQuest()
        {
            if(InDungeon)return Reject("ReturnToTown");
            return Change(p=>{if(p.CheckpointZone!="town")return "ReturnToTown";if(p.QuestIndex>=p.Quests.Count)return "ChapterComplete"; var q=p.Quests[p.QuestIndex];var d=CampaignCatalog.Quests[p.QuestIndex];if(!q.Accepted||q.Progress<d.Target||q.Claimed)return "QuestIncomplete";
                string reward=p.QuestIndex==1?"item.staff_02":p.QuestIndex==3?"item.robe_02":p.QuestIndex==5?"item.charm_03":null;
                if(reward!=null&&!AddEquipment(p,reward,true))return "InventoryFull";q.Claimed=true;p.Gold+=d.Gold;GrantXp(p,d.Xp);p.QuestIndex++;return null;});
        }
        public bool RecordTravel(string zoneId) { return Change(p=>{if(string.IsNullOrEmpty(zoneId))return "InvalidTarget";p.CheckpointZone=zoneId; Progress(p,zoneId=="town"?"return":"travel");return null;}); }
        static void Progress(ProfileData p,string kind)
        {
            if(p.QuestIndex>=p.Quests.Count)return;var q=p.Quests[p.QuestIndex];var d=CampaignCatalog.Quests[p.QuestIndex];if(q.Accepted&&!q.Claimed&&d.TargetKind==kind)q.Progress=Math.Min(d.Target,q.Progress+1);
        }
        static void GrantXp(ProfileData p,int amount) {p.Xp+=amount;while(p.Level<5&&p.Xp>=CampaignCatalog.LevelXp[p.Level-1]){p.Xp-=CampaignCatalog.LevelXp[p.Level-1];p.Level++;}if(p.Level==5)p.Xp=0;}
        public bool RecordKill(string enemyKind,string eventId) { return RecordKill(enemyKind,eventId,profile.Revision); }
        public bool RecordKill(string enemyKind,string eventId,long expectedRevision)
        {
            string kind=(enemyKind??"").ToLowerInvariant().Replace("enemy.","");if(kind=="spirit")kind="wisp";if(kind=="guardian")kind="boss";
            if(kind!="goblin"&&kind!="wisp"&&kind!="wolf"&&kind!="elite"&&kind!="boss")return Reject("InvalidEnemy");if(string.IsNullOrEmpty(eventId))return Reject("InvalidTransaction");
            if(InDungeon){if(expectedRevision!=profile.Revision)return Reject("StaleRevision");if(pendingEvents.Add(eventId))pendingKinds.Add(kind);LastError="";return true;}
            return Commit(eventId,expectedRevision,p=>GrantKill(p,kind,false));
        }
        static string GrantKill(ProfileData p,string kind,bool inbox)
        {
            int xp=kind=="goblin"?15:kind=="wisp"?18:kind=="wolf"?20:kind=="elite"?50:150;
            int gold=kind=="goblin"?4:kind=="wisp"?5:kind=="wolf"?6:kind=="elite"?20:60;
            if(kind=="elite"&&!AddEquipment(p,"item.robe_02",inbox))return "InventoryFull";
            if(kind=="boss"&&!AddEquipment(p,"item.staff_03",inbox))return "InventoryFull";
            GrantXp(p,xp);p.Gold+=gold;Progress(p,kind);if(kind=="wisp"||kind=="wolf"){p.Materials=Math.Min(99,p.Materials+1);Progress(p,"material");}if(kind=="boss")p.BossDefeated=true;return null;
        }
        static bool AddEquipment(ProfileData p,string id,bool allowInbox) { var list=p.Inventory.Count<24?p.Inventory:allowInbox&&p.RewardInbox.Count<24?p.RewardInbox:null;if(list==null)return false;list.Add(new EquipmentData{InstanceId=Guid.NewGuid().ToString("N"),DefinitionId=id});return true; }
        public bool Equip(string id) { if(InDungeon)return Reject("Locked"); return Change(p=>{var item=p.Inventory.Find(x=>x.InstanceId==id);if(item==null)return "InvalidTarget";var def=CampaignCatalog.Item(item.DefinitionId);foreach(var x in p.Inventory)if(CampaignCatalog.Item(x.DefinitionId).Slot==def.Slot)x.Equipped=false;item.Equipped=true;return null;}); }
        public bool Upgrade(string id) { if(InDungeon)return Reject("Locked");return Change(p=>{var item=p.Inventory.Find(x=>x.InstanceId==id);if(item==null)return "InvalidTarget";if(item.Upgrade>=3)return "MaxUpgrade";int cost=new[]{30,60,100}[item.Upgrade];if(p.Gold<cost)return "InsufficientResource";p.Gold-=cost;item.Upgrade++;return null;}); }
        public bool Sell(string id) { if(InDungeon)return Reject("Locked");return Change(p=>{var item=p.Inventory.Find(x=>x.InstanceId==id);if(item==null)return "InvalidTarget";if(item.Equipped)return "Equipped";p.Gold+=CampaignCatalog.Item(item.DefinitionId).Price/2+item.Upgrade*10;p.Inventory.Remove(item);return null;}); }
        public bool ClaimInbox(string id) { return Change(p=>{var item=p.RewardInbox.Find(x=>x.InstanceId==id);if(item==null)return "InvalidTarget";if(p.Inventory.Count>=24)return "InventoryFull";p.RewardInbox.Remove(item);p.Inventory.Add(item);return null;}); }
        public bool BuyPotion(bool hp) {if(InDungeon)return Reject("Locked");return Change(p=>{if(p.Gold<10)return "InsufficientResource";if((hp?p.HpPotions:p.MpPotions)>=99)return "InventoryFull";p.Gold-=10;if(hp)p.HpPotions++;else p.MpPotions++;return null;}); }
        public bool ConsumePotion(bool hp) {return Change(p=>{if((hp?p.HpPotions:p.MpPotions)<=0)return "InsufficientResource";if(hp)p.HpPotions--;else p.MpPotions--;return null;}); }
        public bool BeginDungeon(string id) {if(InDungeon)return Reject("AlreadyInDungeon");if(string.IsNullOrEmpty(id)||id==profile.LastSettledRunId)return Reject("AlreadyCommitted");if(!Change(p=>{p.CheckpointZone="town";return null;}))return false;runId=id;return true;}
        public bool FinishDungeon(bool success)
        {
            if(!InDungeon)return Reject("NoRun");string id=runId;
            bool ok=Commit("run:"+id,profile.Revision,p=>{if(success){foreach(var kind in pendingKinds){string error=GrantKill(p,kind,true);if(error!=null)return error;}}p.LastSettledRunId=id;p.CheckpointZone="town";return null;});if(ok)ClearRun();return ok;
        }
        void ClearRun(){runId=null;pendingKinds.Clear();pendingEvents.Clear();}
    }
}
