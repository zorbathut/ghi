namespace Ghi.Test
{
    using NUnit.Framework;

    [TestFixture]
    public class Base
    {
        [SetUp] [TearDown]
        public void Clean()
        {
            Environment.Clear();

            Def.Database.Clear();
        }
    }
}
