namespace Ghi
{
    using System;
    using System.Collections.Generic;

    public class SystemDef : Def.Def
    {
        public Type type;

        public enum Permissions
        {
            None,
            ReadOnly,
            ReadWrite,
        }

        public Dictionary<ComponentDef, Permissions> singleton = new Dictionary<ComponentDef, Permissions>();

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
