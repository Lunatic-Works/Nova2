using System;
using System.Collections.Generic;

namespace Nova;

public class ObjectPool<T>(Func<T> factory)
{
    private readonly Stack<T> _values = [];
    private readonly Func<T> _factory = factory;

    public T Get()
    {
        if (_values.Count > 0)
        {
            return _values.Pop();
        }
        else
        {
            return _factory.Invoke();
        }
    }

    public void Put(T value)
    {
        _values.Push(value);
    }

    public void Clear()
    {
        _values.Clear();
    }
}
