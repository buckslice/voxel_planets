﻿using UnityEngine;

[ExecuteInEditMode]
[RequireComponent(typeof(Camera))]
[AddComponentMenu("Effects/Raymarch (Generic Complete)")]
public class RaymarchGeneric : SceneViewFilter {
    public Transform lightTransform;
    public Shader effectShader;
    public float rayMarchMaxDist = 64;
    public Texture2D matColorRamp;
    public Texture2D perfColorRamp;
    public bool debugPerformance = false;
    public Color mainColor = new Color(1,1,1,1);

    public Material EffectMaterial {
        get {
            if (!_EffectMaterial && effectShader) {
                _EffectMaterial = new Material(effectShader) {
                    hideFlags = HideFlags.HideAndDontSave
                };
            }

            return _EffectMaterial;
        }
    }
    private Material _EffectMaterial;

    public Camera CurrentCamera {
        get {
            if (!_CurrentCamera)
                _CurrentCamera = GetComponent<Camera>();
            return _CurrentCamera;
        }
    }
    private Camera _CurrentCamera;

    void OnDrawGizmos() {
        Gizmos.color = Color.green;

        Matrix4x4 corners = GetFrustumCorners(CurrentCamera);
        Vector3 pos = CurrentCamera.transform.position;

        for (int x = 0; x < 4; x++) {
            corners.SetRow(x, CurrentCamera.cameraToWorldMatrix * corners.GetRow(x));
            Gizmos.DrawLine(pos, pos + (Vector3)(corners.GetRow(x)));
        }

        /*
        // UNCOMMENT TO DEBUG RAY DIRECTIONS
        Gizmos.color = Color.red;
        int n = 10; // # of intervals
        for (int x = 1; x < n; x++) {
            float i_x = (float)x / (float)n;

            var w_top = Vector3.Lerp(corners.GetRow(0), corners.GetRow(1), i_x);
            var w_bot = Vector3.Lerp(corners.GetRow(3), corners.GetRow(2), i_x);
            for (int y = 1; y < n; y++) {
                float i_y = (float)y / (float)n;
                
                var w = Vector3.Lerp(w_top, w_bot, i_y).normalized;
                Gizmos.DrawLine(pos + (Vector3)w, pos + (Vector3)w * 1.2f);
            }
        }
        */
    }

    [ImageEffectOpaque]
    void OnRenderImage(RenderTexture source, RenderTexture destination) {
        if (!EffectMaterial) {
            Graphics.Blit(source, destination); // do nothing
            return;
        }

        // Set any custom shader variables here.  For example, you could do:
        // EffectMaterial.SetFloat("_MyVariable", 13.37f);
        // This would set the shader uniform _MyVariable to value 13.37

        EffectMaterial.SetVector("_LightDir", lightTransform ? lightTransform.forward : Vector3.down);

        //// Construct a Model Matrix for the Torus
        //Matrix4x4 MatTorus = Matrix4x4.TRS(
        //    Vector3.right * Mathf.Sin(Time.time) * 5,
        //    Quaternion.identity,
        //    Vector3.one);
        //MatTorus *= Matrix4x4.TRS(
        //    Vector3.zero,
        //    Quaternion.Euler(new Vector3(0, 0, (Time.time * 200) % 360)),
        //    Vector3.one);
        //// Send the torus matrix to our shader
        //EffectMaterial.SetMatrix("_MatTorus_InvModel", MatTorus.inverse);

        EffectMaterial.SetColor("_Color", mainColor);
        EffectMaterial.SetTexture("_ColorRamp_Material", matColorRamp);
        EffectMaterial.SetTexture("_ColorRamp_PerfMap", perfColorRamp);

        EffectMaterial.SetFloat("_DrawDistance", rayMarchMaxDist);

        if (EffectMaterial.IsKeywordEnabled("DEBUG_PERFORMANCE") != debugPerformance) {
            if (debugPerformance)
                EffectMaterial.EnableKeyword("DEBUG_PERFORMANCE");
            else
                EffectMaterial.DisableKeyword("DEBUG_PERFORMANCE");
        }

        EffectMaterial.SetMatrix("_FrustumCornersES", GetFrustumCorners(CurrentCamera));
        EffectMaterial.SetMatrix("_CameraInvViewMatrix", CurrentCamera.cameraToWorldMatrix);
        EffectMaterial.SetVector("_CameraWS", CurrentCamera.transform.position);

        CustomGraphicsBlit(source, destination, EffectMaterial, 0);
    }

