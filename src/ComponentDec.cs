using System;

namespace Ghi
{
    public class ComponentDec : Dec.Dec
    {
        public Type type = null;
        public bool singleton = false;
        public bool cow = false;

        [Dec.Index]
        public int index;

        internal Type GetComputedType()
        {
            if (cow)
            {
                return typeof(Cow<>).MakeGenericType(type);
            }
            else
            {
                return type;
            }
        }

        [Dec.Setup]
        private void ValidateType(Action<string> reporter)
        {
            if (type == null)
            {
                reporter("No defined type");
                return;
            }

            if (type.IsValueType && singleton)
            {
                reporter("Singleton components cannot currently be structs or other value types");
            }

            bool hooked = typeof(IOnAdd).IsAssignableFrom(type) || typeof(IOnRemove).IsAssignableFrom(type);

            if (hooked && type.IsValueType)
            {
                reporter("Lifecycle hooks are not supported on value-type components; the hook would receive a boxed copy and lose its writes");
            }

            if (hooked && cow)
            {
                reporter("Lifecycle hooks are not supported on COW components");
            }

            if (hooked && singleton)
            {
                reporter("IOnAdd and IOnRemove are per-entity hooks and never fire on a singleton");
            }
        }
    }
}
