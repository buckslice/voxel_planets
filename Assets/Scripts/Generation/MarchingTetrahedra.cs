using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class MarchingTetrahedra {



    static float surface = 0.0f;

    static Vector3[] EdgeVertex = new Vector3[6];

    static Vector3[] CubePosition = new Vector3[8];

    static Vector3[] TetrahedronPosition = new Vector3[4];

    static float[] TetrahedronValue = new float[4];

    public static MeshData CalculateMeshData(Array3<Voxel> voxels, float voxelSize) {
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
                            z + vertexOffset[i][2]].density;
                    }

                    March(x, y, z, density, verts, tris);
                }
            }
        }

        MeshData md = new MeshData(verts.ToArray(), tris.ToArray());
        List<Color32> colors = new List<Color32>();
        for (int i = 0; i < verts.Count; ++i) {
            colors.Add(new Color32(216, 202, 168, 255));
        }
        md.colors = colors.ToArray();
        return md;
    }

    /// <summary>
    /// MarchCubeTetrahedron performs the Marching Tetrahedrons algorithm on a single cube
    /// </summary>
    static void March(float x, float y, float z, sbyte[] cube, IList<Vector3> vertList, IList<int> indexList) {
        int i, j, vertexInACube;

        //Make a local copy of the cube's corner positions
        for (i = 0; i < 8; i++) {
            CubePosition[i].x = x + vertexOffset[i][0];
            CubePosition[i].y = y + vertexOffset[i][1];
            CubePosition[i].z = z + vertexOffset[i][2];
        }

        for (i = 0; i < 6; i++) {
            for (j = 0; j < 4; j++) {
                vertexInACube = TetrahedronsInACube[i, j];
                TetrahedronPosition[j] = CubePosition[vertexInACube];
                TetrahedronValue[j] = cube[vertexInACube];
            }

            MarchTetrahedron(vertList, indexList);
        }
    }

    /// <summary>
    /// MarchTetrahedron performs the Marching Tetrahedrons algorithm on a single tetrahedron
    /// </summary>
    static void MarchTetrahedron(IList<Vector3> vertList, IList<int> indexList) {
        int i, j, vert, vert0, vert1, idx;
        int flagIndex = 0, edgeFlags;
        float offset, invOffset;

        //Find which vertices are inside of the surface and which are outside
        for (i = 0; i < 4; i++) if (TetrahedronValue[i] <= surface) flagIndex |= 1 << i;

        //Find which edges are intersected by the surface
        edgeFlags = TetrahedronEdgeFlags[flagIndex];

        //If the tetrahedron is entirely inside or outside of the surface, then there will be no intersections
        if (edgeFlags == 0) return;

        //Find the point of intersection of the surface with each edge
        for (i = 0; i < 6; i++) {
            //if there is an intersection on this edge
            if ((edgeFlags & (1 << i)) != 0) {
                vert0 = TetrahedronEdgeConnection[i, 0];
                vert1 = TetrahedronEdgeConnection[i, 1];
                offset = GetOffset(TetrahedronValue[vert0], TetrahedronValue[vert1]);
                invOffset = 1.0f - offset;

                EdgeVertex[i].x = invOffset * TetrahedronPosition[vert0].x + offset * TetrahedronPosition[vert1].x;
                EdgeVertex[i].y = invOffset * TetrahedronPosition[vert0].y + offset * TetrahedronPosition[vert1].y;
                EdgeVertex[i].z = invOffset * TetrahedronPosition[vert0].z + offset * TetrahedronPosition[vert1].z;
            }
        }

        //Save the triangles that were found. There can be up to 2 per tetrahedron
        for (i = 0; i < 2; i++) {
            if (TetrahedronTriangles[flagIndex, 3 * i] < 0) break;

            idx = vertList.Count;

            for (j = 0; j < 3; j++) {
                vert = TetrahedronTriangles[flagIndex, 3 * i + j];
                //indexList.Add(idx + WindingOrder[j]);
                indexList.Add(idx + 2 - j);
                //indexList.Add(idx + j);
                vertList.Add(EdgeVertex[vert]);
            }
        }
    }

    /// <summary>
    /// GetOffset finds the approximate point of intersection of the surface
    /// between two points with the values v1 and v2
    /// </summary>
    static float GetOffset(float v1, float v2) {
        float delta = v2 - v1;
        return (delta == 0.0f) ? surface : (surface - v1) / delta;
    }


    /// <summary>
    /// VertexOffset lists the positions, relative to vertex0, 
    /// of each of the 8 vertices of a cube.
    /// vertexOffset[8][3]
    /// </summary>
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

    /// <summary>
    /// TetrahedronEdgeConnection lists the index of the endpoint vertices for each of the 6 edges of the tetrahedron.
    /// tetrahedronEdgeConnection[6][2]
    /// </summary>
    static readonly int[,] TetrahedronEdgeConnection = new int[,]
    {
            {0,1},  {1,2},  {2,0},  {0,3},  {1,3},  {2,3}
    };

    /// <summary>
    /// TetrahedronEdgeConnection lists the index of verticies from a cube 
    /// that made up each of the six tetrahedrons within the cube.
    /// tetrahedronsInACube[6][4]
    /// </summary>
    static readonly int[,] TetrahedronsInACube = new int[,]
    {
            {0,5,1,6},
            {0,1,2,6},
            {0,2,3,6},
            {0,3,7,6},
            {0,7,4,6},
            {0,4,5,6}
    };

    /// <summary>
    /// For any edge, if one vertex is inside of the surface and the other is outside of 
    /// the surface then the edge intersects the surface
    /// For each of the 4 vertices of the tetrahedron can be two possible states, 
    /// either inside or outside of the surface
    /// For any tetrahedron the are 2^4=16 possible sets of vertex states.
    /// This table lists the edges intersected by the surface for all 16 possible vertex states.
    /// There are 6 edges.  For each entry in the table, if edge #n is intersected, then bit #n is set to 1.
    /// tetrahedronEdgeFlags[16]
    /// </summary>
    static readonly int[] TetrahedronEdgeFlags = new int[]
    {
            0x00, 0x0d, 0x13, 0x1e, 0x26, 0x2b, 0x35, 0x38, 0x38, 0x35, 0x2b, 0x26, 0x1e, 0x13, 0x0d, 0x00
    };

    /// <summary>
    /// For each of the possible vertex states listed in tetrahedronEdgeFlags there
    /// is a specific triangulation of the edge intersection points.  
    /// TetrahedronTriangles lists all of them in the form of 0-2 edge triples 
    /// with the list terminated by the invalid value -1.
    /// tetrahedronTriangles[16][7]
    /// </summary>
    static readonly int[,] TetrahedronTriangles = new int[,]
    {
            {-1, -1, -1, -1, -1, -1, -1},
            { 0,  3,  2, -1, -1, -1, -1},
            { 0,  1,  4, -1, -1, -1, -1},
            { 1,  4,  2,  2,  4,  3, -1},

            { 1,  2,  5, -1, -1, -1, -1},
            { 0,  3,  5,  0,  5,  1, -1},
            { 0,  2,  5,  0,  5,  4, -1},
            { 5,  4,  3, -1, -1, -1, -1},

            { 3,  4,  5, -1, -1, -1, -1},
            { 4,  5,  0,  5,  2,  0, -1},
            { 1,  5,  0,  5,  3,  0, -1},
            { 5,  2,  1, -1, -1, -1, -1},

            { 3,  4,  2,  2,  4,  1, -1},
            { 4,  1,  0, -1, -1, -1, -1},
            { 2,  3,  0, -1, -1, -1, -1},
            {-1, -1, -1, -1, -1, -1, -1}
    };

}