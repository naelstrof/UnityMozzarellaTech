using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Mozzarella))]
public class BaseStreamer : MonoBehaviour {
    [SerializeField][Range(1,1024)]
    protected int particlesPerSecondPerStream = 60;
    protected Mozzarella mozzarella;
    private int currentIndex;
    private float lastTime;
    protected List<Stream> streams;
    protected class Stream {
        public Stream() {
            points = new List<Mozzarella.Point>();
        }
        public List<Mozzarella.Point> points;
        public virtual Mozzarella.Point CreatePoint(BaseStreamer streamer, float time) {
            return new Mozzarella.Point() {
                position = streamer.transform.position,
                prevPosition = streamer.transform.position-Vector3.up,
                volume = 1f,
            };
        }
    }
    public virtual void Awake() {
        mozzarella = GetComponent<Mozzarella>();
        streams = new List<Stream>();
    }
    public virtual void Start() {
        lastTime = Time.time;
    }
    public virtual void Update() {
        float particleCount = (Time.time-lastTime)*particlesPerSecondPerStream;
        int neededParticles = Mathf.Min(Mathf.FloorToInt(particleCount), Mathf.FloorToInt(mozzarella.numParticles/Mathf.Max(streams.Count,1))-streams.Count);
        if (neededParticles < 1 || streams.Count == 0) {
            return;
        }
        for(int i=0;i<streams.Count;i++) {
            int index = currentIndex + (mozzarella.numParticles/streams.Count)*i;
            index = index % mozzarella.numParticles;
            Stream stream = streams[i];
            stream.points.Clear();
            for(int j=0;j<neededParticles;j++) {
                float timeChunk = (Time.time-lastTime)/(float)neededParticles;
                stream.points.Add(stream.CreatePoint(this, Time.time+timeChunk*(float)j));
            }
            if (index+stream.points.Count > mozzarella.numParticles) {
                int leftHalfCount = mozzarella.numParticles-index;
                mozzarella.pointsBuffer.SetData<Mozzarella.Point>(stream.points, 0, index, leftHalfCount);
                mozzarella.pointsBuffer.SetData<Mozzarella.Point>(stream.points, leftHalfCount, 0, stream.points.Count-leftHalfCount);
            } else {
                mozzarella.pointsBuffer.SetData<Mozzarella.Point>(stream.points, 0, index, stream.points.Count);
            }
        }
        currentIndex += neededParticles;
        lastTime = Time.time;
    }
}
