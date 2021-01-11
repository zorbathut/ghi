namespace Ghi
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public class EntityDec : Dec.Dec
    {
        public List<ComponentDec> components;

        internal Dictionary<Type, int> componentIndexDict = new Dictionary<Type, int>();

        public override void ConfigErrors(Action<string> reporter)
        {
            base.ConfigErrors(reporter);

            if (components == null || components.Count == 0)
            {
                reporter("No defined components");
            }
        }

        public override void PostLoad(Action<string> reporter)
        {
            base.PostLoad(reporter);

            foreach (var comp in components)
            {
                var type = comp.type;

                while (type != null)
                {
                    if (componentIndexDict.ContainsKey(type))
                    {
                        componentIndexDict[type] = Environment.COMPONENTINDEX_AMBIGUOUS;
                    }
                    else
                    {
                        componentIndexDict[type] = comp.index;
                    }

                    type = type.BaseType;
                }
            }
        }
    }
}
