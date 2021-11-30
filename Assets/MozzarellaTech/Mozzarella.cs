using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;


public class Mozzarella : MonoBehaviour {
    public static string mainKernelName = "PointsMain";
    [Range(64, 16384)]
    public int numParticles = 512;
    [Range(1,8)]
    public int squirtVolume = 1;
    [Range(0f, 1f)]
    [SerializeField]
    private float viscosity = 0.8f;
    private float particleLifeTime { 
        get {
            int totalParticles = numParticles/squirts.Count;
            float framesPerSecond = 1f/Time.fixedDeltaTime;
            int particlesPerFrame = squirtVolume;
            float particlesPerSecond = squirtVolume*framesPerSecond;
            float lifeTime = ((float)totalParticles)/particlesPerSecond;
            return lifeTime;
        }
    }
    public List<Squirt> squirts;
    //public Mesh mesh;
    //private Material instantiatedMeshMaterial;
    //public Material instancedMeshMaterial;
    public ComputeShader verletProcessor;
    public ComputeBuffer pointsBuffer {get; private set;}
    private ComputeBuffer positionsBuffer;
    private ComputeBuffer squirtsBuffer;
    /*public float pointSize {
        set {
            instantiatedMeshMaterial?.SetFloat("_PointScale", value);
        }
    }*/
    private MozzarellaShaderBlock shaderProperties;
    public delegate void PointsUpdatedAction();
    public PointsUpdatedAction onPointsUpdated;
    private float timer;

