﻿
using System.Collections.Generic;

public class HashCounter<T> : IEnumerable<T> {
    private Dictionary<T, int> _dict;

    public HashCounter() {
        _dict = new Dictionary<T, int>();
    }
    public HashCounter(int capasity) {
        _dict = new Dictionary<T, int>(capasity);
    }

    public int Count { get { return _dict.Keys.Count; } }
    public int this[T key] {
        get {
            int val;
            if (!_dict.TryGetValue(key, out val))
                val = 0;
            return val;
        }
        set {
            _dict[key] = value;
        }
    }

    #region IEnumerable[T] implementation
    public IEnumerator<T> GetEnumerator() { return _dict.Keys.GetEnumerator(); }
    #endregion
    #region IEnumerable implementation
    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { return _dict.Keys.GetEnumerator(); }
    #endregion
}