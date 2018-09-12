namespace Ghi
{
    using System;

    public class Entity
    {
        private readonly object[] components;

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
            return (T)components[Environment.LookupComponentIndex(typeof(T))];
        }
    }
}
