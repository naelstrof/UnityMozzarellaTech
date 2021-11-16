using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Mozzarella))]
public class MozzarellaSquirter : MonoBehaviour {
    [Range(0.1f,10f)]
    [SerializeField]
    private float squirtDuration = 0.5f;
    private int currentIndex;
    [SerializeField]
    private AnimationCurve volumeCurve;
    [SerializeField]
    private AnimationCurve velocityCurve;
    Mozzarella mozzarella;
    void Start() {
        currentIndex = 0;
        mozzarella = GetComponent<Mozzarella>();
    }
    IEnumerator Squirt(int i, float duration) {
        float startTime = Time.time;
        while(Time.time < startTime+duration) {
            float t = (Time.time-startTime)/duration;
            mozzarella.squirts[i] = new Mozzarella.Squirt(transform.position,
            Vector3.up*0.025f+transform.up*velocityCurve.Evaluate(t)*0.1f+UnityEngine.Random.insideUnitSphere*0.1f,
            volumeCurve.Evaluate(t),
            mozzarella.squirts[i].index);
            yield return null;
        }
        mozzarella.squirts[i] = new Mozzarella.Squirt(transform.position, Vector3.zero, 0f, mozzarella.squirts[i].index);
    }
    public void Squirt() {
        StartCoroutine(Squirt(currentIndex, squirtDuration));
        currentIndex = (++currentIndex)%(mozzarella.squirts.Count-1);
    }
}
