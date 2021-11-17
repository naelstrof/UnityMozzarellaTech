using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;


public class Mozzarella : MonoBehaviour {
    public static string mainKernelName = "PointsMain";
    [Range(64, 16384)]
    public int numParticles = 512;
    [Range(0.01f, 1f)]
    public float pointScale = 0.1f;
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
    private bool dirty = false;

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
    private class MozzarellaShaderBlock {
        public int numParticlesID, pointsID, gravityID,
        lengthID, depthTextureID, worldToCameraID,
        cameraInverseProjectionID, cameraVPID, cameraToWorldID,
        nearClipValueID, cameraDepthID,
        farClipValueID, numSquirtsID, squirtsID,
        deltaTimeID, mainKernel;
        public MozzarellaShaderBlock(ComputeShader shader) {
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
            squirtsID = Shader.PropertyToID("_Squirts");
            numSquirtsID = Shader.PropertyToID("_NumSquirts");
            cameraDepthID = Shader.PropertyToID("_CameraDepthTexture");
            mainKernel = shader.FindKernel(Mozzarella.mainKernelName);
        }
    }
    void Awake() {
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
        verletProcessor.SetVector(shaderProperties.gravityID, Physics.gravity*0.4f);
        verletProcessor.SetFloat(shaderProperties.deltaTimeID, Time.fixedDeltaTime);
        verletProcessor.SetFloat(shaderProperties.lengthID, 0.25f*Time.fixedDeltaTime);
    }
    void FixedUpdate() {
        if (Shader.GetGlobalTexture(shaderProperties.cameraDepthID) == null) {
            return;
        }
        float volume = Mathf.Clamp01(Mathf.Sin(Time.time*5f));
        verletProcessor.SetBuffer(0, shaderProperties.pointsID, pointsBuffer);
        verletProcessor.SetInt(shaderProperties.numParticlesID, numParticles);
        for(int i=0;i<squirts.Count;i++) {
            uint index = squirts[i].index;
            index = (++index)%(uint)numParticles;
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

        // Points update
        verletProcessor.Dispatch(shaderProperties.mainKernel, numParticles, 1, 1);
        dirty = true;
    }
    void Update() {
        if (dirty) {
            onPointsUpdated?.Invoke();
            dirty = false;
        }
    }
    void OnDestroy() {
        pointsBuffer.Dispose();
        squirtsBuffer.Dispose();
    }
}
