
using System;

namespace Ghi
{
    public static class Config
    {
        public static Func<string, IDisposable> ProfFactory = str => null;

        // Ghi normally emits specialized IL (via System.Reflection.Emit) to execute systems as fast as possible.
        // When this is false, or when the runtime can't support dynamic code (e.g. AOT/IL2CPP), Ghi falls back to
        // an equivalent reflection-based execution path instead. Set this to false to force the fallback even on a
        // runtime that does support emission; this is primarily useful for exercising the fallback in tests.
        //
        // This is read once per system when Environment.Init() runs, so set it before calling Init().
        public static bool EmitEnabled = true;

        // The effective decision: emission is only used if it's both requested and actually supported by the runtime.
        internal static bool ShouldEmit => EmitEnabled && System.Runtime.CompilerServices.RuntimeFeature.IsDynamicCodeSupported;
    }
}
