namespace Ghi;

internal static class Assert
{
    public static void IsNull(object val)
    {
        if (val != null)
        {
            Dbg.Err($"Value is not null: {val}");
        }
    }

    public static void IsNotNull(object val)
    {
        if (val == null)
        {
            Dbg.Err("Value is null");
        }
    }

    public static void IsTrue(bool val, string msg = null)
    {
        if (!val)
        {
            Dbg.Err(msg ?? "Value is false");
        }
    }

    public static void IsEmpty<T>(System.Collections.Generic.ICollection<T> collection)
    {
        if (collection.Count != 0)
        {
            Dbg.Err($"Collection is not empty: {collection}");
        }
    }

    public static void AreSame(object expected, object actual)
    {
        if (expected != actual)
        {
            Dbg.Err($"Values do not match: expected {expected}, actual {actual}");
        }
    }

    public static void AreEqual<T>(T expected, T actual)
    {
        if (!expected.Equals(actual))
        {
            Dbg.Err($"Values do not match: expected {expected}, actual {actual}");
        }
    }
}
