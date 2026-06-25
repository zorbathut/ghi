using Dec;
using NUnit.Framework;

namespace Ghi.Test
{
    public class Components : Base
    {
        public Components(EmitMode emitMode) : base(emitMode) { }

        [Dec.StaticReferences]
        public static class Decs
        {
            static Decs() { Dec.StaticReferencesAttribute.Initialized(); }

            public static EntityDec EntityModelA;
            public static EntityDec EntityModelB;
        }

        public class SubclassBase : IRecordable
        {
            public virtual void Record(Dec.Recorder recorder) { }
        }

        public class SubclassDerived : SubclassBase
        {
            public override void Record(Dec.Recorder recorder)
            {
                base.Record(recorder);
            }
        }

        public class SubclassDerivedAlternate : SubclassBase
        {
            public override void Record(Dec.Recorder recorder)
            {
                base.Record(recorder);
            }
        }

        [Test]
	    public void Subclass([Values] EnvironmentMode envMode)
	    {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitStaticRefs = new System.Type[] { typeof(Decs) } });
            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <ComponentDec decName=""ComponentA"">
                        <type>SubclassDerived</type>
                    </ComponentDec>

                    <ComponentDec decName=""ComponentB"">
                        <type>SubclassDerivedAlternate</type>
                    </ComponentDec>

                    <EntityDec decName=""EntityModelA"">
                        <components>
                            <li>ComponentA</li>
                        </components>
                    </EntityDec>

                    <EntityDec decName=""EntityModelB"">
                        <components>
                            <li>ComponentA</li>
                            <li>ComponentB</li>
                        </components>
                    </EntityDec>
                </Decs>
            ");
            parser.Finish();

            Environment.Init();
            var env = new Environment();
            using var envActive = new Environment.Scope(env);

            var entityA = env.Add(Decs.EntityModelA);
            var entityB = env.Add(Decs.EntityModelB);

            ProcessEnvMode(env, envMode, env =>
            {
                Assert.AreSame(entityA.ComponentRO<SubclassBase>(), entityA.ComponentRO<SubclassDerived>());
                ExpectErrors(() => entityB.ComponentRO<SubclassBase>(), err => err.Contains("Ambiguous component") && err.Contains(nameof(SubclassBase)));
            });
        }

        [Test]
        public void HasComponent([Values] EnvironmentMode envMode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitStaticRefs = new System.Type[] { typeof(Decs) } });
            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <ComponentDec decName=""ComponentA"">
                        <type>SubclassDerived</type>
                    </ComponentDec>

                    <ComponentDec decName=""ComponentB"">
                        <type>SubclassDerivedAlternate</type>
                    </ComponentDec>

                    <EntityDec decName=""EntityModelA"">
                        <components>
                            <li>ComponentA</li>
                        </components>
                    </EntityDec>

                    <EntityDec decName=""EntityModelB"">
                        <components>
                            <li>ComponentB</li>
                        </components>
                    </EntityDec>
                </Decs>
            ");
            parser.Finish();

            Environment.Init();
            var env = new Environment();
            using var envActive = new Environment.Scope(env);

            var entityA = env.Add(Decs.EntityModelA);
            var entityB = env.Add(Decs.EntityModelB);

            ProcessEnvMode(env, envMode, env =>
            {
                Assert.IsTrue(entityA.HasComponent<SubclassDerived>());
                Assert.IsTrue(!entityA.HasComponent<SubclassDerivedAlternate>());

                Assert.IsTrue(!entityB.HasComponent<SubclassDerived>());
                Assert.IsTrue(entityB.HasComponent<SubclassDerivedAlternate>());

                // runtime-Type overload should agree with the generic form
                Assert.IsTrue(entityA.HasComponent(typeof(SubclassDerived)));
                Assert.IsTrue(!entityA.HasComponent(typeof(SubclassDerivedAlternate)));

                Assert.IsTrue(!entityB.HasComponent(typeof(SubclassDerived)));
                Assert.IsTrue(entityB.HasComponent(typeof(SubclassDerivedAlternate)));
            });
        }

        public class NotAComponent { }

        [Dec.StaticReferences]
        public static class HasComponentErrorDecs
        {
            static HasComponentErrorDecs() { Dec.StaticReferencesAttribute.Initialized(); }

            public static ComponentDec ComponentFailure;
        }
        [Test]
        public void HasComponentError()
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitStaticRefs = new System.Type[] { typeof(Decs), typeof(HasComponentErrorDecs) } });
            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <ComponentDec decName=""ComponentA"">
                        <type>SubclassDerived</type>
                    </ComponentDec>

                    <ComponentDec decName=""ComponentB"">
                        <type>SubclassDerivedAlternate</type>
                    </ComponentDec>

                    <ComponentDec decName=""ComponentFailure"">
                    </ComponentDec>

                    <EntityDec decName=""EntityModelA"">
                        <components>
                            <li>ComponentA</li>
                        </components>
                    </EntityDec>

                    <EntityDec decName=""EntityModelB"">
                        <components>
                            <li>ComponentB</li>
                        </components>
                    </EntityDec>
                </Decs>
            ");
            ExpectErrors(() => parser.Finish(), err => err.Contains("ComponentFailure") && err.Contains("No defined type"));

            Environment.Init();
            var env = new Environment();
            using var envActive = new Environment.Scope(env);

            var entityA = env.Add(Decs.EntityModelA);

            Assert.IsFalse(entityA.HasComponent((ComponentDec)null));
            Assert.IsFalse(entityA.HasComponent((System.Type)null));
            Assert.IsFalse(entityA.HasComponent<NotAComponent>());
            Assert.IsFalse(entityA.HasComponent(HasComponentErrorDecs.ComponentFailure));
        }
    }
}
