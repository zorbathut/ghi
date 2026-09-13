using Dec;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;

namespace Ghi.Test
{
    public class Hooks : Base
    {
        public Hooks(EmitMode emitMode) : base(emitMode) { }

        // Every hook in this file appends (tag, entity) here, so tests can assert on exact firing order and on which entity each hook saw.
        public static List<(string tag, Entity entity)> Log = new();

        public static List<string> Tags()
        {
            return Log.Select(l => l.tag).ToList();
        }

        [SetUp]
        public void ClearLog()
        {
            Log.Clear();
            HookedA.SelfMismatches = 0;
        }

        public class HookedA : IRecordable, IOnAdd, IOnRemove
        {
            public int value;

            // what the most recent OnAdd observed about its entity
            public static bool SeenValid;
            public static int SeenValue;
            public static bool SeenEqualsHeld;
            public static Entity Held;

            // times a hook was invoked on an instance that isn't the one its entity actually holds, which is what a stale dispatch index would produce
            public static int SelfMismatches;

            public void Record(Dec.Recorder recorder)
            {
                recorder.Record(ref value, "value");
            }

            private void CheckSelf(Entity entity)
            {
                if (!ReferenceEquals(this, entity.ComponentRO<HookedA>()))
                {
                    ++SelfMismatches;
                }
            }

            public void OnAdd(Entity entity)
            {
                SeenValid = entity.IsValid();
                SeenValue = entity.ComponentRO<HookedA>().value;
                SeenEqualsHeld = entity == Held;
                CheckSelf(entity);
                Log.Add(("A.add", entity));
            }

            public void OnRemove(Entity entity)
            {
                CheckSelf(entity);
                Log.Add(("A.remove", entity));
            }
        }

        public class HookedB : IRecordable, IOnAdd, IOnRemove
        {
            public void Record(Dec.Recorder recorder) { }

            public void OnAdd(Entity entity)
            {
                Log.Add(("B.add", entity));
            }

            public void OnRemove(Entity entity)
            {
                Log.Add(("B.remove", entity));
            }
        }

        public class HookedGlobal : IRecordable, IOnAddGlobal, IOnRemoveGlobal
        {
            public void Record(Dec.Recorder recorder) { }

            public void OnAddGlobal(Entity entity)
            {
                Log.Add(("G.add", entity));
            }

            public void OnRemoveGlobal(Entity entity)
            {
                Log.Add(("G.remove", entity));
            }
        }

        private const string DecA = @"
            <ComponentDec decName=""A"">
                <type>HookedA</type>
            </ComponentDec>";

        private const string DecB = @"
            <ComponentDec decName=""B"">
                <type>HookedB</type>
            </ComponentDec>";

        private const string DecG = @"
            <ComponentDec decName=""G"">
                <type>HookedGlobal</type>
                <singleton>true</singleton>
            </ComponentDec>";

        private const string DecPlain = @"
            <ComponentDec decName=""Plain"">
                <type>SimpleComponent</type>
            </ComponentDec>

            <EntityDec decName=""PlainEntity"">
                <components>
                    <li>Plain</li>
                </components>
            </EntityDec>";

