namespace Ghi
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public class EntityDef : Def.Def
    {
        public List<ComponentDef> components;

        internal Dictionary<Type, int> componentIndexDict = new Dictionary<Type, int>();

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

        public override IEnumerable<string> PostLoad()
        {
            foreach (var err in base.PostLoad())
            {
                yield return err;
            }

            componentIndexDict = components.Select(c => Tuple.Create(c.type, c.index)).ToDictionary();
        }
    }
}
