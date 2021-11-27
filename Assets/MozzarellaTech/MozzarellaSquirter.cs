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
    [SerializeField][Range(0f,1f)]
    private float velocityMultiplier = 0.1f;
    [SerializeField][Range(0f,1f)]
    private float velocityVariance = 0f;
    Mozzarella mozzarella;
    void Start() {
        currentIndex = 0;
        mozzarella = GetComponent<Mozzarella>();
    }
    IEnumerator Squirt(int i, float duration) {
        float startTime = Time.time;
        while(Time.time < startTime+duration) {
            float t = (Time.time-startTime)/duration;

            float volume = volumeCurve.Evaluate(t);

            mozzarella.squirts[i] = new Mozzarella.Squirt(transform.position,
            Vector3.up*0.025f+transform.up*velocityCurve.Evaluate(t)*velocityMultiplier+UnityEngine.Random.insideUnitSphere*velocityVariance,
            volume,
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
