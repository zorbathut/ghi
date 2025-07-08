using System.Linq;
using Dec;
using NUnit.Framework;

namespace Ghi.Test
{
    [TestFixture]
    public class Compatibility : Base
    {
        public class ComponentA : IRecordable
        {
            public int data;

            public void Record(Dec.Recorder recorder)
            {
                recorder.Record(ref data, "data");
            }
        }
        public class ComponentB : IRecordable
        {
            public string text;

            public void Record(Dec. Recorder recorder)
            {
                recorder.Record(ref text, "text");
            }
        }
        public class ComponentC : IRecordable
        {
            public float value;

            public void Record(Dec.Recorder recorder)
            {
                recorder.Record(ref value, "value");
            }
        }
        public class ComponentD : IRecordable
        {
            public bool flag;

            public void Record(Dec.Recorder recorder)
            {
                recorder.Record(ref flag, "flag");
            }
        }

        [Dec.StaticReferences]
        public static class EntitySingle
        {
            static EntitySingle() { Dec.StaticReferencesAttribute.Initialized(); }

            public static EntityDec Entity;
        }

        [Dec.StaticReferences]
        public static class EntityMultiple
        {
            static EntityMultiple() { Dec.StaticReferencesAttribute.Initialized(); }

            public static EntityDec EntityAlpha;
            public static EntityDec EntityBeta;
            public static EntityDec EntityGamma;
            public static EntityDec EntityDelta;
        }

