namespace Ghi.Test
{
    using NUnit.Framework;
    using System.Linq;

    [TestFixture]
    public class SystemTest : Base
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

        public static class SingletonSystem
        {
            public static int Executions = 0;
            public static void Execute(SimpleComponent simple) { simple.number = 15; ++Executions; }
        }

	    [Test]
	    public void Singleton()
	    {
	        var parser = new Def.Parser(explicitStaticRefs: new System.Type[] { typeof(SystemTestDefs) });
            parser.AddString(@"
                <Defs>
                    <ComponentDef defName=""Singleton"">
                        <type>SimpleComponent</type>
                        <singleton>true</singleton>
                    </ComponentDef>

                    <SystemDef defName=""TestSystem"">
                        <type>SingletonSystem</type>
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

            SingletonSystem.Executions = 0;
            Environment.Process(SystemTestDefs.TestProcess);
            Assert.AreEqual(1, SingletonSystem.Executions);
            Assert.AreEqual(15, Environment.Singleton<SimpleComponent>().number);
	    }
    }
}
