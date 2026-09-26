using UnityEngine;

public class StationLight : MonoBehaviour
{
    public int[] layers;
    public float activeBoost = 1.8f;
    public float idleDim = 0.3f;
    public float doneDim = 0.6f;
    public float pulseSpeed = 3f;

    Light stationLight;
    float startIntensity;

    void Start()
    {
        stationLight = GetComponent<Light>();
        startIntensity = stationLight.intensity;
    }

    void Update()
    {
        int completed = PuzzleManager.Instance.completed;
        bool isCurrent = false;
        bool allDone = true;

        foreach (int layer in layers)
        {
            if (layer == completed)
            {
                isCurrent = true;
            }
            if (layer >= completed)
            {
                allDone = false;
            }
        }

        if (PuzzleManager.Instance.AllDone())
        {
            isCurrent = false;
        }

        float target = startIntensity * idleDim;
        if (isCurrent)
        {
            float pulse = 0.8f + 0.2f * Mathf.Sin(Time.time * pulseSpeed);
            target = startIntensity * activeBoost * pulse;
        }
        else if (allDone)
        {
            target = startIntensity * doneDim;
        }

        stationLight.intensity = Mathf.Lerp(stationLight.intensity, target, Time.deltaTime * 6f);
    }
}
