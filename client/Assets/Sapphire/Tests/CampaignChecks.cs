using System;
using System.IO;

namespace Sapphire
{
    public static class CampaignChecks
    {
        sealed class MemoryStore : IProfileStore
        {
            public ProfileData Saved; public bool Fail;
            public bool TrySave(ProfileData p,out string error){error=Fail?"Injected disk failure":"";if(Fail)return false;Saved=p.Copy();return true;}
            public bool TryLoad(out ProfileData p,out string warning){p=Saved==null?null:Saved.Copy();warning=p==null?"Missing":"";return p!=null;}
        }
        static void Check(bool condition,string message){if(!condition)throw new Exception("CampaignChecks: "+message);}
        public static string RunAll()
        {
            var disk=new MemoryStore();var c=new CampaignService(disk);Check(c.NewGame(),"new profile");Check(c.Stats.Attack==18&&c.Profile.Inventory.Count==3,"starter stats");
            long revision=c.Profile.Revision;Check(c.RecordKill("goblin","event.1",revision),"reward commit");int gold=c.Profile.Gold;Check(c.RecordKill("goblin","event.1",revision),"duplicate accepted");Check(c.Profile.Gold==gold,"duplicate no reward");Check(!c.RecordKill("wolf","stale",revision)&&c.LastError=="StaleRevision","stale revision blocked");
            disk.Fail=true;revision=c.Profile.Revision;Check(!c.BuyPotion(true),"disk failure rejected");Check(c.Profile.Revision==revision&&c.Profile.Gold==gold,"disk failure rollback");disk.Fail=false;
            Check(c.AcceptQuest()&&c.RecordTravel("field1")&&c.RecordTravel("town")&&c.TurnInQuest(),"travel quest");Check(!c.TurnInQuest(),"quest duplicate blocked");Check(c.AcceptQuest(),"kill quest accept");for(int i=0;i<4;i++)Check(c.RecordKill("goblin","quest.kill."+i),"quest kill");Check(c.TurnInQuest(),"kill quest reward");Check(c.Profile.Inventory.Exists(x=>x.DefinitionId=="item.staff_02"),"quest equipment");
            var staff=c.Profile.Inventory.Find(x=>x.DefinitionId=="item.staff_02");Check(c.Equip(staff.InstanceId),"equip");int attack=c.Stats.Attack;Check(c.Upgrade(staff.InstanceId)&&c.Stats.Attack==attack+2,"upgrade stats");Check(!c.Sell(staff.InstanceId),"equipped sell rejected");
            int potions=c.Profile.HpPotions;gold=c.Profile.Gold;int xp=c.Profile.Xp;Check(c.BeginDungeon("failed-run"),"run start");Check(c.RecordKill("boss","pending-boss"),"pending kill");Check(c.Profile.Gold==gold&&c.Profile.Xp==xp,"pending invisible");Check(c.ConsumePotion(true)&&c.FinishDungeon(false),"failure settlement");Check(c.Profile.Gold==gold&&c.Profile.HpPotions==potions-1&&!c.Profile.BossDefeated,"failure drops discarded potion kept");
            Check(c.BeginDungeon("success-run")&&c.RecordKill("boss","boss.1")&&c.RecordKill("boss","boss.1"),"pending idempotence");Check(c.PendingKills==1,"one pending kill");disk.Fail=true;Check(!c.FinishDungeon(true)&&c.InDungeon,"settlement remains retryable");disk.Fail=false;Check(c.FinishDungeon(true)&&c.Profile.Gold==gold+60&&c.Profile.BossDefeated,"success atomically granted");Check(!c.FinishDungeon(true)&&!c.BeginDungeon("success-run"),"settlement duplicate blocked");
            Check(c.BeginDungeon("abandoned-run")&&c.RecordKill("boss","boss.abandoned")&&c.ConsumePotion(false),"abandoned run");var reload=new CampaignService(disk);Check(reload.Load()&&!reload.InDungeon&&reload.Profile.Gold==c.Profile.Gold&&reload.Profile.MpPotions==4,"reload discards pending keeps consumption");
            for(int i=0;i<40;i++)Check(reload.RecordKill("goblin","xp."+i),"level rewards");Check(reload.Profile.Level==5&&reload.Profile.Xp==0,"level cap");
            string folder=System.IO.Path.Combine(Environment.CurrentDirectory,"verification","campaign-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(folder);string file=System.IO.Path.Combine(folder,"profile.json");var fs=new FileProfileStore(file);var saved=new CampaignService(fs);Check(saved.NewGame(),"disk initial save: "+saved.LastError);Check(saved.BuyPotion(true),"disk atomic save: "+saved.LastError);var loaded=new CampaignService(fs);Check(loaded.Load()&&loaded.Profile.HpPotions==6,"disk roundtrip");File.WriteAllText(file,"corrupt");Check(loaded.Load()&&loaded.LoadWarning.Contains("Recovered backup")&&loaded.Profile.HpPotions==5,"backup corruption recovery");Check(loaded.Save(),"save after recovery");File.WriteAllText(file,"corrupt again");Check(loaded.Load()&&loaded.Profile.HpPotions==5,"good backup preserved");File.WriteAllText(file+".bak","also corrupt");Check(!loaded.Load(),"both corrupt refused");
            var future=disk.Saved.Copy();future.SchemaVersion=99;string error;Check(!fs.TrySave(future,out error),"future schema refused");
            return "CampaignChecks passed: economy rollback, idempotency, quest rewards, equipment, dungeon success/failure/reload, level cap, atomic disk roundtrip, corrupt primary/backup and schema rejection. Fixtures: "+folder;
        }
    }
}
