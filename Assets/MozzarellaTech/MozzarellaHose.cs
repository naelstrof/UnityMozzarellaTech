using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Mozzarella))]
public class MozzarellaHose : BaseStreamer {
    [SerializeField][Range(0f,1f)]
    private float startVelocity = 0.1f;
    [SerializeField][Range(0f,1f)]
    private float velocityVariance;
    [SerializeField][Range(1,16)]
    private int streamCount = 4;
    protected class HoseStream : BaseStreamer.Stream {
        public float offset;
        public HoseStream(float offset) {
            this.offset = offset;
        }
        public override Mozzarella.Point CreatePoint(BaseStreamer streamer, float time) {
            if (!(streamer is MozzarellaHose)) {
                throw new UnityException("Tried to use a hose stream on a non-hose behavior.");
            }
            MozzarellaHose hose = streamer as MozzarellaHose;
            Vector3 velocity = Vector3.up*0.025f+hose.transform.up*hose.startVelocity+UnityEngine.Random.insideUnitSphere*hose.velocityVariance;
            float volume = Mathf.Clamp01(Mathf.Abs(Mathf.Sin((time+offset)*2f)));
            if (volume < 0.6f) {
                volume = 0f;
            } else {
                volume = Mathf.Lerp(volume,1f,0.5f);
            }
            return new Mozzarella.Point() {
                position = hose.transform.position,
                prevPosition = hose.transform.position - velocity,
                volume = volume
            };
        }
    }
    public override void Start() {
        base.Start();
        for(int i=0;i<streamCount;i++) {
            streams.Add(new HoseStream(i*0.5f));
        }
    }
    public override void Update() {
        base.Update();
    }
    public void SetStreamCount(int newStreamCount) {
        streamCount = newStreamCount;
        streams.Clear();
        for(int i=0;i<streamCount;i++) {
            streams.Add(new HoseStream(i*0.5f));
        }
    }
    void OnValidate() {
        if (Application.isPlaying && mozzarella != null && streams != null && streams.Count != streamCount) {
            SetStreamCount(streamCount);
        }
    }
}
