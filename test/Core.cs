namespace Ghi.Test
{

using NUnit.Framework;

[TestFixture]
public class CoreTest
{
	[Test]
	public void CreationTest()
	{
	    new Def.Parser(new System.Type[] { }, null);
	}
}

}