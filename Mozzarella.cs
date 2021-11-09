using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class Mozzarella : MonoBehaviour {
    [Range(64, 512)]
    public int numParticles = 512;
    private int numParticlesID;
    private int pointsID;
    private int gravityID;
    private int lengthID;

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

        squirtVolumeID = Shader.PropertyToID("_SquirtVolume");
        squirtVelocityID = Shader.PropertyToID("_SquirtVelocity");
        squirtPositionID = Shader.PropertyToID("_SquirtPosition");
        squirtIndexID = Shader.PropertyToID("_SquirtIndex");
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
        Vector3 vel = (transform.up+Mathf.Sin(Time.time)*Vector3.right).normalized;

        for(int i=0;i<2;i++) {
            verletProcessor.SetBuffer(i, pointsID, points);
        }
        verletProcessor.SetInt(numParticlesID, numParticles);
        verletProcessor.SetVector(squirtPositionID, pos);
        verletProcessor.SetFloat(squirtVolumeID, volume);
        verletProcessor.SetInt(squirtIndexID, squirtIndex);
        squirtIndex = (++squirtIndex)%numParticles;
        verletProcessor.SetVector(squirtVelocityID, vel);

        // Point update
        verletProcessor.Dispatch(0, numParticles, 1, 1);
        // Stick update
        verletProcessor.Dispatch(1, numParticles, 1, 1);
    }
    void OnDestroy() {
        points.Dispose();
    }
    void Update() {
        // Draw to screen
        Graphics.DrawMeshInstancedProcedural(mesh, 0, instantiatedMeshMaterial, new Bounds(Vector3.zero, Vector3.one*1000f), numParticles);
    }
}
