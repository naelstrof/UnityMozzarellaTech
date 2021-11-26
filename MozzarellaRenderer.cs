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
    private static int pointsID;
    private static int pointsScaleID;
    void Awake() {
        material = Material.Instantiate(material);
    }
    void Start() {
        mozzarella = GetComponent<Mozzarella>();
        pointsID = Shader.PropertyToID("_Points");
        pointsScaleID = Shader.PropertyToID("_PointScale");
        material.SetBuffer(pointsID, mozzarella.pointsBuffer);
    }
    void Update() {
        material.SetFloat(pointsScaleID, mozzarella.pointScale);
        // Draw to screen
        Graphics.DrawMeshInstancedProcedural(mesh, 0, material, new Bounds(Vector3.zero, Vector3.one*1000f), mozzarella.numParticles);
    }
}
