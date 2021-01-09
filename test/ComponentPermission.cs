namespace Ghi.Test
{
    using NUnit.Framework;
    using System.Linq;

    [TestFixture]
    public class ComponentPermission : Base
    {
        [Dec.StaticReferences]
        public static class Decs
        {
            static Decs() { Dec.StaticReferencesAttribute.Initialized(); }

            public static ProcessDec TestProcess;
            public static EntityDec EntityModel;
        }

        public static class ComponentPermissionRwSystem
        {
            public static int Executions = 0;

            public static void Execute(Entity entity)
            {
                ++Executions;
                Assert.IsNotNull(entity.Component<SimpleComponent>());
            }
        }

        public static class ComponentPermissionRoSystem
        {
            public static int Executions = 0;

            public static void Execute(Entity entity)
            {
                ++Executions;
                Assert.IsNotNull(entity.ComponentRO<SimpleComponent>());
            }
        }

        [Test]
	    public void PermissionsRwRw()
	    {
            Dec.Config.TestParameters = new Dec.Config.UnitTestParameters { explicitStaticRefs = new System.Type[] { typeof(Decs) } };
            var parser = new Dec.Parser();
            parser.AddString(@"
                <Decs>
                    <ComponentDec decName=""SimpleComponent"">
                        <type>SimpleComponent</type>
                    </ComponentDec>

                    <ComponentDec decName=""StringComponent"">
                        <type>StringComponent</type>
                    </ComponentDec>

                    <EntityDec decName=""EntityModel"">
                        <components>
                            <li>SimpleComponent</li>
                            <li>StringComponent</li>
                        </components>
                    </EntityDec>

                    <SystemDec decName=""TestSystem"">
                        <type>ComponentPermissionRwSystem</type>
                        <iterate>
                            <SimpleComponent>ReadWrite</SimpleComponent>
                            <StringComponent>ReadWrite</StringComponent>
                        </iterate>
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

            Environment.Add(new Entity(Decs.EntityModel));
            Environment.Add(new Entity(Decs.EntityModel));

            ComponentPermissionRwSystem.Executions = 0;
            Environment.Process(Decs.TestProcess);
            Assert.AreEqual(2, ComponentPermissionRwSystem.Executions);
	    }

        [Test]
	    public void PermissionsRwRo()
	    {
            Dec.Config.TestParameters = new Dec.Config.UnitTestParameters { explicitStaticRefs = new System.Type[] { typeof(Decs) } };
            var parser = new Dec.Parser();
            parser.AddString(@"
                <Decs>
                    <ComponentDec decName=""SimpleComponent"">
                        <type>SimpleComponent</type>
                    </ComponentDec>

                    <ComponentDec decName=""StringComponent"">
                        <type>StringComponent</type>
                    </ComponentDec>

                    <EntityDec decName=""EntityModel"">
                        <components>
                            <li>SimpleComponent</li>
                            <li>StringComponent</li>
                        </components>
                    </EntityDec>

                    <SystemDec decName=""TestSystem"">
                        <type>ComponentPermissionRwSystem</type>
                        <iterate>
                            <SimpleComponent>ReadOnly</SimpleComponent>
                            <StringComponent>ReadWrite</StringComponent>
                        </iterate>
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

            Environment.Add(new Entity(Decs.EntityModel));
            Environment.Add(new Entity(Decs.EntityModel));

            ComponentPermissionRwSystem.Executions = 0;
            ExpectErrors(() => Environment.Process(Decs.TestProcess));
            Assert.AreEqual(2, ComponentPermissionRwSystem.Executions);
	    }

        [Test]
	    public void PermissionsRwNo()
	    {
            Dec.Config.TestParameters = new Dec.Config.UnitTestParameters { explicitStaticRefs = new System.Type[] { typeof(Decs) } };
	        var parser = new Dec.Parser();
            parser.AddString(@"
                <Decs>
                    <ComponentDec decName=""SimpleComponent"">
                        <type>SimpleComponent</type>
                    </ComponentDec>

                    <ComponentDec decName=""StringComponent"">
                        <type>StringComponent</type>
                    </ComponentDec>

                    <EntityDec decName=""EntityModel"">
                        <components>
                            <li>SimpleComponent</li>
                            <li>StringComponent</li>
                        </components>
                    </EntityDec>

                    <SystemDec decName=""TestSystem"">
                        <type>ComponentPermissionRwSystem</type>
                        <iterate>
                            <StringComponent>ReadWrite</StringComponent>
                        </iterate>
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

            Environment.Add(new Entity(Decs.EntityModel));
            Environment.Add(new Entity(Decs.EntityModel));

            ComponentPermissionRwSystem.Executions = 0;
            ExpectErrors(() => Environment.Process(Decs.TestProcess));
            Assert.AreEqual(2, ComponentPermissionRwSystem.Executions);
	    }

        [Test]
	    public void PermissionsRoRw()
	    {
            Dec.Config.TestParameters = new Dec.Config.UnitTestParameters { explicitStaticRefs = new System.Type[] { typeof(Decs) } };
            var parser = new Dec.Parser();
            parser.AddString(@"
                <Decs>
                    <ComponentDec decName=""SimpleComponent"">
                        <type>SimpleComponent</type>
                    </ComponentDec>

                    <ComponentDec decName=""StringComponent"">
                        <type>StringComponent</type>
                    </ComponentDec>

                    <EntityDec decName=""EntityModel"">
                        <components>
                            <li>SimpleComponent</li>
                            <li>StringComponent</li>
                        </components>
                    </EntityDec>

                    <SystemDec decName=""TestSystem"">
                        <type>ComponentPermissionRoSystem</type>
                        <iterate>
                            <SimpleComponent>ReadWrite</SimpleComponent>
                            <StringComponent>ReadWrite</StringComponent>
                        </iterate>
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

            Environment.Add(new Entity(Decs.EntityModel));
            Environment.Add(new Entity(Decs.EntityModel));

            ComponentPermissionRoSystem.Executions = 0;
            Environment.Process(Decs.TestProcess);
            Assert.AreEqual(2, ComponentPermissionRoSystem.Executions);
	    }

        [Test]
	    public void PermissionsRoRo()
	    {
            Dec.Config.TestParameters = new Dec.Config.UnitTestParameters { explicitStaticRefs = new System.Type[] { typeof(Decs) } };
            var parser = new Dec.Parser();
            parser.AddString(@"
                <Decs>
                    <ComponentDec decName=""SimpleComponent"">
                        <type>SimpleComponent</type>
                    </ComponentDec>

                    <ComponentDec decName=""StringComponent"">
                        <type>StringComponent</type>
                    </ComponentDec>

                    <EntityDec decName=""EntityModel"">
                        <components>
                            <li>SimpleComponent</li>
                            <li>StringComponent</li>
                        </components>
                    </EntityDec>

                    <SystemDec decName=""TestSystem"">
                        <type>ComponentPermissionRoSystem</type>
                        <iterate>
                            <SimpleComponent>ReadOnly</SimpleComponent>
                            <StringComponent>ReadWrite</StringComponent>
                        </iterate>
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

            Environment.Add(new Entity(Decs.EntityModel));
            Environment.Add(new Entity(Decs.EntityModel));

            ComponentPermissionRoSystem.Executions = 0;
            Environment.Process(Decs.TestProcess);
            Assert.AreEqual(2, ComponentPermissionRoSystem.Executions);
	    }

        [Test]
	    public void PermissionsRoNo()
	    {
            Dec.Config.TestParameters = new Dec.Config.UnitTestParameters { explicitStaticRefs = new System.Type[] { typeof(Decs) } };
            var parser = new Dec.Parser();
            parser.AddString(@"
                <Decs>
                    <ComponentDec decName=""SimpleComponent"">
                        <type>SimpleComponent</type>
                    </ComponentDec>

                    <ComponentDec decName=""StringComponent"">
                        <type>StringComponent</type>
                    </ComponentDec>

                    <EntityDec decName=""EntityModel"">
                        <components>
                            <li>SimpleComponent</li>
                            <li>StringComponent</li>
                        </components>
                    </EntityDec>

                    <SystemDec decName=""TestSystem"">
                        <type>ComponentPermissionRoSystem</type>
                        <iterate>
                            <StringComponent>ReadWrite</StringComponent>
                        </iterate>
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

            Environment.Add(new Entity(Decs.EntityModel));
            Environment.Add(new Entity(Decs.EntityModel));

            ComponentPermissionRoSystem.Executions = 0;
            ExpectErrors(() => Environment.Process(Decs.TestProcess));
            Assert.AreEqual(2, ComponentPermissionRoSystem.Executions);
	    }

        [Test]
	    public void PermissionsRoNoDisabled()
	    {
            Dec.Config.TestParameters = new Dec.Config.UnitTestParameters { explicitStaticRefs = new System.Type[] { typeof(Decs) } };
	        var parser = new Dec.Parser();
            parser.AddString(@"
                <Decs>
                    <ComponentDec decName=""SimpleComponent"">
                        <type>SimpleComponent</type>
                    </ComponentDec>

                    <ComponentDec decName=""StringComponent"">
                        <type>StringComponent</type>
                    </ComponentDec>

                    <EntityDec decName=""EntityModel"">
                        <components>
                            <li>SimpleComponent</li>
                            <li>StringComponent</li>
                        </components>
                    </EntityDec>

                    <SystemDec decName=""TestSystem"">
                        <type>ComponentPermissionRoSystem</type>
                        <permissions>false</permissions>
                        <iterate>
                            <StringComponent>ReadWrite</StringComponent>
                        </iterate>
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

            Environment.Add(new Entity(Decs.EntityModel));
            Environment.Add(new Entity(Decs.EntityModel));

            ComponentPermissionRoSystem.Executions = 0;
            Environment.Process(Decs.TestProcess);
            Assert.AreEqual(2, ComponentPermissionRoSystem.Executions);
	    }

        public static class ComponentPermissionFullIteration
        {
            public static int Executions = 0;

            public static void Execute(Entity entity, SimpleComponent simple, StringComponent str)
            {
                ++Executions;
            }
        }

        [Test]
	    public void PermissionsIterationDisabled()
	    {
            Dec.Config.TestParameters = new Dec.Config.UnitTestParameters { explicitStaticRefs = new System.Type[] { typeof(Decs) } };
	        var parser = new Dec.Parser();
            parser.AddString(@"
                <Decs>
                    <ComponentDec decName=""SimpleComponent"">
                        <type>SimpleComponent</type>
                    </ComponentDec>

                    <ComponentDec decName=""StringComponent"">
                        <type>StringComponent</type>
                    </ComponentDec>

                    <EntityDec decName=""EntityModel"">
                        <components>
                            <li>SimpleComponent</li>
                            <li>StringComponent</li>
                        </components>
                    </EntityDec>

                    <SystemDec decName=""TestSystem"">
                        <type>ComponentPermissionFullIteration</type>
                        <permissions>false</permissions>
                        <iterate>
                            <StringComponent>ReadWrite</StringComponent>
                        </iterate>
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

            Environment.Add(new Entity(Decs.EntityModel));
            Environment.Add(new Entity(Decs.EntityModel));

            ComponentPermissionFullIteration.Executions = 0;
            Environment.Process(Decs.TestProcess);
            Assert.AreEqual(2, ComponentPermissionFullIteration.Executions);
	    }
    }
}
