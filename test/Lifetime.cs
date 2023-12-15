using Dec;

namespace Ghi.Test
{
    using NUnit.Framework;
    using System.Linq;

    [TestFixture]
    public class Lifetime : Base
    {
        [Dec.StaticReferences]
        public static class Defs
        {
            static Defs() { Dec.StaticReferencesAttribute.Initialized(); }

            public static EntityDec EntityModel;
        }

        public class SubclassBase : IRecordable
        {
            public virtual void Record(Dec.Recorder recorder) { }
        }

        [Test]
	    public void Removal([Values] EnvironmentMode envMode)
	    {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitStaticRefs = new System.Type[] { typeof(Defs) } });
            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <ComponentDec decName=""SimpleComponent"">
                        <type>SimpleComponent</type>
                    </ComponentDec>

                    <EntityDec decName=""EntityModel"">
                        <components>
                            <li>SimpleComponent</li>
                        </components>
                    </EntityDec>
                </Decs>
            ");
            parser.Finish();

            Environment.Init();
            var env = new Environment();
            using var envActive = new Environment.Scope(env);

            var entityA = env.Add(Defs.EntityModel);
            Assert.IsNotNull(entityA.TryComponent<SimpleComponent>());
            Assert.IsNotNull(entityA.Component<SimpleComponent>());

            env.Remove(entityA);

            ProcessEnvMode(env, envMode, env =>
            {
                Assert.IsNull(entityA.TryComponent<SimpleComponent>());
                ExpectErrors(() => Assert.IsNull(entityA.Component<SimpleComponent>()));
            });

            var entityB = env.Add(Defs.EntityModel);
            Assert.IsNotNull(entityB.TryComponent<SimpleComponent>());
            Assert.IsNotNull(entityB.Component<SimpleComponent>());
            env.Remove(entityB);

            ProcessEnvMode(env, envMode, env =>
            {
                Assert.IsNull(entityA.TryComponent<SimpleComponent>());
                Assert.IsNull(entityB.TryComponent<SimpleComponent>());

                ExpectErrors(() => Assert.IsNull(entityA.Component<SimpleComponent>()));
                ExpectErrors(() => Assert.IsNull(entityB.Component<SimpleComponent>()));
            });
        }
    }
}
