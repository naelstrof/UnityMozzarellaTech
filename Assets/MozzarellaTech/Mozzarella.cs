using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;


public class Mozzarella : MonoBehaviour {
    public static string mainKernelName = "PointsMain";
    [Range(64, 16384)]
    public int numParticles = 512;

    [Tooltip("Determines how fast a particle shrinks to nothing when far away from its neighboring particles.")]
    [Range(0.5f, 1f)]
    [SerializeField]
    private float viscosity = 0.8f;

    [Range(1f,5f)]
    [SerializeField]
    private float attractionStrength = 1f;

    [Range(0f,0.5f)]
    [SerializeField]
    private float friction = 0.1f;

    public ComputeShader verletProcessor;
    public ComputeBuffer pointsBuffer {get; private set;}
    private MozzarellaShaderBlock shaderProperties;
    public delegate void PointsUpdatedAction();
    public PointsUpdatedAction onPointsUpdated;
    public bool visible {
        get { 
            return timeUntilCull > Time.time;
        }
    }
    private float timeUntilCull;

    public struct Point {
        public Vector3 position;
        public Vector3 prevPosition;
        public float volume;
        public float lastHitTime;
    }
    private class MozzarellaShaderBlock {
        public int numParticlesID, pointsID, gravityID,
        viscosityID, depthTextureID, worldToCameraID,
        cameraInverseProjectionID, cameraVPID, cameraToWorldID,
        nearClipValueID, cameraDepthID, normalsTextureID,
        farClipValueID, cameraNormalsID, frictionID, attractionStrengthID, timeID,
        deltaTimeID, mainKernel;
        public MozzarellaShaderBlock(ComputeShader shader) {
            pointsID = Shader.PropertyToID("_Points");
            timeID = Shader.PropertyToID("_Time");
            deltaTimeID = Shader.PropertyToID("_DeltaTime");
            gravityID = Shader.PropertyToID("_Gravity");
            viscosityID = Shader.PropertyToID("_Viscosity");
            frictionID = Shader.PropertyToID("_Friction");
            attractionStrengthID = Shader.PropertyToID("_AttractionStrength");
            numParticlesID = Shader.PropertyToID("_NumParticles");
            depthTextureID = Shader.PropertyToID("_DepthTexture");
            normalsTextureID = Shader.PropertyToID("_NormalsTexture");
            worldToCameraID = Shader.PropertyToID("_WorldToCamera");
            cameraToWorldID = Shader.PropertyToID("_CameraToWorld");
            cameraInverseProjectionID = Shader.PropertyToID("_CameraInverseProjection");
            nearClipValueID = Shader.PropertyToID("_NearClipValue");
            farClipValueID = Shader.PropertyToID("_FarClipValue");
            cameraVPID = Shader.PropertyToID("_ViewProjection");
            cameraDepthID = Shader.PropertyToID("_CameraDepthTexture");
            cameraNormalsID = Shader.PropertyToID("_CameraNormalsTexture");
            mainKernel = shader.FindKernel(Mozzarella.mainKernelName);
        }
    }
    void Awake() {
        // FIXME: Must run with SSAO enabled on URP in order to get the normals texture. Don't know how to check for that without a direct reference. Blegh!
        // First we gotta get our own local copy
        verletProcessor = ComputeShader.Instantiate(verletProcessor);
        shaderProperties = new MozzarellaShaderBlock(verletProcessor);

        pointsBuffer = new ComputeBuffer(numParticles, sizeof(float)*8);
        List<Point> startingPoints = new List<Point>();
        for(int i=0;i<numParticles;i++) {
            startingPoints.Add(new Point());
        }
        pointsBuffer.SetData<Point>(startingPoints);
    }
    public void SetVisibleUntil(float time) {
        timeUntilCull = time;
    }
    void Start() {
        verletProcessor.SetVector(shaderProperties.gravityID, Physics.gravity);
        verletProcessor.SetFloat(shaderProperties.deltaTimeID, Time.fixedDeltaTime);
        verletProcessor.SetFloat(shaderProperties.viscosityID, viscosity);
        verletProcessor.SetFloat(shaderProperties.frictionID, friction);
        verletProcessor.SetFloat(shaderProperties.attractionStrengthID, attractionStrength);
        verletProcessor.SetInt(shaderProperties.numParticlesID, numParticles);
    }
    void FixedUpdate() {
        if (Shader.GetGlobalTexture(shaderProperties.cameraDepthID) == null) {
            return;
        }
        if (Shader.GetGlobalTexture(shaderProperties.cameraNormalsID) == null) {
            return;
        }
        if (Camera.main == null) {
            return;
        }
        float volume = Mathf.Clamp01(Mathf.Sin(Time.time*5f));
        verletProcessor.SetVector(shaderProperties.timeID, new Vector4(Time.time,0,0,0));
        verletProcessor.SetBuffer(0, shaderProperties.pointsID, pointsBuffer);
        verletProcessor.SetMatrix(shaderProperties.worldToCameraID, Camera.main.worldToCameraMatrix);
        verletProcessor.SetMatrix(shaderProperties.cameraToWorldID, Camera.main.cameraToWorldMatrix);
        verletProcessor.SetMatrix(shaderProperties.cameraInverseProjectionID, Matrix4x4.Inverse(Camera.main.projectionMatrix));
        Matrix4x4 matrixVP = Camera.main.projectionMatrix  * Camera.main.worldToCameraMatrix; // multiplication order matters
        verletProcessor.SetMatrix(shaderProperties.cameraVPID, matrixVP);
        verletProcessor.SetFloat(shaderProperties.nearClipValueID, Camera.main.nearClipPlane);
        verletProcessor.SetFloat(shaderProperties.farClipValueID, Camera.main.farClipPlane);
        verletProcessor.SetTextureFromGlobal(0, shaderProperties.depthTextureID, shaderProperties.cameraDepthID);
        verletProcessor.SetTextureFromGlobal(0, shaderProperties.normalsTextureID, shaderProperties.cameraNormalsID);
        // Points update
        verletProcessor.Dispatch(shaderProperties.mainKernel, numParticles, 1, 1);
        onPointsUpdated?.Invoke();
    }
    void OnDestroy() {
        pointsBuffer.Dispose();
    }
    void OnValidate() {
        if (Application.isPlaying && verletProcessor != null && shaderProperties != null) {
            verletProcessor.SetFloat(shaderProperties.viscosityID, viscosity);
            verletProcessor.SetFloat(shaderProperties.frictionID, friction);
            verletProcessor.SetFloat(shaderProperties.attractionStrengthID, attractionStrength);
        }
    }
}
