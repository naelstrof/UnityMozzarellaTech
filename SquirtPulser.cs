using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Mozzarella))]
public class SquirtPulser : MonoBehaviour {
    Mozzarella mozzarella;
    void Start() {
        mozzarella = GetComponent<Mozzarella>();
    }
    void Update() {
        for(int i=0;i<mozzarella.squirts.Count;i++) {
            mozzarella.squirts[i] = new Mozzarella.Squirt(transform.position,
            Vector3.up*0.025f+transform.up*0.1f+UnityEngine.Random.insideUnitSphere*0.1f,
            Mathf.Clamp01(Mathf.Abs(Mathf.Sin(Time.time*2f))-0.2f),
            mozzarella.squirts[i].index);
        }
    }
}
