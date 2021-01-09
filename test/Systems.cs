namespace Ghi.Test
{
    using NUnit.Framework;
    using System.Linq;

    [TestFixture]
    public class Systems : Base
    {
        [Dec.StaticReferences]
        public static class Decs
        {
            static Decs() { Dec.StaticReferencesAttribute.Initialized(); }

            public static ProcessDec TestProcess;
        }

        public static class NullSystem
        {
            public static int Executions = 0;
            public static void Execute() { ++Executions; }
        }

	    [Test]
	    public void Null()
	    {
            Dec.Config.TestParameters = new Dec.Config.UnitTestParameters { explicitStaticRefs = new System.Type[] { typeof(Decs) } };
            var parser = new Dec.Parser();
            parser.AddString(@"
                <Decs>
                    <SystemDec decName=""TestSystem"">
                        <type>NullSystem</type>
                    </SystemDec>

                    <ProcessDec decName=""TestProcess"">
                        <order>
                            <li>TestSystem</li>
                        </order>
                    </ProcessDec>
                </Decs>
            ");
            parser.Finish();

            Environment.Startup();

            NullSystem.Executions = 0;
            Environment.Process(Decs.TestProcess);
            Assert.AreEqual(1, NullSystem.Executions);
	    }
    }
}
