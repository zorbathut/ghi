namespace Ghi
{
    using System.Collections.Generic;

    public class ProcessDef : Def.Def
    {
        public SystemDef[] order;

        public override IEnumerable<string> ConfigErrors()
        {
            foreach (var err in base.ConfigErrors())
            {
                yield return err;
            }

            if (order == null)
            {
                yield return "No defined order";
            }
        }
    }
}
