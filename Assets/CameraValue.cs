using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CameraValue : MonoBehaviour, IJsonGenericTarget
{
    [Header("Webcam Settings")]
    public int cameraIndex = 0;
    public string deviceName = "";
    [Header("Device Filters")]
    [Tooltip("If non-empty and the selected device's name contains this substring, this CameraValue will skip starting the webcam.")]
    public string[] ignoreDeviceNameContains;
    public int requestedWidth = 1280;
    public int requestedHeight = 720;
    public int requestedFPS = 30;
    public bool mirrorHorizontal = true;
    public bool mirrorVertical = false;

    [Header("Capture Orientation")]
    [Tooltip("If enabled, force horizontal inversion from capture output regardless of per-device settings.")]
    public bool forceFlipHorizontalOnCapture = true;
    [Tooltip("If enabled, force vertical inversion from capture output regardless of per-device settings.")]
    public bool forceFlipVerticalOnCapture = true;

    public Text CameraSetUpText;

    [Header("Debug")]
    [Tooltip("Enable verbose debug logs for CameraValue operations.")]
    public bool verboseDebug = false;

    [Header("Color Key Settings")]
    public Color keyColor = Color.green;
    [Range(0f, 1f)] public float threshold = 0.4f;
    [Range(0f, 1f)] public float smoothness = 0.1f;

    [Header("Auto Key Tuning")]
    [Tooltip("Sample mostly around the ROI edges so centered subjects affect keying less.")]
    public bool autoUseEdgeSampling = true;
    [Range(0f, 0.8f)] public float autoCenterExclusionRatio = 0.4f;
    [Range(0f, 1f)] public float autoThresholdTransparentTarget = 0.95f;
    [Range(0.001f, 0.1f)] public float autoThresholdStep = 0.01f;
    [Range(0.01f, 0.4f)] public float autoDominantClusterTolerance = 0.10f;
    [Range(0f, 1f)] public float autoUpdateLerp = 0.35f;
    [Range(0f, 1f)] public float autoMinInlierRatio = 0.35f;
    [Range(0f, 1f)] public float autoMaxThreshold = 0.7f;
    [Tooltip("If enabled, initial autofocus keeps running until target ratio is reached (or max wait time).")]
    public bool autoExtendFocusUntilTarget = true;
    [Range(1f, 30f)] public float autoFocusMaxWaitSeconds = 10f;

    [Header("Output Settings")]
    [Tooltip("Toggle camera rendering/visibility with a bool value.")]
    [SerializeField] private bool renderByBool = false;
    [Tooltip("Use chroma-key shader output. Turn off to show raw camera texture.")]
    [SerializeField] private bool useShaderOutput = true;

    public bool opaqueToBlack = true;
    [Range(1f, 10f)] public float edgeContrast = 1f;
    [Range(0f, 1f)] public float noiseFilter = 1f;
    [Range(0f, 1f)] public float alphaCutoff = 0.5f;
    [Range(0f, 0.49f)] public float midValueFilter = 0.15f;

    [Header("Left Clipping")]
    public int leftClipPixels = 0;
    public int rightClipPixels = 0;
    public int topClipPixels = 0;
    public int bottomClipPixels = 0;

    // 런타임 데이터 (인스펙터에 표시 안 됨)
    [System.NonSerialized] public WebCamTexture webcamTexture;
    [System.NonSerialized] public Material material;

    private RawImage _targetRawImage;
    private Coroutine _autoKeyRoutine;
    private Coroutine _autoKeyTimeoutRoutine;
    private bool _autoKeyGoalReached;

    JsonGenericUpData _genericData = new JsonGenericUpData();


    bool _isRendering = false;

    public bool IsRendered
    {
        get { return _isRendering; }
        set
        {
            if (_isRendering == value) return;
            _isRendering = value;
            renderByBool = value;
            UpdateRenderBindings();
        }
    }

    public bool RenderByBool
    {
        get { return renderByBool; }
        set
        {
            if (renderByBool == value) return;
            renderByBool = value;
            IsRendered = value;
        }
    }

    public bool UseShaderOutput
    {
        get { return useShaderOutput; }
        set
        {
            if (useShaderOutput == value) return;
            useShaderOutput = value;
            ApplyOutputMode();
        }
    }

    void ApplyOutputMode()
    {
        if (_targetRawImage == null) return;
        _targetRawImage.material = useShaderOutput ? material : null;
    }

    void UpdateRenderBindings()
    {
        if (_targetRawImage == null) return;

        if (_isRendering == false)
        {
            if (webcamTexture != null && webcamTexture.isPlaying)
            {
                webcamTexture.Stop();
            }
            _targetRawImage.texture = null;
            if (material != null) material.mainTexture = null;
            return;
        }

        if (webcamTexture != null)
        {
            if (!webcamTexture.isPlaying)
            {
                webcamTexture.Play();
            }
            _targetRawImage.texture = webcamTexture;
            if (material != null)
            {
                material.mainTexture = webcamTexture;
            }
        }
    }


    public void Initialize(Shader shader)
    {
        _targetRawImage = GetComponent<RawImage>();
        if (_targetRawImage == null)
        {
            Debug.LogError($"[CameraValue] No RawImage found on {gameObject.name}");
            return;
        }

        if (material == null && shader != null)
        {
            material = new Material(shader);
        }

        useShaderOutput = true;
        ApplyOutputMode();

        IsRendered = renderByBool;
    }

    public void StartWebcam(WebCamDevice[] devices)
    {
        if (_targetRawImage == null) return;
        StopWebcam();

        // Build a filtered list of devices excluding any that match the ignore substring
        List<WebCamDevice> filtered = new List<WebCamDevice>();
        for (int i = 0; i < devices.Length; i++)
        {
            var d = devices[i];
            bool ignore = false;

            Debug.Log($"{d.name}");
            for (int j = 0; j < ignoreDeviceNameContains.Length; j++)
            {
                if (!string.IsNullOrEmpty(ignoreDeviceNameContains[j]) && d.name != null && d.name.Contains(ignoreDeviceNameContains[j]))
                {
                    if (verboseDebug) Debug.Log($"[CameraValue] Ignoring device '{d.name}' because it contains '{ignoreDeviceNameContains[j]}'.");
                    ignore = true;
                    break;
                }
            }
            if (ignore) continue;
            filtered.Add(d);
        }

        if (filtered.Count == 0)
        {
            Debug.LogWarning($"[CameraValue] No available cameras for '{gameObject.name}' after applying ignore filter '{ignoreDeviceNameContains}'.");
            return;
        }

        WebCamDevice device = default(WebCamDevice);
        bool found = false;

        // 1) Try exact name match within filtered devices
        if (!string.IsNullOrEmpty(deviceName))
        {
            for (int i = 0; i < filtered.Count; i++)
            {
                if (filtered[i].name == deviceName)
                {
                    device = filtered[i];
                    found = true;
                    break;
                }
            }
        }

        // 2) Try contains name match
        if (!found && !string.IsNullOrEmpty(deviceName))
        {
            for (int i = 0; i < filtered.Count; i++)
            {
                if (filtered[i].name.Contains(deviceName))
                {
                    device = filtered[i];
                    found = true;
                    break;
                }
            }
        }

        // 3) Use cameraIndex within filtered list
        if (!found)
        {
            int idx = Mathf.Clamp(cameraIndex, 0, filtered.Count - 1);
            device = filtered[idx];
            found = true;
        }

        if (!found)
        {
            Debug.LogWarning($"[CameraValue] Could not select a camera for '{gameObject.name}'.");
            return;
        }

        webcamTexture = new WebCamTexture(device.name, requestedWidth, requestedHeight, requestedFPS);

        if (_isRendering)
        {
            webcamTexture.Play();
        }

        UpdateRenderBindings();
        Debug.Log($"[CameraValue] Started '{gameObject.name}' with device '{device.name}'");
    }

    public void StopWebcam()
    {
        if (webcamTexture != null)
        {
            if (webcamTexture.isPlaying) webcamTexture.Stop();
            if (_targetRawImage != null) _targetRawImage.texture = null;
            if (material != null) material.mainTexture = null;
            Destroy(webcamTexture);
            webcamTexture = null;
        }
    }

    public void SetWarningText(string message)
    {
        CameraSetUpText.text = message;
        FadeManager.Instance.SetAlphaOne(CameraSetUpText);
    }

    public void OffWarningText()
    {
        FadeManager.Instance.SetAlphaZero(CameraSetUpText);
    }

    public void ApplyMaterialProperties()
    {
        if (_isRendering == false)
            return;

        if (useShaderOutput == false)
            return;

        if (material == null) return;

        material.SetColor("_KeyColor", keyColor);
        material.SetFloat("_Threshold", threshold);
        material.SetFloat("_Smooth", smoothness);
        material.SetFloat("_Mirror", 0f);
        material.SetFloat("_VFlip", 0f);

        material.SetFloat("_OpaqueToBlack", opaqueToBlack ? 1f : 0f);
        material.SetFloat("_EdgeContrast", edgeContrast);
        material.SetFloat("_NoiseFilter", noiseFilter);
        material.SetFloat("_AlphaCutoff", alphaCutoff);
        material.SetFloat("_MidValueFilter", midValueFilter);

        float clipAmount = 0f;
        float rightClipAmount = 0f;
        float topClipAmount = 0f;
        float bottomClipAmount = 0f;

        if (webcamTexture != null && webcamTexture.width > 0)
        {
            clipAmount = (float)leftClipPixels / webcamTexture.width;
            rightClipAmount = (float)rightClipPixels / webcamTexture.width;
        }
        if (webcamTexture != null && webcamTexture.height > 0)
        {
            topClipAmount = (float)topClipPixels / webcamTexture.height;
            bottomClipAmount = (float)bottomClipPixels / webcamTexture.height;
        }

        material.SetFloat("_LeftClip", Mathf.Clamp01(clipAmount));
        material.SetFloat("_RightClip", Mathf.Clamp01(rightClipAmount));
        material.SetFloat("_TopClip", Mathf.Clamp01(topClipAmount));
        material.SetFloat("_BottomClip", Mathf.Clamp01(bottomClipAmount));
    }

    public void SetClipPixels(int left, int right, int top, int bottom)
    {
        leftClipPixels = Mathf.Max(0, left);
        rightClipPixels = Mathf.Max(0, right);
        topClipPixels = Mathf.Max(0, top);
        bottomClipPixels = Mathf.Max(0, bottom);
    }

    public void AddClipPixels(int leftDelta, int rightDelta, int topDelta, int bottomDelta)
    {
        leftClipPixels = Mathf.Max(0, leftClipPixels + leftDelta);
        rightClipPixels = Mathf.Max(0, rightClipPixels + rightDelta);
        topClipPixels = Mathf.Max(0, topClipPixels + topDelta);
        bottomClipPixels = Mathf.Max(0, bottomClipPixels + bottomDelta);
    }

    void OnValidate()
    {
        leftClipPixels = Mathf.Max(0, leftClipPixels);
        rightClipPixels = Mathf.Max(0, rightClipPixels);
        topClipPixels = Mathf.Max(0, topClipPixels);
        bottomClipPixels = Mathf.Max(0, bottomClipPixels);

        if (Application.isPlaying)
        {
            IsRendered = renderByBool;
            ApplyOutputMode();
        }
        else
        {
            _isRendering = renderByBool;
        }
    }

    void OnDestroy()
    {
        StopWebcam();
        if (material != null)
        {
            Destroy(material);
        }
    }

    public void AutoFocus()
    {
        StartAutoKeyForSeconds(2f, autoExtendFocusUntilTarget);
    }

    // --- Auto Key Color Logic ---
    public void StartAutoKeyForSeconds(float seconds)
    {
        StartAutoKeyForSeconds(seconds, false);
    }

    public void StartAutoKeyForSeconds(float seconds, bool keepUntilGoal)
    {
        if (_autoKeyRoutine != null) StopCoroutine(_autoKeyRoutine);
        if (_autoKeyTimeoutRoutine != null) StopCoroutine(_autoKeyTimeoutRoutine);
        _autoKeyGoalReached = false;

        _autoKeyRoutine = StartCoroutine(AutoPickKeyColorRoutine());
        _autoKeyTimeoutRoutine = StartCoroutine(StopAutoKeyAfterDelay(seconds, keepUntilGoal));
        Debug.Log($"[CameraValue] Auto key color started for '{gameObject.name}' for {seconds} seconds. keepUntilGoal={keepUntilGoal}");
    }

    IEnumerator StopAutoKeyAfterDelay(float delay, bool keepUntilGoal)
    {
        SetWarningText("Auto picking key color...");

        float baseDelay = Mathf.Max(0f, delay);
        float maxWait = Mathf.Max(baseDelay, autoFocusMaxWaitSeconds);
        float elapsed = 0f;

        while (true)
        {
            bool passedBaseDelay = elapsed >= baseDelay;
            bool shouldStop = passedBaseDelay;

            if (keepUntilGoal)
            {
                shouldStop = passedBaseDelay && _autoKeyGoalReached;
                if (elapsed >= maxWait)
                {
                    if (!_autoKeyGoalReached)
                        Debug.LogWarning($"[CameraValue] Auto key goal not reached within max wait ({maxWait:F1}s) on '{gameObject.name}'.");
                    shouldStop = true;
                }
            }

            if (shouldStop)
                break;

            yield return null;
            elapsed += Time.deltaTime;
        }

        if (_autoKeyRoutine != null)
        {
            StopCoroutine(_autoKeyRoutine);
            _autoKeyRoutine = null;
        }
        _autoKeyTimeoutRoutine = null;
        OffWarningText();
        Debug.Log($"[CameraValue] Auto key color stopped for '{gameObject.name}'.");
    }

    IEnumerator AutoPickKeyColorRoutine()
    {
        var wait = new WaitForSeconds(1f);
        while (true)
        {
            if (TryUpdateKeyAndThresholdFromWebcam(out Color sampledKeyColor, out float sampledThreshold, out bool thresholdGoalReached))
            {
                // Smooth updates to avoid visible flicker when webcam noise spikes.
                float lerpFactor = Mathf.Clamp01(autoUpdateLerp);
                keyColor = Color.Lerp(keyColor, sampledKeyColor, lerpFactor);
                threshold = Mathf.Lerp(threshold, sampledThreshold, lerpFactor);
                _autoKeyGoalReached = thresholdGoalReached;
                Debug.Log($"Auto Color/Threshold for {gameObject.name}: {keyColor} / {threshold:F3}");
            }
            yield return wait;
        }
    }

    [Header("Auto Threshold Runtime State")]
    [System.NonSerialized] private bool _autoSearching = false;
    [System.NonSerialized] private List<float> _autoKeyDistances = null;
    [System.NonSerialized] private float _autoCurrentThreshold = 0f;
    [System.NonSerialized] private float _autoMaxThresholdRuntime = 0.7f;
    [System.NonSerialized] private float _autoStepRuntime = 0.01f;
    [System.NonSerialized] private float _autoNextUpdateTime = 0f;
    [System.NonSerialized] private float _autoThresholdStartDelayUntil = 0f; // keyColor 잡은 뒤 threshold 탐색 시작 시간
    [Tooltip("Seconds between automatic threshold increments during auto key.")]
    public float autoThresholdStepInterval = 1.0f;

    bool TryUpdateKeyAndThresholdFromWebcam(out Color sampledKeyColor, out float sampledThreshold, out bool thresholdGoalReached)
    {
        sampledKeyColor = keyColor;
        sampledThreshold = threshold;
        thresholdGoalReached = false;

        if (webcamTexture == null || !webcamTexture.isPlaying || webcamTexture.width <= 16) return false;

        Color32[] pixels;
        try
        {
            pixels = webcamTexture.GetPixels32();
        }
        catch
        {
            return false;
        }

        int w = webcamTexture.width;
        int h = webcamTexture.height;
        int x0 = w / 4, y0 = h / 4;
        int x1 = (w * 3) / 4, y1 = (h * 3) / 4;
        int stepX = Mathf.Max(1, (x1 - x0) / 32);
        int stepY = Mathf.Max(1, (y1 - y0) / 18);

        int cx0 = x0;
        int cy0 = y0;
        int cx1 = x1;
        int cy1 = y1;
        if (autoUseEdgeSampling)
        {
            float exclusion = Mathf.Clamp01(autoCenterExclusionRatio);
            int roiW = x1 - x0;
            int roiH = y1 - y0;
            int padX = Mathf.RoundToInt((roiW * exclusion) * 0.5f);
            int padY = Mathf.RoundToInt((roiH * exclusion) * 0.5f);
            cx0 = x0 + padX;
            cy0 = y0 + padY;
            cx1 = x1 - padX;
            cy1 = y1 - padY;
        }

        long sr = 0, sg = 0, sb = 0;
        int count = 0;
        for (int y = y0; y < y1; y += stepY)
        {
            int row = y * w;
            for (int x = x0; x < x1; x += stepX)
            {
                if (autoUseEdgeSampling && x >= cx0 && x < cx1 && y >= cy0 && y < cy1) continue;

                Color32 c = pixels[row + x];
                sr += c.r;
                sg += c.g;
                sb += c.b;
                count++;
            }
        }

        if (count <= 0) return false;

        float meanR = (float)sr / (255f * count);
        float meanG = (float)sg / (255f * count);
        float meanB = (float)sb / (255f * count);

        List<float> distances = new List<float>(count);
        float sumDist = 0f;
        for (int y = y0; y < y1; y += stepY)
        {
            int row = y * w;
            for (int x = x0; x < x1; x += stepX)
            {
                if (autoUseEdgeSampling && x >= cx0 && x < cx1 && y >= cy0 && y < cy1) continue;

                Color32 c = pixels[row + x];
                float r = c.r / 255f;
                float g = c.g / 255f;
                float b = c.b / 255f;
                float dr = r - meanR;
                float dg = g - meanG;
                float db = b - meanB;
                float dist = Mathf.Sqrt((dr * dr + dg * dg + db * db) / 3f);
                distances.Add(dist);
                sumDist += dist;
            }
        }

        float meanDist = sumDist / distances.Count;
        float variance = 0f;
        for (int i = 0; i < distances.Count; i++)
        {
            float delta = distances[i] - meanDist;
            variance += delta * delta;
        }
        variance /= distances.Count;
        float stdDist = Mathf.Sqrt(variance);

        // Keep only near-cluster colors so moving foreground has less influence.
        float inlierCutoff = meanDist + (stdDist * 1.5f);
        long inlierSr = 0, inlierSg = 0, inlierSb = 0;
        int inlierCount = 0;
        List<Color32> inlierColors = new List<Color32>(distances.Count);

        int sampleIndex = 0;
        for (int y = y0; y < y1; y += stepY)
        {
            int row = y * w;
            for (int x = x0; x < x1; x += stepX)
            {
                if (autoUseEdgeSampling && x >= cx0 && x < cx1 && y >= cy0 && y < cy1) continue;

                float dist = distances[sampleIndex++];
                if (dist > inlierCutoff) continue;

                Color32 c = pixels[row + x];
                inlierSr += c.r;
                inlierSg += c.g;
                inlierSb += c.b;
                inlierCount++;
                inlierColors.Add(c);
            }
        }

        if (inlierCount > 0)
        {
            float inlierRatio = (float)inlierCount / count;
            if (inlierRatio < Mathf.Clamp01(autoMinInlierRatio))
            {
                return false;
            }

            sampledKeyColor = new Color(
                (float)inlierSr / (255f * inlierCount),
                (float)inlierSg / (255f * inlierCount),
                (float)inlierSb / (255f * inlierCount),
                1f
            );

            // Pick dominant color bin first (robust for solid screens with some foreground contamination).
            Dictionary<int, int> binCounts = new Dictionary<int, int>(64);
            for (int i = 0; i < inlierColors.Count; i++)
            {
                Color32 c = inlierColors[i];
                int rb = c.r >> 4;
                int gb = c.g >> 4;
                int bb = c.b >> 4;
                int key = (rb << 8) | (gb << 4) | bb;
                if (binCounts.TryGetValue(key, out int v))
                {
                    binCounts[key] = v + 1;
                }
                else
                {
                    binCounts[key] = 1;
                }
            }

            int dominantKey = 0;
            int dominantCount = -1;
            foreach (KeyValuePair<int, int> kv in binCounts)
            {
                if (kv.Value > dominantCount)
                {
                    dominantCount = kv.Value;
                    dominantKey = kv.Key;
                }
            }

            float dcr = (((dominantKey >> 8) & 0xF) + 0.5f) / 16f;
            float dcg = (((dominantKey >> 4) & 0xF) + 0.5f) / 16f;
            float dcb = ((dominantKey & 0xF) + 0.5f) / 16f;
            float dominantTolerance = Mathf.Clamp(autoDominantClusterTolerance, 0.01f, 0.4f);

            long dsr = 0, dsg = 0, dsb = 0;
            int dominantSampleCount = 0;
            for (int i = 0; i < inlierColors.Count; i++)
            {
                Color32 c = inlierColors[i];
                float r = c.r / 255f;
                float g = c.g / 255f;
                float b = c.b / 255f;
                float dr = r - dcr;
                float dg = g - dcg;
                float db = b - dcb;
                float dist = Mathf.Sqrt((dr * dr + dg * dg + db * db) / 3f);
                if (dist > dominantTolerance) continue;

                dsr += c.r;
                dsg += c.g;
                dsb += c.b;
                dominantSampleCount++;
            }

            if (dominantSampleCount > 0)
            {
                sampledKeyColor = new Color(
                    (float)dsr / (255f * dominantSampleCount),
                    (float)dsg / (255f * dominantSampleCount),
                    (float)dsb / (255f * dominantSampleCount),
                    1f
                );
            }

            // Recalculate distances against the finalized key color using chroma-only distance.
            List<float> keyDistances = new List<float>(inlierColors.Count);
            float kr = sampledKeyColor.r;
            float kg = sampledKeyColor.g;
            float kb = sampledKeyColor.b;

            float keyY = 0.299f * kr + 0.587f * kg + 0.114f * kb;
            float keyCb = (kb - keyY) * 0.5f;
            float keyCr = (kr - keyY) * 0.5f;

            for (int i = 0; i < inlierColors.Count; i++)
            {
                Color32 c = inlierColors[i];
                float r = c.r / 255f;
                float g = c.g / 255f;
                float b = c.b / 255f;

                float y = 0.299f * r + 0.587f * g + 0.114f * b;
                float cb = (b - y) * 0.5f;
                float cr = (r - y) * 0.5f;

                float dCb = cb - keyCb;
                float dCr = cr - keyCr;
                float keyDist = Mathf.Sqrt(dCb * dCb + dCr * dCr);
                keyDistances.Add(keyDist);
            }

            if (keyDistances.Count > 0)
            {
                _autoKeyDistances = keyDistances;
                _autoCurrentThreshold = 0f;
                _autoMaxThresholdRuntime = Mathf.Clamp01(autoMaxThreshold);
                _autoStepRuntime = Mathf.Max(0.0005f, autoThresholdStep);

                // 키값을 갱신한 시점부터 1초 동안은 threshold를 0으로 유지하고, 그 뒤에 탐색 시작
                _autoSearching = false;
                _autoThresholdStartDelayUntil = Time.time + 1.0f; // 1초 대기 후 시작
                _autoNextUpdateTime = _autoThresholdStartDelayUntil; // 첫 스텝 시간도 동일하게 설정
            }

            // threshold는 일단 0에서 시작해서, Update()에서 단계적으로 올린다.
            sampledThreshold = 0f;
            thresholdGoalReached = false;
            return true;
        }

        return false;
    }

    void Update()
    {
        // ...any existing Update logic before auto-threshold section...

        // 키값을 찾은 뒤 일정 시간(예: 1초)이 지난 후에만 autoSearching을 활성화
        if (!_autoSearching && _autoKeyDistances != null && _autoKeyDistances.Count > 0)
        {
            if (Time.time >= _autoThresholdStartDelayUntil)
            {
                _autoSearching = true;
                // _autoNextUpdateTime 은 TryUpdateKeyAndThresholdFromWebcam 에서 설정됨
            }
        }

        // Auto threshold runtime stepping: increment in small steps with visual pause between updates.
        if (_autoSearching && _autoKeyDistances != null && _autoKeyDistances.Count > 0)
        {
            if (Time.time >= _autoNextUpdateTime)
            {
                int total = _autoKeyDistances.Count;
                float targetTransparentRatio = Mathf.Clamp01(autoThresholdTransparentTarget);
                float current = _autoCurrentThreshold;

                int transparentCount = 0;
                for (int i = 0; i < total; i++)
                {
                    if (_autoKeyDistances[i] <= current)
                        transparentCount++;
                }

                float transparentRatio = (float)transparentCount / total;
                threshold = Mathf.Clamp01(current);

                if (transparentRatio >= targetTransparentRatio || current >= _autoMaxThresholdRuntime)
                {
                    _autoSearching = false;
                    _autoKeyGoalReached = transparentRatio >= targetTransparentRatio;
                }
                else
                {
                    _autoCurrentThreshold = Mathf.Min(current + _autoStepRuntime, _autoMaxThresholdRuntime);
                    _autoNextUpdateTime = Time.time + Mathf.Max(0.05f, autoThresholdStepInterval);
                }
            }
        }
    }

    public void Initialize(JsonGenericUpData data)
    {
        _genericData = data;

        data.floatParams.TryGetValue("threshold", out threshold);
        data.floatParams.TryGetValue("smoothness", out smoothness);
        data.floatParams.TryGetValue("autoThresholdTransparentTarget", out autoThresholdTransparentTarget);
        if (data.floatParams.TryGetValue("alphaCutoff", out float loadedAlphaCutoff)) alphaCutoff = loadedAlphaCutoff;
        if (data.floatParams.TryGetValue("midValueFilter", out float loadedMidValueFilter)) midValueFilter = loadedMidValueFilter;
        if (data.intParams != null)
        {
            if (data.intParams.TryGetValue("leftClipPixels", out int loadedLeftClip)) leftClipPixels = Mathf.Max(0, loadedLeftClip);
            if (data.intParams.TryGetValue("rightClipPixels", out int loadedRightClip)) rightClipPixels = Mathf.Max(0, loadedRightClip);
            if (data.intParams.TryGetValue("topClipPixels", out int loadedTopClip)) topClipPixels = Mathf.Max(0, loadedTopClip);
            if (data.intParams.TryGetValue("bottomClipPixels", out int loadedBottomClip)) bottomClipPixels = Mathf.Max(0, loadedBottomClip);
        }
    }

    public JsonGenericUpData Data()
    {
        _genericData.intParams = new Dictionary<string, int>();
        _genericData.floatParams = new Dictionary<string, float>();
        _genericData.boolParams = new Dictionary<string, bool>();

        _genericData.floatParams["threshold"] = threshold;
        _genericData.floatParams["smoothness"] = smoothness;
        _genericData.floatParams["alphaCutoff"] = alphaCutoff;
        _genericData.floatParams["midValueFilter"] = midValueFilter;
        _genericData.floatParams["autoThresholdTransparentTarget"] = autoThresholdTransparentTarget;
        _genericData.intParams["leftClipPixels"] = Mathf.Max(0, leftClipPixels);
        _genericData.intParams["rightClipPixels"] = Mathf.Max(0, rightClipPixels);
        _genericData.intParams["topClipPixels"] = Mathf.Max(0, topClipPixels);
        _genericData.intParams["bottomClipPixels"] = Mathf.Max(0, bottomClipPixels);
        return _genericData;
    }

    public void StopAutoThreshold()
    {
        _autoSearching = false;
        _autoKeyDistances = null;
    }
}
