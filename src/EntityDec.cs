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
        internal ConcurrentDictionary<Type, Func<Environment.Tranche, int, object>> tryComponentGetters = new();
        internal object GetComponentFrom(Type type, Environment.Tranche tranche, int index)
        {
            if (!componentGetters.TryGetValue(type, out var getter))
            {
                (getter, var _) = CreateGetters(type);
            }

            return getter(tranche, index);
        }

        internal object TryGetComponentFrom(Type type, Environment.Tranche tranche, int index)
        {
            if (!tryComponentGetters.TryGetValue(type, out var tryGetter))
            {
                (_, tryGetter) = CreateGetters(type);
            }

            return tryGetter(tranche, index);
        }

        internal bool HasComponent(Type type)
        {
            // should really cache this
            return components.Any(c => type.IsAssignableFrom(c.type));
        }

        private (Func<Environment.Tranche, int, object> getter, Func<Environment.Tranche, int, object> tryGetter) CreateGetters(Type type)
        {
            Func<Environment.Tranche, int, object> getter;
            Func<Environment.Tranche, int, object> tryGetter;

            // look over our components and see if we have something that makes sense
            var matches = components.Select((c, i) => (c.type, i)).Where(c => type.IsAssignableFrom(c.type)).ToArray();
            if (matches.Length == 1)
            {
                var cindex = matches[0].i;
                getter = (tranche, index) => tranche.components[cindex][index];
                tryGetter = getter;
            }
            else if (matches.Length == 0)
            {
                getter = (tranche, index) =>
                {
                    Dbg.Err($"Cannot find match for component {type} in entity {this}");
                    return null;
                };
                tryGetter = (tranche, index) => null;
            }
            else
            {
                getter = (tranche, index) =>
                {
                    Dbg.Err($"Ambiguous component {type} in entity {this}; could be any of {string.Join(", ", matches.Select(m => m.type))}");
                    return null;
                };
                tryGetter = (tranche, index) => null;
            }

            componentGetters.TryAdd(type, getter);
            tryComponentGetters.TryAdd(type, tryGetter);

            return (getter, tryGetter);
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
