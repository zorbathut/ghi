namespace Ghi
{
    using System;

    public class PermissionException : ArgumentException
    {
        public PermissionException()
        {
        }

        public PermissionException(string message)
            : base(message)
        {
        }

        public PermissionException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}
