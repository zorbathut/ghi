
using System;

namespace Ghi
{
    public static class Config
    {
        public static Func<string, IDisposable> ProfFactory = str => null;
    }
}
