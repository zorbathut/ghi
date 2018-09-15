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
        public Dictionary<ComponentDef, Permissions> iterate = new Dictionary<ComponentDef, Permissions>();

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

            foreach (var kvp in singleton)
            {
                if (!kvp.Key.singleton)
                {
                    yield return $"Non-singleton component {kvp.Key} referenced in singleton list";
                }

                if (kvp.Key.immutable && kvp.Value == Permissions.ReadWrite)
                {
                    yield return $"Read-write permission given for immutable component {kvp.Key}";
                }
            }

            foreach (var kvp in iterate)
            {
                if (kvp.Key.singleton)
                {
                    yield return $"Singleton component {kvp.Key} referenced in iteration list";
                }

                if (kvp.Key.immutable && kvp.Value == Permissions.ReadWrite)
                {
                    yield return $"Read-write permission given for immutable component {kvp.Key}";
                }
            }
        }
    }
}
