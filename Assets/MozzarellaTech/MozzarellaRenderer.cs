using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class MozzarellaRenderer : MonoBehaviour {
    private Mozzarella mozzarella;
    [SerializeField]
    private Mesh mesh;
    [SerializeField]
    public Material material;
    [Range(0.001f, 1f)]
    private float pointRadius = 0.1f;
    private static int pointsID;
    private static int pointsScaleID;
    public void SetPointRadius(float radius) {
        pointRadius = radius;
        material.SetFloat(pointsScaleID, pointRadius);
    }
    void Awake() {
        material = Material.Instantiate(material);
    }
    void Start() {
        mozzarella = GetComponent<Mozzarella>();
        pointsID = Shader.PropertyToID("_Points");
        pointsScaleID = Shader.PropertyToID("_PointScale");
        material.SetBuffer(pointsID, mozzarella.pointsBuffer);
        material.SetFloat(pointsScaleID, pointRadius);
    }
    void Update() {
        // Draw to screen
        Graphics.DrawMeshInstancedProcedural(mesh, 0, material, new Bounds(Vector3.zero, Vector3.one*1000f), mozzarella.numParticles);
    }
}
