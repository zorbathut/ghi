namespace Ghi
{
    using System;
    using System.Collections.Generic;

    public static class Environment
    {
        // Global status
        private enum Status
        {
            Uninitialized,
            Idle,
            Processing,
        }
        private static Status GlobalStatus = Status.Uninitialized;

        private static readonly Dictionary<Type, int> ComponentIndexDict = new Dictionary<Type, int>();
        private static readonly HashSet<Entity> Entities = new HashSet<Entity>();

        private static readonly List<Action> PhaseEndActions = new List<Action>();

        private static object[] Singletons;

        public static IEnumerable<Entity> List
        {
            get
            {
                return Entities;
            }
        }

        public static void Startup()
        {
            if (GlobalStatus != Status.Uninitialized)
            {
                Dbg.Err($"Environment starting up while the world is in {GlobalStatus} state; should be {Status.Uninitialized} state");
            }

            foreach (var def in Def.Database<ComponentDef>.List)
            {
                if (ComponentIndexDict.ContainsKey(def.type))
                {
                    Dbg.Err("Found two duplicate ComponentDef's with the same type");
                }

                ComponentIndexDict[def.type] = def.index;
            }

            Singletons = new object[Def.Database<ComponentDef>.Count];
            foreach (var def in Def.Database<ComponentDef>.List)
            {
                if (def.singleton)
                {
                    Singletons[def.index] = Activator.CreateInstance(def.type);
                }
            }

            GlobalStatus = Status.Idle;
        }

        public static void Add(Entity entity)
        {
            if (GlobalStatus == Status.Uninitialized)
            {
                Dbg.Err($"Attempting to add an entity while the world is in {GlobalStatus} state; should not be {Status.Uninitialized} state");
                return;
            }

            if (GlobalStatus == Status.Idle)
            {
                Entities.Add(entity);
            }
            else
            {
                PhaseEndActions.Add(() => Add(entity));
            }
        }

        public static void Remove(Entity entity)
        {
            if (GlobalStatus == Status.Uninitialized)
            {
                Dbg.Err($"Attempting to add an entity while the world is in {GlobalStatus} state; should not be {Status.Uninitialized} state");
                return;
            }

            if (GlobalStatus == Status.Idle)
            {
                PhaseEndActions.Add(() => Remove(entity));
            }
            else
            {
                Entities.Remove(entity);
            }
        }

        public static T Singleton<T>()
        {
            return (T)Singletons[LookupComponentIndex(typeof(T))];
        }

        public static void Clear()
        {
            if (GlobalStatus == Status.Processing)
            {
                Dbg.Err($"Attempting to clear the environment while the world is in {GlobalStatus} state; should not be {Status.Processing} state");
                return;
            }

            ComponentIndexDict.Clear();
            Entities.Clear();
            PhaseEndActions.Clear();

            Singletons = null;

            GlobalStatus = Status.Uninitialized;
        }

        internal static int LookupComponentIndex(Type type)
        {
            return ComponentIndexDict[type];
        }
    }
}
