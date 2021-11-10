using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class Mozzarella : MonoBehaviour {
    [Range(64, 4096)]
    public int numParticles = 512;
    public List<Squirt> squirts;
    private int numParticlesID, pointsID, gravityID, lengthID, depthTextureID, worldToCameraID, cameraInverseProjectionID, cameraVPID, cameraToWorldID, nearClipValueID;
    private int cameraDepthID;
    private int farClipValueID;

    private int numSquirtsID;
    private int squirtsID;
    private int deltaTimeID;
    public Mesh mesh;
    private Material instantiatedMeshMaterial;
    public Material instancedMeshMaterial;
    public ComputeShader verletProcessor;
    private ComputeBuffer points;
    private ComputeBuffer positions;
    private ComputeBuffer squirtsBuffer;

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
        public Squirt(Squirt other, Vector3 newPosition, Vector3 newVelocity) : this() {
            position = newPosition;
            velocity = newVelocity;
            volume = other.volume;
            index = other.index;
        }
        public Squirt(Squirt other, float newVolume) {
            position = other.position;
            velocity = other.velocity;
            volume = newVolume;
            index = other.index;
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

        squirtsID = Shader.PropertyToID("_Squirts");
        numSquirtsID = Shader.PropertyToID("_NumSquirts");
        cameraDepthID = Shader.PropertyToID("_CameraDepthTexture");
        instantiatedMeshMaterial = Material.Instantiate(instancedMeshMaterial);
    }
    void Start() {
        points = new ComputeBuffer(numParticles, sizeof(float)*10);
        squirtsBuffer = new ComputeBuffer(squirts.Count, sizeof(float)*7+sizeof(uint)*1);
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
        for(int i=0;i<squirts.Count;i++) {
            squirts[i] = new Squirt(squirts[i], (uint)(i*(numParticles/squirts.Count)));
        }
        squirtsBuffer.SetData<Squirt>(squirts);
        verletProcessor.SetVector(gravityID, Physics.gravity*0.4f);
        verletProcessor.SetFloat(deltaTimeID, Time.fixedDeltaTime);
        verletProcessor.SetFloat(lengthID, 0.5f*Time.fixedDeltaTime);
        instantiatedMeshMaterial.SetBuffer("_Points", points);
    }
    void FixedUpdate() {
        float volume = Mathf.Clamp01(Mathf.Sin(Time.time*5f));
        verletProcessor.SetBuffer(0, pointsID, points);
        verletProcessor.SetInt(numParticlesID, numParticles);
        for(int i=0;i<squirts.Count;i++) {
            uint index = squirts[i].index;
            index = (++index)%(uint)numParticles;
            squirts[i] = new Squirt(squirts[i],index);
        }
        verletProcessor.SetInt(numSquirtsID, squirts.Count);
        squirtsBuffer.SetData<Squirt>(squirts);
        verletProcessor.SetBuffer(0, squirtsID, squirtsBuffer);
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
        squirtsBuffer.Dispose();
    }
    void Update() {
        // Draw to screen
        Graphics.DrawMeshInstancedProcedural(mesh, 0, instantiatedMeshMaterial, new Bounds(Vector3.zero, Vector3.one*1000f), numParticles);
    }
}
