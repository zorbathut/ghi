namespace Ghi.Test
{
    using NUnit.Framework;
    using System.Linq;

    [TestFixture]
    public class Components : Base
    {
        [Dec.StaticReferences]
        public static class Defs
        {
            static Defs() { Dec.StaticReferencesAttribute.Initialized(); }

            public static EntityDec EntityModelA;
            public static EntityDec EntityModelB;
        }

        public class SubclassBase
        {

        }

        public class SubclassDerived : SubclassBase
        {

        }

        public class SubclassDerivedAlternate : SubclassBase
        {

        }

        [Test]
	    public void Subclass()
	    {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitStaticRefs = new System.Type[] { typeof(Defs) } });
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

            var entityA = env.Add(Defs.EntityModelA);
            Assert.AreSame(entityA.Component<SubclassBase>(), entityA.Component<SubclassDerived>());

            var entityB = env.Add(Defs.EntityModelB);
            ExpectErrors(() => entityB.Component<SubclassBase>());
	    }
    }
}
