using System;
using System.Linq;

namespace Ghi
{

    public class ProcessDec : Dec.Dec
    {
        public SystemDec[] order;

        // Declares that this process mutates no recorded state; that is, running it cannot change anything Environment.Record would write out
        public bool constant = false;

        // Runs after SystemDec's setup so we can rely on its `method` resolution to tell us which systems are usable.
        [Dec.Setup]
        [Dec.SetupAfter(typeof(SystemDec))]
        private void ValidateOrder(Action<string> reporter)
        {
            if (order == null)
            {
                reporter("No defined order");
                return;
            }

            if (order.Any(s => s?.method == null))
            {
                reporter("Order contains null or invalid systems; cleaning");
                order = order.Where(s => s?.method != null).ToArray();
            }
        }
    }
}
