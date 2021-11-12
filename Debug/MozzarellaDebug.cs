using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

// Meant to draw some debug information on the screen as a texture. Not really useful for anything else
public class MozzarellaDebug : MonoBehaviour {
    public RenderTexture destination;
    public ComputeShader testShader;
    int worldToCameraID, cameraToWorldID, cameraInverseProjectionID, nearClipValueID, farClipValueID, depthTextureID, resultTextureID, cameraDepthID, cameraVP, cameraProjectionID;
    public Material targetMaterial;
    void Awake() {
        resultTextureID = Shader.PropertyToID("Result");
        depthTextureID = Shader.PropertyToID("_DepthTexture");
        worldToCameraID = Shader.PropertyToID("_WorldToCamera");
        cameraToWorldID = Shader.PropertyToID("_CameraToWorld");
        cameraInverseProjectionID = Shader.PropertyToID("_CameraInverseProjection");
        cameraProjectionID = Shader.PropertyToID("_CameraProjection");
        nearClipValueID = Shader.PropertyToID("_NearClipValue");
        farClipValueID = Shader.PropertyToID("_FarClipValue");
        cameraDepthID = Shader.PropertyToID("_CameraDepthTexture");
        cameraVP = Shader.PropertyToID("_ViewProjection");
    }
    void Start() {
        destination = new RenderTexture(512,512,24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
        destination.enableRandomWrite = true;

        destination.enableRandomWrite = true;
        testShader.SetTexture(0, resultTextureID, destination);
        targetMaterial.mainTexture = destination;
    }
    void Update() {
        testShader.SetMatrix(worldToCameraID, Camera.main.worldToCameraMatrix);
        testShader.SetMatrix(cameraToWorldID, Camera.main.cameraToWorldMatrix);
        testShader.SetMatrix(cameraInverseProjectionID, Matrix4x4.Inverse(Camera.main.projectionMatrix));
        Matrix4x4 matrixVP = Camera.main.projectionMatrix  * Camera.main.worldToCameraMatrix; //multipication order matters
        testShader.SetMatrix(cameraVP, matrixVP);
        testShader.SetMatrix(cameraProjectionID, Camera.main.projectionMatrix);
        testShader.SetFloat(nearClipValueID, Camera.main.nearClipPlane);
        testShader.SetFloat(farClipValueID, Camera.main.farClipPlane);
        int threadGroupsX = Mathf.CeilToInt ((Camera.main.pixelWidth) / 8.0f);
        int threadGroupsY = Mathf.CeilToInt ((Camera.main.pixelHeight) / 8.0f);
        testShader.SetTextureFromGlobal(0, depthTextureID, cameraDepthID);
        testShader.Dispatch(0,threadGroupsX,threadGroupsY,1);
    }
}
