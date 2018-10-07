namespace Ghi.Test
{
    using NUnit.Framework;
    using System.Linq;

    [TestFixture]
    public class Components : Base
    {
        [Def.StaticReferences]
        public static class Defs
        {
            static Defs() { Def.StaticReferences.Initialized(); }

            public static EntityDef EntityModelA;
            public static EntityDef EntityModelB;
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
	        var parser = new Def.Parser(explicitStaticRefs: new System.Type[] { typeof(Defs) });
            parser.AddString(@"
                <Defs>
                    <ComponentDef defName=""ComponentA"">
                        <type>SubclassDerived</type>
                    </ComponentDef>

                    <ComponentDef defName=""ComponentB"">
                        <type>SubclassDerivedAlternate</type>
                    </ComponentDef>

                    <EntityDef defName=""EntityModelA"">
                        <components>
                            <li>ComponentA</li>
                        </components>
                    </EntityDef>

                    <EntityDef defName=""EntityModelB"">
                        <components>
                            <li>ComponentA</li>
                            <li>ComponentB</li>
                        </components>
                    </EntityDef>
                </Defs>
            ");
            parser.Finish();

            Environment.Startup();

            var entityA = new Ghi.Entity(Defs.EntityModelA);
            Assert.AreSame(entityA.Component<SubclassBase>(), entityA.Component<SubclassDerived>());

            var entityB = new Ghi.Entity(Defs.EntityModelB);
            ExpectException(() => entityB.Component<SubclassBase>());
	    }
    }
}
