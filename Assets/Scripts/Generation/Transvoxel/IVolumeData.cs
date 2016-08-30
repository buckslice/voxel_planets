

public abstract class IVolumeData<T> {

    public abstract T this[int x, int y, int z] { get; set; }

    public T this[Vector3i v] {
        get
        {
            return this[v.x, v.y, v.z];
        }
        set
        {
            this[v.x, v.y, v.z] = value;
        }
    }

    public abstract int ChunkSize { get; set; }

}
