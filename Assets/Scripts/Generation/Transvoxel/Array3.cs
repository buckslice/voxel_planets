using UnityEngine;
using System.Collections;

// 3D cube array class which is actually just a 1D array using index calculations
public class Array3<T> where T : struct {

    public Vector3i pos;
    public int size;

    private T[] data;

    public Array3(int size, Vector3i pos) {
        this.size = size;
        this.pos = pos;
        data = new T[size * size * size];
    }

    public T this[int x, int y, int z] {
        get {
            return data[x + y * size + z * size * size];
        }
        set {
            data[x + y * size + z * size * size] = value;
        }
    }

    public T this[Vector3i v] {
        get {
            return this[v.x, v.y, v.z];
        }
        set {
            this[v.x, v.y, v.z] = value;
        }
    }

}

