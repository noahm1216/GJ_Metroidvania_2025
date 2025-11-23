using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

[RequireComponent(typeof(SplineContainer))]
public class RuntimeRailSplineGenerator : MonoBehaviour
{
    [Header("References")]
    public Transform player;
    public SplineContainer container;

    [Header("Window / Knot Spacing")]
    public float knotSpacing = 2f;
    public float lookAheadDistance = 60f;
    public float keepBehindDistance = 20f;

    [Header("Height / Curvature")]
    public float baseHeight = 0f;
    public float amplitude = 4f;
    public float noiseScale = 0.05f;
    [Range(0f, 1f)] public float heightLerp = 0.25f;

    [Header("Optional Sine Layer")]
    public bool useSine = false;
    public float sineAmplitude = 2f;
    public float sineWavelength = 30f;

    [Header("Randomness")]
    public int seed = 12345;
    public bool regenerateOnStart = true;

    private Spline spline;
    private readonly Queue<float> knotWorldXs = new Queue<float>();
    private float nextSpawnX;
    private float lastHeight;

    private bool isRegenerating;

    void Awake()
    {
        if (container == null) container = GetComponent<SplineContainer>();

        // Grab the spline reference once. (SplineContainer.Spline is the first spline.)
        spline = container.Spline;

        if (regenerateOnStart && player != null)
            RegenerateFromPlayer();
    }

    void Update()
    {
        if (player == null || spline == null) return;

        float playerX = player.position.x;

        // Add knots ahead
        float targetAheadX = playerX + lookAheadDistance;
        while (nextSpawnX < targetAheadX)
            AddKnot();

        // Remove knots behind
        float minKeepX = playerX - keepBehindDistance;
        while (knotWorldXs.Count > 0 && knotWorldXs.Peek() < minKeepX)
            RemoveOldestKnot();
    }

    [ContextMenu("Regenerate From Player")]
    public void RegenerateFromPlayer()
    {
        if (isRegenerating || player == null || spline == null) return;
        isRegenerating = true;

        spline.Clear();
        knotWorldXs.Clear();

        float startX = player.position.x - keepBehindDistance;
        nextSpawnX = startX;

        lastHeight = SampleHeight(nextSpawnX);

        int knotCount = Mathf.CeilToInt((keepBehindDistance + lookAheadDistance) / knotSpacing) + 2;
        for (int i = 0; i < knotCount; i++)
            AddKnot();

        isRegenerating = false;
    }

    private void AddKnot()
    {
        float x = nextSpawnX;
        float rawHeight = SampleHeight(x);

        float h = Mathf.Lerp(lastHeight, rawHeight, 1f - heightLerp);
        lastHeight = h;

        Vector3 worldPos = new Vector3(x, h, 0f);
        float3 localPos = (float3)container.transform.InverseTransformPoint(worldPos);

        var knot = new BezierKnot(localPos)
        {
            Rotation = quaternion.identity
        };

        spline.Add(knot);
        knotWorldXs.Enqueue(x);

        nextSpawnX += knotSpacing;
    }

    private void RemoveOldestKnot()
    {
        if (spline.Count == 0 || knotWorldXs.Count == 0) return;
        spline.RemoveAt(0);
        knotWorldXs.Dequeue();
    }

    private float SampleHeight(float worldX)
    {
        float nx = (worldX + seed * 10.123f) * noiseScale;
        float noise = Mathf.PerlinNoise(nx, seed * 0.001f); // 0..1

        float height = baseHeight + (noise - 0.5f) * 2f * amplitude;

        if (useSine && sineWavelength > 0.001f)
        {
            float sine = Mathf.Sin((worldX / sineWavelength) * Mathf.PI * 2f);
            height += sine * sineAmplitude;
        }

        return height;
    }
}
