using System;
using System.Collections.Generic;

namespace Sapphire
{
    public struct Vec2
    {
        public float X, Y;
        public Vec2(float x, float y) { X=x; Y=y; }
        public float Length { get { return (float)Math.Sqrt(X*X+Y*Y); } }
        public Vec2 Normalized { get { float n=Length; return n>0.0001f ? this/n : new Vec2(); } }
        public static Vec2 operator +(Vec2 a,Vec2 b) { return new Vec2(a.X+b.X,a.Y+b.Y); }
        public static Vec2 operator -(Vec2 a,Vec2 b) { return new Vec2(a.X-b.X,a.Y-b.Y); }
        public static Vec2 operator *(Vec2 a,float b) { return new Vec2(a.X*b,a.Y*b); }
        public static Vec2 operator /(Vec2 a,float b) { return new Vec2(a.X/b,a.Y/b); }
        public static float Dot(Vec2 a,Vec2 b) { return a.X*b.X+a.Y*b.Y; }
    }
    public enum CommandResult { Accepted, Dead, Cooldown, InsufficientResource, Locked, Busy }
    public sealed class CombatActor
    {
        public int Id, Hp,MaxHp,Mp,MaxMp,Attack,Defense,Level=1;
        public string Kind,Name;
        public Vec2 Position,Facing=new Vec2(1,0),TelegraphCenter;
        public bool Dead { get { return Hp<=0; } }
        public bool Boss;
        public float Telegraph,TelegraphRadius,Flash;
        internal int Windup,Recover,Slow,Pattern;
        internal Vec2 Target;
        internal int PathTicks;
        internal Vec2 PathTarget,Waypoint;
    }
    public sealed class CombatProjectile
    {
        public int Id,Damage;
        public Vec2 Position,Direction;
        public bool Hostile;
        public float Remaining;
    }
    public sealed class CombatEvent
    {
        public string Id,Kind,EnemyKind;
        public long Tick;
        public int EntityId,Amount,Xp,Gold;
        public Vec2 Position;
    }
    public struct CombatObstacle
    {
        public float X,Y,Width,Height;
        public CombatObstacle(float x,float y,float width,float height) { X=x;Y=y;Width=width;Height=height; }
    }
    /// <summary>Deterministic 60Hz simulation. UI reads state and drains events after each tick.</summary>
    public sealed class CombatWorld
    {
        public const float Dt=1f/60f;
        public CombatActor Player { get; private set; }
        public readonly List<CombatActor> Enemies=new List<CombatActor>();
        public readonly List<CombatProjectile> Projectiles=new List<CombatProjectile>();
        public readonly List<CombatEvent> Events=new List<CombatEvent>();
        public readonly List<CombatObstacle> Obstacles=new List<CombatObstacle>();
        public readonly float[] Cooldowns=new float[4];
        public readonly float[] MaxCooldowns={3,7,5,10};
        private readonly int[] cooldownTicks=new int[4];
        private readonly int[] costs={12,24,18,22};
        public const int GridMoveTicks=9;
        private int sequence,nextId=1,attackTicks,attackTotal,startup,combo,comboWindow,buffer,skill=-1,dodgeTicks,dodgeCd,invulnerable,shieldTicks,shield,autoPause,manaTicks,gridMoveTicks;
        private Vec2 attackDirection,dodgeDirection,gridFrom,gridTo;
        private string session=Guid.NewGuid().ToString("N");
        public string ZoneId { get; private set; }
        public long Tick { get; private set; }
        public bool Auto;
        public float AttackProgress { get { return attackTotal==0 ? 0 : 1f-(float)attackTicks/attackTotal; } }
        public float DodgeRemaining { get { return dodgeCd*Dt; } }
        public float ShieldRemaining { get { return shieldTicks*Dt; } }
        public bool Cleared { get { return Enemies.TrueForAll(a=>a.Dead); } }
        public CombatWorld(string zoneId,int maxHp=120,int maxMp=100,int attack=18,int defense=4,int level=1)
        {
            Player=new CombatActor{Id=0,Kind="player",Name="사파이어"};
            SetStats(maxHp,maxMp,attack,defense,level,true); EnterZone(zoneId);
        }
        public void SetStats(int maxHp,int maxMp,int attack,int defense,int level,bool heal=false)
        {
            int gain=Math.Max(0,maxHp-Player.MaxHp); Player.MaxHp=Math.Max(1,maxHp);Player.MaxMp=Math.Max(0,maxMp);
            Player.Attack=Math.Max(1,attack);Player.Defense=Math.Max(0,defense);Player.Level=Math.Max(1,level);
            Player.Hp=heal?Player.MaxHp:Math.Min(Player.MaxHp,Player.Hp+gain);Player.Mp=heal?Player.MaxMp:Math.Min(Player.MaxMp,Player.Mp);
        }
        public void Restore(int hp,int mp) { if(Player.Dead)return;Player.Hp=Math.Min(Player.MaxHp,Player.Hp+Math.Max(0,hp));Player.Mp=Math.Min(Player.MaxMp,Player.Mp+Math.Max(0,mp)); }
        public CombatEvent[] DrainEvents() { var result=Events.ToArray();Events.Clear();return result; }
        public void EnterZone(string zoneId)
        {
            ZoneId=zoneId??"town";Enemies.Clear();Projectiles.Clear();Events.Clear();Obstacles.Clear();
            Player.Position=new Vec2(3,6);Player.Facing=new Vec2(0,-1); attackTicks=attackTotal=buffer=combo=comboWindow=dodgeTicks=invulnerable=shieldTicks=shield=gridMoveTicks=0; skill=-1;Auto=false;
            string z=ZoneId.ToLowerInvariant(); if(z.Contains("town"))return;
            if(z.Contains("boss")||z.Contains("dungeon3")) { Spawn("boss",new Vec2(14,5.5f));return; }
            Obstacles.Add(new CombatObstacle(8,3,1.3f,1.4f));Obstacles.Add(new CombatObstacle(11,7.4f,1.3f,1.2f));
            if(z.Contains("elite")||z.Contains("dungeon2")) { Spawn("elite",new Vec2(14,5));Spawn("spirit",new Vec2(15,8));return; }
            Spawn("goblin",new Vec2(10,5));Spawn("goblin",new Vec2(14,2));Spawn("spirit",new Vec2(15,8));Spawn("wolf",new Vec2(17,5));
            if(z.Contains("2")) { Spawn("wolf",new Vec2(15,3));Spawn("spirit",new Vec2(18,8)); }
        }
        public CombatActor Spawn(string kind,Vec2 position)
        {
            int hp=50,atk=10;string name="고블린";
            if(kind=="spirit"){hp=40;atk=12;name="균열 정령";} if(kind=="wolf"){hp=60;atk=14;name="숲 늑대";}
            if(kind=="elite"){hp=180;atk=18;name="정예 수문장";} if(kind=="boss"){hp=650;atk=22;name="균열 수호자";}
            var a=new CombatActor{Id=nextId++,Kind=kind,Name=name,Position=position,Hp=hp,MaxHp=hp,Attack=atk,Boss=kind=="boss",Recover=30};Enemies.Add(a);return a;
        }
        public CommandResult Attack() { autoPause=180;return BeginAttack(); }
        private CommandResult BeginAttack()
        {
            if(Player.Dead)return CommandResult.Dead;if(dodgeTicks>0)return CommandResult.Busy;
            if(attackTicks>0){buffer=11;return CommandResult.Accepted;}
            combo=comboWindow>0?combo%3+1:1;startup=combo==3?12:7;attackTotal=attackTicks=startup+3+(combo==3?18:12);skill=-1;
            attackDirection=Aim(3);return CommandResult.Accepted;
        }
        public CommandResult Cast(int slot)
        {
            autoPause=180;if(Player.Dead)return CommandResult.Dead;if(slot<0||slot>3||Player.Level<slot+1)return CommandResult.Locked;
            if(cooldownTicks[slot]>0)return CommandResult.Cooldown;if(attackTicks>0||dodgeTicks>0)return CommandResult.Busy;
            if(Player.Mp<costs[slot])return CommandResult.InsufficientResource;
            Player.Mp-=costs[slot];cooldownTicks[slot]=(int)(MaxCooldowns[slot]*60);Cooldowns[slot]=MaxCooldowns[slot];combo=comboWindow=buffer=0;
            if(slot==2){GridTeleport(Player.Facing,2);invulnerable=15;Emit("Blink",Player,0);}
            else if(slot==3){shield=40;shieldTicks=240;Emit("Shield",Player,40);}
            else {skill=slot;startup=slot==0?11:15;attackTotal=attackTicks=startup+15;attackDirection=Aim(5);}
            return CommandResult.Accepted;
        }
        public CommandResult Dodge()
        {
            autoPause=180;if(Player.Dead)return CommandResult.Dead;if(dodgeCd>0)return CommandResult.Cooldown;
            if(attackTicks>0&&attackTotal-attackTicks<startup+3)return CommandResult.Busy;
            attackTicks=attackTotal=buffer=0;dodgeTicks=13;invulnerable=11;dodgeCd=72;dodgeDirection=Player.Facing;GridTeleport(dodgeDirection,1);return CommandResult.Accepted;
        }
        private Vec2 Aim(float range)
        {
            CombatActor best=null;float distance=range;
            foreach(var e in Enemies){float d=(e.Position-Player.Position).Length;if(!e.Dead&&d<distance&&Visible(Player.Position,e.Position)){best=e;distance=d;}}
            if(best!=null)Player.Facing=(best.Position-Player.Position).Normalized;return Player.Facing;
        }
        public void Step(Vec2 movement)
        {
            Tick++;if(Player.Dead)return;
            if(movement.Length>0.01f)autoPause=180;else if(autoPause>0)autoPause--;
            if(Auto&&autoPause==0&&!ZoneId.ToLowerInvariant().Contains("boss"))
            {
                CombatActor target=null;float distance=float.MaxValue;foreach(var a in Enemies)if(!a.Dead&&(a.Position-Player.Position).Length<distance){target=a;distance=(a.Position-Player.Position).Length;}
                if(target!=null){if(distance>2.5f)movement=Navigate(Player,target.Position);else BeginAttack();}
            }
            if(movement.Length>1)movement=movement.Normalized;
            movement=Cardinal(movement);
            if(dodgeTicks>0){dodgeTicks--;}
            if(gridMoveTicks>0)
            {
                gridMoveTicks--;float t=1f-(float)gridMoveTicks/GridMoveTicks;Player.Position=gridFrom+(gridTo-gridFrom)*t;
                if(gridMoveTicks==0)Player.Position=gridTo;
            }
            else if(dodgeTicks==0&&movement.Length>.01f&&attackTicks==0)BeginGridMove(movement);
            if(invulnerable>0)invulnerable--;if(dodgeCd>0)dodgeCd--;if(shieldTicks>0&&--shieldTicks==0)shield=0;
            if(comboWindow>0)comboWindow--;if(buffer>0)buffer--;
            for(int i=0;i<4;i++){if(cooldownTicks[i]>0)cooldownTicks[i]--;Cooldowns[i]=cooldownTicks[i]*Dt;}
            if(++manaTicks>=30){manaTicks=0;Player.Mp=Math.Min(Player.MaxMp,Player.Mp+1);}
            Player.Flash=Math.Max(0,Player.Flash-Dt);
            if(attackTicks>0)
            {
                attackTicks--;if(attackTotal-attackTicks==startup)ReleaseAttack();
                if(attackTicks==0){attackTotal=0;comboWindow=36;if(buffer>0){buffer=0;BeginAttack();}}
            }
            // Snapshot prevents boss summoning from mutating the active iteration.
            foreach(var a in Enemies.ToArray()) {a.Flash=Math.Max(0,a.Flash-Dt);if(!a.Dead)StepEnemy(a);}
            StepProjectiles();
        }
        private void ReleaseAttack()
        {
            if(skill==1){foreach(var e in Enemies)if(!e.Dead&&(e.Position-Player.Position).Length<=2&&Visible(Player.Position,e.Position)){Hit(e,(int)(Player.Attack*1.4f));e.Slow=120;}Emit("Frost",Player,0);}
            else {float multiplier=skill==0?2:combo==3?1.5f:combo==2?1.1f:1;Fire(Player.Position,attackDirection,(int)(Player.Attack*multiplier),false,skill==0?5:3);}
        }
        private void StepEnemy(CombatActor a)
        {
            if(a.Slow>0)a.Slow--;
            if(a.Windup>0)
            {
                a.Telegraph=a.Windup*Dt;if(--a.Windup==0)
                {
                    a.Telegraph=0;
                    if(a.Kind=="spirit") {if(Visible(a.Position,a.Target))Fire(a.Position,(a.Target-a.Position).Normalized,a.Attack,true,7);}
                    else if(a.Kind=="wolf"||(a.Boss&&a.Pattern%3==1)) {Vec2 old=a.Position;MoveActor(a,(a.Target-a.Position).Normalized*(a.Boss?4:2.6f));if(SegmentDistance(Player.Position,old,a.Position)<.6f)HitPlayer(a.Attack);}
                    else if((Player.Position-a.TelegraphCenter).Length<=a.TelegraphRadius)HitPlayer(a.Attack);
                    if(a.Boss&&a.Hp<=a.MaxHp/2&&a.Pattern%3==2){Spawn("goblin",a.Position+new Vec2(-1,1));Spawn("spirit",a.Position+new Vec2(1,-1));}
                    a.Recover=a.Boss&&a.Hp<=a.MaxHp/2?36:65;
                }return;
            }
            if(a.Recover>0){a.Recover--;return;}
            float range=a.Kind=="spirit"?5:a.Kind=="wolf"?3:a.Boss?3.3f:1.1f;
            Vec2 delta=Player.Position-a.Position;a.Facing=delta.Normalized;
            if(delta.Length<=range&&Visible(a.Position,Player.Position))
            {
                a.Pattern++;a.Windup=a.Boss?48:30;a.Target=Player.Position;
                a.TelegraphRadius=a.Boss?2.5f:a.Kind=="wolf"?2.6f:a.Kind=="spirit"?.5f:1.2f;
                a.TelegraphCenter=a.Position;a.Telegraph=a.Windup*Dt;
            }
            else MoveActor(a,Navigate(a,Player.Position)*(Dt*(a.Kind=="wolf"?2.6f:1.5f)*(a.Slow>0?.6f:1)));
        }
        private Vec2 Navigate(CombatActor a,Vec2 target)
        {
            if(Visible(a.Position,target))return (target-a.Position).Normalized;
            if(a.PathTicks-->0&&(a.PathTarget-target).Length<.75f&&(a.Waypoint-a.Position).Length>.08f)return (a.Waypoint-a.Position).Normalized;
            a.PathTicks=12;a.PathTarget=target;
            const int width=80,height=44,count=width*height;
            int sx=Math.Max(1,Math.Min(width-2,(int)(a.Position.X*4))),sy=Math.Max(1,Math.Min(height-2,(int)(a.Position.Y*4)));
            int tx=Math.Max(1,Math.Min(width-2,(int)(target.X*4))),ty=Math.Max(1,Math.Min(height-2,(int)(target.Y*4)));
            int start=sy*width+sx,goal=ty*width+tx;
            var parent=new int[count];var score=new int[count];var closed=new bool[count];var open=new List<int>();
            for(int i=0;i<count;i++){parent[i]=-1;score[i]=int.MaxValue;}score[start]=0;open.Add(start);
            int[] dx={1,-1,0,0},dy={0,0,1,-1};bool found=false;
            while(open.Count>0)
            {
                int bestIndex=0,bestF=int.MaxValue;for(int i=0;i<open.Count;i++){int node=open[i],f=score[node]+Math.Abs(node%width-tx)+Math.Abs(node/width-ty);if(f<bestF){bestF=f;bestIndex=i;}}
                int current=open[bestIndex];open.RemoveAt(bestIndex);if(current==goal){found=true;break;}closed[current]=true;
                for(int j=0;j<4;j++)
                {
                    int x=current%width+dx[j],y=current/width+dy[j];if(x<1||x>=width-1||y<1||y>=height-1)continue;int n=y*width+x;
                    if(closed[n]||Blocked(new Vec2((x+.5f)/4,(y+.5f)/4)))continue;
                    int cost=score[current]+1;if(cost>=score[n])continue;score[n]=cost;parent[n]=current;if(!open.Contains(n))open.Add(n);
                }
            }
            if(!found)return new Vec2();int step=goal;while(parent[step]!=start&&parent[step]>=0)step=parent[step];
            a.Waypoint=new Vec2((step%width+.5f)/4,(step/width+.5f)/4);return (a.Waypoint-a.Position).Normalized;
        }
        private void Fire(Vec2 position,Vec2 direction,int damage,bool hostile,float range)
        {Projectiles.Add(new CombatProjectile{Id=nextId++,Position=position,Direction=direction.Normalized,Damage=damage,Hostile=hostile,Remaining=range});}
        private void StepProjectiles()
        {
            for(int i=Projectiles.Count-1;i>=0;i--)
            {
                var p=Projectiles[i];Vec2 old=p.Position;float travel=Math.Min(p.Remaining,(p.Hostile?5:8)*Dt);p.Position+=p.Direction*travel;p.Remaining-=travel;
                bool remove=!Visible(old,p.Position);
                if(!remove)
                {
                    if(p.Hostile){if(SegmentDistance(Player.Position,old,p.Position)<.34f){HitPlayer(p.Damage);remove=true;}}
                    else {CombatActor closest=null;float closestDistance=float.MaxValue;foreach(var e in Enemies)if(!e.Dead&&SegmentDistance(e.Position,old,p.Position)<.42f){float d=(e.Position-old).Length;if(d<closestDistance){closest=e;closestDistance=d;}}if(closest!=null){Hit(closest,p.Damage);remove=true;}}
                }
                if(remove||p.Remaining<=0)Projectiles.RemoveAt(i);
            }
        }
        private void HitPlayer(int raw)
        {
            if(Player.Dead||invulnerable>0)return;int amount=Math.Max(1,raw-Player.Defense),absorbed=Math.Min(shield,amount);shield-=absorbed;amount-=absorbed;
            if(shield==0)shieldTicks=0;if(amount==0)return;Player.Hp=Math.Max(0,Player.Hp-amount);Player.Flash=.18f;Emit("Damage",Player,amount);
            if(Player.Dead){attackTicks=attackTotal=buffer=0;Auto=false;Emit("PlayerDied",Player,0);}
        }
        private void Hit(CombatActor a,int raw)
        {
            if(a.Dead)return;int damage=Math.Max(1,raw-a.Defense);a.Hp=Math.Max(0,a.Hp-damage);a.Flash=.18f;
            if(!a.Boss){a.Windup=0;a.Telegraph=0;a.Recover=12;}Emit("Damage",a,damage);
            if(a.Dead) {a.Telegraph=0;var e=Emit("Killed",a,0);e.Xp=a.Kind=="boss"?150:a.Kind=="elite"?50:a.Kind=="wolf"?20:a.Kind=="spirit"?18:15;e.Gold=a.Kind=="boss"?60:a.Kind=="elite"?20:a.Kind=="wolf"?6:a.Kind=="spirit"?5:4;}
        }
        private CombatEvent Emit(string kind,CombatActor a,int amount)
        {var e=new CombatEvent{Id=session+":"+(++sequence),Tick=Tick,EntityId=a.Id,Kind=kind,EnemyKind=a.Kind,Amount=amount,Position=a.Position};Events.Add(e);return e;}
        private bool Blocked(Vec2 p)
        {
            const float r=.22f;if(p.X<r||p.X>20-r||p.Y<r||p.Y>11-r)return true;
            foreach(var b in Obstacles)if(p.X>b.X-r&&p.X<b.X+b.Width+r&&p.Y>b.Y-r&&p.Y<b.Y+b.Height+r)return true;return false;
        }
        private void MoveActor(CombatActor a,Vec2 delta)
        {
            int steps=Math.Max(1,(int)Math.Ceiling(delta.Length/.08f));Vec2 d=delta/steps;
            for(int i=0;i<steps;i++){Vec2 x=a.Position+new Vec2(d.X,0);if(!Blocked(x))a.Position=x;Vec2 y=a.Position+new Vec2(0,d.Y);if(!Blocked(y))a.Position=y;}
        }
        private bool Visible(Vec2 from,Vec2 to)
        {int steps=Math.Max(1,(int)Math.Ceiling((to-from).Length/.08f));for(int i=1;i<=steps;i++)if(Blocked(from+(to-from)*((float)i/steps)))return false;return true;}
        private static float SegmentDistance(Vec2 p,Vec2 a,Vec2 b)
        {Vec2 ab=b-a;float denominator=Vec2.Dot(ab,ab);float t=denominator<.00001f?0:Math.Max(0,Math.Min(1,Vec2.Dot(p-a,ab)/denominator));return (p-(a+ab*t)).Length;}
        private static Vec2 Cardinal(Vec2 input)
        {
            if(input.Length<.01f)return new Vec2();
            if(Math.Abs(input.X)>Math.Abs(input.Y))return new Vec2(input.X<0?-1:1,0);
            return new Vec2(0,input.Y<0?-1:1);
        }
        private void BeginGridMove(Vec2 direction)
        {
            direction=Cardinal(direction);Player.Facing=direction;Vec2 target=new Vec2((float)Math.Round(Player.Position.X+direction.X),(float)Math.Round(Player.Position.Y+direction.Y));
            if(Blocked(target))return;gridFrom=Player.Position;gridTo=target;gridMoveTicks=GridMoveTicks;
        }
        private void GridTeleport(Vec2 direction,int tiles)
        {
            direction=Cardinal(direction);Vec2 result=new Vec2((float)Math.Round(Player.Position.X),(float)Math.Round(Player.Position.Y));
            for(int i=0;i<tiles;i++){Vec2 next=result+direction;if(Blocked(next))break;result=next;}Player.Position=result;gridMoveTicks=0;
        }
    }
}
