namespace Ghi
{
    using System;
    using System.Collections.Generic;

    public class SystemDec : Dec.Dec
    {
        public Type type;

        public enum Permissions
        {
            None,
            ReadOnly,
            ReadWrite,
        }

        public bool permissions = true;
        public Dictionary<ComponentDec, Permissions> iterate = new Dictionary<ComponentDec, Permissions>();
        public Dictionary<ComponentDec, Permissions> full = new Dictionary<ComponentDec, Permissions>();
        public Dictionary<ComponentDec, Permissions> singleton = new Dictionary<ComponentDec, Permissions>();

        // Cached values derived at startup; used to allow or disallow accesses
        internal bool[] accessibleSingletonsRO;
        internal bool[] accessibleSingletonsRW;
        internal bool[] accessibleComponentsFullRO;
        internal bool[] accessibleComponentsFullRW;
        internal bool[] accessibleComponentsIterateRO;
        internal bool[] accessibleComponentsIterateRW;

        public override void ConfigErrors(Action<string> reporter)
        {
            base.ConfigErrors(reporter);

            if (type == null)
            {
                reporter("No defined type");
            }

            foreach (var kvp in singleton)
            {
                if (!kvp.Key.singleton)
                {
                    reporter($"Non-singleton component {kvp.Key} referenced in singleton list");
                }

                if (kvp.Key.immutable && kvp.Value == Permissions.ReadWrite)
                {
                    reporter($"Read-write permission given for immutable component {kvp.Key}");
                }
            }

            foreach (var kvp in full)
            {
                if (kvp.Key.singleton)
                {
                    reporter($"Singleton component {kvp.Key} referenced in full list");
                }

                if (kvp.Key.immutable && kvp.Value == Permissions.ReadWrite)
                {
                    reporter($"Read-write permission given for immutable component {kvp.Key}");
                }
            }

            foreach (var kvp in iterate)
            {
                if (kvp.Key.singleton)
                {
                    reporter($"Singleton component {kvp.Key} referenced in iteration list");
                }

                if (kvp.Key.immutable && kvp.Value == Permissions.ReadWrite)
                {
                    reporter($"Read-write permission given for immutable component {kvp.Key}");
                }
            }
        }
        
        public override void PostLoad(Action<string> reporter)
        {
            base.PostLoad(reporter);

            // Generate accessibility bitmasks
            accessibleSingletonsRO = new bool[Dec.Database<ComponentDec>.Count];
            accessibleSingletonsRW = new bool[Dec.Database<ComponentDec>.Count];
            accessibleComponentsFullRO = new bool[Dec.Database<ComponentDec>.Count];
            accessibleComponentsFullRW = new bool[Dec.Database<ComponentDec>.Count];
            accessibleComponentsIterateRO = new bool[Dec.Database<ComponentDec>.Count];
            accessibleComponentsIterateRW = new bool[Dec.Database<ComponentDec>.Count];

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

            if (full != null)
            {
                foreach (var kvp in full)
                {
                    if (kvp.Value >= Permissions.ReadOnly)
                    {
                        accessibleComponentsFullRO[kvp.Key.index] = true;
                    }

                    if (kvp.Value >= Permissions.ReadWrite)
                    {
                        accessibleComponentsFullRW[kvp.Key.index] = true;
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

            // always allow immutable singleton access
            foreach (var component in Dec.Database<ComponentDec>.List)
            {
                if (component.singleton && component.immutable)
                {
                    accessibleSingletonsRO[component.index] = true;
                }
            }
        }
    }
}