        private Environment Setup(string decs)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { });
            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, $"<Decs>{decs}</Decs>");
            parser.Finish();

            Environment.Init();
            return new Environment();
        }

        private static EntityDec EntityA
        {
            get
            {
                return Dec.Database<EntityDec>.Get("EntityA");
            }
        }

        private const string EntityAWithA = DecA + @"
            <EntityDec decName=""EntityA"">
                <components>
                    <li>A</li>
                </components>
            </EntityDec>";

        [Test]
        public void OnAddImmediate()
        {
            var env = Setup(EntityAWithA);
            using var envActive = new Environment.Scope(env);

            HookedA.Held = default;
            var ent = env.Add(EntityA, new object[] { new HookedA() { value = 7 } });

            Assert.AreEqual(new[] { ("A.add", ent) }, Log);
            Assert.IsTrue(HookedA.SeenValid);
            Assert.AreEqual(7, HookedA.SeenValue);

            env.Remove(ent);

            Assert.AreEqual(new[] { ("A.add", ent), ("A.remove", ent) }, Log);
        }

        public static class DeferredAdder
        {
            public static int LogCountAfterAdd;

            public static void Execute()
            {
                var env = Environment.Current.Value;
                var ent = env.Add(EntityA);
                HookedA.Held = ent;
                LogCountAfterAdd = Log.Count;

                // the hook has to see this, even though it was set after Add returned
                ent.ComponentRW<HookedA>().value = 42;
            }
        }

        [Test]
        public void OnAddDeferred()
        {
            var env = Setup(EntityAWithA + @"
                <SystemDec decName=""Adder"">
                    <type>DeferredAdder</type>
                </SystemDec>

                <ProcessDec decName=""Process"">
                    <order>
                        <li>Adder</li>
                    </order>
                </ProcessDec>");
            using var envActive = new Environment.Scope(env);

            env.Process(Dec.Database<ProcessDec>.Get("Process"));

            Assert.AreEqual(0, DeferredAdder.LogCountAfterAdd);
            Assert.AreEqual(new[] { ("A.add", HookedA.Held) }, Log);
            Assert.IsTrue(HookedA.SeenValid);
            Assert.AreEqual(42, HookedA.SeenValue);
            Assert.IsTrue(HookedA.SeenEqualsHeld);
        }

        public static class DeferredAdderBoth
        {
            public static Entity HeldA;
            public static Entity HeldPlain;

            public static void Execute()
            {
                var env = Environment.Current.Value;
                HeldA = env.Add(EntityA);
                HeldPlain = env.Add(Dec.Database<EntityDec>.Get("PlainEntity"));
            }
        }

        [Test]
        public void GlobalHooks()
        {
            var env = Setup(EntityAWithA + DecG + DecPlain + @"
                <SystemDec decName=""Adder"">
                    <type>DeferredAdderBoth</type>
                </SystemDec>

                <ProcessDec decName=""Process"">
                    <order>
                        <li>Adder</li>
                    </order>
                </ProcessDec>");
            using var envActive = new Environment.Scope(env);

            var a = env.Add(EntityA);
            var plain = env.Add(Dec.Database<EntityDec>.Get("PlainEntity"));
            Assert.AreEqual(new[] { ("A.add", a), ("G.add", a), ("G.add", plain) }, Log);

            Log.Clear();
            env.Remove(plain);
            env.Remove(a);
            Assert.AreEqual(new[] { ("G.remove", plain), ("G.remove", a), ("A.remove", a) }, Log);

            Log.Clear();
            env.Process(Dec.Database<ProcessDec>.Get("Process"));
            Assert.AreEqual(new[] { ("A.add", DeferredAdderBoth.HeldA), ("G.add", DeferredAdderBoth.HeldA), ("G.add", DeferredAdderBoth.HeldPlain) }, Log);
        }

        [Test]
        public void Order()
        {
            var env = Setup(DecA + DecB + DecG + @"
                <EntityDec decName=""EntityA"">
                    <components>
                        <li>A</li>
                        <li>B</li>
                    </components>
                </EntityDec>");
            using var envActive = new Environment.Scope(env);

            var ent = env.Add(EntityA);
            Assert.AreEqual(new[] { "A.add", "B.add", "G.add" }, Tags());

            Log.Clear();
            env.Remove(ent);
            Assert.AreEqual(new[] { "G.remove", "A.remove", "B.remove" }, Tags());
        }

        // Removes Target from inside Trigger's OnRemove.
        public class RemoveOtherOnRemove : IRecordable, IOnRemove
        {
            public static Entity Trigger;
            public static Entity Target;

            public void Record(Dec.Recorder recorder) { }

            public void OnRemove(Entity entity)
            {
                Log.Add(("R.remove", entity));
                if (entity == Trigger)
                {
                    Environment.Current.Value.Remove(Target);
                }
            }
        }

        // Every (trigger, target) pair over three entities; the trigger-last cases are the ones where the nested removal relocates the trigger mid-dispatch.
        [TestCase(0, 1)]
        [TestCase(0, 2)]
        [TestCase(1, 0)]
        [TestCase(1, 2)]
        [TestCase(2, 0)]
        [TestCase(2, 1)]
        public void ReentrantRemoveFromHook(int trigger, int target)
        {
            // R goes first so A's hook runs after the tranche has been rearranged underneath it
            var env = Setup(DecA + @"
                <ComponentDec decName=""R"">
                    <type>RemoveOtherOnRemove</type>
                </ComponentDec>

                <EntityDec decName=""EntityA"">
                    <components>
                        <li>R</li>
                        <li>A</li>
                    </components>
                </EntityDec>");
            using var envActive = new Environment.Scope(env);

            var ents = new[] { env.Add(EntityA), env.Add(EntityA), env.Add(EntityA) };
            RemoveOtherOnRemove.Trigger = ents[trigger];
            RemoveOtherOnRemove.Target = ents[target];
            Log.Clear();

            env.Remove(ents[trigger]);

            int survivor = 3 - trigger - target;
            Assert.AreEqual(1, env.Count);
            Assert.IsTrue(ents[survivor].IsValid());
            Assert.IsFalse(ents[trigger].IsValid());
            Assert.IsFalse(ents[target].IsValid());
            Assert.AreEqual(new[] { ents[survivor] }, env.List.ToArray());

            // one of each hook per removed entity, whichever order they went in, and each on the right instance
            CollectionAssert.AreEquivalent(new[] { ("R.remove", ents[trigger]), ("A.remove", ents[trigger]), ("R.remove", ents[target]), ("A.remove", ents[target]) }, Log);
            Assert.AreEqual(0, HookedA.SelfMismatches);
        }

        // Removes Target from inside OnAdd.
        public class RemoveOtherOnAdd : IRecordable, IOnAdd
        {
            public static Entity Target;

            public void Record(Dec.Recorder recorder) { }

            public void OnAdd(Entity entity)
            {
                Log.Add(("Q.add", entity));
                if (Target != default)
                {
                    Environment.Current.Value.Remove(Target);
                }
            }
        }

        [Test]
        public void ReentrantRemoveFromAddHook()
        {
            var env = Setup(DecA + @"
                <ComponentDec decName=""Q"">
                    <type>RemoveOtherOnAdd</type>
                </ComponentDec>

                <EntityDec decName=""EntityA"">
                    <components>
                        <li>Q</li>
                        <li>A</li>
                    </components>
                </EntityDec>");
            using var envActive = new Environment.Scope(env);

            RemoveOtherOnAdd.Target = default;
            var first = env.Add(EntityA);
            Log.Clear();

            // the new entity is last in the tranche; removing the first one swaps it to the front between its two add hooks
            RemoveOtherOnAdd.Target = first;
            var second = env.Add(EntityA);

            Assert.AreEqual(new[] { ("Q.add", second), ("A.remove", first), ("A.add", second) }, Log);
            Assert.AreEqual(0, HookedA.SelfMismatches);
            Assert.IsFalse(first.IsValid());
            Assert.IsTrue(second.IsValid());
            Assert.AreEqual(1, env.Count);
        }

        // Adds another entity of the same type from inside OnAdd, Remaining times.
        public class AddOnAdd : IRecordable, IOnAdd
        {
            public static int Remaining;

            public void Record(Dec.Recorder recorder) { }

            public void OnAdd(Entity entity)
            {
                Log.Add(("C.add", entity));
                if (Remaining > 0)
                {
                    --Remaining;
                    Environment.Current.Value.Add(EntityA);
                }
            }
        }

        [Test]
        public void ReentrantAddFromHook()
        {
            var env = Setup(@"
                <ComponentDec decName=""C"">
                    <type>AddOnAdd</type>
                </ComponentDec>

                <EntityDec decName=""EntityA"">
                    <components>
                        <li>C</li>
                    </components>
                </EntityDec>");
            using var envActive = new Environment.Scope(env);

            // enough to grow the tranche's component arrays out from under the outer add several times
            AddOnAdd.Remaining = 40;
            env.Add(EntityA);

            Assert.AreEqual(41, env.Count);
            Assert.IsTrue(env.List.All(e => e.IsValid()));
            Assert.AreEqual(41, Log.Count);
            Assert.AreEqual(41, Log.Select(l => l.entity).Distinct().Count());
            CollectionAssert.AreEquivalent(env.List.ToArray(), Log.Select(l => l.entity).ToArray());
        }

        // Removes the entity being announced, or its partner if it has one.
        public class RemoveOnRemove : IRecordable, IOnRemove
        {
            public static Dictionary<Entity, Entity> Partner = new();

            public void Record(Dec.Recorder recorder) { }

            public void OnRemove(Entity entity)
            {
                Log.Add(("P.remove", entity));
                Environment.Current.Value.Remove(Partner.TryGetValue(entity, out var partner) ? partner : entity);
            }
        }

        private const string RemoveOnRemoveDecs = DecA + @"
            <ComponentDec decName=""P"">
                <type>RemoveOnRemove</type>
            </ComponentDec>

            <EntityDec decName=""EntityA"">
                <components>
                    <li>P</li>
                    <li>A</li>
                </components>
            </EntityDec>";

        [Test]
        public void RemoveSelfFromHook()
        {
            var env = Setup(RemoveOnRemoveDecs);
            using var envActive = new Environment.Scope(env);

            RemoveOnRemove.Partner.Clear();
            var ent = env.Add(EntityA);
            Log.Clear();

            env.Remove(ent);

            Assert.AreEqual(new[] { ("P.remove", ent), ("A.remove", ent) }, Log);
            Assert.IsFalse(ent.IsValid());
            Assert.AreEqual(0, env.Count);
        }

        [Test]
        public void RemoveCascadeFromHook()
        {
            var env = Setup(RemoveOnRemoveDecs);
            using var envActive = new Environment.Scope(env);

            var a = env.Add(EntityA);
            var b = env.Add(EntityA);
            RemoveOnRemove.Partner = new() { { a, b }, { b, a } };
            Log.Clear();

            env.Remove(a);

            // a's first hook removes b, b's first hook asks for a again and that's already underway; each entity's hooks fire exactly once
            Assert.AreEqual(new[] { ("P.remove", a), ("P.remove", b), ("A.remove", b), ("A.remove", a) }, Log);
            Assert.IsFalse(a.IsValid());
            Assert.IsFalse(b.IsValid());
            Assert.AreEqual(0, env.Count);
        }

        public class RemoveSelfOnAdd : IRecordable, IOnAdd
        {
            public void Record(Dec.Recorder recorder) { }

            public void OnAdd(Entity entity)
            {
                Log.Add(("S.add", entity));
                Environment.Current.Value.Remove(entity);
            }
        }

        [Test]
        public void RemoveDuringOnAdd()
        {
            var env = Setup(DecA + DecG + @"
                <ComponentDec decName=""S"">
                    <type>RemoveSelfOnAdd</type>
                </ComponentDec>

                <EntityDec decName=""EntityA"">
                    <components>
                        <li>S</li>
                        <li>A</li>
                    </components>
                </EntityDec>");
            using var envActive = new Environment.Scope(env);

            var ent = env.Add(EntityA);

            // the first component's OnAdd removed the entity, so the remaining add hooks never see it
            Assert.AreEqual(new[] { ("S.add", ent), ("G.remove", ent), ("A.remove", ent) }, Log);
            Assert.IsFalse(ent.IsValid());
            Assert.AreEqual(0, env.Count);
        }

        // Removes every entity it's told about, from the global add hook.
        public class RejectOnAddGlobal : IRecordable, IOnAddGlobal
        {
            public void Record(Dec.Recorder recorder) { }

            public void OnAddGlobal(Entity entity)
            {
                Log.Add(("F.add", entity));
                Environment.Current.Value.Remove(entity);
            }
        }

        [Test]
        public void RemoveDuringOnAddGlobal()
        {
            // F sorts before G, so it runs first and G must find the entity already gone
            var env = Setup(EntityAWithA + DecG + @"
                <ComponentDec decName=""F"">
                    <type>RejectOnAddGlobal</type>
                    <singleton>true</singleton>
                </ComponentDec>");
            using var envActive = new Environment.Scope(env);

            var ent = env.Add(EntityA);

            Assert.AreEqual(new[] { ("A.add", ent), ("F.add", ent), ("G.remove", ent), ("A.remove", ent) }, Log);
            Assert.IsFalse(ent.IsValid());
            Assert.AreEqual(0, env.Count);
        }

        [Test]
        public void SilentAcrossRecord([Values] EnvironmentMode envMode)
        {
            var env = Setup(EntityAWithA + DecG);
            using var envActive = new Environment.Scope(env);

            env.Add(EntityA);
            env.Add(EntityA);
            Assert.AreEqual(4, Log.Count);
            Log.Clear();

            ProcessEnvMode(env, envMode, env =>
            {
                // the round trip itself fires nothing
                Assert.IsEmpty(Log);
                Assert.AreEqual(2, env.Count);

                // and the copy still fires normally
                var ent = env.Add(EntityA);
                env.Remove(ent);
                Assert.AreEqual(new[] { ("A.add", ent), ("G.add", ent), ("G.remove", ent), ("A.remove", ent) }, Log);
            });
        }

        [Test]
        public void SilentOnLoadFill()
        {
            string serialized;
            {
                var env = Setup(DecPlain);
                using var envActive = new Environment.Scope(env);

                env.Add(Dec.Database<EntityDec>.Get("PlainEntity"));
                env.Add(Dec.Database<EntityDec>.Get("PlainEntity"));

                serialized = Dec.Recorder.Write(env);
            }

            Clean();

            // the save predates both the hooked component and the hooked singleton; loading fills them in without announcing anything
            {
                Setup(DecA + DecG + @"
                    <ComponentDec decName=""Plain"">
                        <type>SimpleComponent</type>
                    </ComponentDec>

                    <EntityDec decName=""PlainEntity"">
                        <components>
                            <li>Plain</li>
                            <li>A</li>
                        </components>
                    </EntityDec>");
                var env = Dec.Recorder.Read<Environment>(serialized);
                using var envActive = new Environment.Scope(env);

                Assert.IsEmpty(Log);
                Assert.AreEqual(2, env.Count);
                Assert.IsTrue(env.List.All(e => e.ComponentRO<HookedA>() != null));
                Assert.IsNotNull(env.Singleton<HookedGlobal>());

                var ent = env.Add(Dec.Database<EntityDec>.Get("PlainEntity"));
                Assert.AreEqual(new[] { ("A.add", ent), ("G.add", ent) }, Log);
            }
        }

        public struct StructHook : IOnRemove
        {
            public void OnRemove(Entity entity) { }
        }

        private void ExpectSetupError(string decs, string message)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { });
            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, $"<Decs>{decs}</Decs>");
            ExpectErrors(() => parser.Finish(), err => err.Contains(message));
        }

        [Test]
        public void StructHookIsError()
        {
            ExpectSetupError(@"
                <ComponentDec decName=""S"">
                    <type>StructHook</type>
                </ComponentDec>", "value-type");
        }

        [Test]
        public void CowHookIsError()
        {
            ExpectSetupError(@"
                <ComponentDec decName=""OnRemoveComp"">
                    <type>OnRemoveComp</type>
                    <cow>true</cow>
                </ComponentDec>", "COW");
        }

        [Test]
        public void SingletonEntityHookIsError()
        {
            ExpectSetupError(@"
                <ComponentDec decName=""A"">
                    <type>HookedA</type>
                    <singleton>true</singleton>
                </ComponentDec>", "never fire on a singleton");
        }

        [Test]
        public void NonSingletonGlobalHookIsError()
        {
            ExpectSetupError(@"
                <ComponentDec decName=""G"">
                    <type>HookedGlobal</type>
                </ComponentDec>", "only supported on singleton");
        }

        public class OnRemoveComp : Ghi.IOnRemove
        {
            public static int removed = 0;

            public void OnRemove(Entity entity)
            {
                removed++;
            }
        }

        [Test]
        public void OnRemove()
        {
            OnRemoveComp.removed = 0;

            var env = Setup(@"
                <ComponentDec decName=""OnRemoveComp"">
                    <type>OnRemoveComp</type>
                </ComponentDec>

                <EntityDec decName=""EntityModel"">
                    <components>
                        <li>OnRemoveComp</li>
                    </components>
                </EntityDec>");
            using var envActive = new Environment.Scope(env);

            var ent = env.Add(Dec.Database<EntityDec>.Get("EntityModel"));
            Assert.AreEqual(0, OnRemoveComp.removed);
            env.Remove(ent);
            Assert.AreEqual(1, OnRemoveComp.removed);
        }

        public class RemoveRecorderComp : Ghi.IOnRemove
        {
            public int removed = 0;

            public void OnRemove(Entity entity)
            {
                removed++;
            }
        }

        public static class RecordRemovals
        {
            public static List<int> recorded = new();

            public static void Execute(Entity ent, RemoveRecorderComp rrc)
            {
                recorded.Add(rrc.removed);
            }
        }

        public static class RemoveThing
        {
            public static void Execute(Entity ent, RemoveRecorderComp rrc)
            {
                RecordRemovals.recorded.Add(rrc.removed);
                Environment.Current.Value.Remove(ent);
                RecordRemovals.recorded.Add(rrc.removed);
            }
        }

        [Test]
        public void OnSystemRemove()
        {
            var env = Setup(@"
                <ComponentDec decName=""RemoveRecorderComp"">
                    <type>RemoveRecorderComp</type>
                </ComponentDec>

                <EntityDec decName=""EntityModel"">
                    <components>
                        <li>RemoveRecorderComp</li>
                    </components>
                </EntityDec>

                <SystemDec decName=""RecordRemovals"">
                    <type>RecordRemovals</type>
                </SystemDec>

                <SystemDec decName=""RemoveThing"">
                    <type>RemoveThing</type>
                </SystemDec>

                <ProcessDec decName=""SystemRemoveTest"">
                    <order>
                        <li>RecordRemovals</li>
                        <li>RemoveThing</li>
                        <li>RecordRemovals</li>
                    </order>
                </ProcessDec>");
            using var envActive = new Environment.Scope(env);

            var ent = env.Add(Dec.Database<EntityDec>.Get("EntityModel"));
            RecordRemovals.recorded.Clear();

            var removeRecorder = ent.ComponentRO<RemoveRecorderComp>(); // holding onto this so we can check to make sure it's incremented correctly
            Assert.AreEqual(0, removeRecorder.removed);
            env.Process(Dec.Database<ProcessDec>.Get("SystemRemoveTest"));

            Assert.AreEqual(new List<int>() { 0, 0, 0 }, RecordRemovals.recorded);
            Assert.AreEqual(1, removeRecorder.removed);
        }
    }
}
