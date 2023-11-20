namespace TinyCoin;

public class AtomicBool
{
    private readonly object _lock = new object();
    private bool _value;

    public AtomicBool(bool initialValue)
    {
        _value = initialValue;
    }

    public bool Value
    {
        get
        {
            lock (_lock)
            {
                return _value;
            }
        }
        set
        {
            lock (_lock)
            {
                _value = value;
            }
        }
    }
}

public class AtomicULong
{
    private readonly object _lock = new object();
    private ulong _value;

    public AtomicULong(ulong initialValue)
    {
        _value = initialValue;
    }

    public ulong Value
    {
        get
        {
            lock (_lock)
            {
                return _value;
            }
        }
        set
        {
            lock (_lock)
            {
                _value = value;
            }
        }
    }

    public ulong Increment()
    {
        lock (_lock)
        {
            return ++_value;
        }
    }
}
