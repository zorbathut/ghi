namespace Ghi
{
    using System;
    using System.Collections.Generic;

    public class ComponentDec : Dec.Dec
    {
        public Type type = null;
        public bool singleton = false;
        public bool immutable = false;

        [Dec.Index]
        public int index;

        public override void ConfigErrors(Action<string> reporter)
        {
            base.ConfigErrors(reporter);

            if (type == null)
            {
                reporter("No defined type");
            }
        }
    }
}
