namespace Ghi.Test
{
    using NUnit.Framework;
    using System.Collections.Generic;
    using System.Linq;

    [TestFixture]
    public class Iteration : Base
    {
        [Dec.StaticReferences]
        public static class Decs
        {
            static Decs() { Dec.StaticReferencesAttribute.Initialized(); }

            public static ProcessDec TestProcess;
            public static EntityDec EntityModel;
        }

        public static class IterationSystem
        {
            public static int Executions = 0;
            public static void Execute(SimpleComponent simple) { ++Executions; simple.number = Executions; }
        }

	    [Test]
	    public void Basic()
	    {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitStaticRefs = new System.Type[] { typeof(Decs) } });
            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <ComponentDec decName=""Component"">
                        <type>SimpleComponent</type>
                    </ComponentDec>

                    <EntityDec decName=""EntityModel"">
                        <components>
                            <li>Component</li>
                        </components>
                    </EntityDec>

                    <SystemDec decName=""TestSystem"">
                        <type>IterationSystem</type>
                        <iterate>
                            <Component>ReadWrite</Component>
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

            IterationSystem.Executions = 0;
            Environment.Process(Decs.TestProcess);
            Assert.AreEqual(2, IterationSystem.Executions);

            Entity[] entities = Environment.List.OrderBy(e => e.Component<SimpleComponent>().number).ToArray();
            Assert.AreEqual(1, entities[0].Component<SimpleComponent>().number);
            Assert.AreEqual(2, entities[1].Component<SimpleComponent>().number);
        }

        public static class IterationAddSystem
        {
            public static void Execute(Entity simple) { Environment.Add(new Entity(Decs.EntityModel)); }
        }

        [Test]
	    public void Addition()
	    {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitStaticRefs = new System.Type[] { typeof(Decs) } });
            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <ComponentDec decName=""Component"">
                        <type>SimpleComponent</type>
                    </ComponentDec>

                    <EntityDec decName=""EntityModel"">
                        <components>
                            <li>Component</li>
                        </components>
                    </EntityDec>

                    <SystemDec decName=""TestSystem"">
                        <type>IterationAddSystem</type>
                        <iterate>
                            <Component>ReadWrite</Component>
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

            Assert.AreEqual(2, Environment.List.Count());
            Environment.Process(Decs.TestProcess);
            Assert.AreEqual(4, Environment.List.Count());
        }

        public static class IterationRemoveSystem
        {
            public static void Execute(Entity simple) { Environment.Remove(simple); }
        }

        [Test]
	    public void Removal()
	    {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitStaticRefs = new System.Type[] { typeof(Decs) } });
            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <ComponentDec decName=""Component"">
                        <type>SimpleComponent</type>
                    </ComponentDec>

                    <EntityDec decName=""EntityModel"">
                        <components>
                            <li>Component</li>
                        </components>
                    </EntityDec>

                    <SystemDec decName=""TestSystem"">
                        <type>IterationRemoveSystem</type>
                        <iterate>
                            <Component>ReadWrite</Component>
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

            Assert.AreEqual(2, Environment.List.Count());
            Environment.Process(Decs.TestProcess);
            Assert.AreEqual(0, Environment.List.Count());
        }

        // IterationIndex test
        // ----
        // This tests for a specific rather bizarre indexing issue involving using the wrong index. I doubt this exact bug will happen again, but, hey, extra validation.

        [Dec.StaticReferences]
        public static class IterationIndexDefs
        {
            static IterationIndexDefs() { Dec.StaticReferencesAttribute.Initialized(); }

            public static EntityDec IterationIndexEntityA;
            public static EntityDec IterationIndexEntityB;
            public static ProcessDec IterationIndexProcess;
        }

        public static class IterationIndexSystemA
        {
            public static List<Entity> Touched = new List<Entity>();
            public static void Execute(Entity entity) { Touched.Add(entity); }
        }

        public static class IterationIndexSystemB
        {
            public static List<Entity> Touched = new List<Entity>();
            public static void Execute(Entity entity) { Touched.Add(entity); }
        }

        [Test]
	    public void IterationIndex()
	    {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitStaticRefs = new System.Type[] { typeof(IterationIndexDefs) } });
            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <ComponentDec decName=""ComponentA"">
                        <type>SimpleComponent</type>
                    </ComponentDec>

                    <ComponentDec decName=""ComponentB"">
                        <type>StringComponent</type>
                    </ComponentDec>

                    <EntityDec decName=""IterationIndexEntityA"">
                        <components>
                            <li>ComponentA</li>
                        </components>
                    </EntityDec>

                    <EntityDec decName=""IterationIndexEntityB"">
                        <components>
                            <li>ComponentB</li>
                        </components>
                    </EntityDec>

                    <SystemDec decName=""IterationIndexSystemA"">
                        <type>IterationIndexSystemA</type>
                        <iterate>
                            <ComponentA>ReadWrite</ComponentA>
                        </iterate>
                    </SystemDec>

                    <SystemDec decName=""IterationIndexSystemB"">
                        <type>IterationIndexSystemB</type>
                        <iterate>
                            <ComponentB>ReadWrite</ComponentB>
                        </iterate>
                    </SystemDec>

                    <ProcessDec decName=""IterationIndexProcess"">
                        <order>
                            <li>IterationIndexSystemA</li>
                            <li>IterationIndexSystemB</li>
                        </order>
                    </ProcessDec>
                </Decs>
            ");
            parser.Finish();

            Environment.Startup();

            Environment.Add(new Entity(IterationIndexDefs.IterationIndexEntityA));
            Environment.Add(new Entity(IterationIndexDefs.IterationIndexEntityB));

            Environment.Process(IterationIndexDefs.IterationIndexProcess);

            Assert.AreEqual(1, IterationIndexSystemA.Touched.Count);
            Assert.AreEqual(1, IterationIndexSystemA.Touched.Count);
            Assert.AreEqual(2, Enumerable.Union(IterationIndexSystemA.Touched, IterationIndexSystemB.Touched).Count());
        }
    }
}
