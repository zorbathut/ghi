namespace Ghi
{
    using System;

    public class Entity
    {
        internal readonly object[] components;

        public Entity(EntityDef template)
        {
            components = new object[Def.Database<ComponentDef>.Count];

            foreach (var component in template.components)
            {
                components[component.index] = Activator.CreateInstance(component.type);
            }
        }

        public T Component<T>()
        {
            int index = Environment.ComponentDefDict[typeof(T)].index;
            if (Environment.ActiveSystem != null && !Environment.ActiveSystem.accessibleComponentsFullRW[index] && !(Environment.ActiveEntity == this && Environment.ActiveSystem.accessibleComponentsIterateRW[index]))
            {
                string err = $"Invalid attempt to access component {typeof(T)} in read-write mode from within system {Environment.ActiveSystem}";
                Dbg.Err(err);
                throw new PermissionException(err);
            }

            return (T)components[index];
        }

        public object Component(Type type)
        {
            int index = Environment.ComponentDefDict[type].index;
            if (Environment.ActiveSystem != null && !Environment.ActiveSystem.accessibleComponentsFullRW[index] && !(Environment.ActiveEntity == this && Environment.ActiveSystem.accessibleComponentsIterateRW[index]))
            {
                string err = $"Invalid attempt to access component {type} in read-write mode from within system {Environment.ActiveSystem}";
                Dbg.Err(err);
                throw new PermissionException(err);
            }

            return components[index];
        }

        public T ComponentRO<T>()
        {
            int index = Environment.ComponentDefDict[typeof(T)].index;
            if (Environment.ActiveSystem != null && !Environment.ActiveSystem.accessibleComponentsFullRO[index] && !(Environment.ActiveEntity == this && Environment.ActiveSystem.accessibleComponentsIterateRO[index]))
            {
                string err = $"Invalid attempt to access component {typeof(T)} in read-only mode from within system {Environment.ActiveSystem}";
                Dbg.Err(err);
                throw new PermissionException(err);
            }

            return (T)components[index];
        }

        public object ComponentRO(Type type)
        {
            int index = Environment.ComponentDefDict[type].index;
            if (Environment.ActiveSystem != null && !Environment.ActiveSystem.accessibleComponentsFullRO[index] && !(Environment.ActiveEntity == this && Environment.ActiveSystem.accessibleComponentsIterateRO[index]))
            {
                string err = $"Invalid attempt to access component {type} in read-only mode from within system {Environment.ActiveSystem}";
                Dbg.Err(err);
                throw new PermissionException(err);
            }

            return components[index];
        }
    }
}
