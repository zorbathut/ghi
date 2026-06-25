using NUnit.Framework;
using System.Linq;

namespace Ghi.Test
{

    public class EntityTest : Base
    {
        public EntityTest(EmitMode emitMode) : base(emitMode) { }

        [Dec.StaticReferences]
        public static class EntityTemplateDecs
        {
            static EntityTemplateDecs() { Dec.StaticReferencesAttribute.Initialized(); }

            public static EntityDec EntityModel;
        }

        [Dec.StaticReferences]
        public static class EntityProcessTemplateDecs
        {
            static EntityProcessTemplateDecs() { Dec.StaticReferencesAttribute.Initialized(); }

            public static ProcessDec TestProcess;
        }

	    [Test]
	    public void Creation()
	    {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitStaticRefs = new System.Type[] { typeof(EntityTemplateDecs) } });
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
                </Decs>
            ");
            parser.Finish();

            Environment.Init();
            var env = new Environment();
            using var envActive = new Environment.Scope(env);

            env.Add(EntityTemplateDecs.EntityModel);
            var ents = env.List.ToArray();

            Assert.AreEqual(1, ents.Length);
            Assert.IsTrue(ents[0].ComponentRO<SimpleComponent>() != null);
	    }

        public static class InactiveTestSystem
        {
            public static void Execute()
            {
                var entity = Environment.Current.Value.Add(EntityTemplateDecs.EntityModel);
                entity.ComponentRW<SimpleComponent>().number = 4;
            }
        }

        public static class DeferredComparisonSystem
        {
            public static Entity deferredEntity;

            public static void Execute()
            {
                // Create an entity and store it while it's in deferred state
                // Don't access any components or call Resolve() - keep it deferred
                deferredEntity = Environment.Current.Value.Add(EntityTemplateDecs.EntityModel);
            }
        }

        [Test]
        public void Inactive()
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitStaticRefs = new System.Type[] { typeof(EntityTemplateDecs), typeof(EntityProcessTemplateDecs) } });
            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
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

            Environment.Init();
            var env = new Environment();
            using var envActive = new Environment.Scope(env);

            env.Process(EntityProcessTemplateDecs.TestProcess);

            var ents = env.List.ToArray();

            Assert.AreEqual(1, ents.Length);
            Assert.AreEqual(4, ents[0].ComponentRO<SimpleComponent>().number);
        }

        [Test] [Ignore("Explicit components not currently implemented")]
        public void ExplicitComponent()
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitStaticRefs = new System.Type[] { typeof(EntityTemplateDecs) } });
            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
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

            Environment.Init();
            var env = new Environment();
            using var envActive = new Environment.Scope(env);

            var comp = new SimpleComponent();
            //var entity = new Entity(EntityTemplateDecs.EntityModel, comp);

            //Assert.AreSame(comp, entity.Component<SimpleComponent>());
        }

        [Test] [Ignore("Explicit components not currently implemented")]
        public void ExplicitComponentDupe()
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitStaticRefs = new System.Type[] { typeof(EntityTemplateDecs) } });
            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
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

            Environment.Init();
            var env = new Environment();
            using var envActive = new Environment.Scope(env);

            var comp = new SimpleComponent();
            //ExpectErrors(() => new Entity(EntityTemplateDecs.EntityModel, comp, comp));
        }

        [Test] [Ignore("Explicit components not currently implemented")]
        public void ExplicitComponentInvalid()
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitStaticRefs = new System.Type[] { typeof(EntityTemplateDecs) } });
            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
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

            Environment.Init();
            var env = new Environment();
            using var envActive = new Environment.Scope(env);

            var comp = new StringComponent();
            //ExpectErrors(() => new Entity(EntityTemplateDecs.EntityModel, comp));
        }

        [Test] [Ignore("Explicit components not currently implemented")]
        public void ExplicitComponentWrong()
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitStaticRefs = new System.Type[] { typeof(EntityTemplateDecs) } });
            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
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

            Environment.Init();
            var env = new Environment();
            using var envActive = new Environment.Scope(env);

            var comp = new StringComponent();
            //ExpectErrors(() => new Entity(EntityTemplateDecs.EntityModel, comp));
        }

        public class DerivedComponent : SimpleComponent
        {

        }

        [Test] [Ignore("Explicit components not currently implemented")]
        public void ExplicitComponentDerived()
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitStaticRefs = new System.Type[] { typeof(EntityTemplateDecs) } });
            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
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

            Environment.Init();
            var env = new Environment();
            using var envActive = new Environment.Scope(env);

            var comp = new DerivedComponent();
            //var entity = env.Add(EntityTemplateDecs.EntityModel, comp);

            //Assert.AreSame(comp, entity.Component<SimpleComponent>());
        }

        [Test]
	    public void ToStringNonexistent()
	    {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitStaticRefs = new System.Type[] { typeof(EntityTemplateDecs) } });
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
                </Decs>
            ");
            parser.Finish();

            Environment.Init();
            var env = new Environment();
            using var envActive = new Environment.Scope(env);

            env.Add(EntityTemplateDecs.EntityModel);
            var ents = env.List.ToArray();
            ents[0].ToString(); // we're just checking to make sure it doesn't crash, we actually don't care what it outputs
	    }

        [Test] [Ignore("ToString not currently implemented")]
	    public void ToStringExistent()
	    {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitStaticRefs = new System.Type[] { typeof(EntityTemplateDecs) } });
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
                </Decs>
            ");
            parser.Finish();

            Environment.Init();
            var env = new Environment();
            using var envActive = new Environment.Scope(env);

            /*Environment.Startup(toString: e => "ToStringTest");
            Environment.Add(new Ghi.Entity(EntityTemplateDecs.EntityModel));
            var ents = Environment.List.ToArray();
            Assert.AreEqual("ToStringTest", ents[0].ToString());*/
	    }

        [Test]
        public void Comparison()
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitStaticRefs = new System.Type[] { typeof(EntityTemplateDecs) } });
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
                </Decs>
            ");
            parser.Finish();

            Environment.Init();
            var env = new Environment();
            using var envActive = new Environment.Scope(env);

            var entity1 = env.Add(EntityTemplateDecs.EntityModel);
            var entity2 = env.Add(EntityTemplateDecs.EntityModel);
            var entity2copy = entity2;

            var comp1 = Ghi.EntityComponent<SimpleComponent>.From(entity1);
            var comp2 = Ghi.EntityComponent<SimpleComponent>.From(entity2);
            var comp2copy = Ghi.EntityComponent<SimpleComponent>.From(entity2copy);

            Assert.AreNotEqual(entity1, entity2);
            Assert.AreNotEqual(comp1, comp2);

            Assert.AreEqual(entity2, entity2copy);
            Assert.AreEqual(comp2, comp2copy);

            env.Remove(entity1);
            env.Remove(entity2);

            Assert.AreNotEqual(entity1, entity2);
            Assert.AreNotEqual(comp1, comp2);

            // these aren't really guaranteed, but right now this is how it works
            Assert.AreEqual(entity2, entity2copy);
            Assert.AreEqual(comp2, comp2copy);
        }

        [Test]
        public void ComponentIteration()
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitStaticRefs = new System.Type[] { typeof(EntityTemplateDecs) } });
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
                </Decs>
            ");
            parser.Finish();

            Environment.Init();
            var env = new Environment();
            using var envActive = new Environment.Scope(env);

            var entity = env.Add(EntityTemplateDecs.EntityModel);
            var components = entity.ComponentsRO().ToArray();

            Assert.AreEqual(1, components.Length);
            Assert.IsInstanceOf<SimpleComponent>(components[0]);
            Assert.AreSame(entity.ComponentRO<SimpleComponent>(), components[0]);
        }

        [Test]
        public void ComponentIterationMultiple()
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitStaticRefs = new System.Type[] { typeof(EntityTemplateDecs) } });
            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <ComponentDec decName=""SimpleComp"">
                        <type>SimpleComponent</type>
                    </ComponentDec>

                    <ComponentDec decName=""StringComp"">
                        <type>StringComponent</type>
                    </ComponentDec>

                    <EntityDec decName=""EntityModel"">
                        <components>
                            <li>SimpleComp</li>
                            <li>StringComp</li>
                        </components>
                    </EntityDec>
                </Decs>
            ");
            parser.Finish();

            Environment.Init();
            var env = new Environment();
            using var envActive = new Environment.Scope(env);

            var entity = env.Add(EntityTemplateDecs.EntityModel);
            var components = entity.ComponentsRO().ToArray();

            Assert.AreEqual(2, components.Length);
            Assert.IsInstanceOf<SimpleComponent>(components[0]);
            Assert.IsInstanceOf<StringComponent>(components[1]);
            Assert.AreSame(entity.ComponentRO<SimpleComponent>(), components[0]);
            Assert.AreSame(entity.ComponentRO<StringComponent>(), components[1]);
        }

        [Test]
        public void ComponentIterationLinq()
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitStaticRefs = new System.Type[] { typeof(EntityTemplateDecs) } });
            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <ComponentDec decName=""SimpleComp"">
                        <type>SimpleComponent</type>
                    </ComponentDec>

                    <ComponentDec decName=""StringComp"">
                        <type>StringComponent</type>
                    </ComponentDec>

                    <EntityDec decName=""EntityModel"">
                        <components>
                            <li>SimpleComp</li>
                            <li>StringComp</li>
                        </components>
                    </EntityDec>
                </Decs>
            ");
            parser.Finish();

            Environment.Init();
            var env = new Environment();
            using var envActive = new Environment.Scope(env);

            var entity = env.Add(EntityTemplateDecs.EntityModel);

            // Test that ComponentsRO() works with LINQ
            var simpleComponents = entity.ComponentsRO().OfType<SimpleComponent>().ToArray();
            var stringComponents = entity.ComponentsRO().OfType<StringComponent>().ToArray();
            var componentCount = entity.ComponentsRO().Count();

            Assert.AreEqual(1, simpleComponents.Length);
            Assert.AreEqual(1, stringComponents.Length);
            Assert.AreEqual(2, componentCount);
        }

        [Test]
        public void ComparisonDeferredResolved()
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitStaticRefs = new System.Type[] { typeof(EntityTemplateDecs), typeof(EntityProcessTemplateDecs) } });
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

                    <SystemDec decName=""DeferredSystem"">
                        <type>DeferredComparisonSystem</type>
                    </SystemDec>

                    <ProcessDec decName=""TestProcess"">
                        <order>
                            <li>DeferredSystem</li>
                        </order>
                    </ProcessDec>
                </Decs>
            ");
            parser.Finish();

            Environment.Init();
            var env = new Environment();
            using var envActive = new Environment.Scope(env);

            // Run the process which creates an entity in deferred state
            env.Process(EntityProcessTemplateDecs.TestProcess);

            // Get the deferred entity that was captured by the system
            var entityDeferred = DeferredComparisonSystem.deferredEntity;

            // Get the resolved entity from the environment
            var ents = env.List.ToArray();
            Assert.AreEqual(1, ents.Length);
            var entityResolved = ents[0];

            // The deferred and resolved entities represent the same logical entity
            // Make sure they auto-resolve properly
            Assert.AreEqual(entityDeferred, entityResolved);
        }

        [Test]
        public void ComponentsROGeneric()
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitStaticRefs = new System.Type[] { typeof(EntityTemplateDecs) } });
            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <ComponentDec decName=""SimpleComp"">
                        <type>SimpleComponent</type>
                    </ComponentDec>

                    <ComponentDec decName=""StringComp"">
                        <type>StringComponent</type>
                    </ComponentDec>

                    <EntityDec decName=""EntityModel"">
                        <components>
                            <li>SimpleComp</li>
                            <li>StringComp</li>
                        </components>
                    </EntityDec>
                </Decs>
            ");
            parser.Finish();

            Environment.Init();
            var env = new Environment();
            using var envActive = new Environment.Scope(env);

            var entity = env.Add(EntityTemplateDecs.EntityModel);

            // Test ComponentsRO<T>() generic filtering
            var simpleComponents = entity.ComponentsRO<SimpleComponent>().ToArray();
            var stringComponents = entity.ComponentsRO<StringComponent>().ToArray();

            Assert.AreEqual(1, simpleComponents.Length);
            Assert.AreEqual(1, stringComponents.Length);
            Assert.AreSame(entity.ComponentRO<SimpleComponent>(), simpleComponents[0]);
            Assert.AreSame(entity.ComponentRO<StringComponent>(), stringComponents[0]);
        }

        [Test]
        public void ComponentsRWGeneric()
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitStaticRefs = new System.Type[] { typeof(EntityTemplateDecs) } });
            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <ComponentDec decName=""SimpleComp"">
                        <type>SimpleComponent</type>
                    </ComponentDec>

                    <ComponentDec decName=""StringComp"">
                        <type>StringComponent</type>
                    </ComponentDec>

                    <EntityDec decName=""EntityModel"">
                        <components>
                            <li>SimpleComp</li>
                            <li>StringComp</li>
                        </components>
                    </EntityDec>
                </Decs>
            ");
            parser.Finish();

            Environment.Init();
            var env = new Environment();
            using var envActive = new Environment.Scope(env);

            var entity = env.Add(EntityTemplateDecs.EntityModel);

            // Test ComponentsRW<T>() generic filtering
            var simpleComponents = entity.ComponentsRW<SimpleComponent>().ToArray();
            var stringComponents = entity.ComponentsRW<StringComponent>().ToArray();

            Assert.AreEqual(1, simpleComponents.Length);
            Assert.AreEqual(1, stringComponents.Length);
            Assert.AreSame(entity.ComponentRW<SimpleComponent>(), simpleComponents[0]);
            Assert.AreSame(entity.ComponentRW<StringComponent>(), stringComponents[0]);
        }

        public class CowTestComponent : Dec.IRecordable
        {
            public int value;

            public void Record(Dec.Recorder recorder)
            {
                recorder.Record(ref value, nameof(value));
            }
        }

        public static class CowInitSystem
        {
            public static void Execute(ref Cow<CowTestComponent> cowComponent)
            {
                cowComponent.Set(new CowTestComponent { value = 42 });
            }
        }

        [Dec.StaticReferences]
        public static class CowTestDecs
        {
            static CowTestDecs() { Dec.StaticReferencesAttribute.Initialized(); }

            public static EntityDec CowEntity;
            public static EntityDec MixedEntity;
            public static ProcessDec CowInitProcess;
        }

        [Test]
        public void ComponentsROCow()
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitStaticRefs = new System.Type[] { typeof(CowTestDecs) } });
            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <ComponentDec decName=""CowComp"">
                        <type>CowTestComponent</type>
                        <cow>true</cow>
                    </ComponentDec>

                    <EntityDec decName=""CowEntity"">
                        <components>
                            <li>CowComp</li>
                        </components>
                    </EntityDec>

                    <EntityDec decName=""MixedEntity"">
                        <components>
                            <li>CowComp</li>
                        </components>
                    </EntityDec>

                    <SystemDec decName=""CowInitSystem"">
                        <type>CowInitSystem</type>
                    </SystemDec>

                    <ProcessDec decName=""CowInitProcess"">
                        <order>
                            <li>CowInitSystem</li>
                        </order>
                    </ProcessDec>
                </Decs>
            ");
            parser.Finish();

            Environment.Init();
            var env = new Environment();

            CowTestComponent cowOriginal;
            {
                using var envActive = new Environment.Scope(env);

                var entity = env.Add(CowTestDecs.CowEntity);
                env.Process(CowTestDecs.CowInitProcess);

                // Get the COW component via ComponentsRO
                var components = entity.ComponentsRO().ToArray();
                Assert.AreEqual(1, components.Length);
                Assert.IsInstanceOf<CowTestComponent>(components[0]);

                cowOriginal = (CowTestComponent)components[0];
                Assert.AreEqual(42, cowOriginal.value);
            }

            // Clone the environment
            var envClone = Dec.Recorder.Clone(env);

            {
                using var envActive = new Environment.Scope(envClone);

                var entity = envClone.List.First();

                // ComponentsRO should return the same reference (no clone)
                var components = entity.ComponentsRO().ToArray();
                Assert.AreEqual(1, components.Length);
                Assert.AreSame(cowOriginal, components[0]);
            }
        }

        [Test]
        public void ComponentsRWCow()
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitStaticRefs = new System.Type[] { typeof(CowTestDecs) } });
            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <ComponentDec decName=""CowComp"">
                        <type>CowTestComponent</type>
                        <cow>true</cow>
                    </ComponentDec>

                    <EntityDec decName=""CowEntity"">
                        <components>
                            <li>CowComp</li>
                        </components>
                    </EntityDec>

                    <EntityDec decName=""MixedEntity"">
                        <components>
                            <li>CowComp</li>
                        </components>
                    </EntityDec>

                    <SystemDec decName=""CowInitSystem"">
                        <type>CowInitSystem</type>
                    </SystemDec>

                    <ProcessDec decName=""CowInitProcess"">
                        <order>
                            <li>CowInitSystem</li>
                        </order>
                    </ProcessDec>
                </Decs>
            ");
            parser.Finish();

            Environment.Init();
            var env = new Environment();

            CowTestComponent cowOriginal;
            {
                using var envActive = new Environment.Scope(env);

                var entity = env.Add(CowTestDecs.CowEntity);
                env.Process(CowTestDecs.CowInitProcess);

                // Get the original COW value
                var components = entity.ComponentsRO().ToArray();
                cowOriginal = (CowTestComponent)components[0];
            }

            // Clone the environment
            var envClone = Dec.Recorder.Clone(env);

            {
                using var envActive = new Environment.Scope(envClone);

                var entity = envClone.List.First();

                // ComponentsRW should trigger a clone
                var components = entity.ComponentsRW().ToArray();
                Assert.AreEqual(1, components.Length);
                var cowClone = (CowTestComponent)components[0];

                // Should be a different reference (cloned)
                Assert.AreNotSame(cowOriginal, cowClone);
                // But same value
                Assert.AreEqual(42, cowClone.value);

                // Subsequent RO calls should return the cloned value
                var componentsRO = entity.ComponentsRO().ToArray();
                Assert.AreSame(cowClone, componentsRO[0]);
            }
        }

        [Test]
        public void ComponentsMixedCowAndNonCow()
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitStaticRefs = new System.Type[] { typeof(CowTestDecs) } });
            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <ComponentDec decName=""SimpleComp"">
                        <type>SimpleComponent</type>
                    </ComponentDec>

                    <ComponentDec decName=""CowComp"">
                        <type>CowTestComponent</type>
                        <cow>true</cow>
                    </ComponentDec>

                    <EntityDec decName=""CowEntity"">
                        <components>
                            <li>CowComp</li>
                        </components>
                    </EntityDec>

                    <EntityDec decName=""MixedEntity"">
                        <components>
                            <li>SimpleComp</li>
                            <li>CowComp</li>
                        </components>
                    </EntityDec>

                    <SystemDec decName=""CowInitSystem"">
                        <type>CowInitSystem</type>
                    </SystemDec>

                    <ProcessDec decName=""CowInitProcess"">
                        <order>
                            <li>CowInitSystem</li>
                        </order>
                    </ProcessDec>
                </Decs>
            ");
            parser.Finish();

            Environment.Init();
            var env = new Environment();
            using var envActive = new Environment.Scope(env);

            var entity = env.Add(CowTestDecs.MixedEntity);
            env.Process(CowTestDecs.CowInitProcess);

            // Test ComponentsRO with mixed COW and non-COW components
            var componentsRO = entity.ComponentsRO().ToArray();
            Assert.AreEqual(2, componentsRO.Length);
            Assert.IsInstanceOf<SimpleComponent>(componentsRO[0]);
            Assert.IsInstanceOf<CowTestComponent>(componentsRO[1]);

            // Test ComponentsRW with mixed COW and non-COW components
            var componentsRW = entity.ComponentsRW().ToArray();
            Assert.AreEqual(2, componentsRW.Length);
            Assert.IsInstanceOf<SimpleComponent>(componentsRW[0]);
            Assert.IsInstanceOf<CowTestComponent>(componentsRW[1]);

            // Non-COW component should be the same reference
            Assert.AreSame(componentsRO[0], componentsRW[0]);

            // COW component accessed via RO vs RW (no clone since same environment)
            // In the same environment, RW doesn't clone
            Assert.AreSame(componentsRO[1], componentsRW[1]);
        }
    }
}
