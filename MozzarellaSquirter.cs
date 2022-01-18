using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Mozzarella))]
public class MozzarellaSquirter : BaseStreamer {
    [Range(0.1f,10f)]
    [SerializeField]
    private float squirtDuration = 0.5f;
    [Range(1,16)]
    [SerializeField]
    private int squirtCount = 1;
    [SerializeField]
    private AnimationCurve volumeCurve;
    [SerializeField]
    private AnimationCurve velocityCurve;
    [SerializeField][Range(0f,1f)]
    private float velocityMultiplier = 0.1f;
    [SerializeField][Range(0f,1f)]
    private float velocityVariance = 0f;

    protected class SquirtStream : BaseStreamer.Stream {
        private float startTime;
        private float duration;
        public SquirtStream( float startTime, float duration ) {
            this.startTime = startTime;
            this.duration = duration;
        }
        public bool IsFinished() {
            return Time.time > startTime + duration;
        }
        public override Mozzarella.Point CreatePoint(BaseStreamer streamer, float time) {
            if (!(streamer is MozzarellaSquirter)) {
                throw new UnityException("Tried to use a squirter stream on a non-squirter behavior.");
            }
            float t = (Time.time-startTime)/duration;
            MozzarellaSquirter squirter = streamer as MozzarellaSquirter;
            Vector3 velocity = Vector3.up*0.025f+squirter.transform.up*squirter.velocityCurve.Evaluate(t)*squirter.velocityMultiplier+UnityEngine.Random.insideUnitSphere*squirter.velocityVariance;
            float volume = squirter.volumeCurve.Evaluate(t);
            return new Mozzarella.Point() {
                position = squirter.transform.position,
                prevPosition = squirter.transform.position - velocity,
                volume = volume,
                registerHitEvent = 1,
            };
        }
    }
    public override void Update() {
        base.Update();
        for(int i=streams.Count-1;i>=0;i--) {
            SquirtStream squirter = streams[i] as SquirtStream;
            if (squirter.IsFinished()) {
                streams.RemoveAt(i);
            }
        }
    }
    public void Squirt() {
        mozzarella.SetVisibleUntil(Time.time + squirtDuration + 5f);
        for(int i=0;i<squirtCount;i++) {
            streams.Add(new SquirtStream(Time.time, squirtDuration));
        }
    }
}
