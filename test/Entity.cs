namespace Ghi.Test
{
    using NUnit.Framework;
    using System.Linq;

    [TestFixture]
    public class EntityTest : Base
    {
        [Def.StaticReferences]
        public static class EntityTemplateDefs
        {
            static EntityTemplateDefs() { Def.StaticReferences.Initialized(); }

            public static EntityDef EntityModel;
        }

        [Def.StaticReferences]
        public static class EntityProcessTemplateDefs
        {
            static EntityProcessTemplateDefs() { Def.StaticReferences.Initialized(); }

            public static ProcessDef TestProcess;
        }

	    [Test]
	    public void Creation()
	    {
	        var parser = new Def.Parser(explicitStaticRefs: new System.Type[] { typeof(EntityTemplateDefs) });
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
                </Defs>
            ");
            parser.Finish();

            Environment.Startup();
            Environment.Add(new Ghi.Entity(EntityTemplateDefs.EntityModel));
            var ents = Environment.List.ToArray();

            Assert.AreEqual(1, ents.Length);
            Assert.IsTrue(ents[0].Component<SimpleComponent>() != null);
	    }

        public static class InactiveTestSystem
        {
            public static void Execute()
            {
                var entity = new Entity(EntityTemplateDefs.EntityModel);
                Environment.Add(entity);    // We intentionally add it first; we should still be able to muck with it

                entity.Component<SimpleComponent>().number = 4;
            }
        }

        [Test]
        public void Inactive()
        {
            var parser = new Def.Parser(explicitStaticRefs: new System.Type[] { typeof(EntityTemplateDefs), typeof(EntityProcessTemplateDefs) });
            parser.AddString(@"
                <Defs>
                    <ComponentDef defName=""EntityComponent"">
                        <type>SimpleComponent</type>
                    </ComponentDef>

                    <EntityDef defName=""EntityModel"">
                        <components>
                            <li>EntityComponent</li>
                        </components>
                    </EntityDef>

                    <SystemDef defName=""TestSystem"">
                        <type>InactiveTestSystem</type>
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

            Environment.Process(EntityProcessTemplateDefs.TestProcess);

            var ents = Environment.List.ToArray();

            Assert.AreEqual(1, ents.Length);
            Assert.AreEqual(4, ents[0].Component<SimpleComponent>().number);
        }

        [Test]
        public void ExplicitComponent()
        {
            var parser = new Def.Parser(explicitStaticRefs: new System.Type[] { typeof(EntityTemplateDefs) });
            parser.AddString(@"
                <Defs>
                    <ComponentDef defName=""EntityComponent"">
                        <type>SimpleComponent</type>
                    </ComponentDef>

                    <EntityDef defName=""EntityModel"">
                        <components>
                            <li>EntityComponent</li>
                        </components>
                    </EntityDef>
                </Defs>
            ");
            parser.Finish();

            Environment.Startup();

            var comp = new SimpleComponent();
            var entity = new Entity(EntityTemplateDefs.EntityModel, comp);

            Assert.AreSame(comp, entity.Component<SimpleComponent>());
        }

        [Test]
        public void ExplicitComponentDupe()
        {
            var parser = new Def.Parser(explicitStaticRefs: new System.Type[] { typeof(EntityTemplateDefs) });
            parser.AddString(@"
                <Defs>
                    <ComponentDef defName=""EntityComponent"">
                        <type>SimpleComponent</type>
                    </ComponentDef>

                    <EntityDef defName=""EntityModel"">
                        <components>
                            <li>EntityComponent</li>
                        </components>
                    </EntityDef>
                </Defs>
            ");
            parser.Finish();

            Environment.Startup();

            var comp = new SimpleComponent();
            ExpectErrors(() => new Entity(EntityTemplateDefs.EntityModel, comp, comp));
        }

        [Test]
        public void ExplicitComponentInvalid()
        {
            var parser = new Def.Parser(explicitStaticRefs: new System.Type[] { typeof(EntityTemplateDefs) });
            parser.AddString(@"
                <Defs>
                    <ComponentDef defName=""EntityComponent"">
                        <type>SimpleComponent</type>
                    </ComponentDef>

                    <EntityDef defName=""EntityModel"">
                        <components>
                            <li>EntityComponent</li>
                        </components>
                    </EntityDef>
                </Defs>
            ");
            parser.Finish();

            Environment.Startup();

            var comp = new StringComponent();
            ExpectErrors(() => new Entity(EntityTemplateDefs.EntityModel, comp));
        }

        [Test]
        public void ExplicitComponentWrong()
        {
            var parser = new Def.Parser(explicitStaticRefs: new System.Type[] { typeof(EntityTemplateDefs) });
            parser.AddString(@"
                <Defs>
                    <ComponentDef defName=""EntityComponent"">
                        <type>SimpleComponent</type>
                    </ComponentDef>

                    <ComponentDef defName=""NonEntityComponent"">
                        <type>StringComponent</type>
                    </ComponentDef>

                    <EntityDef defName=""EntityModel"">
                        <components>
                            <li>EntityComponent</li>
                        </components>
                    </EntityDef>
                </Defs>
            ");
            parser.Finish();

            Environment.Startup();

            var comp = new StringComponent();
            ExpectErrors(() => new Entity(EntityTemplateDefs.EntityModel, comp));
        }

        public class DerivedComponent : SimpleComponent
        {

        }

        [Test]
        public void ExplicitComponentDerived()
        {
            var parser = new Def.Parser(explicitStaticRefs: new System.Type[] { typeof(EntityTemplateDefs) });
            parser.AddString(@"
                <Defs>
                    <ComponentDef defName=""EntityComponent"">
                        <type>SimpleComponent</type>
                    </ComponentDef>

                    <EntityDef defName=""EntityModel"">
                        <components>
                            <li>EntityComponent</li>
                        </components>
                    </EntityDef>
                </Defs>
            ");
            parser.Finish();

            Environment.Startup();

            var comp = new DerivedComponent();
            var entity = new Entity(EntityTemplateDefs.EntityModel, comp);

            Assert.AreSame(comp, entity.Component<SimpleComponent>());
        }
    }
}
