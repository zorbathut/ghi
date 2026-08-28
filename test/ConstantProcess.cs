using Dec;
using NUnit.Framework;
using System.Linq;

namespace Ghi.Test
{
    // A ProcessDec can declare that it mutates no recorded state. Environment uses that to allow recording (and therefore checksumming) an environment while such a process is running, and to complain if the process turns around and adds or removes an entity anyway.
    public class ConstantProcess : Base
    {
        public ConstantProcess(EmitMode emitMode) : base(emitMode) { }

        [Dec.StaticReferences]
        public static class Decs
        {
            static Decs() { Dec.StaticReferencesAttribute.Initialized(); }

            public static EntityDec EntityModel;
            public static ProcessDec ConstantProcess;
            public static ProcessDec MutatingProcess;
        }

        public static class NullSystem
        {
            public static void Execute() { }
        }

        // ConstantProcess and MutatingProcess both run whichever system the test names, so each test picks the behavior it wants with a system type and then chooses which process to run it under.
        private void Parse(string systemType)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitStaticRefs = new System.Type[] { typeof(Decs) } });
            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, $@"
                <Decs>
                    <ComponentDec decName=""Component"">
                        <type>SimpleComponent</type>
                    </ComponentDec>

                    <ComponentDec decName=""Singleton"">
                        <type>StringComponent</type>
                        <singleton>true</singleton>
                    </ComponentDec>

                    <EntityDec decName=""EntityModel"">
                        <components>
                            <li>Component</li>
                        </components>
                    </EntityDec>

                    <SystemDec decName=""TestSystem"">
                        <type>{systemType}</type>
                    </SystemDec>

                    <ProcessDec decName=""ConstantProcess"">
                        <constant>true</constant>
                        <order>
                            <li>TestSystem</li>
                        </order>
                    </ProcessDec>

                    <ProcessDec decName=""MutatingProcess"">
                        <order>
                            <li>TestSystem</li>
                        </order>
                    </ProcessDec>
                </Decs>
            ");
            parser.Finish();
        }

        [Test]
        public void Declaration()
        {
            Parse(nameof(NullSystem));

            Assert.IsFalse(Decs.MutatingProcess.constant);
            Assert.IsTrue(Decs.ConstantProcess.constant);
        }

        public static class StateSystem
        {
            public static bool Processing;
            public static bool Mutating;

            public static void Execute()
            {
                var env = Environment.Current.Value;
                Processing = env.IsProcessing;
                Mutating = env.IsMutating;
            }
        }

        [Test]
        public void State([Values] EnvironmentMode envMode)
        {
            Parse(nameof(StateSystem));

            Environment.Init();
            var env = new Environment();
            using var envActive = new Environment.Scope(env);

            ProcessEnvMode(env, envMode, env =>
            {
                StateSystem.Processing = false;
                StateSystem.Mutating = false;

                Assert.IsFalse(env.IsProcessing);
                Assert.IsFalse(env.IsMutating);

                env.Process(Decs.MutatingProcess);
                Assert.IsTrue(StateSystem.Processing);
                Assert.IsTrue(StateSystem.Mutating);

                Assert.IsFalse(env.IsProcessing);
                Assert.IsFalse(env.IsMutating);

                env.Process(Decs.ConstantProcess);
                Assert.IsTrue(StateSystem.Processing);
                Assert.IsFalse(StateSystem.Mutating);

                Assert.IsFalse(env.IsProcessing);
                Assert.IsFalse(env.IsMutating);
            });
        }

        public static class RecordSystem
        {
            public static ulong Checksum;
            public static string Written;

            public static void Execute()
            {
                var env = Environment.Current.Value;
                Checksum = Dec.Recorder.Checksum(env);
                Written = Dec.Recorder.Write(env);
            }
        }

        [Test]
        public void RecordDuringConstant([Values] EnvironmentMode envMode)
        {
            Parse(nameof(RecordSystem));

            Environment.Init();
            var env = new Environment();
            using var envActive = new Environment.Scope(env);

            env.Add(Decs.EntityModel).ComponentRW<SimpleComponent>().number = 42;

            ProcessEnvMode(env, envMode, env =>
            {
                RecordSystem.Checksum = 0;
                RecordSystem.Written = null;

                env.Process(Decs.ConstantProcess);

                // a constant process changes nothing, so the checksum we grabbed mid-process is still the right answer
                Assert.AreEqual(Dec.Recorder.Checksum(env), RecordSystem.Checksum);

                var envDupe = Dec.Recorder.Read<Environment>(RecordSystem.Written);
                using var dupeActive = new Environment.Scope(envDupe);
                Assert.AreEqual(1, envDupe.Count);
                Assert.AreEqual(42, envDupe.List.Single().ComponentRO<SimpleComponent>().number);
            });
        }

        [Test]
        public void RecordDuringMutating([Values] EnvironmentMode envMode)
        {
            Parse(nameof(RecordSystem));

            Environment.Init();
            var env = new Environment();
            using var envActive = new Environment.Scope(env);

            env.Add(Decs.EntityModel).ComponentRW<SimpleComponent>().number = 42;

            ProcessEnvMode(env, envMode, env =>
            {
                ExpectErrors(() => env.Process(Decs.MutatingProcess), err => err.Contains("non-constant process"));
            });
        }

        public static class AddSystem
        {
            public static void Execute()
            {
                Environment.Current.Value.Add(Decs.EntityModel);
            }
        }

        [Test]
        public void AddDuringConstant([Values] EnvironmentMode envMode)
        {
            Parse(nameof(AddSystem));

            Environment.Init();
            var env = new Environment();
            using var envActive = new Environment.Scope(env);

            ProcessEnvMode(env, envMode, env =>
            {
                // the add is a declaration violation, but it's reported and then honored, not swallowed
                ExpectErrors(() => env.Process(Decs.ConstantProcess), err => err.Contains("constant declaration"));
                Assert.AreEqual(1, env.Count);
            });
        }

        [Test]
        public void AddDuringMutating([Values] EnvironmentMode envMode)
        {
            Parse(nameof(AddSystem));

            Environment.Init();
            var env = new Environment();
            using var envActive = new Environment.Scope(env);

            ProcessEnvMode(env, envMode, env =>
            {
                env.Process(Decs.MutatingProcess);
                Assert.AreEqual(1, env.Count);
            });
        }

        public static class RemoveSystem
        {
            public static Entity Target;

            public static void Execute()
            {
                Environment.Current.Value.Remove(Target);
            }
        }

        [Test]
        public void RemoveDuringConstant([Values] EnvironmentMode envMode)
        {
            Parse(nameof(RemoveSystem));

            Environment.Init();
            var env = new Environment();
            using var envActive = new Environment.Scope(env);

            RemoveSystem.Target = env.Add(Decs.EntityModel);

            ProcessEnvMode(env, envMode, env =>
            {
                ExpectErrors(() => env.Process(Decs.ConstantProcess), err => err.Contains("constant declaration"));
                Assert.AreEqual(0, env.Count);
            });
        }

        public static class SetComponentSystem
        {
            public static void Execute(Entity entity)
            {
                entity.SetComponent(new SimpleComponent { number = 7 });
            }
        }

        [Test]
        public void SetComponentDuringConstant([Values] EnvironmentMode envMode)
        {
            Parse(nameof(SetComponentSystem));

            Environment.Init();
            var env = new Environment();
            using var envActive = new Environment.Scope(env);

            env.Add(Decs.EntityModel);

            ProcessEnvMode(env, envMode, env =>
            {
                // replacing a component wholesale is recorded-state mutation, same as an add
                ExpectErrors(() => env.Process(Decs.ConstantProcess), err => err.Contains("constant declaration"));
                Assert.AreEqual(7, env.List.Single().ComponentRO<SimpleComponent>().number);
            });
        }

        [Test]
        public void RemoveDuringMutating([Values] EnvironmentMode envMode)
        {
            Parse(nameof(RemoveSystem));

            Environment.Init();
            var env = new Environment();
            using var envActive = new Environment.Scope(env);

            RemoveSystem.Target = env.Add(Decs.EntityModel);

            ProcessEnvMode(env, envMode, env =>
            {
                env.Process(Decs.MutatingProcess);
                Assert.AreEqual(0, env.Count);
            });
        }

        public static class SingletonSetSystem
        {
            public static StringComponent Assigned;

            public static void Execute()
            {
                Assigned = new StringComponent();
                Environment.Current.Value.SingletonSet(Assigned);
            }
        }

        [Test]
        public void SingletonSetDuringConstant([Values] EnvironmentMode envMode)
        {
            Parse(nameof(SingletonSetSystem));

            Environment.Init();
            var env = new Environment();
            using var envActive = new Environment.Scope(env);

            ProcessEnvMode(env, envMode, env =>
            {
                // singletons are recorded state, so replacing one is the same class of violation as an add
                ExpectErrors(() => env.Process(Decs.ConstantProcess), err => err.Contains("constant declaration"));
            });
        }

        [Test]
        public void SingletonSetDuringMutating([Values] EnvironmentMode envMode)
        {
            Parse(nameof(SingletonSetSystem));

            Environment.Init();
            var env = new Environment();
            using var envActive = new Environment.Scope(env);

            ProcessEnvMode(env, envMode, env =>
            {
                env.Process(Decs.MutatingProcess);
                Assert.AreSame(SingletonSetSystem.Assigned, env.Singleton<StringComponent>());
            });
        }

        [Test]
        public void ConstantThenMutating([Values] EnvironmentMode envMode)
        {
            // if a constant process wrongly left the environment marked as constant, the mutating process that follows would report itself safe to record
            Parse(nameof(RecordSystem));

            Environment.Init();
            var env = new Environment();
            using var envActive = new Environment.Scope(env);

            ProcessEnvMode(env, envMode, env =>
            {
                env.Process(Decs.ConstantProcess);

                Assert.IsFalse(env.IsProcessing);
                Assert.IsFalse(env.IsMutating);

                ExpectErrors(() => env.Process(Decs.MutatingProcess), err => err.Contains("non-constant process"));
            });
        }
    }
}
