using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class MozzarellaHitEventListener : MonoBehaviour {
    private Mozzarella mozzarella;
    [Range(2, 32)]
    public int hitEventCount = 8;
    private ComputeBuffer hitEventsBuffer;
    private HitEvent[] hitEventsGet;
    private static int hitEventsID;
    private static int clearHitEventKernel;
    private static int hitEventCountID;
    [SerializeField]
    [Tooltip("Async callbacks take more memory, but lead to far smoother framerates.")]
    private bool useAsyncCallbacks = false;
    public struct HitEvent {
        public Vector3 position;
        public float volume;
    }
    public delegate void HitEventAction(List<HitEvent> hitEvent);
    private List<HitEvent> hitEvents;
    public event HitEventAction OnDepthBufferHit;
    // Start is called before the first frame update
    void Awake() {
        hitEvents = new List<HitEvent>();
        hitEventsID = Shader.PropertyToID("_HitEvents");
        hitEventCountID = Shader.PropertyToID("_NumHitEvents");
    }
    void Start() {
        mozzarella = GetComponent<Mozzarella>();
        mozzarella.verletProcessor.EnableKeyword("HIT_EVENTS");
        hitEventsBuffer = new ComputeBuffer(hitEventCount, sizeof(float)*4);
        hitEventsGet = new HitEvent[hitEventCount];
        for(int i=0;i<hitEventCount;i++) {
            hitEvents.Add(new HitEvent(){position = Vector3.zero, volume = 0f});
        }
        hitEventsBuffer.SetData<HitEvent>(hitEvents);
        mozzarella.verletProcessor.SetInt(hitEventCountID, hitEventCount);
        mozzarella.verletProcessor.SetBuffer(mozzarella.verletProcessor.FindKernel(Mozzarella.mainKernelName), hitEventsID, hitEventsBuffer);
        clearHitEventKernel = mozzarella.verletProcessor.FindKernel("ClearHitEvents");
        mozzarella.verletProcessor.SetBuffer(clearHitEventKernel, hitEventsID, hitEventsBuffer);
        mozzarella.onPointsUpdated += OnPointsUpdated;
    }
    void OnPointsUpdated() {
        if (SystemInfo.supportsAsyncGPUReadback && useAsyncCallbacks) {
            AsyncGPUReadback.Request(hitEventsBuffer, sizeof(float)*4*hitEventCount, 0, GetHitEvents);
        } else {
            hitEventsBuffer.GetData(hitEventsGet);
            hitEvents.Clear();
            foreach(var hitEvent in hitEventsGet) {
                if (hitEvent.volume != 0f) {
                    hitEvents.Add(hitEvent);
                }
            }
            OnDepthBufferHit?.Invoke(hitEvents);
            mozzarella.verletProcessor.Dispatch(clearHitEventKernel, hitEventCount, 1, 1);
        }
    }
    void GetHitEvents(AsyncGPUReadbackRequest request) {
        if (!Application.isPlaying) {
            return;
        }
        hitEvents.Clear();
        foreach(var hitEvent in request.GetData<HitEvent>()) {
            if (hitEvent.volume != 0f) {
                hitEvents.Add(hitEvent);
            }
        }
        OnDepthBufferHit?.Invoke(hitEvents);
        mozzarella.verletProcessor.Dispatch(clearHitEventKernel, hitEventCount, 1, 1);
    }
    void OnDestroy() {
        hitEventsBuffer.Dispose();
        if (mozzarella != null) {
            mozzarella.verletProcessor.DisableKeyword("HIT_EVENTS");
            mozzarella.onPointsUpdated -= OnPointsUpdated;
        }
    }
}
