namespace Ghi.Test
{
    using NUnit.Framework;
    using System.Linq;

    
    [Def.StaticReferences]
    public static class EntityTemplateDefs
    {
        static EntityTemplateDefs() { Def.StaticReferences.Initialized(); }

        public static EntityTemplateDef EntityModel;
    }

    public class SimpleComponent
    {
        public int number;
    }

    [TestFixture]
    public class Entity : Base
    {
	    [Test]
	    public void CreationTest()
	    {
	        var parser = new Def.Parser(explicitStaticRefs: new System.Type[] { typeof(EntityTemplateDefs) });
            parser.AddString(@"
                <Defs>
                    <ComponentDef defName=""Component"">
                        <type>SimpleComponent</type>
                    </ComponentDef>

                    <EntityTemplateDef defName=""EntityModel"">
                        <components>
                            <li>Component</li>
                        </components>
                    </EntityTemplateDef>
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
