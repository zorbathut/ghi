
namespace Ghi;

// copy-on-write handle for making Ghi.Environment clones
// note: *this has weird semantics*.
// this provides COW support if and only if it's cloned as part of an Environment. local operator= assignments do not behave like you may want them to.
// if you do a local operator=, both of them are safely readable as long as neither of them have been written to
// if you ever write to one of them, the other one is left is an undefined state. perhaps it will change! perhaps it will not! you will never know.
// so if you ever make an operator=-assigned COW, make sure all but one of them are discarded before you write to any.
[Dec.CloneStructPiecewise]   // copies value's ref and revision, just like we want
public struct Cow<T> : Dec.IRecordable
{
    private T value;
    private long revision;

    public long GetRevision()
    {
        return revision;
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

    public void Set(T newValue)
    {
        value = newValue;

        var env = Environment.Current.Value;
        revision = env?.UniqueId ?? 0;  // we actually allow this as valid, although it'll get instacloned when it's read within an environment
    }

    public void Record(Dec.Recorder recorder)
    {
        recorder.RecordAsThis(ref value);
        // revision is irrelevant, it's just a marker for the COW system
    }
}
