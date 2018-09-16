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
    }
}
