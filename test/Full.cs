namespace Ghi.Test
{
    using NUnit.Framework;
    using System.Linq;

    [TestFixture]
    public class Full : Base
    {
        [Dec.StaticReferences]
        public static class Decs
        {
            static Decs() { Dec.StaticReferencesAttribute.Initialized(); }

            public static ProcessDec TestProcess;
            public static EntityDec Entity;
        }

        public static class FullPermissionRwSystem
        {
            public static int Executions = 0;

            public static void Execute()
            {
                ++Executions;
                Assert.IsNotNull(Environment.List.First().Component<SimpleComponent>());
            }
        }

        public static class FullPermissionRoSystem
        {
            public static int Executions = 0;

            public static void Execute()
            {
                ++Executions;
                Assert.IsNotNull(Environment.List.First().ComponentRO<SimpleComponent>());
            }
        }

        [Test]
	    public void PermissionsRwRw()
	    {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitStaticRefs = new System.Type[] { typeof(Decs) } });
            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <ComponentDec decName=""Component"">
                        <type>SimpleComponent</type>
                    </ComponentDec>

                    <EntityDec decName=""Entity"">
                        <components>
                            <li>Component</li>
                        </components>
                    </EntityDec>

                    <SystemDec decName=""TestSystem"">
                        <type>FullPermissionRwSystem</type>
                        <full>
                            <Component>ReadWrite</Component>
                        </full>
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
            Environment.Add(new Entity(Decs.Entity));

            FullPermissionRwSystem.Executions = 0;
            Environment.Process(Decs.TestProcess);
            Assert.AreEqual(2, FullPermissionRoSystem.Executions);
	    }

        [Test]
	    public void PermissionsRwRo()
	    {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitStaticRefs = new System.Type[] { typeof(Decs) } });
            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <ComponentDec decName=""Component"">
                        <type>SimpleComponent</type>
                    </ComponentDec>

                    <EntityDec decName=""Entity"">
                        <components>
                            <li>Component</li>
                        </components>
                    </EntityDec>

                    <SystemDec decName=""TestSystem"">
                        <type>FullPermissionRwSystem</type>
                        <full>
                            <Component>ReadOnly</Component>
                        </full>
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
            Environment.Add(new Entity(Decs.Entity));

            FullPermissionRwSystem.Executions = 0;
            ExpectErrors(() => Environment.Process(Decs.TestProcess));
            Assert.AreEqual(2, FullPermissionRoSystem.Executions);
	    }

        [Test]
	    public void PermissionsRwNo()
	    {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitStaticRefs = new System.Type[] { typeof(Decs) } });
            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <ComponentDec decName=""Component"">
                        <type>SimpleComponent</type>
                    </ComponentDec>

                    <EntityDec decName=""Entity"">
                        <components>
                            <li>Component</li>
                        </components>
                    </EntityDec>

                    <SystemDec decName=""TestSystem"">
                        <type>FullPermissionRwSystem</type>
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
            Environment.Add(new Entity(Decs.Entity));

            FullPermissionRwSystem.Executions = 0;
            ExpectErrors(() => Environment.Process(Decs.TestProcess));
            Assert.AreEqual(2, FullPermissionRoSystem.Executions);
	    }

        [Test]
	    public void PermissionsRoRw()
	    {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitStaticRefs = new System.Type[] { typeof(Decs) } });
            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <ComponentDec decName=""Component"">
                        <type>SimpleComponent</type>
                    </ComponentDec>

                    <EntityDec decName=""Entity"">
                        <components>
                            <li>Component</li>
                        </components>
                    </EntityDec>

                    <SystemDec decName=""TestSystem"">
                        <type>FullPermissionRoSystem</type>
                        <full>
                            <Component>ReadWrite</Component>
                        </full>
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
            Environment.Add(new Entity(Decs.Entity));

            FullPermissionRoSystem.Executions = 0;
            Environment.Process(Decs.TestProcess);
            Assert.AreEqual(2, FullPermissionRoSystem.Executions);
	    }

        [Test]
	    public void PermissionsRoRo()
	    {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitStaticRefs = new System.Type[] { typeof(Decs) } });
            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <ComponentDec decName=""Component"">
                        <type>SimpleComponent</type>
                    </ComponentDec>

                    <EntityDec decName=""Entity"">
                        <components>
                            <li>Component</li>
                        </components>
                    </EntityDec>

                    <SystemDec decName=""TestSystem"">
                        <type>FullPermissionRoSystem</type>
                        <full>
                            <Component>ReadOnly</Component>
                        </full>
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
            Environment.Add(new Entity(Decs.Entity));

            FullPermissionRoSystem.Executions = 0;
            Environment.Process(Decs.TestProcess);
            Assert.AreEqual(2, FullPermissionRoSystem.Executions);
	    }

        [Test]
	    public void PermissionsRoNo()
	    {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitStaticRefs = new System.Type[] { typeof(Decs) } });
            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <ComponentDec decName=""Component"">
                        <type>SimpleComponent</type>
                    </ComponentDec>

                    <EntityDec decName=""Entity"">
                        <components>
                            <li>Component</li>
                        </components>
                    </EntityDec>

                    <SystemDec decName=""TestSystem"">
                        <type>FullPermissionRoSystem</type>
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
            Environment.Add(new Entity(Decs.Entity));

            FullPermissionRoSystem.Executions = 0;
            ExpectErrors(() => Environment.Process(Decs.TestProcess));
            Assert.AreEqual(2, FullPermissionRoSystem.Executions);
	    }
    }
}
