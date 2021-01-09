namespace Ghi.Test
{
    using NUnit.Framework;
    using System.Linq;

    [TestFixture]
    public class EntityTest : Base
    {
        [Dec.StaticReferences]
        public static class EntityTemplateDecs
        {
            static EntityTemplateDecs() { Dec.StaticReferencesAttribute.Initialized(); }

            public static EntityDec EntityModel;
        }

        [Dec.StaticReferences]
        public static class EntityProcessTemplateDefs
        {
            static EntityProcessTemplateDefs() { Dec.StaticReferencesAttribute.Initialized(); }

            public static ProcessDec TestProcess;
        }

	    [Test]
	    public void Creation()
	    {
            Dec.Config.TestParameters = new Dec.Config.UnitTestParameters { explicitStaticRefs = new System.Type[] { typeof(EntityTemplateDecs) } };
            var parser = new Dec.Parser();
            parser.AddString(@"
                <Decs>
                    <ComponentDec decName=""Component"">
                        <type>SimpleComponent</type>
                    </ComponentDec>

                    <EntityDec decName=""EntityModel"">
                        <components>
                            <li>Component</li>
                        </components>
                    </EntityDec>
                </Decs>
            ");
            parser.Finish();

            Environment.Startup();
            Environment.Add(new Ghi.Entity(EntityTemplateDecs.EntityModel));
            var ents = Environment.List.ToArray();

            Assert.AreEqual(1, ents.Length);
            Assert.IsTrue(ents[0].Component<SimpleComponent>() != null);
	    }

        public static class InactiveTestSystem
        {
            public static void Execute()
            {
                var entity = new Entity(EntityTemplateDecs.EntityModel);
                Environment.Add(entity);    // We intentionally add it first; we should still be able to muck with it

                entity.Component<SimpleComponent>().number = 4;
            }
        }

        [Test]
        public void Inactive()
        {
            Dec.Config.TestParameters = new Dec.Config.UnitTestParameters { explicitStaticRefs = new System.Type[] { typeof(EntityTemplateDecs), typeof(EntityProcessTemplateDefs) } };
            var parser = new Dec.Parser();
            parser.AddString(@"
                <Decs>
                    <ComponentDec decName=""EntityComponent"">
                        <type>SimpleComponent</type>
                    </ComponentDec>

                    <EntityDec decName=""EntityModel"">
                        <components>
                            <li>EntityComponent</li>
                        </components>
                    </EntityDec>

                    <SystemDec decName=""TestSystem"">
                        <type>InactiveTestSystem</type>
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

            Environment.Process(EntityProcessTemplateDefs.TestProcess);

            var ents = Environment.List.ToArray();

            Assert.AreEqual(1, ents.Length);
            Assert.AreEqual(4, ents[0].Component<SimpleComponent>().number);
        }

        [Test]
        public void ExplicitComponent()
        {
            Dec.Config.TestParameters = new Dec.Config.UnitTestParameters { explicitStaticRefs = new System.Type[] { typeof(EntityTemplateDecs) } };
            var parser = new Dec.Parser();
            parser.AddString(@"
                <Decs>
                    <ComponentDec decName=""EntityComponent"">
                        <type>SimpleComponent</type>
                    </ComponentDec>

                    <EntityDec decName=""EntityModel"">
                        <components>
                            <li>EntityComponent</li>
                        </components>
                    </EntityDec>
                </Decs>
            ");
            parser.Finish();

            Environment.Startup();

            var comp = new SimpleComponent();
            var entity = new Entity(EntityTemplateDecs.EntityModel, comp);

            Assert.AreSame(comp, entity.Component<SimpleComponent>());
        }

        [Test]
        public void ExplicitComponentDupe()
        {
            Dec.Config.TestParameters = new Dec.Config.UnitTestParameters { explicitStaticRefs = new System.Type[] { typeof(EntityTemplateDecs) } };
            var parser = new Dec.Parser();
            parser.AddString(@"
                <Decs>
                    <ComponentDec decName=""EntityComponent"">
                        <type>SimpleComponent</type>
                    </ComponentDec>

                    <EntityDec decName=""EntityModel"">
                        <components>
                            <li>EntityComponent</li>
                        </components>
                    </EntityDec>
                </Decs>
            ");
            parser.Finish();

            Environment.Startup();

            var comp = new SimpleComponent();
            ExpectErrors(() => new Entity(EntityTemplateDecs.EntityModel, comp, comp));
        }

        [Test]
        public void ExplicitComponentInvalid()
        {
            Dec.Config.TestParameters = new Dec.Config.UnitTestParameters { explicitStaticRefs = new System.Type[] { typeof(EntityTemplateDecs) } };
            var parser = new Dec.Parser();
            parser.AddString(@"
                <Decs>
                    <ComponentDec decName=""EntityComponent"">
                        <type>SimpleComponent</type>
                    </ComponentDec>

                    <EntityDec decName=""EntityModel"">
                        <components>
                            <li>EntityComponent</li>
                        </components>
                    </EntityDec>
                </Decs>
            ");
            parser.Finish();

            Environment.Startup();

            var comp = new StringComponent();
            ExpectErrors(() => new Entity(EntityTemplateDecs.EntityModel, comp));
        }

        [Test]
        public void ExplicitComponentWrong()
        {
            Dec.Config.TestParameters = new Dec.Config.UnitTestParameters { explicitStaticRefs = new System.Type[] { typeof(EntityTemplateDecs) } };
            var parser = new Dec.Parser();
            parser.AddString(@"
                <Decs>
                    <ComponentDec decName=""EntityComponent"">
                        <type>SimpleComponent</type>
                    </ComponentDec>

                    <ComponentDec decName=""NonEntityComponent"">
                        <type>StringComponent</type>
                    </ComponentDec>

                    <EntityDec decName=""EntityModel"">
                        <components>
                            <li>EntityComponent</li>
                        </components>
                    </EntityDec>
                </Decs>
            ");
            parser.Finish();

            Environment.Startup();

            var comp = new StringComponent();
            ExpectErrors(() => new Entity(EntityTemplateDecs.EntityModel, comp));
        }

        public class DerivedComponent : SimpleComponent
        {

        }

        [Test]
        public void ExplicitComponentDerived()
        {
            Dec.Config.TestParameters = new Dec.Config.UnitTestParameters { explicitStaticRefs = new System.Type[] { typeof(EntityTemplateDecs) } };
            var parser = new Dec.Parser();
            parser.AddString(@"
                <Decs>
                    <ComponentDec decName=""EntityComponent"">
                        <type>SimpleComponent</type>
                    </ComponentDec>

                    <EntityDec decName=""EntityModel"">
                        <components>
                            <li>EntityComponent</li>
                        </components>
                    </EntityDec>
                </Decs>
            ");
            parser.Finish();

            Environment.Startup();

            var comp = new DerivedComponent();
            var entity = new Entity(EntityTemplateDecs.EntityModel, comp);

            Assert.AreSame(comp, entity.Component<SimpleComponent>());
        }

        [Test]
	    public void ToStringNonexistent()
	    {
            Dec.Config.TestParameters = new Dec.Config.UnitTestParameters { explicitStaticRefs = new System.Type[] { typeof(EntityTemplateDecs) } };
            var parser = new Dec.Parser();
            parser.AddString(@"
                <Decs>
                    <ComponentDec decName=""Component"">
                        <type>SimpleComponent</type>
                    </ComponentDec>

                    <EntityDec decName=""EntityModel"">
                        <components>
                            <li>Component</li>
                        </components>
                    </EntityDec>
                </Decs>
            ");
            parser.Finish();

            Environment.Startup();
            Environment.Add(new Ghi.Entity(EntityTemplateDecs.EntityModel));
            var ents = Environment.List.ToArray();
            ents[0].ToString(); // we're just checking to make sure it doesn't crash, we actually don't care what it outputs
	    }

        [Test]
	    public void ToStringExistent()
	    {
            Dec.Config.TestParameters = new Dec.Config.UnitTestParameters { explicitStaticRefs = new System.Type[] { typeof(EntityTemplateDecs) } };
            var parser = new Dec.Parser();
            parser.AddString(@"
                <Decs>
                    <ComponentDec decName=""Component"">
                        <type>SimpleComponent</type>
                    </ComponentDec>

                    <EntityDec decName=""EntityModel"">
                        <components>
                            <li>Component</li>
                        </components>
                    </EntityDec>
                </Decs>
            ");
            parser.Finish();

            Environment.Startup(toString: e => "ToStringTest");
            Environment.Add(new Ghi.Entity(EntityTemplateDecs.EntityModel));
            var ents = Environment.List.ToArray();
            Assert.AreEqual("ToStringTest", ents[0].ToString());
	    }
    }
}
