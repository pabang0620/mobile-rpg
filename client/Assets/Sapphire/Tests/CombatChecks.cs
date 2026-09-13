using System;
using System.Linq;

namespace Sapphire.Tests
{
    public static class CombatChecks
    {
        private static void Check(bool value,string name) { if(!value)throw new Exception("Combat check: "+name); }
        private static void Ticks(CombatWorld world,int count,Vec2 direction=new Vec2()) { for(int i=0;i<count;i++)world.Step(direction); }
        public static void RunAll()
        {
            var a=new CombatWorld("town");var start=a.Player.Position;a.Step(new Vec2(1,0));Check(a.Player.Position.X==start.X,"grid move starts without teleport");Ticks(a,CombatWorld.GridMoveTicks,new Vec2());Check(a.Player.Position.X==start.X+1&&a.Player.Position.Y==start.Y,"one input moves exactly one tile");
            var b=new CombatWorld("town");b.Step(new Vec2(1,1));Ticks(b,CombatWorld.GridMoveTicks,new Vec2());Check(b.Player.Position.X==start.X&&b.Player.Position.Y==start.Y+1,"diagonal input resolves to one cardinal tile");
            Ticks(a,1000,new Vec2(-1,0));Check(a.Player.Position.X>=1,"grid world boundary");
            var collision=new CombatWorld("town",120,100,18,4,5);collision.Obstacles.Add(new CombatObstacle(4,4,1,3));
            collision.Player.Facing=new Vec2(1,0);collision.Cast(2);Check(collision.Player.Position.X<4,"blink cannot tunnel through obstacle");
            var locked=new CombatWorld("town");int mp=locked.Player.Mp;Check(locked.Cast(3)==CommandResult.Locked&&locked.Player.Mp==mp,"locked command has no cost");
            var skill=new CombatWorld("town",120,100,18,4,5);Check(skill.Cast(3)==CommandResult.Accepted,"shield accepted");mp=skill.Player.Mp;
            Check(skill.Cast(3)==CommandResult.Cooldown&&skill.Player.Mp==mp,"cooldown rejection has no cost");Ticks(skill,240);Check(skill.ShieldRemaining==0,"shield expires in simulation ticks");
            var fight=new CombatWorld("town",120,100,999,4,5);var target=fight.Spawn("goblin",new Vec2(4.5f,5.5f));target.Recover=1000;
            fight.Attack();Ticks(fight,40);Check(target.Dead,"projectile damages target");Check(fight.Events.Count(e=>e.Kind=="Killed")==1,"single kill event");
            fight.Attack();Ticks(fight,40);Check(fight.Events.Count(e=>e.Kind=="Killed")==1,"dead target cannot grant duplicate reward");
            var drained=fight.DrainEvents();Check(drained.Length>0&&fight.DrainEvents().Length==0,"events drain exactly once");
            var blocked=new CombatWorld("town",120,100,999);blocked.Obstacles.Add(new CombatObstacle(4,4,1,3));var behind=blocked.Spawn("goblin",new Vec2(5.3f,5.5f));behind.Recover=1000;
            blocked.Attack();Ticks(blocked,60);Check(behind.Hp==behind.MaxHp,"projectile cannot hit through wall");
            var dodge=new CombatWorld("town");dodge.Attack();Check(dodge.Dodge()==CommandResult.Busy,"cannot dodge startup");Ticks(dodge,11);Check(dodge.Dodge()==CommandResult.Accepted,"can dodge recovery");
            var enemy=new CombatWorld("town");var goblin=enemy.Spawn("goblin",new Vec2(3.5f,5.5f));goblin.Recover=0;enemy.Step(new Vec2());
            Check(goblin.Telegraph>=.45f&&enemy.Player.Hp==120,"melee telegraph before damage");Ticks(enemy,30);Check(enemy.Player.Hp<120,"melee resolves after telegraph");
            var boss=new CombatWorld("boss");Check(boss.Enemies.Count==1&&boss.Enemies[0].Boss,"boss zone spawn");
            var guardian=boss.Enemies[0];guardian.Position=new Vec2(4,5.5f);guardian.Hp=300;guardian.Pattern=1;guardian.Recover=0;
            boss.Step(new Vec2());Check(guardian.Telegraph>=.7f,"boss warning duration");Ticks(boss,48);Check(boss.Enemies.Count==3,"phase two summons");
            var path=new CombatWorld("town",10000);path.Player.Position=new Vec2(10,3.5f);path.Obstacles.Add(new CombatObstacle(8,3,1,1));
            var walker=path.Spawn("goblin",new Vec2(7,3.5f));walker.Recover=0;Ticks(path,300);Check((walker.Position-path.Player.Position).Length<1.2f,"A star routes around obstacle");
            var mana=new CombatWorld("town",120,0,18,4,5);Check(mana.Cast(0)==CommandResult.InsufficientResource&&mana.Cooldowns[0]==0,"insufficient mana does not consume cooldown");
        }
    }
}
