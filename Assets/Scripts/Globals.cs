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
    public static int color = Shader.PropertyToID("_Color");
    public static int mainTex = Shader.PropertyToID("_MainTex");

    public static int transparency = Shader.PropertyToID("_Transparency");
    public static int localOffset = Shader.PropertyToID("_LocalOffset");
}