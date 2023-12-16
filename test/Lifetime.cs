using Dec;

namespace Ghi.Test
{
    using NUnit.Framework;
    using System.Linq;

    [TestFixture]
    public class Lifetime : Base
    {
        [Dec.StaticReferences]
        public static class RemovalDecs
        {
            static RemovalDecs() { Dec.StaticReferencesAttribute.Initialized(); }

            public static EntityDec EntityModel;
        }

        public class SubclassBase : IRecordable
        {
            public virtual void Record(Dec.Recorder recorder) { }
        }

        [Test]
	    public void Removal([Values] EnvironmentMode envMode)
	    {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitStaticRefs = new System.Type[] { typeof(RemovalDecs) } });
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

            var entityA = env.Add(RemovalDecs.EntityModel);
            Assert.IsNotNull(entityA.TryComponent<SimpleComponent>());
            Assert.IsNotNull(entityA.Component<SimpleComponent>());

            env.Remove(entityA);

            ProcessEnvMode(env, envMode, env =>
            {
                Assert.IsNull(entityA.TryComponent<SimpleComponent>());
                ExpectErrors(() => Assert.IsNull(entityA.Component<SimpleComponent>()));
            });

            var entityB = env.Add(RemovalDecs.EntityModel);
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

        [Dec.StaticReferences]
        public static class LiveAdditionDecs
        {
            static LiveAdditionDecs() { Dec.StaticReferencesAttribute.Initialized(); }

            public static EntityDec EntityModel;
            public static ProcessDec Process;
        }

        public static class LiveAdditionCreator
        {
            public static void Execute()
            {
                var env = Environment.Current.Value;
                var entity = env.Add(LiveAdditionDecs.EntityModel);
                entity.Component<StringComponent>().str = "beefs";
            }
        }

        [Test]
	    public void LiveAddition([Values] EnvironmentMode envMode)
	    {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitStaticRefs = new System.Type[] { typeof(LiveAdditionDecs) } });
            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <ComponentDec decName=""StringComponent"">
                        <type>StringComponent</type>
                    </ComponentDec>

                    <EntityDec decName=""EntityModel"">
                        <components>
                            <li>StringComponent</li>
                        </components>
                    </EntityDec>

                    <SystemDec decName=""Creator"">
                        <type>LiveAdditionCreator</type>
                    </SystemDec>

                    <ProcessDec decName=""Process"">
                        <order>
                            <li>Creator</li>
                        </order>
                    </ProcessDec>
                </Decs>
            ");
            parser.Finish();

            Environment.Init();
            var env = new Environment();
            using var envActive = new Environment.Scope(env);

            env.Process(LiveAdditionDecs.Process);

            ProcessEnvMode(env, envMode, env =>
            {
                var entities = env.List.ToArray();
                Assert.AreEqual(1, entities.Length);
                Assert.IsTrue(entities.All(e => e.Component<StringComponent>().str == "beefs"));
            });
        }
    }
}
