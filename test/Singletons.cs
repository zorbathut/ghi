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
                        <singleton>
                            <Singleton>ReadWrite</Singleton>
                        </singleton>
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

            SingletonSystem.Executions = 0;
            Environment.Process(Decs.TestProcess);
            Assert.AreEqual(1, SingletonSystem.Executions);
            Assert.AreEqual(15, Environment.Singleton<SimpleComponent>().number);
	    }

        [Test]
	    public void SingletonPermissions()
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

            Environment.Startup();

            SingletonSystem.Executions = 0;
            ExpectErrors(() => Environment.Process(Decs.TestProcess));
            Assert.AreEqual(0, SingletonSystem.Executions);
	    }

        [Test]
	    public void SingletonROSuffix()
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
                        <singleton>
                            <Singleton>ReadOnly</Singleton>
                        </singleton>
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

            SingletonSystem.Executions = 0;
            ExpectWarnings(() => Environment.Process(Decs.TestProcess));
            Assert.AreEqual(1, SingletonSystem.Executions);
            Assert.AreEqual(15, Environment.Singleton<SimpleComponent>().number);
	    }

        public static class SingletonROSystem
        {
            public static int Executions = 0;

            // okay, we're not supposed to write here; on the other hand, this is the best way to verify that it's doing the right thing
            public static void Execute(SimpleComponent simple_ro) { simple_ro.number = 15; ++Executions; }
        }

        [Test]
	    public void SingletonROValid()
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
                        <type>SingletonROSystem</type>
                        <singleton>
                            <Singleton>ReadOnly</Singleton>
                        </singleton>
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

            SingletonROSystem.Executions = 0;
            Environment.Process(Decs.TestProcess);
            Assert.AreEqual(1, SingletonROSystem.Executions);
            Assert.AreEqual(15, Environment.Singleton<SimpleComponent>().number);
	    }

        public static class SingletonPermissionRwSystem
        {
            public static int Executions = 0;

            public static void Execute()
            {
                ++Executions;
                Assert.IsNotNull(Environment.Singleton<SimpleComponent>());
            }
        }

        public static class SingletonPermissionRoSystem
        {
            public static int Executions = 0;

            public static void Execute()
            {
                ++Executions;
                Assert.IsNotNull(Environment.SingletonRO<SimpleComponent>());
            }
        }

        [Test]
	    public void PermissionsRwRw()
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
                        <type>SingletonPermissionRwSystem</type>
                        <singleton>
                            <Singleton>ReadWrite</Singleton>
                        </singleton>
                    </SystemDec>

                    <ProcessDec decName=""TestProcess"">
                        <order>
                            <li>TestSystem</li>
                            <li>TestSystem</li>
                        </order>
                    </ProcessDec>
                </Decs>
            ");
            parser.Finish();

            Environment.Startup();

            SingletonPermissionRwSystem.Executions = 0;
            Environment.Process(Decs.TestProcess);
            Assert.AreEqual(2, SingletonPermissionRwSystem.Executions);
	    }

        [Test]
	    public void PermissionsRwRo()
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
                        <type>SingletonPermissionRwSystem</type>
                        <singleton>
                            <Singleton>ReadOnly</Singleton>
                        </singleton>
                    </SystemDec>

                    <ProcessDec decName=""TestProcess"">
                        <order>
                            <li>TestSystem</li>
                            <li>TestSystem</li>
                        </order>
                    </ProcessDec>
                </Decs>
            ");
            parser.Finish();

            Environment.Startup();

            SingletonPermissionRwSystem.Executions = 0;
            ExpectErrors(() => Environment.Process(Decs.TestProcess));
            Assert.AreEqual(2, SingletonPermissionRwSystem.Executions);
	    }

        [Test]
	    public void PermissionsRwNo()
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
                        <type>SingletonPermissionRwSystem</type>
                    </SystemDec>

                    <ProcessDec decName=""TestProcess"">
                        <order>
                            <li>TestSystem</li>
                            <li>TestSystem</li>
                        </order>
                    </ProcessDec>
                </Decs>
            ");
            parser.Finish();

            Environment.Startup();

            SingletonPermissionRwSystem.Executions = 0;
            ExpectErrors(() => Environment.Process(Decs.TestProcess));
            Assert.AreEqual(2, SingletonPermissionRwSystem.Executions);
	    }

        [Test]
	    public void PermissionsRoRw()
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
                        <type>SingletonPermissionRoSystem</type>
                        <singleton>
                            <Singleton>ReadWrite</Singleton>
                        </singleton>
                    </SystemDec>

                    <ProcessDec decName=""TestProcess"">
                        <order>
                            <li>TestSystem</li>
                            <li>TestSystem</li>
                        </order>
                    </ProcessDec>
                </Decs>
            ");
            parser.Finish();

            Environment.Startup();

            SingletonPermissionRoSystem.Executions = 0;
            Environment.Process(Decs.TestProcess);
            Assert.AreEqual(2, SingletonPermissionRoSystem.Executions);
	    }

        [Test]
	    public void PermissionsRoRo()
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
                        <type>SingletonPermissionRoSystem</type>
                        <singleton>
                            <Singleton>ReadOnly</Singleton>
                        </singleton>
                    </SystemDec>

                    <ProcessDec decName=""TestProcess"">
                        <order>
                            <li>TestSystem</li>
                            <li>TestSystem</li>
                        </order>
                    </ProcessDec>
                </Decs>
            ");
            parser.Finish();

            Environment.Startup();

            SingletonPermissionRoSystem.Executions = 0;
            Environment.Process(Decs.TestProcess);
            Assert.AreEqual(2, SingletonPermissionRoSystem.Executions);
	    }

        [Test]
	    public void PermissionsRoNo()
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
                        <type>SingletonPermissionRoSystem</type>
                    </SystemDec>

                    <ProcessDec decName=""TestProcess"">
                        <order>
                            <li>TestSystem</li>
                            <li>TestSystem</li>
                        </order>
                    </ProcessDec>
                </Decs>
            ");
            parser.Finish();

            Environment.Startup();

            SingletonPermissionRoSystem.Executions = 0;
            ExpectErrors(() => Environment.Process(Decs.TestProcess));
            Assert.AreEqual(2, SingletonPermissionRoSystem.Executions);
	    }

        [Test]
	    public void PermissionsRwIm()
	    {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitStaticRefs = new System.Type[] { typeof(Decs) } });
            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <ComponentDec decName=""Singleton"">
                        <type>SimpleComponent</type>
                        <singleton>true</singleton>
                        <immutable>true</immutable>
                    </ComponentDec>

                    <SystemDec decName=""TestSystem"">
                        <type>SingletonPermissionRwSystem</type>
                    </SystemDec>

                    <ProcessDec decName=""TestProcess"">
                        <order>
                            <li>TestSystem</li>
                            <li>TestSystem</li>
                        </order>
                    </ProcessDec>
                </Decs>
            ");
            parser.Finish();

            Environment.Startup();

            SingletonPermissionRoSystem.Executions = 0;
            ExpectErrors(() => Environment.Process(Decs.TestProcess));
            Assert.AreEqual(0, SingletonPermissionRoSystem.Executions);
	    }

        [Test]
	    public void PermissionsRoIm()
	    {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitStaticRefs = new System.Type[] { typeof(Decs) } });
            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <ComponentDec decName=""Singleton"">
                        <type>SimpleComponent</type>
                        <singleton>true</singleton>
                        <immutable>true</immutable>
                    </ComponentDec>

                    <SystemDec decName=""TestSystem"">
                        <type>SingletonPermissionRoSystem</type>
                    </SystemDec>

                    <ProcessDec decName=""TestProcess"">
                        <order>
                            <li>TestSystem</li>
                            <li>TestSystem</li>
                        </order>
                    </ProcessDec>
                </Decs>
            ");
            parser.Finish();

            Environment.Startup();

            SingletonPermissionRoSystem.Executions = 0;
            Environment.Process(Decs.TestProcess);
            Assert.AreEqual(2, SingletonPermissionRoSystem.Executions);
	    }
    }
}
