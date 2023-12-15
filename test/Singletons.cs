
namespace Ghi.Test
{
    using NUnit.Framework;
    using System.Linq;

    [TestFixture]
    public class Singletons : Base
    {
        [Dec.StaticReferences]
        public static class Decs
        {
            static Decs() { Dec.StaticReferencesAttribute.Initialized(); }

            public static ProcessDec TestProcess;
        }

        public static class SingletonSystem
        {
            public static int Executions = 0;
            public static void Execute(SimpleComponent simple) { simple.number = 15; ++Executions; }
        }

	    [Test]
	    public void Singleton()
	    {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitStaticRefs = new System.Type[] { typeof(Decs) } });
            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <ComponentDec decName=""Singleton"">
                        <type>SimpleComponent</type>
                        <singleton>true</singleton>
                    </ComponentDec>

                    <SystemDec decName=""TestSystem"">
                        <type>SingletonSystem</type>
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

            SingletonSystem.Executions = 0;
            env.Process(Decs.TestProcess);
            Assert.AreEqual(1, SingletonSystem.Executions);
            Assert.AreEqual(15, env.Singleton<SimpleComponent>().number);
	    }
    }
}
