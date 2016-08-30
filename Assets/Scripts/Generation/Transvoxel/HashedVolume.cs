using UnityEngine;
using System.Collections.Generic;

public class HashedVolume<T> : IVolumeData<T> where T:struct {

    int chunkSize = 64;
    public Dictionary<Vector3i, Array3<T>> data = new Dictionary<Vector3i, Array3<T>>();

    public override T this[int x, int y, int z]
    {
        get
        {
            if(x < 0 || y < 0 || z < 0) {
                return default(T);
            }

            Vector3i v = new Vector3i(x / chunkSize, y / chunkSize, z / chunkSize) * chunkSize;

            Array3<T> a;
            if(data.TryGetValue(v, out a)){
                return a[x % chunkSize, y % chunkSize, z % chunkSize];
            } else {
                return default(T);
            }

        }

        set
        {
            Vector3i v = new Vector3i(x / chunkSize, y / chunkSize, z / chunkSize) * chunkSize;
            Array3<T> a;

            if (!data.ContainsKey(v)) {
                a = new Array3<T>(chunkSize, v);
                data[v] = a;
            } else {
                a = data[v];
            }

            a[x % chunkSize, y % chunkSize, z % chunkSize] = value;
        }
    }

    public override int ChunkSize
    {
        get
        {
            return chunkSize;
        }
        set
        {
            chunkSize = value;
        }
    }

}
