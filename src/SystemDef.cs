namespace Ghi
{
    using System;
    using System.Collections.Generic;

    public class SystemDef : Def.Def
    {
        public Type type;

        public override IEnumerable<string> ConfigErrors()
        {
            foreach (var err in base.ConfigErrors())
            {
                yield return err;
            }

            if (type == null)
            {
                yield return "No defined type";
            }
        }
    }
}
