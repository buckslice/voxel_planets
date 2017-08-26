using UnityEngine;
using System.Collections.Generic;

public static class TransvoxelExtractor {

#if false
    public static MeshData GenLodCell(Array3<sbyte> chunk, int lod) {

        MeshBuilder mesh = new MeshBuilder();

        sbyte[] density = new sbyte[8];
        int size = chunk.size;
        for (int x = 0; x < size; ++x) {
            for(int y = 0; y < size; ++y) {
                for(int z = 0; z < size; ++z) {
                    // send this cell the 8 density values at its corners
                    for(int i = 0; i < 8; ++i) {
                        density[i] = chunk[
                            x + Tables.vertexOffset[i, 0],
                            y + Tables.vertexOffset[i, 1],
                            z + Tables.vertexOffset[i, 2]];
                    }

                    //PolygonizeCell(new Vector3i(x, y, z), mesh, lod);
                }
            }
        }

        return mesh.ToMeshData();
    }

    private const float ONE_OVER_256 = 1.0f / 256.0f;

    // pos is xyz position of cell and density is values at corners
    public static void PolygonizeRegularCell(Vector3i pos, Array3<sbyte> voxels, List<Vector3> verts, List<int> indices) {

        byte dirMask = (byte)((pos.x > 0 ? 1 : 0) | ((pos.y > 0 ? 1 : 0) << 1) | ((pos.z > 0 ? 1 : 0) << 2));

        //byte near = 0;
        // compute which six faces of the block that vertex is near (in boundary cell)
        // skip this for now since no transitions

        Vector3i[] corners = Tables.CornerIndex;
        for(int i = 0; i < corners.Length; ++i) {
            corners[i] += pos;
        }

        sbyte[] density = new sbyte[] {
            voxels[corners[0]],
            voxels[corners[1]],
            voxels[corners[2]],
            voxels[corners[3]],
            voxels[corners[4]],
            voxels[corners[5]],
            voxels[corners[6]],
            voxels[corners[7]]
        };

        uint caseCode = (uint)(
            ((density[0] >> 7) & 0x01)
          | ((density[1] >> 6) & 0x02)
          | ((density[2] >> 5) & 0x04)
          | ((density[3] >> 4) & 0x08)
          | ((density[4] >> 3) & 0x10)
          | ((density[5] >> 2) & 0x20)
          | ((density[6] >> 1) & 0x40)
          | (density[7] & 0x80));

        var c = Tables.RegularCellClass[caseCode];
        var data = Tables.RegularCellData[c];

        byte numTris = (byte)data.GetTriangleCount();
        byte numVerts = (byte)data.GetVertexCount();

        int[] localVertexMapping = new int[12];

        // generate vertex positions by interpolating along
        // each of the edges that intersect the isosurface
        for(int i = 0; i < numVerts; ++i) {
            ushort edgeCode = Tables.RegularVertexData[caseCode][i];
            byte v0 = HiNibble((byte)(edgeCode & 0xFF));
            byte v1 = LoNibble((byte)(edgeCode & 0xFF));

            Vector3i p0 = corners[v0];
            Vector3i p1 = corners[v1];

            int d0 = voxels[p0];
            int d1 = voxels[p1];

            Debug.Assert(v0 < v1);

            int t = (d1 << 8) / (d1 - d0);
            int u = 0x0100 - t;

            float t0 = t * ONE_OVER_256;
            float t1 = u * ONE_OVER_256;

            if ((t & 0x00ff) != 0) {
                // vertex lies in the interior of the edge
                byte dir = HiNibble((byte)(edgeCode >> 8));
                byte idx = LoNibble((byte)(edgeCode >> 8));
                //bool present = (dir & dirMask) == dir;

                localVertexMapping[i] = verts.Count;
                Vector3 pi = Interp(p0.ToVector3(), p1.ToVector3(), p0, p1, voxels);
                verts.Add(pi);

            } else if (true || t == 0 && v1 == 7) {

                // check if this is right
                Vector3 pi = new Vector3(
                    p0.x * t0 + p1.x * t1,
                    p0.y * t0 + p1.y * t1,
                    p0.z * t0 + p1.z * t1);

                localVertexMapping[i] = verts.Count;
                verts.Add(pi);
            } 

        }

        for(int t = 0; t < numTris; ++t) {
            for(int i = 0; i < 3; ++i) {
                indices.Add(localVertexMapping[data.GetIndices()[t * 3 + i]]);
            }
        }

    }

