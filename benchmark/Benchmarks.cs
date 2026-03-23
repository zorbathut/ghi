using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Dec;
using Ghi;
using System;
using System.Reflection;

namespace Ghi.Benchmark
{
    // ── Entity Components (20) ──────────────────────────────────────────

    public class Position : IRecordable
    {
        public float x;
        public float y;
        public float z;
        public void Record(Recorder recorder)
        {
            recorder.Record(ref x, "x");
            recorder.Record(ref y, "y");
            recorder.Record(ref z, "z");
        }
    }

    public class Velocity : IRecordable
    {
        public float dx;
        public float dy;
        public float dz;
        public void Record(Recorder recorder)
        {
            recorder.Record(ref dx, "dx");
            recorder.Record(ref dy, "dy");
            recorder.Record(ref dz, "dz");
        }
    }

    public class Health : IRecordable
    {
        public int current;
        public int max;
        public float regenRate;
        public void Record(Recorder recorder)
        {
            recorder.Record(ref current, "current");
            recorder.Record(ref max, "max");
            recorder.Record(ref regenRate, "regenRate");
        }
    }

    public class Stats : IRecordable
    {
        public int strength;
        public int agility;
        public int intelligence;
        public int endurance;
        public void Record(Recorder recorder)
        {
            recorder.Record(ref strength, "strength");
            recorder.Record(ref agility, "agility");
            recorder.Record(ref intelligence, "intelligence");
            recorder.Record(ref endurance, "endurance");
        }
    }

    public class Equipment : IRecordable
    {
        public int weaponId;
        public int armorId;
        public int accessoryId;
        public float totalWeight;
        public void Record(Recorder recorder)
        {
            recorder.Record(ref weaponId, "weaponId");
            recorder.Record(ref armorId, "armorId");
            recorder.Record(ref accessoryId, "accessoryId");
            recorder.Record(ref totalWeight, "totalWeight");
        }
    }

    public class Inventory : IRecordable
    {
        public int capacity;
        public int usedSlots;
        public float totalWeight;
        public void Record(Recorder recorder)
        {
            recorder.Record(ref capacity, "capacity");
            recorder.Record(ref usedSlots, "usedSlots");
            recorder.Record(ref totalWeight, "totalWeight");
        }
    }

    public class AI : IRecordable
    {
        public int state;
        public float aggroRange;
        public float leashRange;
        public bool alerted;
        public void Record(Recorder recorder)
        {
            recorder.Record(ref state, "state");
            recorder.Record(ref aggroRange, "aggroRange");
            recorder.Record(ref leashRange, "leashRange");
            recorder.Record(ref alerted, "alerted");
        }
    }

    public class Damage : IRecordable
    {
        public int baseDamage;
        public float critChance;
        public float critMultiplier;
        public void Record(Recorder recorder)
        {
            recorder.Record(ref baseDamage, "baseDamage");
            recorder.Record(ref critChance, "critChance");
            recorder.Record(ref critMultiplier, "critMultiplier");
        }
    }

    public class Collider : IRecordable
    {
        public float radius;
        public float height;
        public bool isTrigger;
        public void Record(Recorder recorder)
        {
            recorder.Record(ref radius, "radius");
            recorder.Record(ref height, "height");
            recorder.Record(ref isTrigger, "isTrigger");
        }
    }

    public class Physics : IRecordable
    {
        public float mass;
        public float drag;
        public float bounciness;
        public bool isKinematic;
        public void Record(Recorder recorder)
        {
            recorder.Record(ref mass, "mass");
            recorder.Record(ref drag, "drag");
            recorder.Record(ref bounciness, "bounciness");
            recorder.Record(ref isKinematic, "isKinematic");
        }
    }

    public class Experience : IRecordable
    {
        public int level;
        public int currentXp;
        public int xpToNext;
        public void Record(Recorder recorder)
        {
            recorder.Record(ref level, "level");
            recorder.Record(ref currentXp, "currentXp");
            recorder.Record(ref xpToNext, "xpToNext");
        }
    }

