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

        internal static readonly Dictionary<Type, ComponentDef> ComponentDefDict = new Dictionary<Type, ComponentDef>();
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
                if (ComponentDefDict.ContainsKey(def.type))
                {
                    Dbg.Err("Found two duplicate ComponentDef's with the same type");
                }

                ComponentDefDict[def.type] = def;
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
            return (T)Singletons[ComponentDefDict[typeof(T)].index];
        }

        public static object Singleton(Type type)
        {
            return Singletons[ComponentDefDict[type].index];
        }

        public static void Process(ProcessDef process)
        {
            if (GlobalStatus != Status.Idle)
            {
                Dbg.Err($"Trying to run process while the world is in {GlobalStatus} state; should be {Status.Idle} state");
            }
            GlobalStatus = Status.Processing;

            foreach (var system in process.order)
            {
                var executeMethod = system.type.GetMethod("Execute", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                var methodParameters = executeMethod.GetParameters();

                var activeParameters = new object[methodParameters.Length];
                for (int i = 0; i < methodParameters.Length; ++i)
                {
                    ComponentDef component = ComponentDefDict[methodParameters[i].ParameterType];
                    if (component != null && component.singleton)
                    {
                        // test permission

                        // test for parameter suffix

                        activeParameters[i] = Singletons[component.index];
                    }
                }

                system.type.GetMethod("Execute", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static).Invoke(null, activeParameters);
            }

            GlobalStatus = Status.Idle;
        }

        public static void Clear()
        {
            if (GlobalStatus == Status.Processing)
            {
                Dbg.Err($"Attempting to clear the environment while the world is in {GlobalStatus} state; should not be {Status.Processing} state");
                return;
            }

            ComponentDefDict.Clear();
            Entities.Clear();
            PhaseEndActions.Clear();

            Singletons = null;

            GlobalStatus = Status.Uninitialized;
        }
    }
}
