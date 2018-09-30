namespace Ghi
{
    using System.Collections.Generic;
    using System.Linq;

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

            if (order.Any(s => s == null))
            {
                yield return "Order contains null systems; cleaning";
                order = order.Where(s => s != null).ToArray();
            }
        }
    }
}