    public class Targeting : IRecordable
    {
        public float range;
        public float fieldOfView;
        public int priority;
        public void Record(Recorder recorder)
        {
            recorder.Record(ref range, "range");
            recorder.Record(ref fieldOfView, "fieldOfView");
            recorder.Record(ref priority, "priority");
        }
    }

    public class Cooldown : IRecordable
    {
        public float duration;
        public float remaining;
        public bool ready;
        public void Record(Recorder recorder)
        {
            recorder.Record(ref duration, "duration");
            recorder.Record(ref remaining, "remaining");
            recorder.Record(ref ready, "ready");
        }
    }

    public class Animation : IRecordable
    {
        public int currentFrame;
        public int totalFrames;
        public float speed;
        public bool looping;
        public void Record(Recorder recorder)
        {
            recorder.Record(ref currentFrame, "currentFrame");
            recorder.Record(ref totalFrames, "totalFrames");
            recorder.Record(ref speed, "speed");
            recorder.Record(ref looping, "looping");
        }
    }

    public class Audio : IRecordable
    {
        public float volume;
        public float pitch;
        public bool playing;
        public void Record(Recorder recorder)
        {
            recorder.Record(ref volume, "volume");
            recorder.Record(ref pitch, "pitch");
            recorder.Record(ref playing, "playing");
        }
    }

    public class Sprite : IRecordable
    {
        public int atlasIndex;
        public float scaleX;
        public float scaleY;
        public int sortOrder;
        public void Record(Recorder recorder)
        {
            recorder.Record(ref atlasIndex, "atlasIndex");
            recorder.Record(ref scaleX, "scaleX");
            recorder.Record(ref scaleY, "scaleY");
            recorder.Record(ref sortOrder, "sortOrder");
        }
    }

    public class StatusEffects : IRecordable
    {
        public int activeCount;
        public float totalDuration;
        public bool stunned;
        public void Record(Recorder recorder)
        {
            recorder.Record(ref activeCount, "activeCount");
            recorder.Record(ref totalDuration, "totalDuration");
            recorder.Record(ref stunned, "stunned");
        }
    }

    public class Movement : IRecordable
    {
        public float speed;
        public float acceleration;
        public bool grounded;
        public void Record(Recorder recorder)
        {
            recorder.Record(ref speed, "speed");
            recorder.Record(ref acceleration, "acceleration");
            recorder.Record(ref grounded, "grounded");
        }
    }

    public class Pathfinding : IRecordable
    {
        public int waypointIndex;
        public int totalWaypoints;
        public float recalcTimer;
        public bool hasPath;
        public void Record(Recorder recorder)
        {
            recorder.Record(ref waypointIndex, "waypointIndex");
            recorder.Record(ref totalWaypoints, "totalWaypoints");
            recorder.Record(ref recalcTimer, "recalcTimer");
            recorder.Record(ref hasPath, "hasPath");
        }
    }

    public class Loot : IRecordable
    {
        public int tableId;
        public float dropChance;
        public int minItems;
        public int maxItems;
        public void Record(Recorder recorder)
        {
            recorder.Record(ref tableId, "tableId");
            recorder.Record(ref dropChance, "dropChance");
            recorder.Record(ref minItems, "minItems");
            recorder.Record(ref maxItems, "maxItems");
        }
    }

    // ── Singleton Components (3) ────────────────────────────────────────

    public class GameState : IRecordable
    {
        public int tick;
        public float elapsed;
        public bool paused;
        public void Record(Recorder recorder)
        {
            recorder.Record(ref tick, "tick");
            recorder.Record(ref elapsed, "elapsed");
            recorder.Record(ref paused, "paused");
        }
    }

    public class WorldConfig : IRecordable
    {
        public float gravity;
        public float friction;
        public int maxEntities;
        public void Record(Recorder recorder)
        {
            recorder.Record(ref gravity, "gravity");
            recorder.Record(ref friction, "friction");
            recorder.Record(ref maxEntities, "maxEntities");
        }
    }

    public class SpawnQueue : IRecordable
    {
        public int pendingCount;
        public float nextSpawnTime;
        public void Record(Recorder recorder)
        {
            recorder.Record(ref pendingCount, "pendingCount");
            recorder.Record(ref nextSpawnTime, "nextSpawnTime");
        }
    }

    // ── Dec Static References ───────────────────────────────────────────