    private struct Point {
        public Vector3 position;
        public Vector3 prevPosition;
        public Vector3 savedPosition;
        public float volume;
    }
    [System.Serializable]
    public struct Squirt {
        public Squirt(Squirt other) : this() {
            position = other.position;
            velocity = other.velocity;
            volume = other.volume;
            index = other.index;
        }
        public Squirt(Squirt other, uint newIndex) : this() {
            position = other.position;
            velocity = other.velocity;
            volume = other.volume;
            index = newIndex;
        }
        public Squirt(Vector3 pos, Vector3 vel, float volume, uint index) {
            position = pos;
            velocity = vel;
            this.volume = volume;
            this.index = index;
        }
        public Vector3 position;
        public Vector3 velocity;
        public float volume;
        public uint index;
    }
    public void SetViscosity(float viscosity) {
        this.viscosity = viscosity;
        verletProcessor.SetFloat(shaderProperties.viscosityID, viscosity);
    }
    private class MozzarellaShaderBlock {
        public int numParticlesID, pointsID, gravityID,
        viscosityID, depthTextureID, worldToCameraID,
        cameraInverseProjectionID, cameraVPID, cameraToWorldID,
        nearClipValueID, cameraDepthID, normalsTextureID,
        farClipValueID, numSquirtsID, squirtsID, cameraNormalsID,
        deltaTimeID, squirtIncrementAmountID, mainKernel;
        public MozzarellaShaderBlock(ComputeShader shader) {
            pointsID = Shader.PropertyToID("_Points");
            deltaTimeID = Shader.PropertyToID("_DeltaTime");
            gravityID = Shader.PropertyToID("_Gravity");
            viscosityID = Shader.PropertyToID("_Viscosity");
            numParticlesID = Shader.PropertyToID("_NumParticles");
            depthTextureID = Shader.PropertyToID("_DepthTexture");
            normalsTextureID = Shader.PropertyToID("_NormalsTexture");
            worldToCameraID = Shader.PropertyToID("_WorldToCamera");
            cameraToWorldID = Shader.PropertyToID("_CameraToWorld");
            cameraInverseProjectionID = Shader.PropertyToID("_CameraInverseProjection");
            nearClipValueID = Shader.PropertyToID("_NearClipValue");
            farClipValueID = Shader.PropertyToID("_FarClipValue");
            cameraVPID = Shader.PropertyToID("_ViewProjection");
            squirtsID = Shader.PropertyToID("_Squirts");
            numSquirtsID = Shader.PropertyToID("_NumSquirts");
            cameraDepthID = Shader.PropertyToID("_CameraDepthTexture");
            cameraNormalsID = Shader.PropertyToID("_CameraNormalsTexture");
            squirtIncrementAmountID = Shader.PropertyToID("_SquirtIncrementAmount");
            mainKernel = shader.FindKernel(Mozzarella.mainKernelName);
        }
    }
    void Awake() {
        // FIXME: Must run with SSAO enabled on URP in order to get the normals texture. Don't know how to check for that without a direct reference. Blegh!
        // First we gotta get our own local copy
        verletProcessor = ComputeShader.Instantiate(verletProcessor);
        shaderProperties = new MozzarellaShaderBlock(verletProcessor);
        pointsBuffer = new ComputeBuffer(numParticles, sizeof(float)*10);
        squirtsBuffer = new ComputeBuffer(squirts.Count, sizeof(float)*7+sizeof(uint)*1);
    }
    void Start() {
        List<Point> startingPoints = new List<Point>();
        for(int i=0;i<numParticles;i++) {
            Vector3 rand = UnityEngine.Random.insideUnitSphere;
            startingPoints.Add(new Point() {
                position = rand,
                prevPosition = rand,
                volume = 0f,
            });
        }
        pointsBuffer.SetData<Point>(startingPoints);
        for(int i=0;i<squirts.Count;i++) {
            squirts[i] = new Squirt(squirts[i], (uint)(i*(numParticles/squirts.Count)));
        }
        squirtsBuffer.SetData<Squirt>(squirts);
        verletProcessor.SetVector(shaderProperties.gravityID, Physics.gravity);
        verletProcessor.SetFloat(shaderProperties.deltaTimeID, Time.fixedDeltaTime);
        verletProcessor.SetFloat(shaderProperties.viscosityID, viscosity);
        verletProcessor.SetInt(shaderProperties.squirtIncrementAmountID, squirtVolume);
    }
    bool ShouldUpdate() {
        foreach(var squirt in squirts) {
            if (squirt.volume > 0f) {
                return true;
            }
        }
        return false;
    }
    void FixedUpdate() {
        if (ShouldUpdate()) {
            timer = particleLifeTime;
        }
        if (timer <= 0f) {
            return;
        }
        timer -= Time.deltaTime;
        if (Shader.GetGlobalTexture(shaderProperties.cameraDepthID) == null) {
            return;
        }
        if (Shader.GetGlobalTexture(shaderProperties.cameraNormalsID) == null) {
            return;
        }
        float volume = Mathf.Clamp01(Mathf.Sin(Time.time*5f));
        verletProcessor.SetBuffer(0, shaderProperties.pointsID, pointsBuffer);
        verletProcessor.SetInt(shaderProperties.numParticlesID, numParticles);
        for(int i=0;i<squirts.Count;i++) {
            uint index = squirts[i].index;
            index += (uint)squirtVolume;
            index = index%(uint)numParticles;
            squirts[i] = new Squirt(squirts[i],index);
        }
        verletProcessor.SetInt(shaderProperties.numSquirtsID, squirts.Count);
        squirtsBuffer.SetData<Squirt>(squirts);
        verletProcessor.SetBuffer(0, shaderProperties.squirtsID, squirtsBuffer);
        verletProcessor.SetMatrix(shaderProperties.worldToCameraID, Camera.main.worldToCameraMatrix);
        verletProcessor.SetMatrix(shaderProperties.cameraToWorldID, Camera.main.cameraToWorldMatrix);
        verletProcessor.SetMatrix(shaderProperties.cameraInverseProjectionID, Matrix4x4.Inverse(Camera.main.projectionMatrix));
        Matrix4x4 matrixVP = Camera.main.projectionMatrix  * Camera.main.worldToCameraMatrix; //multipication order matters
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
        squirtsBuffer.Dispose();
    }
    void OnValidate() {
        if (Application.isPlaying && verletProcessor != null && shaderProperties != null) {
            verletProcessor.SetInt(shaderProperties.squirtIncrementAmountID, squirtVolume);
            verletProcessor.SetFloat(shaderProperties.viscosityID, viscosity);
        }
    }
}
