using System;
using UnityEngine;

public struct Vector3i {
    public int x;
    public int y;
    public int z;

    public Vector3i(int x, int y, int z) {
        this.x = x;
        this.y = y;
        this.z = z;
    }

    public Vector3i(int[] arr) {
        x = arr[0];
        y = arr[1];
        z = arr[2];
    }

    public void Add(Vector3i v) {
        x += v.x;
        y += v.y;
        z += v.z;
    }

    public void Sub(Vector3i v) {
        x -= v.x;
        y -= v.y;
        z -= v.z;
    }

    public void Mul(int scalar) {
        x *= scalar;
        y *= scalar;
        z *= scalar;
    }

    public void Div(int scalar) {
        x /= scalar;
        y /= scalar;
        z /= scalar;
    }

    public int this[int i]
    {
        get
        {
            if (i == 0)
                return x;
            if (i == 1)
                return y;
            if (i == 2)
                return z;

            throw new ArgumentOutOfRangeException(string.Format("There is no value at {0} index.", i));
        }
        set
        {
            if (i == 0)
                x = value;
            if (i == 1)
                y = value;
            if (i == 2)
                z = value;

            throw new ArgumentOutOfRangeException(string.Format("There is no value at {0} index.", i));
        }
    }

    public override string ToString() {
        return "(" + x + "," + y + "," + z + ")";
    }

    public Vector3 ToVector3() {
        return new Vector3(x, y, z);
    }

    public static Vector3i UnitX = new Vector3i(1, 0, 0);
    public static Vector3i UnitY = new Vector3i(0, 1, 0);
    public static Vector3i UnitZ = new Vector3i(0, 0, 1);
    public static Vector3i Zero = new Vector3i(0, 0, 0);
    public static Vector3i One = new Vector3i(1, 1, 1);

    public static explicit operator Vector3i(Vector3 v) {
        return new Vector3i((int)v.x, (int)v.y, (int)v.z);
    }

    public static Vector3i operator +(Vector3i v0, Vector3i v1) {
        return new Vector3i(v0.x + v1.x, v0.y + v1.y, v0.z + v1.z);
    }

    public static Vector3i operator -(Vector3i v0, Vector3i v1) {
        return new Vector3i(v0.x - v1.x, v0.y - v1.y, v0.z - v1.z);
    }
    public static Vector3i operator /(Vector3i v0, Vector3i v1) {
        return new Vector3i(v0.x / v1.x, v0.y / v1.y, v0.z / v1.z);
    }

    public static Vector3i operator %(Vector3i v0, int i) {
        return new Vector3i(v0.x % i, v0.y % i, v0.z % i);
    }

    public static Vector3i operator *(Vector3i v0, Vector3i v1) {
        return new Vector3i(v0.x * v1.x, v0.y * v1.y, v0.z * v1.z);
    }

    public static Vector3i operator *(Vector3i v, int s) {
        return new Vector3i(v.x * s, v.y * s, v.z * s);
    }
    public static Vector3i operator *(int s, Vector3i v) {
        return v * s;
    }

    public static Vector3i operator /(Vector3i v, int s) {
        return new Vector3i(v.x / s, v.y / s, v.z / s);
    }

    public static bool operator <(Vector3i a, Vector3i b) {
        return a.x < b.x && a.y < b.y && a.z < b.z;
    }

    public static bool operator >(Vector3i a, Vector3i b) {
        return a.x > b.x && a.y > b.y && a.z > b.z;
    }

    public static bool operator <=(Vector3i a, Vector3i b) {
        return a.x <= b.x && a.y <= b.y && a.z <= b.z;
    }

    public static bool operator >=(Vector3i a, Vector3i b) {
        return a.x >= b.x && a.y >= b.y && a.z >= b.z;
    }
   
    public bool Equals(Vector3i other) {
        return other.y == y && other.z == z && other.x == x;
    }

    public override bool Equals(object obj) {
        if (ReferenceEquals(null, obj)) return false;
        if (obj.GetType() != typeof(Vector3i)) return false;
        return Equals((Vector3i)obj);
    }

    public override int GetHashCode() {
        unchecked {
            int result = y;
            result = (result * 397) ^ z;
            result = (result * 397) ^ x;
            return result;
        }
    }
}