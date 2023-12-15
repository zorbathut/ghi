namespace Ghi
{
    using System;
    using System.Linq;

    public class ProcessDec : Dec.Dec
    {
        public SystemDec[] order;

        public override void ConfigErrors(Action<string> reporter)
        {
            base.ConfigErrors(reporter);

            if (order == null)
            {
                reporter("No defined order");
            }

            if (order.Any(s => s == null))
            {
                reporter("Order contains null systems; cleaning");
                order = order.Where(s => s != null).ToArray();
            }
        }
    }
}
