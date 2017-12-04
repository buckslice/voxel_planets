using UnityEngine;
using System.Collections;

public static class Tags {
    public const string Player = "Player";

}

public static class Layers {
    public const int ObeysGravity = 8;
    public const int Terrain = 9;
}

public static class ShaderProps {
    public static int Color = Shader.PropertyToID("_Color");
    public static int MainTex = Shader.PropertyToID("_MainTex");

    public static int Offset = Shader.PropertyToID("_Offset");
    public static int LocalOffset = Shader.PropertyToID("_LocalOffset");
    public static int Resolution = Shader.PropertyToID("_Resolution");
    public static int Size = Shader.PropertyToID("_Size");
    public static int Transparency = Shader.PropertyToID("_Transparency");

    // marching cubes stuff
    public static int triangles = Shader.PropertyToID("triangles");
    public static int model = Shader.PropertyToID("model");
    public static int trianglesRW = Shader.PropertyToID("trianglesRW");
    public static int densityTexture = Shader.PropertyToID("densityTexture");
    public static int argBuffer = Shader.PropertyToID("argBuffer");
}