    public static class Decs
    {
        static Decs() { }

        // Entity decs
        public static EntityDec PlayerDec;
        public static EntityDec EnemyDec;
        public static EntityDec ProjectileDec;
        public static EntityDec NPCDec;
        public static EntityDec PickupDec;
        public static EntityDec ObstacleDec;
        public static EntityDec EffectDec;
        public static EntityDec TriggerDec;
        public static EntityDec SpawnerDec;
        public static EntityDec VehicleDec;
        public static EntityDec TurretDec;
        public static EntityDec DecorationDec;
    }

    // ── Benchmarks ──────────────────────────────────────────────────────

    [MemoryDiagnoser]
    public class GhiBenchmarks
    {
        private Environment emptyEnv;
        private Environment populatedEnv;

        private const string DecXml = @"
<Decs>
    <!-- Singleton components -->
    <ComponentDec decName=""GameStateDec"">
        <type>GameState</type>
        <singleton>true</singleton>
    </ComponentDec>
    <ComponentDec decName=""WorldConfigDec"">
        <type>WorldConfig</type>
        <singleton>true</singleton>
    </ComponentDec>
    <ComponentDec decName=""SpawnQueueDec"">
        <type>SpawnQueue</type>
        <singleton>true</singleton>
    </ComponentDec>

    <!-- Entity components -->
    <ComponentDec decName=""PositionDec""><type>Position</type></ComponentDec>
    <ComponentDec decName=""VelocityDec""><type>Velocity</type></ComponentDec>
    <ComponentDec decName=""HealthDec""><type>Health</type></ComponentDec>
    <ComponentDec decName=""StatsDec""><type>Stats</type></ComponentDec>
    <ComponentDec decName=""EquipmentDec""><type>Equipment</type></ComponentDec>
    <ComponentDec decName=""InventoryDec""><type>Inventory</type></ComponentDec>
    <ComponentDec decName=""AIDec""><type>AI</type></ComponentDec>
    <ComponentDec decName=""DamageDec""><type>Damage</type></ComponentDec>
    <ComponentDec decName=""ColliderDec""><type>Collider</type></ComponentDec>
    <ComponentDec decName=""PhysicsDec""><type>Physics</type></ComponentDec>
    <ComponentDec decName=""ExperienceDec""><type>Experience</type></ComponentDec>
    <ComponentDec decName=""TargetingDec""><type>Targeting</type></ComponentDec>
    <ComponentDec decName=""CooldownDec""><type>Cooldown</type></ComponentDec>
    <ComponentDec decName=""AnimationDec""><type>Animation</type></ComponentDec>
    <ComponentDec decName=""AudioDec""><type>Audio</type></ComponentDec>
    <ComponentDec decName=""SpriteDec""><type>Sprite</type></ComponentDec>
    <ComponentDec decName=""StatusEffectsDec""><type>StatusEffects</type></ComponentDec>
    <ComponentDec decName=""MovementDec""><type>Movement</type></ComponentDec>
    <ComponentDec decName=""PathfindingDec""><type>Pathfinding</type></ComponentDec>
    <ComponentDec decName=""LootDec""><type>Loot</type></ComponentDec>

