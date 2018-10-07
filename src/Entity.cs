namespace Ghi
{
    using System;
    using System.Linq;

    public class Entity
    {
        internal EntityDef def;
        internal readonly object[] components;
        internal bool active;

        public Entity(EntityDef template) : this(template, null) { }

        public Entity(EntityDef template, params object[] insertions)
        {
            def = template;
            components = new object[Def.Database<ComponentDef>.Count];

            if (insertions != null)
            {
                foreach (var element in insertions)
                {
                    // we could certainly cache the result of this if we wanted it to be faster
                    Type matching = element.GetType();
                    int idx = def.componentIndexDict.TryGetValue(matching, Environment.COMPONENTINDEX_MISSING);
                    while (idx == Environment.COMPONENTINDEX_MISSING && matching.BaseType != null)
                    {
                        matching = matching.BaseType;
                        idx = def.componentIndexDict.TryGetValue(matching, Environment.COMPONENTINDEX_MISSING);
                    }

                    if (idx == Environment.COMPONENTINDEX_MISSING || idx == Environment.COMPONENTINDEX_AMBIGUOUS || !Def.Database<ComponentDef>.List[idx].type.IsAssignableFrom(element.GetType()))
                    {
                        Dbg.Err($"Attempted construction with non-component type {element.GetType()} when initializing {def}");
                        continue;
                    }

                    if (!def.components.Any(c => c.index == idx))
                    {
                        Dbg.Err($"Received invalid entity component parameter type {element.GetType()} when initializing {def}");
                        continue;
                    }

                    if (components[idx] != null)
                    {
                        Dbg.Err($"Received duplicate entity component parameters {components[idx]} and {element} when initializing {def}");
                    }

                    components[idx] = element;
                }
            }

            foreach (var component in def.components)
            {
                if (components[component.index] == null)
                {
                    components[component.index] = Activator.CreateInstance(component.type);
                }
            }
        }

        public T Component<T>()
        {
            int index = def.componentIndexDict.TryGetValue(typeof(T), Environment.COMPONENTINDEX_MISSING);
            if (index == Environment.COMPONENTINDEX_MISSING)
            {
                string err = $"Invalid attempt to access non-component type {typeof(T)}";
                Dbg.Err(err);
                throw new PermissionException(err);
            }

            if (index == Environment.COMPONENTINDEX_AMBIGUOUS)
            {
                string err = $"Invalid attempt to access ambiguous type {typeof(T)} from entity {def}";
                Dbg.Err(err);
                throw new AmbiguityException(err);
            }

            if (active && Environment.ActiveSystem != null && !Environment.ActiveSystem.accessibleComponentsFullRW[index] && !(Environment.ActiveEntity == this && Environment.ActiveSystem.accessibleComponentsIterateRW[index]))
            {
                string err = $"Invalid attempt to access component {typeof(T)} in read-write mode from within system {Environment.ActiveSystem}";
                Dbg.Err(err);
                throw new PermissionException(err);
            }

            return (T)components[index];
        }

        public object Component(Type type)
        {
            int index = def.componentIndexDict.TryGetValue(type, Environment.COMPONENTINDEX_MISSING);
            if (index == Environment.COMPONENTINDEX_MISSING)
            {
                string err = $"Invalid attempt to access non-component type {type}";
                Dbg.Err(err);
                throw new PermissionException(err);
            }

            if (index == Environment.COMPONENTINDEX_AMBIGUOUS)
            {
                string err = $"Invalid attempt to access ambiguous type {type} from entity {def}";
                Dbg.Err(err);
                throw new AmbiguityException(err);
            }

            if (active && Environment.ActiveSystem != null && !Environment.ActiveSystem.accessibleComponentsFullRW[index] && !(Environment.ActiveEntity == this && Environment.ActiveSystem.accessibleComponentsIterateRW[index]))
            {
                string err = $"Invalid attempt to access component {type} in read-write mode from within system {Environment.ActiveSystem}";
                Dbg.Err(err);
                throw new PermissionException(err);
            }

            return components[index];
        }

        public T ComponentRO<T>()
        {
            int index = def.componentIndexDict.TryGetValue(typeof(T), Environment.COMPONENTINDEX_MISSING);
            if (index == Environment.COMPONENTINDEX_MISSING)
            {
                string err = $"Invalid attempt to access non-component type {typeof(T)}";
                Dbg.Err(err);
                throw new PermissionException(err);
            }

            if (index == Environment.COMPONENTINDEX_AMBIGUOUS)
            {
                string err = $"Invalid attempt to access ambiguous type {typeof(T)} from entity {def}";
                Dbg.Err(err);
                throw new AmbiguityException(err);
            }

            if (active && Environment.ActiveSystem != null && !Environment.ActiveSystem.accessibleComponentsFullRO[index] && !(Environment.ActiveEntity == this && Environment.ActiveSystem.accessibleComponentsIterateRO[index]))
            {
                string err = $"Invalid attempt to access component {typeof(T)} in read-only mode from within system {Environment.ActiveSystem}";
                Dbg.Err(err);
                throw new PermissionException(err);
            }

            return (T)components[index];
        }

        public object ComponentRO(Type type)
        {
            int index = def.componentIndexDict.TryGetValue(type, Environment.COMPONENTINDEX_MISSING);
            if (index == Environment.COMPONENTINDEX_MISSING)
            {
                string err = $"Invalid attempt to access non-component type {type}";
                Dbg.Err(err);
                throw new PermissionException(err);
            }

            if (index == Environment.COMPONENTINDEX_AMBIGUOUS)
            {
                string err = $"Invalid attempt to access ambiguous type {type} from entity {def}";
                Dbg.Err(err);
                throw new AmbiguityException(err);
            }

            if (active && Environment.ActiveSystem != null && !Environment.ActiveSystem.accessibleComponentsFullRO[index] && !(Environment.ActiveEntity == this && Environment.ActiveSystem.accessibleComponentsIterateRO[index]))
            {
                string err = $"Invalid attempt to access component {type} in read-only mode from within system {Environment.ActiveSystem}";
                Dbg.Err(err);
                throw new PermissionException(err);
            }

            return components[index];
        }
    }
}
