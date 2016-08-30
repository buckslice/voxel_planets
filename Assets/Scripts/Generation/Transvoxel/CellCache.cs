using UnityEngine;
using System.Collections;

public class CellCache {

    class ReuseCell {
        public readonly int[] verts;

        public ReuseCell(int size) {
            verts = new int[size];

            for(int i = 0; i < size; ++i) {
                verts[i] = -1;
            }
        }
    }

    class RegularCellCache {
        private readonly ReuseCell[][] cache;
        private int chunkSize;

        public RegularCellCache(int chunkSize) {
            this.chunkSize = chunkSize;

            cache = new ReuseCell[2][];

            cache[0] = new ReuseCell[chunkSize * chunkSize];
            cache[1] = new ReuseCell[chunkSize * chunkSize]; 

            for(int i = 0; i < chunkSize * chunkSize; ++i) {
                cache[0][i] = new ReuseCell(4);
                cache[1][i] = new ReuseCell(4);
            }
        }

        public ReuseCell GetReusedIndex(Vector3i pos, byte rDir) {
            int rx = rDir & 0x01;
            int rz = (rDir >> 1) & 0x01;
            int ry = (rDir >> 2) & 0x01;

            int dx = pos.x - rx;
            int dy = pos.y - ry;
            int dz = pos.z - rz;

            Debug.Assert(dx >= 0 && dy >= 0 && dz >= 0);
            return cache[dx & 1][dy * chunkSize + dz];
        }

        public ReuseCell this[int x, int y, int z]
        {
            set
            {
                Debug.Assert(x >= 0 && y >= 0 && z >= 0);
                cache[x & 1][y * chunkSize + z] = value;
            }
        }


        public ReuseCell this[Vector3i v]
        {
            set { this[v.x, v.y, v.z] = value; }
        }


        public void SetReusableIndex(Vector3i pos, byte reuseIndex, ushort p) {
            cache[pos.x & 1][pos.y * chunkSize + pos.z].verts[reuseIndex] = p;
        }
    }

    //class TransitionCache {
    //    private readonly ReuseCell[] cache;

    //    public TransitionCache() {
    //        const int cacheSize = 0; // 2 * 
    //    }
    //}

}
