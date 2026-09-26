using UnityEngine;

// Put on the spotlight above a station (counter, mortar, stove, tray).
// The light over the current step glows brighter and breathes; the others dim.
// This is the main non-text signal for "where do I go next?".
[RequireComponent(typeof(Light))]
public class StationLight : MonoBehaviour
{
    [SerializeField] int[] layers;
    [SerializeField] float activeBoost = 1.8f;
    [SerializeField] float idleDim = 0.3f;
    [SerializeField] float doneDim = 0.6f;
    [SerializeField] float pulseSpeed = 3f;

    Light stationLight;
    float baseIntensity;

    void Awake()
    {
        stationLight = GetComponent<Light>();
        baseIntensity = stationLight.intensity;
    }

    void Update()
    {
        PuzzleManager pm = PuzzleManager.Instance;
        bool active = !pm.AllComplete && System.Array.IndexOf(layers, pm.Completed) >= 0;
        bool done = true;
        foreach (int layer in layers) done &= pm.Completed > layer;

        float target;
        if (active) target = baseIntensity * activeBoost * (0.8f + 0.2f * Mathf.Sin(Time.time * pulseSpeed));
        else if (done) target = baseIntensity * doneDim;
        else target = baseIntensity * idleDim;

        stationLight.intensity = Mathf.Lerp(stationLight.intensity, target, 1f - Mathf.Exp(-6f * Time.deltaTime));
    }
}
