using UnityEngine;
using System.Collections;

public static class WorldGenerator {
    
    // size is length of arrays
    // depth is octree depth
    // voxelSize is how big each voxel should be
    // radius is radius of planet
    // position is starting position of quadtree
    public static float[][][] CreateVoxels(
        int size, int depth, float voxelSize, float radius, Vector3 pos) {

        float[][][] voxels = VoxelUtils.Init3DArray<float>(size);
        
        int s = (depth == 0) ? 0 : 1;
        s = 0;  // temp until figure out data inheritance
        for (int x = s; x < size; x += s + 1) {
            for (int y = s; y < size; y += s + 1) {
                for (int z = s; z < size; z += s + 1) {
                    Vector3 worldPos = new Vector3(x, y, z) * voxelSize + pos;

                    float sqrMag = worldPos.sqrMagnitude;
                    //float blend = 1f;

                    float freq = 0.01f;
                    double d = SimplexNoise.noise(worldPos.x * freq, worldPos.y * freq, worldPos.z * freq);
                    //WorleySample w = Noise.Worley3(worldPos.x * freq, worldPos.y * freq, worldPos.z * freq, 2, DistanceFunction.EUCLIDIAN);
                    //double d = w.F[1] - w.F[0];
                    
                    //d = (d + 1.0) / 2.0;
                    float surfHeight = radius;
                    surfHeight += (float)d * 20f;

                    // need to figure out way to distribute smooth values over certain ranges
                    // for each block type so it looks smooth
                    // like give each block id a 0-1float range and blend between
                    //if (sqrMag < surfHeight * surfHeight) {   // assumes world is at origin
                    //    voxels[x][y][z] = (surfHeight * surfHeight - sqrMag)/1000f;
                    //} else {
                    //    voxels[x][y][z] = Mathf.Max(-1000f, (surfHeight * surfHeight - sqrMag))/1000f;
                    //}

                    // sqrt of 100,000 is around 300 so blending over that range
                    // so what if voxels was a byte array then blend from 0-127 inside then 127-255 outside
                    // and use each bit for lerping smoothly
                    // need to factor in stride somehow too probly
                    // because when low lod levels the distance to surface will be greater
                    voxels[x][y][z] = (surfHeight * surfHeight - sqrMag) / 100000f;

                    //voxels[x][y][z] = (float)d;

                    voxels[x][y][z] = Mathf.Clamp(voxels[x][y][z], -1.0f, 1.0f);
                }
            }
        }

        return voxels;
    }

    // this creates a control map used to texture the mesh based on the slope
    // of the vert. Its very basic and if you modify how this works you will
    // need to modify the shader as well.
    // TODO switch to Color32 probably
    public static Color32[] GenerateControlMap(Vector3[] normals) {
        int size = normals.Length;
        Color32[] control = new Color32[size];

        for (int i = 0; i < size; i++) {
            float dpUp = Vector3.Dot(normals[i], Vector3.up);

            //Red channel is the sand on flat areas
            float R = (Mathf.Max(0.0f, dpUp) < 0.8f) ? 0.0f : 1.0f;
            //Green channel is the gravel on the sloped areas
            float G = Mathf.Pow(Mathf.Abs(dpUp), 2.0f);

            //Whats left end up being the rock face on the vertical areas
            control[i] = new Color32((byte)(R * 255), (byte)(G * 255), 0, 0);
            //control[i] = new Color32(R, G, 0, 0);
        }
        return control;
    }



}
