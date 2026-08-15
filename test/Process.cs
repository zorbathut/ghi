
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
