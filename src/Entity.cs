namespace Ghi
{
    using System;
    using System.Linq;

    public class Entity
    {
        internal readonly object[] components;
        internal bool active;

        public Entity(EntityDef template) : this(template, null)
        {
            components = new object[Def.Database<ComponentDef>.Count];

            foreach (var component in template.components)
            {
                components[component.index] = Activator.CreateInstance(component.type);
            }
        }

        public Entity(EntityDef template, params object[] insertions)
        {
            components = new object[Def.Database<ComponentDef>.Count];

            if (insertions != null)
            {
                foreach (var element in insertions)
                {
                    // we could certainly cache the result of this if we wanted it to be faster
                    Type matching = element.GetType();
                    int idx = template.componentIndexDict.TryGetValue(matching, -1);
                    while (idx == -1 && matching.BaseType != null)
                    {
                        matching = matching.BaseType;
                        idx = template.componentIndexDict.TryGetValue(matching, -1);
                    }

                    if (idx == -1)
                    {
                        Dbg.Err($"Attempted construction with non-component type {element.GetType()} when initializing {template}");
                        continue;
                    }

                    if (!template.components.Any(c => c.index == idx))
                    {
                        Dbg.Err($"Received invalid entity component parameter type {element.GetType()} when initializing {template}");
                        continue;
                    }

                    if (components[idx] != null)
                    {
                        Dbg.Err($"Received duplicate entity component parameters {components[idx]} and {element} when initializing {template}");
                    }

                    components[idx] = element;
                }
            }

            foreach (var component in template.components)
            {
                if (components[component.index] == null)
                {
                    components[component.index] = Activator.CreateInstance(component.type);
                }
            }
        }

        public T Component<T>()
        {
            int index = Environment.ComponentIndexDict.TryGetValue(typeof(T), -1);
            if (index == -1)
            {
                string err = $"Invalid attempt to access non-component type {typeof(T)}";
                Dbg.Err(err);
                throw new PermissionException(err);
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
            int index = Environment.ComponentIndexDict.TryGetValue(type, -1);
            if (index == -1)
            {
                string err = $"Invalid attempt to access non-component type {type}";
                Dbg.Err(err);
                throw new PermissionException(err);
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
            int index = Environment.ComponentIndexDict.TryGetValue(typeof(T), -1);
            if (index == -1)
            {
                string err = $"Invalid attempt to access non-component type {typeof(T)}";
                Dbg.Err(err);
                throw new PermissionException(err);
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
            int index = Environment.ComponentIndexDict.TryGetValue(type, -1);
            if (index == -1)
            {
                string err = $"Invalid attempt to access non-component type {type}";
                Dbg.Err(err);
                throw new PermissionException(err);
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