        [Test]
        public void ComponentReorder()
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitStaticRefs = new System.Type[] { typeof(EntitySingle) } });

            string serialized;
            ulong checksum;

            {
                var parser = new Dec.Parser();
                parser.AddString(Dec.Parser.FileType.Xml, @"
                    <Decs>
                        <ComponentDec decName=""A"">
                            <type>ComponentA</type>
                        </ComponentDec>

                        <ComponentDec decName=""B"">
                            <type>ComponentB</type>
                        </ComponentDec>

                        <ComponentDec decName=""C"">
                            <type>ComponentC</type>
                        </ComponentDec>

                        <EntityDec decName=""Entity"">
                            <components>
                                <li>A</li>
                                <li>B</li>
                                <li>C</li>
                            </components>
                        </EntityDec>
                    </Decs>
                ");
                parser.Finish();

                Environment.Init();
                var env = new Environment();
                using var envActive = new Environment.Scope(env);

                var ent = env.Add(EntitySingle.Entity);

                // set component values
                ent.ComponentRW<ComponentA>().data = 42;
                ent.ComponentRW<ComponentB>().text = "Hello";
                ent.ComponentRW<ComponentC>().value = 3.14f;

                serialized = Dec.Recorder.Write(env, pretty: true);
                checksum = Dec.Recorder.Checksum(env);
            }

            // reboot!
            Clean();

            {
                var parser = new Dec.Parser();
                parser.AddString(Dec.Parser.FileType.Xml, @"
                    <Decs>
                        <ComponentDec decName=""A"">
                            <type>ComponentA</type>
                        </ComponentDec>

                        <ComponentDec decName=""B"">
                            <type>ComponentB</type>
                        </ComponentDec>

                        <ComponentDec decName=""C"">
                            <type>ComponentC</type>
                        </ComponentDec>

                        <EntityDec decName=""Entity"">
                            <components>
                                <li>C</li>
                                <li>B</li>
                                <li>A</li>
                            </components>
                        </EntityDec>
                    </Decs>
                ");
                parser.Finish();

                Environment.Init();
                var env = Dec.Recorder.Read<Ghi.Environment>(serialized);
                using var envActive = new Environment.Scope(env);

                // grab our entity
                var ent = env.List.First();
                Assert.IsTrue(ent.IsValid());

                // check component values
                Assert.AreEqual(42, ent.ComponentRO<ComponentA>().data);
                Assert.AreEqual("Hello", ent.ComponentRO<ComponentB>().text);
                Assert.AreEqual(3.14f, ent.ComponentRO<ComponentC>().value);

                //Assert.AreEqual(checksum, Dec.Recorder.Checksum(env));
            }
        }

        [Test]
        public void ComponentAdd()
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitStaticRefs = new System.Type[] { typeof(EntitySingle) } });

            string serialized;

            {
                var parser = new Dec.Parser();
                parser.AddString(Dec.Parser.FileType.Xml, @"
                    <Decs>
                        <ComponentDec decName=""A"">
                            <type>ComponentA</type>
                        </ComponentDec>

                        <ComponentDec decName=""C"">
                            <type>ComponentC</type>
                        </ComponentDec>

                        <EntityDec decName=""Entity"">
                            <components>
                                <li>A</li>
                                <li>C</li>
                            </components>
                        </EntityDec>
                    </Decs>
                ");
                parser.Finish();

                Environment.Init();
                var env = new Environment();
                using var envActive = new Environment.Scope(env);

                var ent = env.Add(EntitySingle.Entity);

                // set component values
                ent.ComponentRW<ComponentA>().data = 99;
                ent.ComponentRW<ComponentC>().value = 1.23f;

                serialized = Dec.Recorder.Write(env, pretty: true);
            }

            // reboot!
            Clean();

            {
                var parser = new Dec.Parser();
                parser.AddString(Dec.Parser.FileType.Xml, @"
                    <Decs>
                        <ComponentDec decName=""A"">
                            <type>ComponentA</type>
                        </ComponentDec>

                        <ComponentDec decName=""B"">
                            <type>ComponentB</type>
                        </ComponentDec>

                        <ComponentDec decName=""C"">
                            <type>ComponentC</type>
                        </ComponentDec>

                        <ComponentDec decName=""D"">
                            <type>ComponentD</type>
                        </ComponentDec>

                        <EntityDec decName=""Entity"">
                            <components>
                                <li>A</li>
                                <li>B</li>
                                <li>C</li>
                                <li>D</li>
                            </components>
                        </EntityDec>
                    </Decs>
                ");
                parser.Finish();

                Environment.Init();
                var env = Dec.Recorder.Read<Ghi.Environment>(serialized);
                using var envActive = new Environment.Scope(env);

                // grab our entity
                var ent = env.List.First();
                Assert.IsTrue(ent.IsValid());

                // check original component values
                Assert.AreEqual(99, ent.ComponentRO<ComponentA>().data);
                Assert.AreEqual(1.23f, ent.ComponentRO<ComponentC>().value);

                // check new components exist with default values
                Assert.IsNotNull(ent.HasComponent<ComponentB>());
                Assert.AreEqual(null, ent.ComponentRO<ComponentB>().text);
                Assert.IsNotNull(ent.HasComponent<ComponentD>());
                Assert.AreEqual(false, ent.ComponentRO<ComponentD>().flag);
            }
        }

        [Test]
        public void ComponentRemove()
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitStaticRefs = new System.Type[] { typeof(EntitySingle) } });

            string serialized;
            ulong checksum;

            {
                var parser = new Dec.Parser();
                parser.AddString(Dec.Parser.FileType.Xml, @"
                    <Decs>
                        <ComponentDec decName=""A"">
                            <type>ComponentA</type>
                        </ComponentDec>

                        <ComponentDec decName=""B"">
                            <type>ComponentB</type>
                        </ComponentDec>

                        <ComponentDec decName=""C"">
                            <type>ComponentC</type>
                        </ComponentDec>

                        <ComponentDec decName=""D"">
                            <type>ComponentD</type>
                        </ComponentDec>

                        <EntityDec decName=""Entity"">
                            <components>
                                <li>A</li>
                                <li>B</li>
                                <li>C</li>
                                <li>D</li>
                            </components>
                        </EntityDec>
                    </Decs>
                ");
                parser.Finish();

                Environment.Init();
                var env = new Environment();
                using var envActive = new Environment.Scope(env);

                var ent = env.Add(EntitySingle.Entity);

                // set component values
                ent.ComponentRW<ComponentA>().data = 77;
                ent.ComponentRW<ComponentB>().text = "Removed";
                ent.ComponentRW<ComponentC>().value = 5.55f;
                ent.ComponentRW<ComponentD>().flag = true;

                serialized = Dec.Recorder.Write(env, pretty: true);
                checksum = Dec.Recorder.Checksum(env);
            }

            // reboot!
            Clean();

            {
                var parser = new Dec.Parser();
                parser.AddString(Dec.Parser.FileType.Xml, @"
                    <Decs>
                        <ComponentDec decName=""A"">
                            <type>ComponentA</type>
                        </ComponentDec>

                        <ComponentDec decName=""C"">
                            <type>ComponentC</type>
                        </ComponentDec>

                        <EntityDec decName=""Entity"">
                            <components>
                                <li>A</li>
                                <li>C</li>
                            </components>
                        </EntityDec>
                    </Decs>
                ");
                parser.Finish();

                Environment.Init();
                var env = Dec.Recorder.Read<Ghi.Environment>(serialized);
                using var envActive = new Environment.Scope(env);

                // grab our entity
                var ent = env.List.First();
                Assert.IsTrue(ent.IsValid());

                // check remaining component values
                Assert.AreEqual(77, ent.ComponentRO<ComponentA>().data);
                Assert.AreEqual(5.55f, ent.ComponentRO<ComponentC>().value);

                // check removed components are null
                Assert.IsFalse(ent.HasComponent<ComponentB>());
                Assert.IsFalse(ent.HasComponent<ComponentD>());

                //Assert.AreEqual(checksum, Dec.Recorder.Checksum(env));
            }
        }

        [Test]
        public void EntityReorder()
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitStaticRefs = new System.Type[] { typeof(EntityMultiple) } });

            string serialized;

            {
                var parser = new Dec.Parser();
                parser.AddString(Dec.Parser.FileType.Xml, @"
                    <Decs>
                        <ComponentDec decName=""A"">
                            <type>ComponentA</type>
                        </ComponentDec>

                        <ComponentDec decName=""B"">
                            <type>ComponentB</type>
                        </ComponentDec>

                        <ComponentDec decName=""C"">
                            <type>ComponentC</type>
                        </ComponentDec>

                        <EntityDec decName=""EntityAlpha"">
                            <components>
                                <li>A</li>
                            </components>
                        </EntityDec>

                        <EntityDec decName=""EntityBeta"">
                            <components>
                                <li>B</li>
                            </components>
                        </EntityDec>

                        <EntityDec decName=""EntityGamma"">
                            <components>
                                <li>C</li>
                            </components>
                        </EntityDec>
                    </Decs>
                ");
                // we expect errors here because of missing static references
                ExpectGeneral(() => parser.Finish(), error: ExpectationType.Tolerate, errorValidator: err => err.Contains("Static reference class"));

                Environment.Init();
                var env = new Environment();
                using var envActive = new Environment.Scope(env);

                // Add entities in specific order
                var alpha = env.Add(EntityMultiple.EntityAlpha);
                var beta = env.Add(EntityMultiple.EntityBeta);
                var gamma = env.Add(EntityMultiple.EntityGamma);

                // Entities should be in same order
                var entities = env.List.ToArray();
                Assert.AreEqual(3, entities.Length);
                Assert.AreEqual(alpha, entities[0]);
                Assert.AreEqual(beta, entities[1]);
                Assert.AreEqual(gamma, entities[2]);

                // Set values
                alpha.ComponentRW<ComponentA>().data = 10;
                beta.ComponentRW<ComponentB>().text = "Beta";
                gamma.ComponentRW<ComponentC>().value = 2.5f;

                serialized = Dec.Recorder.Write(env, pretty: true);
            }

            // reboot!
            Clean();

            {
                var parser = new Dec.Parser();
                parser.AddString(Dec.Parser.FileType.Xml, @"
                    <Decs>
                        <ComponentDec decName=""A"">
                            <type>ComponentA</type>
                        </ComponentDec>

                        <ComponentDec decName=""B"">
                            <type>ComponentB</type>
                        </ComponentDec>

                        <ComponentDec decName=""C"">
                            <type>ComponentC</type>
                        </ComponentDec>

                        <EntityDec decName=""EntityGamma"">
                            <components>
                                <li>C</li>
                            </components>
                        </EntityDec>

                        <EntityDec decName=""EntityBeta"">
                            <components>
                                <li>B</li>
                            </components>
                        </EntityDec>

                        <EntityDec decName=""EntityAlpha"">
                            <components>
                                <li>A</li>
                            </components>
                        </EntityDec>
                    </Decs>
                ");
                // we expect errors here because of missing static references
                ExpectGeneral(() => parser.Finish(), error: ExpectationType.Tolerate, errorValidator: err => err.Contains("Static reference class"));

                Environment.Init();
                var env = Dec.Recorder.Read<Ghi.Environment>(serialized);
                using var envActive = new Environment.Scope(env);

                var entities = env.List.ToArray();
                Assert.AreEqual(3, entities.Length);

                // Check values
                Assert.AreEqual(10, entities.Single(ent => ent.HasComponent<ComponentA>()).ComponentRO<ComponentA>().data);
                Assert.AreEqual("Beta", entities.Single(ent => ent.HasComponent<ComponentB>()).ComponentRO<ComponentB>().text);
                Assert.AreEqual(2.5f, entities.Single(ent => ent.HasComponent<ComponentC>()).ComponentRO<ComponentC>().value);
            }
        }

        [Test]
        public void EntityAdd()
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitStaticRefs = new System.Type[] { typeof(EntityMultiple) } });

            string serialized;

            {
                var parser = new Dec.Parser();
                parser.AddString(Dec.Parser.FileType.Xml, @"
                    <Decs>
                        <ComponentDec decName=""A"">
                            <type>ComponentA</type>
                        </ComponentDec>

                        <ComponentDec decName=""C"">
                            <type>ComponentC</type>
                        </ComponentDec>

                        <EntityDec decName=""EntityAlpha"">
                            <components>
                                <li>A</li>
                            </components>
                        </EntityDec>

                        <EntityDec decName=""EntityGamma"">
                            <components>
                                <li>C</li>
                            </components>
                        </EntityDec>
                    </Decs>
                ");
                // we expect errors here because of missing static references
                ExpectGeneral(() => parser.Finish(), error: ExpectationType.Tolerate, errorValidator: err => err.Contains("Static reference class"));

                Environment.Init();
                var env = new Environment();
                using var envActive = new Environment.Scope(env);

                // Add only two entities
                var alpha = env.Add(EntityMultiple.EntityAlpha);
                var gamma = env.Add(EntityMultiple.EntityGamma);

                // Set values
                alpha.ComponentRW<ComponentA>().data = 20;
                gamma.ComponentRW<ComponentC>().value = 1.23f;

                serialized = Dec.Recorder.Write(env, pretty: true);
            }

            // reboot!
            Clean();

            {
                var parser = new Dec.Parser();
                parser.AddString(Dec.Parser.FileType.Xml, @"
                    <Decs>
                        <ComponentDec decName=""A"">
                            <type>ComponentA</type>
                        </ComponentDec>

                        <ComponentDec decName=""B"">
                            <type>ComponentB</type>
                        </ComponentDec>

                        <ComponentDec decName=""C"">
                            <type>ComponentC</type>
                        </ComponentDec>

                        <ComponentDec decName=""D"">
                            <type>ComponentD</type>
                        </ComponentDec>

                        <EntityDec decName=""EntityAlpha"">
                            <components>
                                <li>A</li>
                            </components>
                        </EntityDec>

                        <EntityDec decName=""EntityBeta"">
                            <components>
                                <li>B</li>
                            </components>
                        </EntityDec>

                        <EntityDec decName=""EntityGamma"">
                            <components>
                                <li>C</li>
                            </components>
                        </EntityDec>

                        <EntityDec decName=""EntityDelta"">
                            <components>
                                <li>D</li>
                            </components>
                        </EntityDec>
                    </Decs>
                ");
                ExpectGeneral(() => parser.Finish(), error: ExpectationType.Tolerate, errorValidator: err => err.Contains("Static reference class"));

                Environment.Init();
                var env = Dec.Recorder.Read<Ghi.Environment>(serialized);
                using var envActive = new Environment.Scope(env);

                // Original entities should exist
                var entities = env.List.ToArray();
                Assert.AreEqual(2, entities.Length);

                // Check original values
                Assert.AreEqual(20, entities[0].ComponentRO<ComponentA>().data);
                Assert.AreEqual(1.23f, entities[1].ComponentRO<ComponentC>().value);

                // New entity types are available but not instantiated
                Assert.IsNotNull(EntityMultiple.EntityBeta);
                Assert.IsNotNull(EntityMultiple.EntityDelta);
            }
        }

        [Test]
        public void EntityRemove()
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitStaticRefs = new System.Type[] { typeof(EntityMultiple) } });

            string serialized;

            {
                var parser = new Dec.Parser();
                parser.AddString(Dec.Parser.FileType.Xml, @"
                    <Decs>
                        <ComponentDec decName=""A"">
                            <type>ComponentA</type>
                        </ComponentDec>

                        <ComponentDec decName=""B"">
                            <type>ComponentB</type>
                        </ComponentDec>

                        <ComponentDec decName=""C"">
                            <type>ComponentC</type>
                        </ComponentDec>

                        <ComponentDec decName=""D"">
                            <type>ComponentD</type>
                        </ComponentDec>

                        <EntityDec decName=""EntityAlpha"">
                            <components>
                                <li>A</li>
                            </components>
                        </EntityDec>

                        <EntityDec decName=""EntityBeta"">
                            <components>
                                <li>B</li>
                            </components>
                        </EntityDec>

                        <EntityDec decName=""EntityGamma"">
                            <components>
                                <li>C</li>
                            </components>
                        </EntityDec>

                        <EntityDec decName=""EntityDelta"">
                            <components>
                                <li>D</li>
                            </components>
                        </EntityDec>
                    </Decs>
                ");
                // we expect errors here because of missing static references
                ExpectGeneral(() => parser.Finish(), error: ExpectationType.Tolerate, errorValidator: err => err.Contains("Static reference class"));

                Environment.Init();
                var env = new Environment();
                using var envActive = new Environment.Scope(env);

                // Add all four entities
                var alpha = env.Add(EntityMultiple.EntityAlpha);
                var beta = env.Add(EntityMultiple.EntityBeta);
                var gamma = env.Add(EntityMultiple.EntityGamma);
                var delta = env.Add(EntityMultiple.EntityDelta);

                // Set values
                alpha.ComponentRW<ComponentA>().data = 30;
                beta.ComponentRW<ComponentB>().text = "ToRemove";
                gamma.ComponentRW<ComponentC>().value = 9.9f;
                delta.ComponentRW<ComponentD>().flag = true;

                serialized = Dec.Recorder.Write(env, pretty: true);
            }

            // reboot!
            Clean();

            {
                var parser = new Dec.Parser();
                parser.AddString(Dec.Parser.FileType.Xml, @"
                    <Decs>
                        <ComponentDec decName=""A"">
                            <type>ComponentA</type>
                        </ComponentDec>

                        <ComponentDec decName=""C"">
                            <type>ComponentC</type>
                        </ComponentDec>

                        <EntityDec decName=""EntityAlpha"">
                            <components>
                                <li>A</li>
                            </components>
                        </EntityDec>

                        <EntityDec decName=""EntityGamma"">
                            <components>
                                <li>C</li>
                            </components>
                        </EntityDec>
                    </Decs>
                ");
                // we expect errors here because of missing static references
                ExpectGeneral(() => parser.Finish(), error: ExpectationType.Tolerate, errorValidator: err => err.Contains("Static reference class"));

                Environment.Init();
                Ghi.Environment env = default;
                ExpectErrors(() => env = Dec.Recorder.Read<Ghi.Environment>(serialized));
                using var envActive = new Environment.Scope(env);

                // Should be down to two entities
                var entities = env.List.ToArray();
                Assert.AreEqual(2, entities.Length);

                // Check remaining entity values
                Assert.AreEqual(30, entities[0].ComponentRO<ComponentA>().data);
                Assert.AreEqual(9.9f, entities[1].ComponentRO<ComponentC>().value);

                // EntityDecs that were removed should be null
                Assert.IsNull(EntityMultiple.EntityBeta);
                Assert.IsNull(EntityMultiple.EntityDelta);
            }
        }

        [Test]
        public void SingletonReorder()
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { });

            string serialized;

            {
                var parser = new Dec.Parser();
                parser.AddString(Dec.Parser.FileType.Xml, @"
                    <Decs>
                        <ComponentDec decName=""SingletonA"">
                            <type>ComponentA</type>
                            <singleton>true</singleton>
                        </ComponentDec>

                        <ComponentDec decName=""SingletonB"">
                            <type>ComponentB</type>
                            <singleton>true</singleton>
                        </ComponentDec>

                        <ComponentDec decName=""SingletonC"">
                            <type>ComponentC</type>
                            <singleton>true</singleton>
                        </ComponentDec>
                    </Decs>
                ");
                parser.Finish();

                Environment.Init();
                var env = new Environment();
                using var envActive = new Environment.Scope(env);

                // Set singleton values
                env.Singleton<ComponentA>().data = 100;
                env.Singleton<ComponentB>().text = "SingletonB";
                env.Singleton<ComponentC>().value = 7.77f;

                serialized = Dec.Recorder.Write(env, pretty: true);
            }

            // reboot!
            Clean();

            {
                var parser = new Dec.Parser();
                parser.AddString(Dec.Parser.FileType.Xml, @"
                    <Decs>
                        <ComponentDec decName=""SingletonC"">
                            <type>ComponentC</type>
                            <singleton>true</singleton>
                        </ComponentDec>

                        <ComponentDec decName=""SingletonB"">
                            <type>ComponentB</type>
                            <singleton>true</singleton>
                        </ComponentDec>

                        <ComponentDec decName=""SingletonA"">
                            <type>ComponentA</type>
                            <singleton>true</singleton>
                        </ComponentDec>
                    </Decs>
                ");
                parser.Finish();

                Environment.Init();
                var env = Dec.Recorder.Read<Ghi.Environment>(serialized);
                using var envActive = new Environment.Scope(env);

                // Check singleton values persist despite reordering
                Assert.AreEqual(100, env.Singleton<ComponentA>().data);
                Assert.AreEqual("SingletonB", env.Singleton<ComponentB>().text);
                Assert.AreEqual(7.77f, env.Singleton<ComponentC>().value);
            }
        }

        [Test]
        public void SingletonAdd()
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { });

            string serialized;

            {
                var parser = new Dec.Parser();
                parser.AddString(Dec.Parser.FileType.Xml, @"
                    <Decs>
                        <ComponentDec decName=""SingletonA"">
                            <type>ComponentA</type>
                            <singleton>true</singleton>
                        </ComponentDec>

                        <ComponentDec decName=""SingletonB"">
                            <type>ComponentB</type>
                            <singleton>true</singleton>
                        </ComponentDec>
                    </Decs>
                ");
                parser.Finish();

                Environment.Init();
                var env = new Environment();
                using var envActive = new Environment.Scope(env);

                // Set singleton values
                env.Singleton<ComponentA>().data = 200;
                env.Singleton<ComponentB>().text = "Original";

                serialized = Dec.Recorder.Write(env, pretty: true);
            }

            // reboot!
            Clean();

            {
                var parser = new Dec.Parser();
                parser.AddString(Dec.Parser.FileType.Xml, @"
                    <Decs>
                        <ComponentDec decName=""SingletonA"">
                            <type>ComponentA</type>
                            <singleton>true</singleton>
                        </ComponentDec>

                        <ComponentDec decName=""SingletonB"">
                            <type>ComponentB</type>
                            <singleton>true</singleton>
                        </ComponentDec>

                        <ComponentDec decName=""SingletonC"">
                            <type>ComponentC</type>
                            <singleton>true</singleton>
                        </ComponentDec>

                        <ComponentDec decName=""SingletonD"">
                            <type>ComponentD</type>
                            <singleton>true</singleton>
                        </ComponentDec>
                    </Decs>
                ");
                parser.Finish();

                Environment.Init();
                var env = Dec.Recorder.Read<Ghi.Environment>(serialized);
                using var envActive = new Environment.Scope(env);

                // Check original singleton values
                Assert.AreEqual(200, env.Singleton<ComponentA>().data);
                Assert.AreEqual("Original", env.Singleton<ComponentB>().text);

                // Check new singletons exist with default values
                Assert.IsNotNull(env.Singleton<ComponentC>());
                Assert.AreEqual(0f, env.Singleton<ComponentC>().value);
                Assert.IsNotNull(env.Singleton<ComponentD>());
                Assert.AreEqual(false, env.Singleton<ComponentD>().flag);
            }
        }

        [Test]
        public void SingletonRemove()
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { });

            string serialized;

            {
                var parser = new Dec.Parser();
                parser.AddString(Dec.Parser.FileType.Xml, @"
                    <Decs>
                        <ComponentDec decName=""SingletonA"">
                            <type>ComponentA</type>
                            <singleton>true</singleton>
                        </ComponentDec>

                        <ComponentDec decName=""SingletonB"">
                            <type>ComponentB</type>
                            <singleton>true</singleton>
                        </ComponentDec>

                        <ComponentDec decName=""SingletonC"">
                            <type>ComponentC</type>
                            <singleton>true</singleton>
                        </ComponentDec>

                        <ComponentDec decName=""SingletonD"">
                            <type>ComponentD</type>
                            <singleton>true</singleton>
                        </ComponentDec>
                    </Decs>
                ");
                parser.Finish();

                Environment.Init();
                var env = new Environment();
                using var envActive = new Environment.Scope(env);

                // Set singleton values
                env.Singleton<ComponentA>().data = 300;
                env.Singleton<ComponentB>().text = "ToRemove";
                env.Singleton<ComponentC>().value = 8.88f;
                env.Singleton<ComponentD>().flag = true;

                serialized = Dec.Recorder.Write(env, pretty: true);
            }

            // reboot!
            Clean();

            {
                var parser = new Dec.Parser();
                parser.AddString(Dec.Parser.FileType.Xml, @"
                    <Decs>
                        <ComponentDec decName=""SingletonA"">
                            <type>ComponentA</type>
                            <singleton>true</singleton>
                        </ComponentDec>

                        <ComponentDec decName=""SingletonC"">
                            <type>ComponentC</type>
                            <singleton>true</singleton>
                        </ComponentDec>
                    </Decs>
                ");
                parser.Finish();

                Environment.Init();
                var env = Dec.Recorder.Read<Ghi.Environment>(serialized);
                using var envActive = new Environment.Scope(env);

                // Check remaining singleton values
                Assert.AreEqual(300, env.Singleton<ComponentA>().data);
                Assert.AreEqual(8.88f, env.Singleton<ComponentC>().value);
            }
        }
    }
}
