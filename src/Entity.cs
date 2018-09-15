namespace Ghi
{
    using System;

    public class Entity
    {
        internal readonly object[] components;

        public Entity(EntityTemplateDef template)
        {
            components = new object[Def.Database<ComponentDef>.Count];

            foreach (var component in template.components)
            {
                components[component.index] = Activator.CreateInstance(component.type);
            }
        }

        public T Component<T>()
        {
            return (T)components[Environment.ComponentDefDict[typeof(T)].index];
        }

        public object Component(Type type)
        {
            return components[Environment.ComponentDefDict[type].index];
        }
    }
}
