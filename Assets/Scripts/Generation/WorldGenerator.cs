using UnityEngine;
using System.Collections;

public static class WorldGenerator {

    // size is length of arrays
    // depth is octree depth
    // voxelSize is how big each voxel should be
    // radius is radius of planet
    // position is starting position of quadtree
    public static Array3<Voxel> CreateVoxels(
        int size, int depth, float voxelSize, Vector3 pos) {

        //float[][][] voxels = VoxelUtils.Init3DArray<float>(size);

        // Vector3i is for some hash lookup shit i think if i wanted to do
        // like hashset<Vector3i, Array3?)
        Array3<Voxel> voxels = new Array3<Voxel>(size, Vector3i.Zero); 

        //int s = (depth == 0) ? 0 : 1;
        //s = 0;  // temp until figure out data inheritance
        //for (int x = s; x < size; x += s + 1) {
        //    for (int y = s; y < size; y += s + 1) {
        //        for (int z = s; z < size; z += s + 1) {
        //            Vector3 worldPos = new Vector3(x, y, z) * voxelSize + pos;

        //            voxels[x, y, z] = Density.Eval(worldPos);
        //            //voxels[x,y,z] = (sbyte)Mathf.Clamp(Density.Eval(worldPos), -128.0f, 127.0f);

        //        }
        //    }
        //}

        // figure this out more..
        // add command to regenerate with different levels here so you can see changes in realtime
        // to better understand why it isnt working with r as voxelSize / 2.0f

        // sbyte goes from -128 to 127 (so -128, -1 and 0 to 127 should be range)

        int x, y, z;
        for (z = 0; z < size; ++z) {
            for (y = 0; y < size; ++y) {
                for (x = 0; x < size; ++x) {
                    Vector3 worldPos = new Vector3(x, y, z) * voxelSize + pos;
                    voxels[x, y, z] = Density.Eval(worldPos, voxelSize);
                }
            }
        }

        // could incorporate into loops above probably (this is probably microoptimization tho i dunno)
        //// figure out if chunk will have a mesh
        //bool set = false;
        //bool positive = false;
        //for (x = 0; x < size; ++x) {
        //    for (y = 0; y < size; ++y) {
        //        for (z = 0; z < size; ++z) {
        //            if (!set) {
        //                positive = voxels[x, y, z] >= MarchingCubes.isoLevel;
        //                set = true;
        //            }else if((positive && voxels[x,y,z] < MarchingCubes.isoLevel)||
        //                    (!positive && voxels[x,y,z] >= MarchingCubes.isoLevel)) {
        //                needsMesh = true;
        //                return voxels;
        //            }
        //        }
        //    }
        //}
        //needsMesh = false;
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
