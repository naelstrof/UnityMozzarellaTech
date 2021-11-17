using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Mozzarella))]
public class MozzarellaHose : MonoBehaviour {
    Mozzarella mozzarella;
    void Start() {
        mozzarella = GetComponent<Mozzarella>();
    }
    void Update() {
        for(int i=0;i<mozzarella.squirts.Count;i++) {
            float volume = Mathf.Clamp01(Mathf.Abs(Mathf.Sin(Time.time*2f+i*5f))-0.2f);
            if (volume < 0.3f) {
                volume = 0f;
            } else {
                volume = Mathf.Lerp(volume, 1f, 0.25f);
            }
            mozzarella.squirts[i] = new Mozzarella.Squirt(transform.position,
            Vector3.up*0.025f+transform.up*0.1f+UnityEngine.Random.insideUnitSphere*0.1f,
            volume,
            mozzarella.squirts[i].index);
        }
    }
}
