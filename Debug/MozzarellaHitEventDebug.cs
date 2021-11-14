using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Mozzarella))]
public class MozzarellaHitEventDebug : MonoBehaviour {
    Mozzarella mozzarella;
    void Start() {
        mozzarella = GetComponent<Mozzarella>();
        mozzarella.OnDepthBufferHit += OnDepthBufferHit;
    }
    void OnDepthBufferHit(Mozzarella.HitEvent hitEvent) {
        Debug.DrawLine(hitEvent.position, hitEvent.position + Vector3.up * hitEvent.volume, Color.white, Time.fixedDeltaTime);
    }
}
