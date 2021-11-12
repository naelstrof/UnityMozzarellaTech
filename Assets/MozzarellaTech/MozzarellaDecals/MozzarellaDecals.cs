using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Mozzarella))]
public class MozzarellaDecals : MonoBehaviour {
    [SerializeField]
    private LayerMask hitMask;
    [SerializeField]
    private Material projector;
    private Collider[] colliders;
    private Mozzarella mozzarella;
    void Awake() {
        colliders = new Collider[32];
    }
    void Start() {
        mozzarella = GetComponent<Mozzarella>();
        mozzarella.OnDepthBufferHit += OnDepthBufferHit;
    }
    void OnDepthBufferHit(Mozzarella.HitEvent hitEvent) {
        float size = hitEvent.volume*0.5f;
        int hits = Physics.OverlapSphereNonAlloc(hitEvent.position, hitEvent.volume*0.25f, colliders, hitMask, QueryTriggerInteraction.UseGlobal );
        for (int i=0;i<hits;i++) {
            Collider c = colliders[i];
            Renderer renderer = c.GetComponent<Renderer>();
            if (renderer == null) {
                continue;
            }
            PaintDecal.instance.RenderDecal(renderer, projector, hitEvent.position - Vector3.forward*size*0.5f, Quaternion.identity, Vector2.one*size*0.5f, size);
        }
    }
}
