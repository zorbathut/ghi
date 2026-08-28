
using Dec;
using NUnit.Framework;

namespace Ghi.Test
{
    public class Process : Base
    {
        public Process(EmitMode emitMode) : base(emitMode) { }

        [Dec.StaticReferences]
        public static class Decs
        {
            static Decs() { Dec.StaticReferencesAttribute.Initialized(); }

            public static ProcessDec TestProcess;
        }

        public static class ValidSystem
        {
            public static void Execute() { }
        }

        public static class BrokenSystem
        {
            // no Execute method; the SystemDec referencing this is invalid
        }

        [Test]
        public void InvalidSystemsCleaned()
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitStaticRefs = new System.Type[] { typeof(Decs) } });
            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <SystemDec decName=""ValidSystem"">
                        <type>ValidSystem</type>
                    </SystemDec>

                    <SystemDec decName=""BrokenSystem"">
                        <type>BrokenSystem</type>
                    </SystemDec>

                    <ProcessDec decName=""TestProcess"">
                        <order>
                            <li>ValidSystem</li>
                            <li>BrokenSystem</li>
                        </order>
                    </ProcessDec>
                </Decs>
            ");
            ExpectErrors(() => parser.Finish(), str =>
                str.Contains("does not have a static Execute method") ||
                str.Contains("null or invalid systems"));

            Assert.AreEqual(1, Decs.TestProcess.order.Length);
            Assert.AreSame(Database<SystemDec>.Get("ValidSystem"), Decs.TestProcess.order[0]);
        }

        [Dec.StaticReferences]
        public static class NestedDecs
        {
            static NestedDecs() { Dec.StaticReferencesAttribute.Initialized(); }

            public static EntityDec EntityModel;
            public static ProcessDec Outer;
            public static ProcessDec Inner;
        }

        public static class NestedInnerSystem
        {
            public static void Execute() { }
        }

        public static class NestedOuterSystem
        {
            public static int CountAfterAdd;

            public static void Execute()
            {
                var env = Environment.Current.Value;
                env.Process(NestedDecs.Inner);
                env.Add(NestedDecs.EntityModel);
                CountAfterAdd = env.Count;
            }
        }

        [Test]
        public void NestedRefused()
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitStaticRefs = new System.Type[] { typeof(NestedDecs) } });
            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <ComponentDec decName=""Component"">
                        <type>SimpleComponent</type>
                    </ComponentDec>

                    <EntityDec decName=""EntityModel"">
                        <components>
                            <li>Component</li>
                        </components>
                    </EntityDec>

                    <SystemDec decName=""OuterSystem"">
                        <type>NestedOuterSystem</type>
                    </SystemDec>

                    <SystemDec decName=""InnerSystem"">
                        <type>NestedInnerSystem</type>
                    </SystemDec>

                    <ProcessDec decName=""Outer"">
                        <order>
                            <li>OuterSystem</li>
                        </order>
                    </ProcessDec>

                    <ProcessDec decName=""Inner"">
                        <order>
                            <li>InnerSystem</li>
                        </order>
                    </ProcessDec>
                </Decs>
            ");
            parser.Finish();

            Environment.Init();
            var env = new Environment();
            using var envActive = new Environment.Scope(env);

            NestedOuterSystem.CountAfterAdd = -1;

            ExpectErrors(() => env.Process(NestedDecs.Outer), err => err.Contains("Trying to run process"));

            // the nested process is refused rather than run, so it can't end the outer process out from under the system that called it - if it did, this add would apply immediately instead of deferring, mutating a tranche mid-iteration
            Assert.AreEqual(0, NestedOuterSystem.CountAfterAdd);
            Assert.AreEqual(1, env.Count);
        }

        [Dec.StaticReferences]
        public static class PhaseEndDecs
        {
            static PhaseEndDecs() { Dec.StaticReferencesAttribute.Initialized(); }

            public static EntityDec EntityModel;
            public static ProcessDec TestProcess;
        }

        // OnRemove fires from inside the phase-end action loop, which is the window between systems: no system is executing, but the process is still very much running.
        public class PhaseEndComponent : Dec.IRecordable, IOnRemove
        {
            public static bool AddWasImmediate;

            public void Record(Dec.Recorder recorder) { }

            public void OnRemove(Entity entity)
            {
                var env = Environment.Current.Value;

                // still mid-process, so this has to be rejected
                Dec.Recorder.Write(env);

                // ...but no system is iterating a tranche, so this doesn't need to defer
                int before = env.Count;
                env.Add(PhaseEndDecs.EntityModel);
                AddWasImmediate = env.Count == before + 1;
            }
        }

        public static class PhaseEndRemoveSystem
        {
            public static void Execute(Entity entity)
            {
                Environment.Current.Value.Remove(entity);
            }
        }

        [Test]
        public void PhaseEndIsStillInProcess()
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitStaticRefs = new System.Type[] { typeof(PhaseEndDecs) } });
            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <ComponentDec decName=""Component"">
                        <type>PhaseEndComponent</type>
                    </ComponentDec>

                    <EntityDec decName=""EntityModel"">
                        <components>
                            <li>Component</li>
                        </components>
                    </EntityDec>

                    <SystemDec decName=""TestSystem"">
                        <type>PhaseEndRemoveSystem</type>
                    </SystemDec>

                    <ProcessDec decName=""TestProcess"">
                        <order>
                            <li>TestSystem</li>
                        </order>
                    </ProcessDec>
                </Decs>
            ");
            parser.Finish();

            Environment.Init();
            var env = new Environment();
            using var envActive = new Environment.Scope(env);

            env.Add(PhaseEndDecs.EntityModel);
            PhaseEndComponent.AddWasImmediate = false;

            ExpectErrors(() => env.Process(PhaseEndDecs.TestProcess), err => err.Contains("Attempting to record an environment during"));

            Assert.IsTrue(PhaseEndComponent.AddWasImmediate);
        }

        [Test]
        public void NoOrder()
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitStaticRefs = new System.Type[] { typeof(Decs) } });
            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <SystemDec decName=""ValidSystem"">
                        <type>ValidSystem</type>
                    </SystemDec>

                    <ProcessDec decName=""TestProcess"" />
                </Decs>
            ");
            // the validator rejects any follow-on NullReferenceException from the setup function
            ExpectErrors(() => parser.Finish(), str => str.Contains("No defined order"));

            Assert.IsNull(Decs.TestProcess.order);
        }
    }
}
