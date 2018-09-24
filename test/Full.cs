namespace Ghi.Test
{
    using NUnit.Framework;
    using System.Linq;

    [TestFixture]
    public class Full : Base
    {
        [Def.StaticReferences]
        public static class FullTestDefs
        {
            static FullTestDefs() { Def.StaticReferences.Initialized(); }

            public static ProcessDef TestProcess;
            public static EntityDef Entity;
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
	        var parser = new Def.Parser(explicitStaticRefs: new System.Type[] { typeof(FullTestDefs) });
            parser.AddString(@"
                <Defs>
                    <ComponentDef defName=""Component"">
                        <type>SimpleComponent</type>
                    </ComponentDef>

                    <EntityDef defName=""Entity"">
                        <components>
                            <li>Component</li>
                        </components>
                    </EntityDef>

                    <SystemDef defName=""TestSystem"">
                        <type>FullPermissionRwSystem</type>
                        <full>
                            <Component>ReadWrite</Component>
                        </full>
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
            Environment.Add(new Entity(FullTestDefs.Entity));

            FullPermissionRwSystem.Executions = 0;
            Environment.Process(FullTestDefs.TestProcess);
            Assert.AreEqual(2, FullPermissionRoSystem.Executions);
	    }

        [Test]
	    public void PermissionsRwRo()
	    {
	        var parser = new Def.Parser(explicitStaticRefs: new System.Type[] { typeof(FullTestDefs) });
            parser.AddString(@"
                <Defs>
                    <ComponentDef defName=""Component"">
                        <type>SimpleComponent</type>
                    </ComponentDef>

                    <EntityDef defName=""Entity"">
                        <components>
                            <li>Component</li>
                        </components>
                    </EntityDef>

                    <SystemDef defName=""TestSystem"">
                        <type>FullPermissionRwSystem</type>
                        <full>
                            <Component>ReadOnly</Component>
                        </full>
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
            Environment.Add(new Entity(FullTestDefs.Entity));

            FullPermissionRwSystem.Executions = 0;
            ExpectErrors(() => Environment.Process(FullTestDefs.TestProcess));
            Assert.AreEqual(2, FullPermissionRoSystem.Executions);
	    }

        [Test]
	    public void PermissionsRwNo()
	    {
	        var parser = new Def.Parser(explicitStaticRefs: new System.Type[] { typeof(FullTestDefs) });
            parser.AddString(@"
                <Defs>
                    <ComponentDef defName=""Component"">
                        <type>SimpleComponent</type>
                    </ComponentDef>

                    <EntityDef defName=""Entity"">
                        <components>
                            <li>Component</li>
                        </components>
                    </EntityDef>

                    <SystemDef defName=""TestSystem"">
                        <type>FullPermissionRwSystem</type>
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
            Environment.Add(new Entity(FullTestDefs.Entity));

            FullPermissionRwSystem.Executions = 0;
            ExpectErrors(() => Environment.Process(FullTestDefs.TestProcess));
            Assert.AreEqual(2, FullPermissionRoSystem.Executions);
	    }

        [Test]
	    public void PermissionsRoRw()
	    {
	        var parser = new Def.Parser(explicitStaticRefs: new System.Type[] { typeof(FullTestDefs) });
            parser.AddString(@"
                <Defs>
                    <ComponentDef defName=""Component"">
                        <type>SimpleComponent</type>
                    </ComponentDef>

                    <EntityDef defName=""Entity"">
                        <components>
                            <li>Component</li>
                        </components>
                    </EntityDef>

                    <SystemDef defName=""TestSystem"">
                        <type>FullPermissionRoSystem</type>
                        <full>
                            <Component>ReadWrite</Component>
                        </full>
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
            Environment.Add(new Entity(FullTestDefs.Entity));

            FullPermissionRoSystem.Executions = 0;
            Environment.Process(FullTestDefs.TestProcess);
            Assert.AreEqual(2, FullPermissionRoSystem.Executions);
	    }

        [Test]
	    public void PermissionsRoRo()
	    {
	        var parser = new Def.Parser(explicitStaticRefs: new System.Type[] { typeof(FullTestDefs) });
            parser.AddString(@"
                <Defs>
                    <ComponentDef defName=""Component"">
                        <type>SimpleComponent</type>
                    </ComponentDef>

                    <EntityDef defName=""Entity"">
                        <components>
                            <li>Component</li>
                        </components>
                    </EntityDef>

                    <SystemDef defName=""TestSystem"">
                        <type>FullPermissionRoSystem</type>
                        <full>
                            <Component>ReadOnly</Component>
                        </full>
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
            Environment.Add(new Entity(FullTestDefs.Entity));

            FullPermissionRoSystem.Executions = 0;
            Environment.Process(FullTestDefs.TestProcess);
            Assert.AreEqual(2, FullPermissionRoSystem.Executions);
	    }

        [Test]
	    public void PermissionsRoNo()
	    {
	        var parser = new Def.Parser(explicitStaticRefs: new System.Type[] { typeof(FullTestDefs) });
            parser.AddString(@"
                <Defs>
                    <ComponentDef defName=""Component"">
                        <type>SimpleComponent</type>
                    </ComponentDef>

                    <EntityDef defName=""Entity"">
                        <components>
                            <li>Component</li>
                        </components>
                    </EntityDef>

                    <SystemDef defName=""TestSystem"">
                        <type>FullPermissionRoSystem</type>
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
            Environment.Add(new Entity(FullTestDefs.Entity));

            FullPermissionRoSystem.Executions = 0;
            ExpectErrors(() => Environment.Process(FullTestDefs.TestProcess));
            Assert.AreEqual(2, FullPermissionRoSystem.Executions);
	    }
    }
}
