namespace Ghi
{
    using System.Collections.Generic;

    public class EntityDef : Def.Def
    {
        public List<ComponentDef> components;

        public override IEnumerable<string> ConfigErrors()
        {
            foreach (var err in base.ConfigErrors())
            {
                yield return err;
            }

            if (components == null || components.Count == 0)
            {
                yield return "No defined components";
            }
        }
    }
}
