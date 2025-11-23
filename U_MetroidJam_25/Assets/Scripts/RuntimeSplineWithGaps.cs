using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Splines;
using Unity.Mathematics;

[RequireComponent(typeof(SplineContainer))]
public class RuntimeRailSplineWithGaps : MonoBehaviour
{
    [Header("References")]
    public Transform player;
    public SplineContainer container;

    [Header("Spline Window")]
    public float knotSpacing = 2f;
    public float lookAheadDistance = 60f;
    public float keepBehindDistance = 20f;

    [Header("Height")]
    public float baseHeight = 0f;
    public float amplitude = 4f;
    public float noiseScale = 0.05f;
    [Range(0f, 1f)] public float heightLerp = 0.25f;

    [Header("Gaps")]
    public float gapChancePerKnot = 0.08f;  // chance to START a gap at any knot
    public float minGapLength = 3f;
    public float maxGapLength = 8f;

    [Header("Randomness")]
    public int seed = 12345;
    public bool regenerateOnStart = true;

    public struct Gap
    {
        public float startX;
        public float endX;
        public Gap(float s, float e) { startX = s; endX = e; }
        public bool Contains(float x) => x >= startX && x <= endX;
    }

    public readonly List<Gap> gaps = new List<Gap>();

    Spline spline;
    //readonly Queue<float> knotWorldXs = new Queue<float>();
    float nextSpawnX;
    float lastHeight;

    // Track if we are currently skipping knots for a gap
    float currentGapEndX = float.NegativeInfinity;
    System.Random rng;

    void Reset() => container = GetComponent<SplineContainer>();

    void Awake()
    {
        if (container == null) container = GetComponent<SplineContainer>();
        spline = container.Spline;

        rng = new System.Random(seed);

        if (regenerateOnStart && player != null)
            RegenerateFromPlayer();
    }

    [ContextMenu("Regenerate From Player")]
    public void RegenerateFromPlayer()
    {
        spline.Clear();
        //knotWorldXs.Clear();
        gaps.Clear();

        float startX = player.position.x - keepBehindDistance;
        nextSpawnX = startX;
        lastHeight = SampleHeight(nextSpawnX);

        currentGapEndX = float.NegativeInfinity;

        int knotCount = Mathf.CeilToInt((keepBehindDistance + lookAheadDistance) / knotSpacing) + 2;
        for (int i = 0; i < knotCount; i++)
            AddOrSkipKnot();
    }

    void Update()
    {
        if (player == null) return;

        float playerX = player.position.x;

        float targetAheadX = playerX + lookAheadDistance;
        while (nextSpawnX < targetAheadX)
            AddOrSkipKnot();

        // --- NEW DESPAWN LOGIC ---
        float minKeepX = playerX - keepBehindDistance;

        while (spline.Count > 0)
        {
            // Get world X of the first REAL knot
            float3 firstLocal = spline[0].Position;
            Vector3 firstWorld = container.transform.TransformPoint((Vector3)firstLocal);

            if (firstWorld.x < minKeepX)
                spline.RemoveAt(0);
            else
                break;
        }
    }

    void AddOrSkipKnot()
    {
        float x = nextSpawnX;

        // If we're inside an active gap, don't add knots (visual hole)
        if (x < currentGapEndX)
        {
            nextSpawnX += knotSpacing;
            return;
        }

        // Possibly start a new gap
        if (rng.NextDouble() < gapChancePerKnot)
        {
            float gapLen = Mathf.Lerp(minGapLength, maxGapLength, (float)rng.NextDouble());
            float gapStart = x;
            float gapEnd = x + gapLen;

            gaps.Add(new Gap(gapStart, gapEnd));
            currentGapEndX = gapEnd;

            // Skip this knot too so the gap begins cleanly
            nextSpawnX += knotSpacing;
            return;
        }

        // Normal knot add
        float rawHeight = SampleHeight(x);
        float h = Mathf.Lerp(lastHeight, rawHeight, 1f - heightLerp);
        lastHeight = h;

        Vector3 worldPos = new Vector3(x, h, 0f);
        float3 localPos = (float3)container.transform.InverseTransformPoint(worldPos);

        spline.Add(new BezierKnot(localPos));
        //knotWorldXs.Enqueue(x);

        nextSpawnX += knotSpacing;
    }

    // void RemoveOldestKnot()
    // {
    //     if (spline.Count == 0 || knotWorldXs.Count == 0) return;
    //     spline.RemoveAt(0);
    //     knotWorldXs.Dequeue();
    // }

    float SampleHeight(float worldX)
    {
        float nx = (worldX + seed * 10.123f) * noiseScale;
        float noise = Mathf.PerlinNoise(nx, seed * 0.001f);
        return baseHeight + (noise - 0.5f) * 2f * amplitude;
    }

    // --------- Helper the cart will call ----------
    public bool IsGapAtX(float worldX)
    {
        // gaps list is small, linear scan is fine
        for (int i = 0; i < gaps.Count; i++)
            if (gaps[i].Contains(worldX))
                return true;
        return false;
    }
}