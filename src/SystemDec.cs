namespace Ghi
{
    using System;
    using System.Reflection;

    public class SystemDec : Dec.Dec
    {
        public Type type;

        // first param is the tranches, second param is the singletons
        [NonSerialized] internal Action<Environment.Tranche[], object[]> process;

        [NonSerialized] internal MethodInfo method;

        public override void ConfigErrors(Action<string> reporter)
        {
            base.ConfigErrors(reporter);

            if (type == null)
            {
                reporter("No defined type");
                return;
            }

            method = type.GetMethod("Execute", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
            if (method == null)
            {
                reporter("Type does not have a static Execute method");
            }
            else if (method.ReturnType != typeof(void))
            {
                reporter("Type's Execute method does not return void");
            }
        }
    }
}
