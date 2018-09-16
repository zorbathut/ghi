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

        // Cached values derived at startup; used to allow or disallow accesses
        internal bool[] accessibleSingletonsRO;
        internal bool[] accessibleSingletonsRW;
        internal bool[] accessibleComponentsFullRO;
        internal bool[] accessibleComponentsFullRW;
        internal bool[] accessibleComponentsIterateRO;
        internal bool[] accessibleComponentsIterateRW;

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
        
        public override IEnumerable<string> PostLoad()
        {
            foreach (var err in base.PostLoad())
            {
                yield return err;
            }

            // Generate accessibility bitmasks
            accessibleSingletonsRO = new bool[Def.Database<ComponentDef>.Count];
            accessibleSingletonsRW = new bool[Def.Database<ComponentDef>.Count];
            accessibleComponentsFullRO = new bool[Def.Database<ComponentDef>.Count];
            accessibleComponentsFullRW = new bool[Def.Database<ComponentDef>.Count];
            accessibleComponentsIterateRO = new bool[Def.Database<ComponentDef>.Count];
            accessibleComponentsIterateRW = new bool[Def.Database<ComponentDef>.Count];

            if (singleton != null)
            {
                foreach (var kvp in singleton)
                {
                    if (kvp.Value >= Permissions.ReadOnly)
                    {
                        accessibleSingletonsRO[kvp.Key.index] = true;
                    }

                    if (kvp.Value >= Permissions.ReadWrite)
                    {
                        accessibleSingletonsRW[kvp.Key.index] = true;
                    }
                }
            }

            if (iterate != null)
            {
                foreach (var kvp in iterate)
                {
                    if (kvp.Value >= Permissions.ReadOnly)
                    {
                        accessibleComponentsIterateRO[kvp.Key.index] = true;
                    }

                    if (kvp.Value >= Permissions.ReadWrite)
                    {
                        accessibleComponentsIterateRW[kvp.Key.index] = true;
                    }
                }
            }
        }
    }
}
