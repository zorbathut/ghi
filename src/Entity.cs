namespace Ghi
{
    using System;
    using System.Linq;

    public class Entity
    {
        internal EntityDec dec;
        internal readonly object[] components;
        internal bool active;

        public Entity(EntityDec template) : this(template, null) { }

        public Entity(EntityDec template, params object[] insertions)
        {
            dec = template;
            components = new object[Dec.Database<ComponentDec>.Count];

            if (insertions != null)
            {
                foreach (var element in insertions)
                {
                    // we could certainly cache the result of this if we wanted it to be faster
                    Type matching = element.GetType();
                    int idx = dec.componentIndexDict.TryGetValue(matching, Environment.COMPONENTINDEX_MISSING);
                    while (idx == Environment.COMPONENTINDEX_MISSING && matching.BaseType != null)
                    {
                        matching = matching.BaseType;
                        idx = dec.componentIndexDict.TryGetValue(matching, Environment.COMPONENTINDEX_MISSING);
                    }

                    if (idx == Environment.COMPONENTINDEX_MISSING || idx == Environment.COMPONENTINDEX_AMBIGUOUS || !Dec.Database<ComponentDec>.List[idx].type.IsAssignableFrom(element.GetType()))
                    {
                        Dbg.Err($"Attempted construction with non-component type {element.GetType()} when initializing {dec}");
                        continue;
                    }

                    if (!dec.components.Any(c => c.index == idx))
                    {
                        Dbg.Err($"Received invalid entity component parameter type {element.GetType()} when initializing {dec}");
                        continue;
                    }

                    if (components[idx] != null)
                    {
                        Dbg.Err($"Received duplicate entity component parameters {components[idx]} and {element} when initializing {dec}");
                    }

                    components[idx] = element;
                }
            }

            foreach (var component in dec.components)
            {
                if (components[component.index] == null)
                {
                    components[component.index] = Activator.CreateInstance(component.type);
                }
            }
        }
        
        public bool HasComponent<T>()
        {
            return dec.componentIndexDict.TryGetValue(typeof(T), Environment.COMPONENTINDEX_MISSING) >= 0;
        }

        public bool HasComponent(Type type)
        {
            return dec.componentIndexDict.TryGetValue(type, Environment.COMPONENTINDEX_MISSING) >= 0;
        }

        public T Component<T>()
        {
            int index = dec.componentIndexDict.TryGetValue(typeof(T), Environment.COMPONENTINDEX_MISSING);
            if (index == Environment.COMPONENTINDEX_MISSING)
            {
                string err = $"Invalid attempt to access non-component type {typeof(T)}";
                Dbg.Err(err);
                throw new PermissionException(err);
            }

            if (index == Environment.COMPONENTINDEX_AMBIGUOUS)
            {
                string err = $"Invalid attempt to access ambiguous type {typeof(T)} from entity {dec}";
                Dbg.Err(err);
                throw new AmbiguityException(err);
            }

            if (active && Environment.ActiveSystem != null && Environment.ActiveSystem.permissions && !Environment.ActiveSystem.accessibleComponentsFullRW[index] && !(Environment.ActiveEntity == this && Environment.ActiveSystem.accessibleComponentsIterateRW[index]))
            {
                string err = $"Invalid attempt to access component {typeof(T)} in read-write mode from within system {Environment.ActiveSystem}";
                Dbg.Err(err);
                throw new PermissionException(err);
            }

            return (T)components[index];
        }

        public object Component(Type type)
        {
            int index = dec.componentIndexDict.TryGetValue(type, Environment.COMPONENTINDEX_MISSING);
            if (index == Environment.COMPONENTINDEX_MISSING)
            {
                string err = $"Invalid attempt to access non-component type {type}";
                Dbg.Err(err);
                throw new PermissionException(err);
            }

            if (index == Environment.COMPONENTINDEX_AMBIGUOUS)
            {
                string err = $"Invalid attempt to access ambiguous type {type} from entity {dec}";
                Dbg.Err(err);
                throw new AmbiguityException(err);
            }

            if (active && Environment.ActiveSystem != null && Environment.ActiveSystem.permissions && !Environment.ActiveSystem.accessibleComponentsFullRW[index] && !(Environment.ActiveEntity == this && Environment.ActiveSystem.accessibleComponentsIterateRW[index]))
            {
                string err = $"Invalid attempt to access component {type} in read-write mode from within system {Environment.ActiveSystem}";
                Dbg.Err(err);
                throw new PermissionException(err);
            }

            return components[index];
        }

        public T ComponentRO<T>()
        {
            int index = dec.componentIndexDict.TryGetValue(typeof(T), Environment.COMPONENTINDEX_MISSING);
            if (index == Environment.COMPONENTINDEX_MISSING)
            {
                string err = $"Invalid attempt to access non-component type {typeof(T)}";
                Dbg.Err(err);
                throw new PermissionException(err);
            }

            if (index == Environment.COMPONENTINDEX_AMBIGUOUS)
            {
                string err = $"Invalid attempt to access ambiguous type {typeof(T)} from entity {dec}";
                Dbg.Err(err);
                throw new AmbiguityException(err);
            }

            if (active && Environment.ActiveSystem != null && Environment.ActiveSystem.permissions && !Environment.ActiveSystem.accessibleComponentsFullRO[index] && !(Environment.ActiveEntity == this && Environment.ActiveSystem.accessibleComponentsIterateRO[index]))
            {
                string err = $"Invalid attempt to access component {typeof(T)} in read-only mode from within system {Environment.ActiveSystem}";
                Dbg.Err(err);
                throw new PermissionException(err);
            }

            return (T)components[index];
        }

        public object ComponentRO(Type type)
        {
            int index = dec.componentIndexDict.TryGetValue(type, Environment.COMPONENTINDEX_MISSING);
            if (index == Environment.COMPONENTINDEX_MISSING)
            {
                string err = $"Invalid attempt to access non-component type {type}";
                Dbg.Err(err);
                throw new PermissionException(err);
            }

            if (index == Environment.COMPONENTINDEX_AMBIGUOUS)
            {
                string err = $"Invalid attempt to access ambiguous type {type} from entity {dec}";
                Dbg.Err(err);
                throw new AmbiguityException(err);
            }

            if (active && Environment.ActiveSystem != null && Environment.ActiveSystem.permissions && !Environment.ActiveSystem.accessibleComponentsFullRO[index] && !(Environment.ActiveEntity == this && Environment.ActiveSystem.accessibleComponentsIterateRO[index]))
            {
                string err = $"Invalid attempt to access component {type} in read-only mode from within system {Environment.ActiveSystem}";
                Dbg.Err(err);
                throw new PermissionException(err);
            }

            return components[index];
        }

        public override string ToString()
        {
            if (Environment.EntityToString != null)
            {
                return Environment.EntityToString(this);
            }
            else
            {
                return base.ToString();
            }
        }
    }
}
