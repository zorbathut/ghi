using Dec;
using NUnit.Framework;
using System;
using System.Linq;

namespace Ghi.Test
{
    // Process calls out to two pieces of host code that aren't systems - lifecycle hooks, from the phase-end action loop, and the profiler scope around each system. An exception from either used to escape Process, which left status stuck mid-process and the environment permanently unable to run another one.
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

        public class ThrowingRemoveGlobal : IRecordable, IOnRemoveGlobal
        {
            public void Record(Dec.Recorder recorder) { }

            public void OnRemoveGlobal(Entity entity)
            {
                throw new InvalidOperationException("global onremove blew up");
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

                    <ComponentDec decName=""ThrowingGlobal"">
                        <type>ThrowingRemoveGlobal</type>
                        <singleton>true</singleton>
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

            // neither throwing hook gets to cancel the removal, skip the component hook that follows it, or wedge the environment
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

        public class ThrowingAddComponent : IRecordable, IOnAdd
        {
            public void Record(Dec.Recorder recorder) { }

            public void OnAdd(Entity entity)
            {
                throw new InvalidOperationException("onadd blew up");
            }
        }

        public class CountingAddComponent : IRecordable, IOnAdd
        {
            public static int Additions;

            public void Record(Dec.Recorder recorder) { }

            public void OnAdd(Entity entity)
            {
                ++Additions;
            }
        }

        public class ThrowingAddGlobal : IRecordable, IOnAddGlobal
        {
            public void Record(Dec.Recorder recorder) { }

            public void OnAddGlobal(Entity entity)
            {
                throw new InvalidOperationException("global onadd blew up");
            }
        }

        public class CountingAddGlobal : IRecordable, IOnAddGlobal
        {
            public static int Additions;

            public void Record(Dec.Recorder recorder) { }

            public void OnAddGlobal(Entity entity)
            {
                ++Additions;
            }
        }

        public static class AddSystem
        {
            public static void Execute()
            {
                Environment.Current.Value.Add(Dec.Database<EntityDec>.Get("AddModel"));
            }
        }

        [Test]
        public void AddHandlerThrows()
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { });
            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <ComponentDec decName=""ThrowingAdd"">
                        <type>ThrowingAddComponent</type>
                    </ComponentDec>

                    <ComponentDec decName=""CountingAdd"">
                        <type>CountingAddComponent</type>
                    </ComponentDec>

                    <ComponentDec decName=""GlobalA"">
                        <type>ThrowingAddGlobal</type>
                        <singleton>true</singleton>
                    </ComponentDec>

                    <ComponentDec decName=""GlobalB"">
                        <type>CountingAddGlobal</type>
                        <singleton>true</singleton>
                    </ComponentDec>

                    <!-- singletons dispatch in DecName order, so the thrower has to sort first for the counter to prove anything -->
                    <EntityDec decName=""AddModel"">
                        <components>
                            <li>ThrowingAdd</li>
                            <li>CountingAdd</li>
                        </components>
                    </EntityDec>

                    <SystemDec decName=""TestSystem"">
                        <type>AddSystem</type>
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

            var process = Dec.Database<ProcessDec>.Get("TestProcess");
            CountingAddComponent.Additions = 0;
            CountingAddGlobal.Additions = 0;

            ExpectErrors(() => env.Process(process), err => err.Contains("blew up"));

            // a throwing hook doesn't cancel the add, skip the sibling component hook, skip the global hooks, or wedge the environment
            Assert.AreEqual(1, env.Count);
            Assert.AreEqual(1, CountingAddComponent.Additions);
            Assert.AreEqual(1, CountingAddGlobal.Additions);

            ExpectErrors(() => env.Process(process), err => err.Contains("blew up"));
            Assert.AreEqual(2, env.Count);
            Assert.AreEqual(2, CountingAddComponent.Additions);
            Assert.AreEqual(2, CountingAddGlobal.Additions);
        }
    }
}
