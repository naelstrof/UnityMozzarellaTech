using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MozzarellaHitEventListener))]
public class MozzarellaDecals : MonoBehaviour {
    [SerializeField]
    [Range(0.001f,1f)]
    private float decalSize = 0.1f;
    [SerializeField]
    private LayerMask hitMask;
    [SerializeField]
    private Material projector;
    private Collider[] colliders;
    void Awake() {
        colliders = new Collider[32];
    }
    void Start() {
        GetComponent<MozzarellaHitEventListener>().OnDepthBufferHit += OnDepthBufferHit;
    }
    void OnDepthBufferHit(List<MozzarellaHitEventListener.HitEvent> hitEvents) {
        foreach(var hitEvent in hitEvents) {
            DrawDecal(hitEvent);
        }
    }
    void DrawDecal(MozzarellaHitEventListener.HitEvent hitEvent) {
        float size = hitEvent.volume*decalSize;
        SkinnedMeshDecals.PaintDecal.RenderDecalInSphere(hitEvent.position, size, projector, Quaternion.identity, hitMask);
    }
}
