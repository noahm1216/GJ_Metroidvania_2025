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

    [Header("Gaps")]
    [Tooltip("Random distance of rail between gaps (world units).")]
    public Vector2 railLengthRange = new Vector2(40f, 80f);

    [Tooltip("Random length of a gap (world units).")]
    public Vector2 gapLengthRange = new Vector2(8f, 16f);

    private readonly List<Vector2> gaps = new List<Vector2>();
    public IReadOnlyList<Vector2> Gaps => gaps;

    public SplineInstantiate splineInstantiator;

    // Represents one continuous rail section (one spline) in the container
    private class RailChunk
    {
        public Spline spline;
        public float minX = float.PositiveInfinity;
        public float maxX = float.NegativeInfinity;
    }

    private readonly List<RailChunk> chunks = new List<RailChunk>();
    private RailChunk currentChunk;

    private float nextSpawnX;
    private float lastHeight;
    private bool isRegenerating;

    // Gap state
    private bool inGap;
    private float gapEndX;
    private float nextGapStartX;
    private System.Random rng;

    void Awake()
    {
        if (container == null) container = GetComponent<SplineContainer>();
        rng = new System.Random(seed);

        if (regenerateOnStart && player != null)
            RegenerateFromPlayer();
    }

    void Update()
    {
        if (player == null || container == null) return;

        float playerX = player.position.x;

        // 👉 Extend how far ahead we generate by the max possible gap length
        float maxGapLength = Mathf.Max(gapLengthRange.x, gapLengthRange.y);
        float targetAheadX = playerX + lookAheadDistance + maxGapLength;

        // Add knots ahead (pushing through gaps + beyond)
        while (nextSpawnX < targetAheadX)
            AddKnot();

        // Remove whole chunks (splines) that are fully behind the keep distance
        float minKeepX = playerX - keepBehindDistance;
        RemoveChunksBehind(minKeepX);
    }

    [ContextMenu("Regenerate From Player")]
    public void RegenerateFromPlayer()
    {
        if (isRegenerating || player == null || container == null) return;
        isRegenerating = true;

        // Clear all splines in the container
        container.Splines = System.Array.Empty<Spline>();

        chunks.Clear();
        currentChunk = null;

        float startX = player.position.x - keepBehindDistance;
        nextSpawnX = startX;

        lastHeight = SampleHeight(nextSpawnX);

        // Reset gap state
        inGap = false;
        ScheduleNextGap(startX);

        // Make the first rail chunk
        CreateNewChunk();

        // 👉 Use the same "ahead + max gap" logic for initial fill
        float maxGapLength = Mathf.Max(gapLengthRange.x, gapLengthRange.y);
        float initialAheadX = player.position.x + lookAheadDistance + maxGapLength;

        while (nextSpawnX < initialAheadX)
            AddKnot();

        isRegenerating = false;
    }

    private void AddKnot()
    {
        float x = nextSpawnX;

        // ----- GAP LOGIC -----

        // If we're not in a gap yet and we've reached the planned gap start, enter a gap.
        if (!inGap && x >= nextGapStartX)
        {
            inGap = true;
            float gapLength = RandomRange(gapLengthRange);
            gapEndX = x + gapLength;

            // (optional) record gap range if you ever want it
            gaps.Add(new Vector2(x, gapEndX));
        }

        // While in a gap, skip adding knots until we move past gapEndX.
        if (inGap)
        {
            if (x < gapEndX)
            {
                // Still inside gap: advance X but don't create a knot or spline.
                nextSpawnX += knotSpacing;
                return;
            }
            else
            {
                // Just exited the gap.
                inGap = false;
                ScheduleNextGap(x);

                // Start a new spline for the next rail chunk
                CreateNewChunk();

                // Reset smoothing baseline so the slope isn't insane
                lastHeight = SampleHeight(x);
            }
        }

        // Ensure we have a current chunk (spline) to write into
        if (currentChunk == null)
            CreateNewChunk();

        // ----- NORMAL KNOT CREATION -----

        float rawHeight = SampleHeight(x);
        float h = Mathf.Lerp(lastHeight, rawHeight, 1f - heightLerp);
        lastHeight = h;

        Vector3 worldPos = new Vector3(x, h, 0f);
        float3 localPos = (float3)container.transform.InverseTransformPoint(worldPos);

        var knot = new BezierKnot(localPos)
        {
            Rotation = quaternion.identity
        };

        currentChunk.spline.Add(knot);
        
        // 👉 NEW: give the *first* knot in this chunk a proper tangent
        if (currentChunk.spline.Count == 1)
        {
            // Predict where the *next* knot will roughly be
            float xNext = x + knotSpacing;
            float hNext = SampleHeight(xNext);
            Vector3 worldNext = new Vector3(xNext, hNext, 0f);
            float3 localNext = (float3)container.transform.InverseTransformPoint(worldNext);

            var k0 = currentChunk.spline[0];
            // Use half the segment as tangent length so it's not crazy long
            k0.TangentOut = (localNext - k0.Position) * 0.5f;
            currentChunk.spline[0] = k0;
        }
        // ---------------------------------

        // Track extents for chunk trimming
        if (x < currentChunk.minX) currentChunk.minX = x;
        if (x > currentChunk.maxX) currentChunk.maxX = x;

        nextSpawnX += knotSpacing;
    }

    // Remove entire rail chunks that are fully behind minKeepX
    private void RemoveChunksBehind(float minKeepX)
    {
        if (chunks.Count == 0) return;

        // Work on a temporary list of splines we can modify
        var splineList = new List<Spline>(container.Splines);

        int i = 0;
        while (i < chunks.Count)
        {
            var chunk = chunks[i];

            // If this chunk's max X is still behind the keep zone, remove it.
            if (chunk.maxX < minKeepX)
            {
                int splineIndex = splineList.IndexOf(chunk.spline);
                if (splineIndex >= 0)
                    splineList.RemoveAt(splineIndex);

                if (chunk == currentChunk)
                    currentChunk = null;

                chunks.RemoveAt(i);
                // don't increment i; list shrank
            }
            else
            {
                i++;
            }
        }

        container.Splines = splineList.ToArray();

        // If we removed currentChunk and there are still chunks, pick the last one as current
        if (currentChunk == null && chunks.Count > 0)
            currentChunk = chunks[chunks.Count - 1];
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

    // ----- GAP HELPERS -----

    private void ScheduleNextGap(float fromX)
    {
        float railLength = RandomRange(railLengthRange);
        nextGapStartX = fromX + railLength;
    }

    private float RandomRange(Vector2 range)
    {
        if (range.y <= range.x) return range.x;
        double t = rng.NextDouble();
        return (float)(range.x + t * (range.y - range.x));
    }

    private void CreateNewChunk()
    {
        // Get current splines as a mutable list
        var splineList = new List<Spline>(container.Splines);

        var newSpline = new Spline();
        splineList.Add(newSpline);
        container.Splines = splineList.ToArray();

        currentChunk = new RailChunk
        {
            spline = newSpline
        };

        chunks.Add(currentChunk);

        if (splineInstantiator != null)
            splineInstantiator.UpdateInstances();
    }
}
