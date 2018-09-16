namespace Ghi.Test
{
    using NUnit.Framework;
    using System.Linq;

    [TestFixture]
    public class Components : Base
    {
        [Def.StaticReferences]
        public static class SystemTestDefs
        {
            static SystemTestDefs() { Def.StaticReferences.Initialized(); }

            public static ProcessDef TestProcess;
            public static EntityDef EntityModel;
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
	        var parser = new Def.Parser(explicitStaticRefs: new System.Type[] { typeof(SystemTestDefs) });
            parser.AddString(@"
                <Defs>
                    <ComponentDef defName=""SimpleComponent"">
                        <type>SimpleComponent</type>
                    </ComponentDef>

                    <ComponentDef defName=""StringComponent"">
                        <type>StringComponent</type>
                    </ComponentDef>

                    <EntityDef defName=""EntityModel"">
                        <components>
                            <li>SimpleComponent</li>
                            <li>StringComponent</li>
                        </components>
                    </EntityDef>

                    <SystemDef defName=""TestSystem"">
                        <type>ComponentPermissionRwSystem</type>
                        <iterate>
                            <SimpleComponent>ReadWrite</SimpleComponent>
                            <StringComponent>ReadWrite</StringComponent>
                        </iterate>
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

            Environment.Add(new Entity(SystemTestDefs.EntityModel));
            Environment.Add(new Entity(SystemTestDefs.EntityModel));

            ComponentPermissionRwSystem.Executions = 0;
            Environment.Process(SystemTestDefs.TestProcess);
            Assert.AreEqual(2, ComponentPermissionRwSystem.Executions);
	    }

        [Test]
	    public void PermissionsRwRo()
	    {
	        var parser = new Def.Parser(explicitStaticRefs: new System.Type[] { typeof(SystemTestDefs) });
            parser.AddString(@"
                <Defs>
                    <ComponentDef defName=""SimpleComponent"">
                        <type>SimpleComponent</type>
                    </ComponentDef>

                    <ComponentDef defName=""StringComponent"">
                        <type>StringComponent</type>
                    </ComponentDef>

                    <EntityDef defName=""EntityModel"">
                        <components>
                            <li>SimpleComponent</li>
                            <li>StringComponent</li>
                        </components>
                    </EntityDef>

                    <SystemDef defName=""TestSystem"">
                        <type>ComponentPermissionRwSystem</type>
                        <iterate>
                            <SimpleComponent>ReadOnly</SimpleComponent>
                            <StringComponent>ReadWrite</StringComponent>
                        </iterate>
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

            Environment.Add(new Entity(SystemTestDefs.EntityModel));
            Environment.Add(new Entity(SystemTestDefs.EntityModel));

            ComponentPermissionRwSystem.Executions = 0;
            ExpectErrors(() => Environment.Process(SystemTestDefs.TestProcess));
            Assert.AreEqual(2, ComponentPermissionRwSystem.Executions);
	    }

        [Test]
	    public void PermissionsRwNo()
	    {
	        var parser = new Def.Parser(explicitStaticRefs: new System.Type[] { typeof(SystemTestDefs) });
            parser.AddString(@"
                <Defs>
                    <ComponentDef defName=""SimpleComponent"">
                        <type>SimpleComponent</type>
                    </ComponentDef>

                    <ComponentDef defName=""StringComponent"">
                        <type>StringComponent</type>
                    </ComponentDef>

                    <EntityDef defName=""EntityModel"">
                        <components>
                            <li>SimpleComponent</li>
                            <li>StringComponent</li>
                        </components>
                    </EntityDef>

                    <SystemDef defName=""TestSystem"">
                        <type>ComponentPermissionRwSystem</type>
                        <iterate>
                            <StringComponent>ReadWrite</StringComponent>
                        </iterate>
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

            Environment.Add(new Entity(SystemTestDefs.EntityModel));
            Environment.Add(new Entity(SystemTestDefs.EntityModel));

            ComponentPermissionRwSystem.Executions = 0;
            ExpectErrors(() => Environment.Process(SystemTestDefs.TestProcess));
            Assert.AreEqual(2, ComponentPermissionRwSystem.Executions);
	    }

        [Test]
	    public void PermissionsRoRw()
	    {
	        var parser = new Def.Parser(explicitStaticRefs: new System.Type[] { typeof(SystemTestDefs) });
            parser.AddString(@"
                <Defs>
                    <ComponentDef defName=""SimpleComponent"">
                        <type>SimpleComponent</type>
                    </ComponentDef>

                    <ComponentDef defName=""StringComponent"">
                        <type>StringComponent</type>
                    </ComponentDef>

                    <EntityDef defName=""EntityModel"">
                        <components>
                            <li>SimpleComponent</li>
                            <li>StringComponent</li>
                        </components>
                    </EntityDef>

                    <SystemDef defName=""TestSystem"">
                        <type>ComponentPermissionRoSystem</type>
                        <iterate>
                            <SimpleComponent>ReadWrite</SimpleComponent>
                            <StringComponent>ReadWrite</StringComponent>
                        </iterate>
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

            Environment.Add(new Entity(SystemTestDefs.EntityModel));
            Environment.Add(new Entity(SystemTestDefs.EntityModel));

            ComponentPermissionRoSystem.Executions = 0;
            Environment.Process(SystemTestDefs.TestProcess);
            Assert.AreEqual(2, ComponentPermissionRoSystem.Executions);
	    }

        [Test]
	    public void PermissionsRoRo()
	    {
	        var parser = new Def.Parser(explicitStaticRefs: new System.Type[] { typeof(SystemTestDefs) });
            parser.AddString(@"
                <Defs>
                    <ComponentDef defName=""SimpleComponent"">
                        <type>SimpleComponent</type>
                    </ComponentDef>

                    <ComponentDef defName=""StringComponent"">
                        <type>StringComponent</type>
                    </ComponentDef>

                    <EntityDef defName=""EntityModel"">
                        <components>
                            <li>SimpleComponent</li>
                            <li>StringComponent</li>
                        </components>
                    </EntityDef>

                    <SystemDef defName=""TestSystem"">
                        <type>ComponentPermissionRoSystem</type>
                        <iterate>
                            <SimpleComponent>ReadOnly</SimpleComponent>
                            <StringComponent>ReadWrite</StringComponent>
                        </iterate>
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

            Environment.Add(new Entity(SystemTestDefs.EntityModel));
            Environment.Add(new Entity(SystemTestDefs.EntityModel));

            ComponentPermissionRoSystem.Executions = 0;
            Environment.Process(SystemTestDefs.TestProcess);
            Assert.AreEqual(2, ComponentPermissionRoSystem.Executions);
	    }

        [Test]
	    public void PermissionsRoNo()
	    {
	        var parser = new Def.Parser(explicitStaticRefs: new System.Type[] { typeof(SystemTestDefs) });
            parser.AddString(@"
                <Defs>
                    <ComponentDef defName=""SimpleComponent"">
                        <type>SimpleComponent</type>
                    </ComponentDef>

                    <ComponentDef defName=""StringComponent"">
                        <type>StringComponent</type>
                    </ComponentDef>

                    <EntityDef defName=""EntityModel"">
                        <components>
                            <li>SimpleComponent</li>
                            <li>StringComponent</li>
                        </components>
                    </EntityDef>

                    <SystemDef defName=""TestSystem"">
                        <type>ComponentPermissionRoSystem</type>
                        <iterate>
                            <StringComponent>ReadWrite</StringComponent>
                        </iterate>
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

            Environment.Add(new Entity(SystemTestDefs.EntityModel));
            Environment.Add(new Entity(SystemTestDefs.EntityModel));

            ComponentPermissionRoSystem.Executions = 0;
            ExpectErrors(() => Environment.Process(SystemTestDefs.TestProcess));
            Assert.AreEqual(2, ComponentPermissionRoSystem.Executions);
	    }
    }
}
