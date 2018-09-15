namespace Ghi.Test
{
    using NUnit.Framework;
    using System.Linq;

    [TestFixture]
    public class Systems : Base
    {
        [Def.StaticReferences]
        public static class SystemTestDefs
        {
            static SystemTestDefs() { Def.StaticReferences.Initialized(); }

            public static ProcessDef TestProcess;
        }

        public static class NullSystem
        {
            public static int Executions = 0;
            public static void Execute() { ++Executions; }
        }

	    [Test]
	    public void Null()
	    {
	        var parser = new Def.Parser(explicitStaticRefs: new System.Type[] { typeof(SystemTestDefs) });
            parser.AddString(@"
                <Defs>
                    <SystemDef defName=""TestSystem"">
                        <type>NullSystem</type>
                    </SystemDef>

                    <ProcessDef defName=""TestProcess"">
                        <order>
                            <li>TestSystem</li>
                        </order>
                    </ProcessDef>
                </Defs>
            ");
            parser.Finish();

            Environment.Startup();

            NullSystem.Executions = 0;
            Environment.Process(SystemTestDefs.TestProcess);
            Assert.AreEqual(1, NullSystem.Executions);
	    }
    }
}
