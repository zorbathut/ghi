namespace Ghi
{
    using System;
    using System.Collections.Generic;

    public class ComponentDef : Def.Def
    {
        public Type type = null;
        public bool singleton = false;
        public bool immutable = false;

        public override IEnumerable<string> ConfigErrors()
        {
            foreach (var err in base.ConfigErrors())
            {
                yield return err;
            }

            if (type == null)
            {
                yield return "No defined type";
            }
        }
    }
}
