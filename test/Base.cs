namespace Ghi.Test
{
    using NUnit.Framework;

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

    public class StringComponent
    {
        public string str;
    }

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
