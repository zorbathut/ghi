using Dec;
using NUnit.Framework;
using System;
using System.Linq;

namespace Ghi.Test
{
    // Process calls out to two pieces of host code that aren't systems - IOnRemove handlers, from the phase-end action loop, and the profiler scope around each system. An exception from either used to escape Process, which left status stuck mid-process and the environment permanently unable to run another one.
    public class ProcessExceptions : Base
    {
        public ProcessExceptions(EmitMode emitMode) : base(emitMode) { }

        [Dec.StaticReferences]
        public static class Decs
        {
            static Decs() { Dec.StaticReferencesAttribute.Initialized(); }

            public static EntityDec EntityModel;
            public static ProcessDec TestProcess;
        }

        public class ThrowingRemoveComponent : IRecordable, IOnRemove
        {
            public void Record(Dec.Recorder recorder) { }

            public void OnRemove(Entity entity)
            {
                throw new InvalidOperationException("onremove blew up");
            }
        }

        public class CountingRemoveComponent : IRecordable, IOnRemove
        {
            public static int Removals;

            public void Record(Dec.Recorder recorder) { }

            public void OnRemove(Entity entity)
            {
                ++Removals;
            }
        }

        public static class RemoveSystem
        {
            public static void Execute(Entity entity)
            {
                Environment.Current.Value.Remove(entity);
            }
        }

        public static class CountingSystem
        {
            public static int Executions;

            public static void Execute()
            {
                ++Executions;
            }
        }

        private void Parse(string systemType)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitStaticRefs = new System.Type[] { typeof(Decs) } });
            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, $@"
                <Decs>
                    <ComponentDec decName=""Throwing"">
                        <type>ThrowingRemoveComponent</type>
                    </ComponentDec>

                    <ComponentDec decName=""Counting"">
                        <type>CountingRemoveComponent</type>
                    </ComponentDec>

                    <EntityDec decName=""EntityModel"">
                        <components>
                            <li>Throwing</li>
                            <li>Counting</li>
                        </components>
                    </EntityDec>

                    <SystemDec decName=""TestSystem"">
                        <type>{systemType}</type>
                    </SystemDec>

                    <ProcessDec decName=""TestProcess"">
                        <order>
                            <li>TestSystem</li>
                        </order>
                    </ProcessDec>
                </Decs>
            ");
            parser.Finish();
        }

        [Test]
        public void RemoveHandlerThrows()
        {
            Parse(nameof(RemoveSystem));

            Environment.Init();
            var env = new Environment();
            using var envActive = new Environment.Scope(env);

            env.Add(Decs.EntityModel);
            CountingRemoveComponent.Removals = 0;

            ExpectErrors(() => env.Process(Decs.TestProcess), err => err.Contains("onremove blew up"));

            // the throwing handler doesn't get to cancel the removal, skip the other components' handlers, or wedge the environment
            Assert.AreEqual(0, env.Count);
            Assert.AreEqual(1, CountingRemoveComponent.Removals);

            // and the environment isn't left stuck mid-process
            env.Process(Decs.TestProcess);
        }

        private class ThrowingProfiler : IDisposable
        {
            public void Dispose()
            {
                throw new InvalidOperationException("profiler dispose blew up");
            }
        }

        [Test]
        public void ProfilerDisposeThrows()
        {
            Parse(nameof(CountingSystem));

            Environment.Init();
            var env = new Environment();
            using var envActive = new Environment.Scope(env);

            CountingSystem.Executions = 0;

            try
            {
                Config.ProfFactory = str => new ThrowingProfiler();
                ExpectErrors(() => env.Process(Decs.TestProcess), err => err.Contains("profiler dispose blew up"));
            }
            finally
            {
                Config.ProfFactory = str => null;
            }

            Assert.AreEqual(1, CountingSystem.Executions);

            // and the environment isn't left stuck mid-process
            env.Process(Decs.TestProcess);
            Assert.AreEqual(2, CountingSystem.Executions);
        }

        [Test]
        public void ProfilerFactoryThrows()
        {
            Parse(nameof(CountingSystem));

            Environment.Init();
            var env = new Environment();
            using var envActive = new Environment.Scope(env);

            CountingSystem.Executions = 0;

            try
            {
                Config.ProfFactory = str => throw new InvalidOperationException("profiler factory blew up");
                ExpectErrors(() => env.Process(Decs.TestProcess), err => err.Contains("profiler factory blew up"));
            }
            finally
            {
                Config.ProfFactory = str => null;
            }

            // a broken profiler is an observability problem, not a reason to skip the work
            Assert.AreEqual(1, CountingSystem.Executions);

            // and the environment isn't left stuck mid-process
            env.Process(Decs.TestProcess);
            Assert.AreEqual(2, CountingSystem.Executions);
        }
    }
}
