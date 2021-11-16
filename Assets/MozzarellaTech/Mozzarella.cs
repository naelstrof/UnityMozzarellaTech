using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class Mozzarella : MonoBehaviour {
    [Range(64, 16384)]
    public int numParticles = 512;
    [Range(4, 16)]
    public int hitEventCount = 8;
    public List<Squirt> squirts;
    private int numParticlesID, pointsID, gravityID, lengthID, depthTextureID, worldToCameraID, cameraInverseProjectionID, cameraVPID, cameraToWorldID, nearClipValueID;
    private int cameraDepthID;
    private int numHitEventsID;
    private int farClipValueID;

    private int numSquirtsID;
    private int squirtsID;
    private int hitEventsID;
    private int deltaTimeID;
    public Mesh mesh;
    private Material instantiatedMeshMaterial;
    public Material instancedMeshMaterial;
    public ComputeShader verletProcessor;
    private ComputeBuffer pointsBuffer;
    private ComputeBuffer hitEventsBuffer;
    private ComputeBuffer positionsBuffer;
    private ComputeBuffer squirtsBuffer;
    private List<HitEvent> hitEvents;
    public delegate void HitEventAction(HitEvent hitEvent);
    public event HitEventAction OnDepthBufferHit;
    public float pointSize {
        set {
            instantiatedMeshMaterial?.SetFloat("_PointScale", value);
        }
    }

    private struct Point {
        public Vector3 position;
        public Vector3 prevPosition;
        public Vector3 savedPosition;
        public float volume;
    }
    public struct HitEvent {
        public Vector3 position;
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
        hitEvents = new List<HitEvent>();
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
        hitEventsID = Shader.PropertyToID("_HitEvents");

        numHitEventsID = Shader.PropertyToID("_NumHitEvents");
        squirtsID = Shader.PropertyToID("_Squirts");
        numSquirtsID = Shader.PropertyToID("_NumSquirts");
        cameraDepthID = Shader.PropertyToID("_CameraDepthTexture");
        instantiatedMeshMaterial = Material.Instantiate(instancedMeshMaterial);
    }
    void Start() {
        pointsBuffer = new ComputeBuffer(numParticles, sizeof(float)*10);
        hitEventsBuffer = new ComputeBuffer(hitEventCount, sizeof(float)*4);
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
        pointsBuffer.SetData<Point>(startingPoints);
        for(int i=0;i<squirts.Count;i++) {
            squirts[i] = new Squirt(squirts[i], (uint)(i*(numParticles/squirts.Count)));
        }
        squirtsBuffer.SetData<Squirt>(squirts);
        for(int i=0;i<hitEventCount;i++) {
            hitEvents.Add(new HitEvent() {position = Vector3.zero, volume = 0f});
        }
        hitEventsBuffer.SetData<HitEvent>(hitEvents);
        verletProcessor.SetInt(numHitEventsID, hitEvents.Count);
        verletProcessor.SetVector(gravityID, Physics.gravity*0.4f);
        verletProcessor.SetFloat(deltaTimeID, Time.fixedDeltaTime);
        verletProcessor.SetFloat(lengthID, 0.25f*Time.fixedDeltaTime);
        instantiatedMeshMaterial.SetBuffer("_Points", pointsBuffer);
    }
    void FixedUpdate() {
        if (Shader.GetGlobalTexture(cameraDepthID) == null) {
            return;
        }
        float volume = Mathf.Clamp01(Mathf.Sin(Time.time*5f));
        verletProcessor.SetBuffer(0, pointsID, pointsBuffer);
        verletProcessor.SetInt(numParticlesID, numParticles);
        for(int i=0;i<squirts.Count;i++) {
            uint index = squirts[i].index;
            index = (++index)%(uint)numParticles;
            squirts[i] = new Squirt(squirts[i],index);
        }
        verletProcessor.SetInt(numSquirtsID, squirts.Count);
        squirtsBuffer.SetData<Squirt>(squirts);
        verletProcessor.SetBuffer(0, hitEventsID, hitEventsBuffer);
        verletProcessor.SetBuffer(0, squirtsID, squirtsBuffer);
        verletProcessor.SetMatrix(worldToCameraID, Camera.main.worldToCameraMatrix);
        verletProcessor.SetMatrix(cameraToWorldID, Camera.main.cameraToWorldMatrix);
        verletProcessor.SetMatrix(cameraInverseProjectionID, Matrix4x4.Inverse(Camera.main.projectionMatrix));
        Matrix4x4 matrixVP = Camera.main.projectionMatrix  * Camera.main.worldToCameraMatrix; //multipication order matters
        verletProcessor.SetMatrix(cameraVPID, matrixVP);

        verletProcessor.SetFloat(nearClipValueID, Camera.main.nearClipPlane);
        verletProcessor.SetFloat(farClipValueID, Camera.main.farClipPlane);
        verletProcessor.SetTextureFromGlobal(0, depthTextureID, cameraDepthID);

        // Points update
        verletProcessor.Dispatch(0, numParticles, 1, 1);
        AsyncGPUReadback.Request(hitEventsBuffer, sizeof(float)*4*hitEventCount, 0, GetHitEvents);
    }
    void GetHitEvents(AsyncGPUReadbackRequest request) {
        if (!Application.isPlaying) {
            return;
        }
        foreach(var hitEvent in request.GetData<HitEvent>()) {
            if (hitEvent.volume != 0f) {
                OnDepthBufferHit?.Invoke(hitEvent);
            }
        }
        // Reset them!
        hitEventsBuffer.SetData<HitEvent>(hitEvents);
    }
    void OnDestroy() {
        pointsBuffer.Dispose();
        squirtsBuffer.Dispose();
        hitEventsBuffer.Dispose();
    }
    void Update() {
        // Draw to screen
        Graphics.DrawMeshInstancedProcedural(mesh, 0, instantiatedMeshMaterial, new Bounds(Vector3.zero, Vector3.one*1000f), numParticles);
    }
}
