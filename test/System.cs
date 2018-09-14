namespace Ghi.Test
{
    using NUnit.Framework;
    using System.Linq;

    [TestFixture]
    public class SystemTest : Base
    {
        [Def.StaticReferences]
        public static class NullDefs
        {
            static NullDefs() { Def.StaticReferences.Initialized(); }

            public static ProcessDef TestProcess;
        }

        public static class NullSystem
        {
            public static int executions = 0;
            public static void Execute() { ++executions; }
        }

	    [Test]
	    public void Null()
	    {
	        var parser = new Def.Parser(explicitStaticRefs: new System.Type[] { typeof(NullDefs) });
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

            NullSystem.executions = 0;
            Environment.Process(NullDefs.TestProcess);
            Assert.AreEqual(1, NullSystem.executions);
	    }
    }
}
