using Dec;
using NUnit.Framework;
using System.Collections.Generic;

namespace Ghi.Test
{
    public class Hooks : Base
    {
        public Hooks(EmitMode emitMode) : base(emitMode) { }

        private Environment Setup(string decs)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { });
            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, $"<Decs>{decs}</Decs>");
            parser.Finish();

            Environment.Init();
            return new Environment();
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
