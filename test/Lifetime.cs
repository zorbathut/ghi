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
                    <ComponentDec decName=""StringComponent"">
                        <type>StringComponent</type>
                    </ComponentDec>

                    <EntityDec decName=""EntityModel"">
                        <components>
                            <li>StringComponent</li>
                        </components>
                    </EntityDec>
                </Decs>
            ");
            parser.Finish();

            Environment.Init();
            var env = new Environment();
            using var envActive = new Environment.Scope(env);

            var entityA = env.Add(RemovalDecs.EntityModel);
            Assert.IsNotNull(entityA.TryComponent<StringComponent>());
            Assert.IsNotNull(entityA.Component<StringComponent>());

            env.Remove(entityA);

            ProcessEnvMode(env, envMode, env =>
            {
                Assert.IsNull(entityA.TryComponent<StringComponent>());
                ExpectErrors(() => Assert.IsNull(entityA.Component<StringComponent>()));
            });

            var entityB = env.Add(RemovalDecs.EntityModel);
            Assert.IsNotNull(entityB.TryComponent<StringComponent>());
            Assert.IsNotNull(entityB.Component<StringComponent>());
            env.Remove(entityB);

            ProcessEnvMode(env, envMode, env =>
            {
                Assert.IsNull(entityA.TryComponent<StringComponent>());
                Assert.IsNull(entityB.TryComponent<StringComponent>());

                ExpectErrors(() => Assert.IsNull(entityA.Component<StringComponent>()));
                ExpectErrors(() => Assert.IsNull(entityB.Component<StringComponent>()));
            });

            var entityC = env.Add(RemovalDecs.EntityModel);
            var entityD = env.Add(RemovalDecs.EntityModel);
            var entityE = env.Add(RemovalDecs.EntityModel);
            var entityF = env.Add(RemovalDecs.EntityModel);

            entityC.Component<StringComponent>().str = "C";
            entityD.Component<StringComponent>().str = "D";
            entityE.Component<StringComponent>().str = "E";
            entityF.Component<StringComponent>().str = "F";

            Assert.AreEqual("C", entityC.Component<StringComponent>().str);
            Assert.AreEqual("D", entityD.Component<StringComponent>().str);
            Assert.AreEqual("E", entityE.Component<StringComponent>().str);
            Assert.AreEqual("F", entityF.Component<StringComponent>().str);

            env.Remove(entityD);

            ProcessEnvMode(env, envMode, env =>
            {
                Assert.AreEqual("C", entityC.Component<StringComponent>().str);
                Assert.IsNull(entityD.TryComponent<StringComponent>());
                Assert.AreEqual("E", entityE.Component<StringComponent>().str);
                Assert.AreEqual("F", entityF.Component<StringComponent>().str);

                Assert.AreEqual(env.List.Select(e => e.Component<StringComponent>().str).OrderBy(s => s).ToArray(), new string[] { "C", "E", "F" });
            });

            env.Remove(entityF);

            ProcessEnvMode(env, envMode, env =>
            {
                Assert.AreEqual("C", entityC.Component<StringComponent>().str);
                Assert.IsNull(entityD.TryComponent<StringComponent>());
                Assert.AreEqual("E", entityE.Component<StringComponent>().str);
                Assert.IsNull(entityF.TryComponent<StringComponent>());

                Assert.AreEqual(env.List.Select(e => e.Component<StringComponent>().str).OrderBy(s => s).ToArray(), new string[] { "C", "E" });
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
