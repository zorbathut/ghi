namespace Ghi.Test
{
    using NUnit.Framework;
    using System.Linq;

    [TestFixture]
    public class Iteration : Base
    {
        [Def.StaticReferences]
        public static class SystemTestDefs
        {
            static SystemTestDefs() { Def.StaticReferences.Initialized(); }

            public static ProcessDef TestProcess;
            public static EntityDef EntityModel;
        }

        public static class IterationSystem
        {
            public static int Executions = 0;
            public static void Execute(SimpleComponent simple) { ++Executions; simple.number = Executions; }
        }

	    [Test]
	    public void Basic()
	    {
	        var parser = new Def.Parser(explicitStaticRefs: new System.Type[] { typeof(SystemTestDefs) });
            parser.AddString(@"
                <Defs>
                    <ComponentDef defName=""Component"">
                        <type>SimpleComponent</type>
                    </ComponentDef>

                    <EntityDef defName=""EntityModel"">
                        <components>
                            <li>Component</li>
                        </components>
                    </EntityDef>

                    <SystemDef defName=""TestSystem"">
                        <type>IterationSystem</type>
                        <iterate>
                            <Component>ReadWrite</Component>
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

            IterationSystem.Executions = 0;
            Environment.Process(SystemTestDefs.TestProcess);
            Assert.AreEqual(2, IterationSystem.Executions);

            Entity[] entities = Environment.List.OrderBy(e => e.Component<SimpleComponent>().number).ToArray();
            Assert.AreEqual(1, entities[0].Component<SimpleComponent>().number);
            Assert.AreEqual(2, entities[1].Component<SimpleComponent>().number);
        }

        public static class IterationAddSystem
        {
            public static void Execute(Entity simple) { Environment.Add(new Entity(SystemTestDefs.EntityModel)); }
        }

        [Test]
	    public void Addition()
	    {
	        var parser = new Def.Parser(explicitStaticRefs: new System.Type[] { typeof(SystemTestDefs) });
            parser.AddString(@"
                <Defs>
                    <ComponentDef defName=""Component"">
                        <type>SimpleComponent</type>
                    </ComponentDef>

                    <EntityDef defName=""EntityModel"">
                        <components>
                            <li>Component</li>
                        </components>
                    </EntityDef>

                    <SystemDef defName=""TestSystem"">
                        <type>IterationAddSystem</type>
                        <iterate>
                            <Component>ReadWrite</Component>
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

            Assert.AreEqual(2, Environment.List.Count());
            Environment.Process(SystemTestDefs.TestProcess);
            Assert.AreEqual(4, Environment.List.Count());
        }

        public static class IterationRemoveSystem
        {
            public static void Execute(Entity simple) { Environment.Remove(simple); }
        }

        [Test]
	    public void Removal()
	    {
	        var parser = new Def.Parser(explicitStaticRefs: new System.Type[] { typeof(SystemTestDefs) });
            parser.AddString(@"
                <Defs>
                    <ComponentDef defName=""Component"">
                        <type>SimpleComponent</type>
                    </ComponentDef>

                    <EntityDef defName=""EntityModel"">
                        <components>
                            <li>Component</li>
                        </components>
                    </EntityDef>

                    <SystemDef defName=""TestSystem"">
                        <type>IterationRemoveSystem</type>
                        <iterate>
                            <Component>ReadWrite</Component>
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

            Assert.AreEqual(2, Environment.List.Count());
            Environment.Process(SystemTestDefs.TestProcess);
            Assert.AreEqual(0, Environment.List.Count());
        }
    }
}
