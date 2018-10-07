namespace Ghi
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public static class Environment
    {
        internal const int COMPONENTINDEX_MISSING = -1;
        internal const int COMPONENTINDEX_AMBIGUOUS = -2;

        // Global status
        private enum Status
        {
            Uninitialized,
            Idle,
            Processing,
        }
        private static Status GlobalStatus = Status.Uninitialized;

        internal static readonly Dictionary<Type, ComponentDef> ComponentDefDict = new Dictionary<Type, ComponentDef>();
        internal static readonly Dictionary<Type, int> ComponentIndexDict = new Dictionary<Type, int>();
        private static readonly HashSet<Entity> Entities = new HashSet<Entity>();

        private static readonly List<Action> PhaseEndActions = new List<Action>();

        private static object[] Singletons;

        internal static SystemDef ActiveSystem;
        internal static Entity ActiveEntity;

        public static int Count
        {
            get
            {
                return Entities.Count;
            }
        }

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

            if (entity.active)
            {
                Dbg.Err($"Attempting to add an entity that is already active");
                return;
            }

            if (GlobalStatus == Status.Idle)
            {
                Entities.Add(entity);
                entity.active = true;
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

            if (!entity.active)
            {
                Dbg.Err($"Attempting to remove an entity that is already inactive");
                return;
            }

            if (GlobalStatus == Status.Idle)
            {
                Entities.Remove(entity);
                entity.active = false;
            }
            else
            {
                PhaseEndActions.Add(() => Remove(entity));
            }
        }

        public static T Singleton<T>()
        {
            int index = Environment.ComponentIndexDict.TryGetValue(typeof(T), -1);
            if (index == -1)
            {
                string err = $"Invalid attempt to access non-component type {typeof(T)}";
                Dbg.Err(err);
                throw new PermissionException(err);
            }

            if (ActiveSystem != null && !ActiveSystem.accessibleSingletonsRW[index])
            {
                string err = $"Invalid attempt to access singleton {typeof(T)} in read-write mode from within system {ActiveSystem}";
                Dbg.Err(err);
                throw new PermissionException(err);
            }

            return (T)Singletons[index];
        }

        public static object Singleton(Type type)
        {
            int index = Environment.ComponentIndexDict.TryGetValue(type, -1);
            if (index == -1)
            {
                string err = $"Invalid attempt to access non-component type {type}";
                Dbg.Err(err);
                throw new PermissionException(err);
            }

            if (ActiveSystem != null && !ActiveSystem.accessibleSingletonsRW[index])
            {
                string err = $"Invalid attempt to access singleton {type} in read-write mode from within system {ActiveSystem}";
                Dbg.Err(err);
                throw new PermissionException(err);
            }

            return Singletons[index];
        }

        public static T SingletonRO<T>()
        {
            int index = Environment.ComponentIndexDict.TryGetValue(typeof(T), -1);
            if (index == -1)
            {
                string err = $"Invalid attempt to access non-component type {typeof(T)}";
                Dbg.Err(err);
                throw new PermissionException(err);
            }

            if (ActiveSystem != null && !ActiveSystem.accessibleSingletonsRO[index])
            {
                string err = $"Invalid attempt to access singleton {typeof(T)} in read-only mode from within system {ActiveSystem}";
                Dbg.Err(err);
                throw new PermissionException(err);
            }

            return (T)Singletons[index];
        }

        public static object SingletonRO(Type type)
        {
            int index = Environment.ComponentIndexDict.TryGetValue(type, -1);
            if (index == -1)
            {
                string err = $"Invalid attempt to access non-component type {type}";
                Dbg.Err(err);
                throw new PermissionException(err);
            }

            if (ActiveSystem != null && !ActiveSystem.accessibleSingletonsRO[index])
            {
                string err = $"Invalid attempt to access singleton {type} in read-only mode from within system {ActiveSystem}";
                Dbg.Err(err);
                throw new PermissionException(err);
            }

            return Singletons[index];
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
                try
                {
                    ActiveSystem = system;

                    var executeMethod = system.type.GetMethod("Execute", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                    var methodParameters = executeMethod.GetParameters();

                    var activeParameters = new object[methodParameters.Length];
                    for (int i = 0; i < methodParameters.Length; ++i)
                    {
                        if (methodParameters[i].ParameterType == typeof(Ghi.Entity))
                        {
                            continue;
                        }

                        ComponentDef component = ComponentDefDict[methodParameters[i].ParameterType];
                        if (component != null && component.singleton)
                        {
                            if (!system.accessibleSingletonsRO[component.index])
                            {
                                var err = $"{system}: Attempted to use singleton {component} without any permission";
                                Dbg.Err(err);
                                throw new PermissionException(err);
                            }

                            if (!system.accessibleSingletonsRW[component.index] && !methodParameters[i].Name.EndsWith("_ro"))
                            {
                                Dbg.Wrn($"{system}: Using read-only singleton {component} without \"_ro\" suffix");
                            }

                            activeParameters[i] = Singletons[component.index];
                        }
                    }

                    if (system.iterate.Count == 0)
                    {
                        // No-iteration pathway
                        try
                        {
                            executeMethod.Invoke(null, activeParameters);
                        }
                        catch (Exception e)
                        {
                            Dbg.Ex(e);
                        }
                    }
                    else
                    {
                        // Entity-iteration pathway

                        // Doing this once per run is silly, it should be precalculated
                        int[] requiredIndices = system.iterate.Keys.Select(comp => comp.index).OrderBy(x => x).ToArray();
                        foreach (var entity in List)
                        {
                            ActiveEntity = entity;

                            bool valid = true;
                            for (int k = 0; k < requiredIndices.Length; ++k)
                            {
                                if (entity.components[requiredIndices[k]] == null)
                                {
                                    valid = false;
                                    break;
                                }
                            }

                            if (!valid)
                            {
                                continue;
                            }

                            for (int i = 0; i < methodParameters.Length; ++i)
                            {
                                if (methodParameters[i].ParameterType == typeof(Entity))
                                {
                                    activeParameters[i] = entity;
                                    continue;
                                }

                                ComponentDef component = ComponentDefDict[methodParameters[i].ParameterType];
                                if (component != null && !component.singleton)
                                {
                                    var permission = system.iterate.TryGetValue(component);
                                    if (permission == SystemDef.Permissions.None)
                                    {
                                        Dbg.Err($"{system}: Attempted to use component {component} without any permission");
                                    }

                                    if (permission == SystemDef.Permissions.ReadOnly && !methodParameters[i].Name.EndsWith("_ro"))
                                    {
                                        Dbg.Wrn($"{system}: Using read-only component {component} without \"_ro\" suffix");
                                    }

                                    activeParameters[i] = entity.components[component.index];
                                }
                            }

                            try
                            {
                                executeMethod.Invoke(null, activeParameters);
                            }
                            catch (Exception e)
                            {
                                Dbg.Ex(e);
                            }
                        }

                        ActiveEntity = null;
                    }
                    
                }
                catch (Exception e)
                {
                    Dbg.Ex(e);
                }
            }

            // clean up everything, even the things that should have already been cleaned up, just in case
            ActiveSystem = null;
            ActiveEntity = null;

            GlobalStatus = Status.Idle;

            foreach (var action in PhaseEndActions)
            {
                action();
            }
            PhaseEndActions.Clear();
        }

        public static void Clear()
        {
            if (GlobalStatus == Status.Processing)
            {
                Dbg.Err($"Attempting to clear the environment while the world is in {GlobalStatus} state; should not be {Status.Processing} state");
                // but we'll do it anyway
            }

            ComponentDefDict.Clear();
            ComponentIndexDict.Clear();
            Entities.Clear();
            PhaseEndActions.Clear();

            Singletons = null;

            ActiveSystem = null;
            ActiveEntity = null;

            GlobalStatus = Status.Uninitialized;
        }
    }
}
