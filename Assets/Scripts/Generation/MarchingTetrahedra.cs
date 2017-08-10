using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class MarchingTetrahedra {


    public static MeshData CalculateMeshData(Array3<sbyte> voxels, float voxelSize) {
        List<Vector3> verts = new List<Vector3>();
        List<int> tris = new List<int>();

        sbyte[] density = new sbyte[8];

        int end = voxels.size - 1;

        for (int z = 0; z < end; ++z) {
            for (int y = 0; y < end; ++y) {
                for (int x = 0; x < end; ++x) {
                    for (int i = 0; i < 8; ++i) {
                        density[i] = voxels[
                            x + vertexOffset[i][0],
                            y + vertexOffset[i][1],
                            z + vertexOffset[i][2]];
                    }

                    Vector3 p = new Vector3(x, y, z);
                    for (int i = 0; i < tetraList.Length; ++i) {
                        int[] T = tetraList[i];
                        int triIndex = 0;
                        if (density[T[0]] < 0) triIndex |= 1;
                        if (density[T[1]] < 0) triIndex |= 2;
                        if (density[T[2]] < 0) triIndex |= 4;
                        if (density[T[3]] < 0) triIndex |= 8;

                        int i0, i1, i2, i3;
                        switch (triIndex) {
                            case 0x00:
                            case 0x0F:
                                break;
                            case 0x0E:
                                i0 = Interp(p, T[0], T[1], density, verts);
                                i1 = Interp(p, T[0], T[3], density, verts);
                                i2 = Interp(p, T[0], T[2], density, verts);
                                tris.Add(i0); tris.Add(i1); tris.Add(i2);
                                break;
                            case 0x01:
                                i0 = Interp(p, T[0], T[1], density, verts);
                                i1 = Interp(p, T[0], T[2], density, verts);
                                i2 = Interp(p, T[0], T[3], density, verts);
                                tris.Add(i0); tris.Add(i1); tris.Add(i2);
                                break;
                            case 0x0D:
                                i0 = Interp(p, T[1], T[0], density, verts);
                                i1 = Interp(p, T[1], T[2], density, verts);
                                i2 = Interp(p, T[1], T[3], density, verts);
                                tris.Add(i0); tris.Add(i1); tris.Add(i2);
                                break;
                            case 0x02:
                                i0 = Interp(p, T[1], T[0], density, verts);
                                i1 = Interp(p, T[1], T[3], density, verts);
                                i2 = Interp(p, T[1], T[2], density, verts);
                                tris.Add(i0); tris.Add(i1); tris.Add(i2);
                                break;
                            case 0x0C:
                                i0 = Interp(p, T[1], T[2], density, verts);
                                i1 = Interp(p, T[1], T[3], density, verts);
                                i2 = Interp(p, T[0], T[3], density, verts);
                                i3 = Interp(p, T[0], T[2], density, verts);
                                tris.Add(i0); tris.Add(i1); tris.Add(i2); tris.Add(i2); tris.Add(i3); tris.Add(i0);
                                break;
                            case 0x03:
                                i0 = Interp(p, T[1], T[2], density, verts);
                                i1 = Interp(p, T[0], T[2], density, verts);
                                i2 = Interp(p, T[0], T[3], density, verts);
                                i3 = Interp(p, T[1], T[3], density, verts);
                                tris.Add(i0); tris.Add(i1); tris.Add(i2); tris.Add(i2); tris.Add(i3); tris.Add(i0);
                                break;
                            case 0x04:
                                i0 = Interp(p, T[2], T[0], density, verts);
                                i1 = Interp(p, T[2], T[1], density, verts);
                                i2 = Interp(p, T[2], T[3], density, verts);
                                tris.Add(i0); tris.Add(i1); tris.Add(i2);
                                break;
                            case 0x0B:
                                i0 = Interp(p, T[2], T[0], density, verts);
                                i1 = Interp(p, T[2], T[3], density, verts);
                                i2 = Interp(p, T[2], T[1], density, verts);
                                tris.Add(i0); tris.Add(i1); tris.Add(i2);
                                break;
                            case 0x05:
                                i0 = Interp(p, T[0], T[1], density, verts);
                                i1 = Interp(p, T[1], T[2], density, verts);
                                i2 = Interp(p, T[2], T[3], density, verts);
                                i3 = Interp(p, T[0], T[3], density, verts);
                                tris.Add(i0); tris.Add(i1); tris.Add(i2); tris.Add(i2); tris.Add(i3); tris.Add(i0);
                                break;
                            case 0x0A:
                                i0 = Interp(p, T[0], T[1], density, verts);
                                i1 = Interp(p, T[0], T[3], density, verts);
                                i2 = Interp(p, T[2], T[3], density, verts);
                                i3 = Interp(p, T[1], T[2], density, verts);
                                tris.Add(i0); tris.Add(i1); tris.Add(i2); tris.Add(i2); tris.Add(i3); tris.Add(i0);
                                break;
                            case 0x06:
                                i0 = Interp(p, T[2], T[3], density, verts);
                                i1 = Interp(p, T[0], T[2], density, verts);
                                i2 = Interp(p, T[0], T[1], density, verts);
                                i3 = Interp(p, T[1], T[3], density, verts);
                                tris.Add(i0); tris.Add(i1); tris.Add(i2); tris.Add(i2); tris.Add(i3); tris.Add(i0);
                                break;
                            case 0x09:
                                i0 = Interp(p, T[2], T[3], density, verts);
                                i1 = Interp(p, T[1], T[3], density, verts);
                                i2 = Interp(p, T[0], T[1], density, verts);
                                i3 = Interp(p, T[0], T[2], density, verts);
                                tris.Add(i0); tris.Add(i1); tris.Add(i2); tris.Add(i2); tris.Add(i3); tris.Add(i0);
                                break;
                            case 0x07:
                                i0 = Interp(p, T[3], T[0], density, verts);
                                i1 = Interp(p, T[3], T[1], density, verts);
                                i2 = Interp(p, T[3], T[2], density, verts);
                                tris.Add(i0); tris.Add(i1); tris.Add(i2);
                                break;
                            case 0x08:
                                i0 = Interp(p, T[3], T[0], density, verts);
                                i1 = Interp(p, T[3], T[2], density, verts);
                                i2 = Interp(p, T[3], T[1], density, verts);
                                tris.Add(i0); tris.Add(i1); tris.Add(i2);
                                break;
                        }
                    }
                }
            }
        }

        return new MeshData(verts.ToArray(), tris.ToArray());

    }

    static int Interp(Vector3 pos, int i0, int i1, sbyte[] density, List<Vector3> verts) {
        float d0 = density[i0];
        float d1 = density[i1];
        int[] p0 = vertexOffset[i0];
        int[] p1 = vertexOffset[i1];
        float t = d0 - d1;
        if (Mathf.Abs(t) > 1e-6) {
            t = d0 / t;
        }
        pos.x += p0[0] + t * (p1[0] - p0[0]);
        pos.y += p0[1] + t * (p1[1] - p0[1]);
        pos.z += p0[2] + t * (p1[2] - p0[2]);

        verts.Add(pos);
        return verts.Count - 1;
    }


    static int[][] tetraList = new int[][] {
        new int[]{0,2,3,7},
        new int[]{0,6,2,7},
        new int[]{0,4,6,7},
        new int[]{0,6,1,2},
        new int[]{0,1,6,4},
        new int[]{5,6,1,4}
    };

    static int[][] vertexOffset = new int[][] {
        new int[]{0, 0, 0},
        new int[]{1, 0, 0},
        new int[]{1, 1, 0},
        new int[]{0, 1, 0},
        new int[]{0, 0, 1},
        new int[]{1, 0, 1},
        new int[]{1, 1, 1},
        new int[]{0, 1, 1}
    };

}