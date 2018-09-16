namespace Ghi.Test
{
    using NUnit.Framework;
    using System.Linq;

    [TestFixture]
    public class Singletons : Base
    {
        [Def.StaticReferences]
        public static class SystemTestDefs
        {
            static SystemTestDefs() { Def.StaticReferences.Initialized(); }

            public static ProcessDef TestProcess;
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
                        <singleton>
                            <Singleton>ReadWrite</Singleton>
                        </singleton>
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

        [Test]
	    public void SingletonPermissions()
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
            ExpectErrors(() => Environment.Process(SystemTestDefs.TestProcess));
            Assert.AreEqual(1, SingletonSystem.Executions);
            Assert.AreEqual(15, Environment.Singleton<SimpleComponent>().number);
	    }

        [Test]
	    public void SingletonROSuffix()
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
                        <singleton>
                            <Singleton>ReadOnly</Singleton>
                        </singleton>
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
            ExpectWarnings(() => Environment.Process(SystemTestDefs.TestProcess));
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
	        var parser = new Def.Parser(explicitStaticRefs: new System.Type[] { typeof(SystemTestDefs) });
            parser.AddString(@"
                <Defs>
                    <ComponentDef defName=""Singleton"">
                        <type>SimpleComponent</type>
                        <singleton>true</singleton>
                    </ComponentDef>

                    <SystemDef defName=""TestSystem"">
                        <type>SingletonROSystem</type>
                        <singleton>
                            <Singleton>ReadOnly</Singleton>
                        </singleton>
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

            SingletonROSystem.Executions = 0;
            Environment.Process(SystemTestDefs.TestProcess);
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
	        var parser = new Def.Parser(explicitStaticRefs: new System.Type[] { typeof(SystemTestDefs) });
            parser.AddString(@"
                <Defs>
                    <ComponentDef defName=""Singleton"">
                        <type>SimpleComponent</type>
                        <singleton>true</singleton>
                    </ComponentDef>

                    <SystemDef defName=""TestSystem"">
                        <type>SingletonPermissionRwSystem</type>
                        <singleton>
                            <Singleton>ReadWrite</Singleton>
                        </singleton>
                    </SystemDef>

                    <ProcessDef defName=""TestProcess"">
                        <order>
                            <li>TestSystem</li>
                            <li>TestSystem</li>
                        </order>
                    </ProcessDef>
                </Defs>
            ");
            parser.Finish();

            Environment.Startup();

            SingletonPermissionRwSystem.Executions = 0;
            Environment.Process(SystemTestDefs.TestProcess);
            Assert.AreEqual(2, SingletonPermissionRwSystem.Executions);
	    }

        [Test]
	    public void PermissionsRwRo()
	    {
	        var parser = new Def.Parser(explicitStaticRefs: new System.Type[] { typeof(SystemTestDefs) });
            parser.AddString(@"
                <Defs>
                    <ComponentDef defName=""Singleton"">
                        <type>SimpleComponent</type>
                        <singleton>true</singleton>
                    </ComponentDef>

                    <SystemDef defName=""TestSystem"">
                        <type>SingletonPermissionRwSystem</type>
                        <singleton>
                            <Singleton>ReadOnly</Singleton>
                        </singleton>
                    </SystemDef>

                    <ProcessDef defName=""TestProcess"">
                        <order>
                            <li>TestSystem</li>
                            <li>TestSystem</li>
                        </order>
                    </ProcessDef>
                </Defs>
            ");
            parser.Finish();

            Environment.Startup();

            SingletonPermissionRwSystem.Executions = 0;
            ExpectErrors(() => Environment.Process(SystemTestDefs.TestProcess));
            Assert.AreEqual(2, SingletonPermissionRwSystem.Executions);
	    }

        [Test]
	    public void PermissionsRwNo()
	    {
	        var parser = new Def.Parser(explicitStaticRefs: new System.Type[] { typeof(SystemTestDefs) });
            parser.AddString(@"
                <Defs>
                    <ComponentDef defName=""Singleton"">
                        <type>SimpleComponent</type>
                        <singleton>true</singleton>
                    </ComponentDef>

                    <SystemDef defName=""TestSystem"">
                        <type>SingletonPermissionRwSystem</type>
                    </SystemDef>

                    <ProcessDef defName=""TestProcess"">
                        <order>
                            <li>TestSystem</li>
                            <li>TestSystem</li>
                        </order>
                    </ProcessDef>
                </Defs>
            ");
            parser.Finish();

            Environment.Startup();

            SingletonPermissionRwSystem.Executions = 0;
            ExpectErrors(() => Environment.Process(SystemTestDefs.TestProcess));
            Assert.AreEqual(2, SingletonPermissionRwSystem.Executions);
	    }

        [Test]
	    public void PermissionsRoRw()
	    {
	        var parser = new Def.Parser(explicitStaticRefs: new System.Type[] { typeof(SystemTestDefs) });
            parser.AddString(@"
                <Defs>
                    <ComponentDef defName=""Singleton"">
                        <type>SimpleComponent</type>
                        <singleton>true</singleton>
                    </ComponentDef>

                    <SystemDef defName=""TestSystem"">
                        <type>SingletonPermissionRoSystem</type>
                        <singleton>
                            <Singleton>ReadWrite</Singleton>
                        </singleton>
                    </SystemDef>

                    <ProcessDef defName=""TestProcess"">
                        <order>
                            <li>TestSystem</li>
                            <li>TestSystem</li>
                        </order>
                    </ProcessDef>
                </Defs>
            ");
            parser.Finish();

            Environment.Startup();

            SingletonPermissionRoSystem.Executions = 0;
            Environment.Process(SystemTestDefs.TestProcess);
            Assert.AreEqual(2, SingletonPermissionRoSystem.Executions);
	    }

        [Test]
	    public void PermissionsRoRo()
	    {
	        var parser = new Def.Parser(explicitStaticRefs: new System.Type[] { typeof(SystemTestDefs) });
            parser.AddString(@"
                <Defs>
                    <ComponentDef defName=""Singleton"">
                        <type>SimpleComponent</type>
                        <singleton>true</singleton>
                    </ComponentDef>

                    <SystemDef defName=""TestSystem"">
                        <type>SingletonPermissionRoSystem</type>
                        <singleton>
                            <Singleton>ReadOnly</Singleton>
                        </singleton>
                    </SystemDef>

                    <ProcessDef defName=""TestProcess"">
                        <order>
                            <li>TestSystem</li>
                            <li>TestSystem</li>
                        </order>
                    </ProcessDef>
                </Defs>
            ");
            parser.Finish();

            Environment.Startup();

            SingletonPermissionRoSystem.Executions = 0;
            Environment.Process(SystemTestDefs.TestProcess);
            Assert.AreEqual(2, SingletonPermissionRoSystem.Executions);
	    }

        [Test]
	    public void PermissionsRoNo()
	    {
	        var parser = new Def.Parser(explicitStaticRefs: new System.Type[] { typeof(SystemTestDefs) });
            parser.AddString(@"
                <Defs>
                    <ComponentDef defName=""Singleton"">
                        <type>SimpleComponent</type>
                        <singleton>true</singleton>
                    </ComponentDef>

                    <SystemDef defName=""TestSystem"">
                        <type>SingletonPermissionRoSystem</type>
                    </SystemDef>

                    <ProcessDef defName=""TestProcess"">
                        <order>
                            <li>TestSystem</li>
                            <li>TestSystem</li>
                        </order>
                    </ProcessDef>
                </Defs>
            ");
            parser.Finish();

            Environment.Startup();

            SingletonPermissionRoSystem.Executions = 0;
            ExpectErrors(() => Environment.Process(SystemTestDefs.TestProcess));
            Assert.AreEqual(2, SingletonPermissionRoSystem.Executions);
	    }
    }
}
