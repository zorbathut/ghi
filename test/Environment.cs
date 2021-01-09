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
            Dec.Config.TestParameters = new Dec.Config.UnitTestParameters { explicitStaticRefs = new System.Type[] { } };
            var parser = new Dec.Parser();
            parser.AddString(@"
                <Decs>
                    <ComponentDec decName=""SingComponent"">
                        <type>SimpleComponent</type>
                        <singleton>true</singleton>
                    </ComponentDec>

                    <ComponentDec decName=""EntComponent"">
                        <type>StringComponent</type>
                    </ComponentDec>
                </Decs>
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
