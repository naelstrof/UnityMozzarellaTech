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
        int hits = Physics.OverlapSphereNonAlloc(hitEvent.position, size, colliders, hitMask, QueryTriggerInteraction.UseGlobal );
        for (int i=0;i<hits;i++) {
            Collider c = colliders[i];
            Renderer renderer = c.GetComponent<Renderer>();
            if (renderer == null) {
                continue;
            }
            SkinnedMeshDecals.PaintDecal.RenderDecal(renderer, projector, hitEvent.position - Vector3.forward*size*0.5f, Quaternion.identity, Vector2.one*size*0.5f, size);
        }
    }
}