    /// \brief Stores the normalized rays representing the camera frustum in a 4x4 matrix.  Each row is a vector.
    /// 
    /// The following rays are stored in each row (in eyespace, not worldspace):
    /// Top Left corner:     row=0
    /// Top Right corner:    row=1
    /// Bottom Right corner: row=2
    /// Bottom Left corner:  row=3
    private Matrix4x4 GetFrustumCorners(Camera cam) {
        float camFov = cam.fieldOfView;
        float camAspect = cam.aspect;

        Matrix4x4 frustumCorners = Matrix4x4.identity;

        float fovWHalf = camFov * 0.5f;

        float tan_fov = Mathf.Tan(fovWHalf * Mathf.Deg2Rad);

        Vector3 toRight = Vector3.right * tan_fov * camAspect;
        Vector3 toTop = Vector3.up * tan_fov;

        Vector3 topLeft = (-Vector3.forward - toRight + toTop);
        Vector3 topRight = (-Vector3.forward + toRight + toTop);
        Vector3 bottomRight = (-Vector3.forward + toRight - toTop);
        Vector3 bottomLeft = (-Vector3.forward - toRight - toTop);

        frustumCorners.SetRow(0, topLeft);
        frustumCorners.SetRow(1, topRight);
        frustumCorners.SetRow(2, bottomRight);
        frustumCorners.SetRow(3, bottomLeft);

        return frustumCorners;
    }

    /// \brief Custom version of Graphics.Blit that encodes frustum corner indices into the input vertices.
    /// 
    /// In a shader you can expect the following frustum cornder index information to get passed to the z coordinate:
    /// Top Left vertex:     z=0, u=0, v=0
    /// Top Right vertex:    z=1, u=1, v=0
    /// Bottom Right vertex: z=2, u=1, v=1
    /// Bottom Left vertex:  z=3, u=1, v=0
    /// 
    /// \warning You may need to account for flipped UVs on DirectX machines due to differing UV semantics
    ///          between OpenGL and DirectX.  Use the shader define UNITY_UV_STARTS_AT_TOP to account for this.
    static void CustomGraphicsBlit(RenderTexture source, RenderTexture dest, Material fxMaterial, int passNr) {
        RenderTexture.active = dest;

        fxMaterial.SetTexture("_MainTex", source);  // not sure what this is used for

        GL.PushMatrix();
        GL.LoadOrtho(); // Note: z value of vertices don't make a difference because we are using ortho projection

        fxMaterial.SetPass(passNr);

        GL.Begin(GL.QUADS);

        // Here, GL.MultitexCoord2(0, x, y) assigns the value (x, y) to the TEXCOORD0 slot in the shader.
        // GL.Vertex3(x,y,z) queues up a vertex at position (x, y, z) to be drawn.  Note that we are storing
        // our own custom frustum information in the z coordinate.
        GL.MultiTexCoord2(0, 0.0f, 0.0f);
        GL.Vertex3(0.0f, 0.0f, 3.0f); // BL

        GL.MultiTexCoord2(0, 1.0f, 0.0f);
        GL.Vertex3(1.0f, 0.0f, 2.0f); // BR

        GL.MultiTexCoord2(0, 1.0f, 1.0f);
        GL.Vertex3(1.0f, 1.0f, 1.0f); // TR

        GL.MultiTexCoord2(0, 0.0f, 1.0f);
        GL.Vertex3(0.0f, 1.0f, 0.0f); // TL

        GL.End();
        GL.PopMatrix();
    }

}