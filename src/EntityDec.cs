using System.Collections.Concurrent;

namespace Ghi
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public class EntityDec : Dec.Dec
    {
        public List<ComponentDec> components;

        // this needs to be deterministic, so right now we're not using Dec for it because Dec's isn't
        [NonSerialized] internal int index;

        internal ConcurrentDictionary<Type, Func<Environment.Tranche, int, object>> componentGetters = new();
        internal object GetComponentFrom(Type type, Environment.Tranche tranche, int index)
        {
            if (!componentGetters.TryGetValue(type, out var getter))
            {
                // look over our components and see if we have something that makes sense
                var matches = components.Select(c => (c.type, c.index)).Where(c => type.IsAssignableFrom(c.type)).ToArray();
                if (matches.Length == 1)
                {
                    var cindex = matches[0].index;
                    getter = (tranche, index) => tranche.components[cindex][index];
                }
                else if (matches.Length == 0)
                {
                    Dbg.Err($"Cannot find match for component {type} in entity {this}");
                    getter = (tranche, index) => null;
                }
                else
                {
                    Dbg.Err($"Ambiguous component {type} in entity {this}; could be any of {string.Join(", ", matches.Select(m => m.type))}");
                    getter = (tranche, index) => null;
                }

                componentGetters.TryAdd(type, getter);
            }

            return getter(tranche, index);
        }

        public override void ConfigErrors(Action<string> reporter)
        {
            base.ConfigErrors(reporter);

            if (components == null || components.Count == 0)
            {
                reporter("No defined components");
            }
        }
    }
}
