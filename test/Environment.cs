namespace Ghi.Test
{
    using NUnit.Framework;
    using System.Linq;

    [TestFixture]
    public class EnvironmentTest : Base
    {
	    [Test]
	    public void Singleton()
	    {
	        var parser = new Def.Parser(explicitStaticRefs: new System.Type[] { });
            parser.AddString(@"
                <Defs>
                    <ComponentDef defName=""SingComponent"">
                        <type>SimpleComponent</type>
                        <singleton>true</singleton>
                    </ComponentDef>

                    <ComponentDef defName=""EntComponent"">
                        <type>StringComponent</type>
                    </ComponentDef>
                </Defs>
            ");
            parser.Finish();

            Environment.Startup();

            var simp = Environment.Singleton<SimpleComponent>();
            Assert.IsNotNull(simp);

            var str = Environment.Singleton<StringComponent>();
            Assert.IsNull(str);
	    }
    }
}
