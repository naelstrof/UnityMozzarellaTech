using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class Mozzarella : MonoBehaviour {
    [Range(64, 2048)]
    public int numParticles = 512;
    private int numParticlesID, pointsID, gravityID, lengthID, depthTextureID, worldToCameraID, cameraInverseProjectionID, cameraVPID, cameraToWorldID, nearClipValueID;
    private int cameraDepthID;
    private int farClipValueID;

    private int squirtIndex;
    private int squirtIndexID;
    private int squirtPositionID;
    private int squirtVelocityID;
    private int squirtVolumeID;
    private int deltaTimeID;
    public Mesh mesh;
    private Material instantiatedMeshMaterial;
    public Material instancedMeshMaterial;
    public ComputeShader verletProcessor;
    private ComputeBuffer points;
    private ComputeBuffer positions;

    private struct Point {
        public Vector3 position;
        public Vector3 prevPosition;
        public Vector3 savedPosition;
        public float volume;
    }
    void Awake() {
        pointsID = Shader.PropertyToID("_Points");
        deltaTimeID = Shader.PropertyToID("_DeltaTime");
        gravityID = Shader.PropertyToID("_Gravity");
        lengthID = Shader.PropertyToID("_Length");
        numParticlesID = Shader.PropertyToID("_NumParticles");
        depthTextureID = Shader.PropertyToID("_DepthTexture");
        worldToCameraID = Shader.PropertyToID("_WorldToCamera");
        cameraToWorldID = Shader.PropertyToID("_CameraToWorld");
        cameraInverseProjectionID = Shader.PropertyToID("_CameraInverseProjection");
        nearClipValueID = Shader.PropertyToID("_NearClipValue");
        farClipValueID = Shader.PropertyToID("_FarClipValue");
        cameraVPID = Shader.PropertyToID("_ViewProjection");

        squirtVolumeID = Shader.PropertyToID("_SquirtVolume");
        squirtVelocityID = Shader.PropertyToID("_SquirtVelocity");
        squirtPositionID = Shader.PropertyToID("_SquirtPosition");
        squirtIndexID = Shader.PropertyToID("_SquirtIndex");
        cameraDepthID = Shader.PropertyToID("_CameraDepthTexture");
        instantiatedMeshMaterial = Material.Instantiate(instancedMeshMaterial);
    }
    void Start() {
        points = new ComputeBuffer(numParticles, sizeof(float)*10);
        List<Point> startingPoints = new List<Point>();
        for(int i=0;i<numParticles;i++) {
            Vector3 rand = UnityEngine.Random.insideUnitSphere;
            startingPoints.Add(new Point() {
                position = rand,
                prevPosition = rand,
                volume = 0f,
            });
        }
        points.SetData<Point>(startingPoints);
        verletProcessor.SetVector(gravityID, Physics.gravity);
        verletProcessor.SetFloat(deltaTimeID, Time.fixedDeltaTime);
        verletProcessor.SetFloat(lengthID, 1f*Time.fixedDeltaTime);
        instantiatedMeshMaterial.SetBuffer("_Points", points);
    }
    void FixedUpdate() {
        float volume = Mathf.Clamp01(Mathf.Sin(Time.time*5f));
        Vector3 pos = transform.position+UnityEngine.Random.insideUnitSphere*0.01f;
        Vector3 vel = (transform.up+Mathf.Sin(Time.time*8f)*transform.right+Mathf.Cos(Time.time*8f)*transform.forward).normalized*0.1f;

        verletProcessor.SetBuffer(0, pointsID, points);
        verletProcessor.SetInt(numParticlesID, numParticles);
        verletProcessor.SetVector(squirtPositionID, pos);
        verletProcessor.SetFloat(squirtVolumeID, volume);
        verletProcessor.SetInt(squirtIndexID, squirtIndex);
        squirtIndex = (++squirtIndex)%numParticles;
        verletProcessor.SetVector(squirtVelocityID, vel);
        verletProcessor.SetMatrix(worldToCameraID, Camera.main.worldToCameraMatrix);
        verletProcessor.SetMatrix(cameraToWorldID, Camera.main.cameraToWorldMatrix);
        verletProcessor.SetMatrix(cameraInverseProjectionID, Matrix4x4.Inverse(Camera.main.projectionMatrix));
        Matrix4x4 matrixVP = Camera.main.projectionMatrix  * Camera.main.worldToCameraMatrix; //multipication order matters
        verletProcessor.SetMatrix(cameraVPID, matrixVP);

        verletProcessor.SetFloat(nearClipValueID, Camera.main.nearClipPlane);
        verletProcessor.SetFloat(farClipValueID, Camera.main.farClipPlane);
        verletProcessor.SetTextureFromGlobal(0, depthTextureID, cameraDepthID);

        // Point update
        verletProcessor.Dispatch(0, numParticles, 1, 1);
    }
    void OnDestroy() {
        points.Dispose();
    }
    void Update() {
        // Draw to screen
        Graphics.DrawMeshInstancedProcedural(mesh, 0, instantiatedMeshMaterial, new Bounds(Vector3.zero, Vector3.one*1000f), numParticles);
    }
}