    <!-- Entity types (12) -->
    <EntityDec decName=""PlayerDec"">
        <components>
            <li>PositionDec</li><li>VelocityDec</li><li>HealthDec</li>
            <li>StatsDec</li><li>EquipmentDec</li><li>InventoryDec</li>
        </components>
    </EntityDec>
    <EntityDec decName=""EnemyDec"">
        <components>
            <li>PositionDec</li><li>VelocityDec</li><li>HealthDec</li>
            <li>AIDec</li><li>DamageDec</li>
        </components>
    </EntityDec>
    <EntityDec decName=""ProjectileDec"">
        <components>
            <li>PositionDec</li><li>VelocityDec</li>
            <li>DamageDec</li><li>ColliderDec</li>
        </components>
    </EntityDec>
    <EntityDec decName=""NPCDec"">
        <components>
            <li>PositionDec</li><li>HealthDec</li>
            <li>AIDec</li><li>InventoryDec</li>
        </components>
    </EntityDec>
    <EntityDec decName=""PickupDec"">
        <components>
            <li>PositionDec</li><li>ColliderDec</li><li>ExperienceDec</li>
        </components>
    </EntityDec>
    <EntityDec decName=""ObstacleDec"">
        <components>
            <li>PositionDec</li><li>ColliderDec</li><li>PhysicsDec</li>
        </components>
    </EntityDec>
    <EntityDec decName=""EffectDec"">
        <components>
            <li>PositionDec</li><li>AnimationDec</li><li>AudioDec</li>
        </components>
    </EntityDec>
    <EntityDec decName=""TriggerDec"">
        <components>
            <li>PositionDec</li><li>ColliderDec</li><li>TargetingDec</li>
        </components>
    </EntityDec>
    <EntityDec decName=""SpawnerDec"">
        <components>
            <li>PositionDec</li><li>CooldownDec</li><li>TargetingDec</li>
        </components>
    </EntityDec>
    <EntityDec decName=""VehicleDec"">
        <components>
            <li>PositionDec</li><li>VelocityDec</li><li>PhysicsDec</li>
            <li>HealthDec</li><li>ColliderDec</li>
        </components>
    </EntityDec>
    <EntityDec decName=""TurretDec"">
        <components>
            <li>PositionDec</li><li>TargetingDec</li><li>DamageDec</li>
            <li>CooldownDec</li><li>HealthDec</li>
        </components>
    </EntityDec>
    <EntityDec decName=""DecorationDec"">
        <components>
            <li>PositionDec</li><li>SpriteDec</li><li>AnimationDec</li>
        </components>
    </EntityDec>
</Decs>";

        [GlobalSetup]
        public void Setup()
        {
            Dec.Config.WarningHandler = _ => { };
            Dec.Config.ErrorHandler = _ => { };
            Dec.Config.ExceptionHandler = _ => { };
            Dec.Config.UsingNamespaces = new string[] { "Ghi", "Ghi.Benchmark" };

            // UnitTestParameters is internal, so we construct it via reflection
            var utpType = typeof(Dec.Config).GetNestedType("UnitTestParameters", BindingFlags.NonPublic);
            var utp = Activator.CreateInstance(utpType);
            utpType.GetField("explicitStaticRefs").SetValue(utp, new Type[] { typeof(Decs) });
            typeof(Dec.Config).GetField("TestParameters", BindingFlags.NonPublic | BindingFlags.Static)
                .SetValue(null, utp);

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, DecXml);
            parser.Finish();

            Environment.Init();

            // Pre-build empty env for clone benchmark
            emptyEnv = new Environment();
            var emptyScope = new Environment.Scope(emptyEnv);

            // Pre-build populated env for clone benchmark (~100 entities)
            populatedEnv = new Environment();
            using (var popScope = new Environment.Scope(populatedEnv))
            {
                // 12 entity types, ~8 each = 96 entities
                var entityDecs = new[]
                {
                    Decs.PlayerDec, Decs.EnemyDec, Decs.ProjectileDec, Decs.NPCDec,
                    Decs.PickupDec, Decs.ObstacleDec, Decs.EffectDec, Decs.TriggerDec,
                    Decs.SpawnerDec, Decs.VehicleDec, Decs.TurretDec, Decs.DecorationDec,
                };

                foreach (var dec in entityDecs)
                {
                    for (int i = 0; i < 8; i++)
                    {
                        populatedEnv.Add(dec);
                    }
                }
            }

            // Restore the empty scope as "active" (benchmarks will manage their own scopes)
            // Actually, we need to leave scopes clean. Dispose the empty scope too.
            emptyScope.Dispose();
        }

        [Benchmark]
        public Environment CreateEnvironment()
        {
            return new Environment();
        }

        [Benchmark]
        public Environment CloneFreshEnvironment()
        {
            return Dec.Recorder.Clone(emptyEnv);
        }

        [Benchmark]
        public Environment ClonePopulatedEnvironment()
        {
            return Dec.Recorder.Clone(populatedEnv);
        }
    }

    public class Program
    {
        public static void Main(string[] args)
        {
            BenchmarkRunner.Run<GhiBenchmarks>(args: args);
        }
    }
}
