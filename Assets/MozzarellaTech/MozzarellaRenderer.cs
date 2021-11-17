using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class MozzarellaRenderer : MonoBehaviour {
    private Mozzarella mozzarella;
    [SerializeField]
    private Mesh mesh;
    [SerializeField]
    private Material material;
    private static int pointsID;
    private static int pointsScaleID;
    public void Start() {
        mozzarella = GetComponent<Mozzarella>();
        pointsID = Shader.PropertyToID("_Points");
        pointsScaleID = Shader.PropertyToID("_PointScale");
        material = Material.Instantiate(material);
        material.SetBuffer(pointsID, mozzarella.pointsBuffer);
    }
    public void Update() {
        material.SetFloat(pointsScaleID, mozzarella.pointScale);
        // Draw to screen
        Graphics.DrawMeshInstancedProcedural(mesh, 0, material, new Bounds(Vector3.zero, Vector3.one*1000f), mozzarella.numParticles);
    }
}