    private static Vector3 Interp(Vector3 v0, Vector3 v1, Vector3i p0, Vector3i p1, Array3<sbyte> voxels) {
        sbyte s0 = voxels[p0];
        sbyte s1 = voxels[p1];

        int t = (s1 << 8) / (s1 - s0);
        int u = 0x0100 - t;


        if ((t & 0x00ff) == 0) {
            // The generated vertex lies at one of the corners so there 
            // is no need to subdivide the interval.
            if (t == 0) {
                return v1;
            }
            return v0;
        } else {

            Vector3 vm = (v0 + v1) / 2;
            Vector3i pm = (p0 + p1) / 2;

            sbyte sm = voxels[pm];

            // Determine which of the sub-intervals that contain 
            // the intersection with the isosurface.
            if (Sign(s0) != Sign(sm)) {
                v1 = vm;
                p1 = pm;
                s1 = sm;
            } else {
                v0 = vm;
                p0 = pm;
                s0 = sm;
            }

            t = (s1 << 8) / (s1 - s0);
            u = 0x0100 - t;

            return v0 * t * ONE_OVER_256 + v1 * u * ONE_OVER_256;
        }
    }

    private static int Sign(sbyte b) {
        return (b >> 7) & 1;
    }

    private static byte HiNibble(byte b) {
        return (byte)(((b) >> 4) & 0x0F);
    }

    private static byte LoNibble(byte b) {
        return (byte)(b & 0x0F);
    }

    private static void PolygonizeCell(Vector3i pos, sbyte[] density, MeshBuilder mesh, int lod) {

        byte directionMask = (byte)((pos.x > 0 ? 1 : 0) | ((pos.z > 0 ? 1 : 0) << 1) | ((pos.y > 0 ? 1 : 0) << 2));

        byte caseCode = GetCaseCode(density);
        if ((caseCode ^ ((density[7] >> 7) & 0xFF)) == 0) //for this cases there is no triangulation
            return;

        byte regularCellClass = Tables.RegularCellClass[caseCode];
        ushort[] vertexLocations = Tables.RegularVertexData[caseCode];

        Tables.RegularCell c = Tables.RegularCellData[regularCellClass];
        long vertexCount = c.GetVertexCount();
        long triangleCount = c.GetTriangleCount();
        byte[] indexOffset = c.GetIndices();    // index offsets for current cell
        int[] mappedIndices = new int[indexOffset.Length];

        for(int i = 0; i < vertexCount; ++i) {

            byte edge = (byte)(vertexLocations[i] >> 8);
            byte reuseIndex = (byte)(edge & 0xF); // vertex id which should be created or reused 1,2 or 3
            byte rDir = (byte)(edge >> 4); // the direction to go to reach a previous cell for reusing 

            byte v0 = (byte)((vertexLocations[i] >> 4) & 0x0F); // first corner index
            byte v1 = (byte)((vertexLocations[i]) & 0x0F); // second corner index

            sbyte d0 = density[v0];
            sbyte d1 = density[v1];

            Debug.Assert(v1 > v0);

            int t = (d1 << 8) / (d1 - d0);
            int u = 0x0100 - t;
            float t0 = t / 256f;
            float t1 = u / 256f;

            int index = -1;

            // todo: try the caching stuff later

            if(index == -1) {
                Vector3 p0 = new Vector3(
                    pos.x + Tables.vertexOffset[v0, 0],
                    pos.y + Tables.vertexOffset[v0, 1],
                    pos.z + Tables.vertexOffset[v0, 2]);
                Vector3 p1 = new Vector3(
                    pos.x + Tables.vertexOffset[v1, 0],
                    pos.y + Tables.vertexOffset[v1, 1],
                    pos.z + Tables.vertexOffset[v1, 2]);

                mesh.AddVertex(InterpolateVoxelVector(t, p0, p1));

            }

            mappedIndices[i] = index;
        }

    }

    private static Vector3 InterpolateVoxelVector(long t, Vector3 a, Vector3 b) {
        long u = 0x0100 - t; //256 - t
        float s = 1.0f / 256.0f;
        Vector3 ret = a * t + b * u; // density interpolation
        ret *= s; // todo: shift to shader ! 
        return ret;
    }

    private static byte GetCaseCode(sbyte[] density) {
        byte code = 0;
        byte konj = 0x01;
        for (int i = 0; i < density.Length; i++) {
            code |= (byte)((density[i] >> (density.Length - 1 - i)) & konj);
            konj <<= 1;
        }

        return code;
    }
#endif

}
