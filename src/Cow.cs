
namespace Ghi;

[Dec.CloneWithAssignment]   // copies value's ref and revision, just like we want
public struct Cow<T> : Dec.IRecordable where T : class, new()
{
    private T value;
    private long revision;

    public Cow()
    {
        value = new T();
        revision = Environment.Current.Value.UniqueId;
    }

    public Cow(T value)
    {
        this.value = value;
        revision = Environment.Current.Value.UniqueId;
    }

    public T GetRW()
    {
        var env = Environment.Current.Value;
        if (env == null)
        {
            Dbg.Err("Attempted to get a RW version of a COW structure without a working environment!");
            return value;
        }

        if (env.UniqueId == revision)
        {
            // we're good! we have the only unique version of this
            return value;
        }

        // gotta replace it ;.;
        // in theory it's possible we have the only ref remaining to it, but it's hard to detect that so I'm not
        value = Dec.Recorder.Clone(value);
        revision = env.UniqueId;
        return value;
    }

    public T GetRO()
    {
        // it hasn't changed, and we won't change it, so everything is fine
        // yay
        return value;
    }

    public void Record(Dec.Recorder recorder)
    {
        recorder.RecordAsThis(ref value);
        // revision is irrelevant, it's just a marker for the COW system
    }
}
