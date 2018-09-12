namespace Ghi
{
    using System;

    internal static class Dbg
    {
        internal static void Inf(string format)
        {
            Def.Config.InfoHandler(format);
        }

        internal static void Inf(string format, params object[] args)
        {
            Def.Config.InfoHandler(string.Format(format, args));
        }

        internal static void Wrn(string format)
        {
            Def.Config.WarningHandler(format);
        }

        internal static void Wrn(string format, params object[] args)
        {
            Def.Config.WarningHandler(string.Format(format, args));
        }

        internal static void Err(string format)
        {
            Def.Config.ErrorHandler(format);
        }

        internal static void Err(string format, params object[] args)
        {
            Def.Config.ErrorHandler(string.Format(format, args));
        }

        internal static void Ex(Exception e)
        {
            Def.Config.ExceptionHandler(e);
        }
    }
}