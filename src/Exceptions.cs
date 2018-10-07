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

    public class AmbiguityException : ArgumentException
    {
        public AmbiguityException()
        {
        }

        public AmbiguityException(string message)
            : base(message)
        {
        }

        public AmbiguityException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}
