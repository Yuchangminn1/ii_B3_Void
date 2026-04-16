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
    [Tooltip("Log keyed transparent percentage every frame while rendering.")]
    public bool debugPercentEveryFrame = false;

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
    [Range(1f, 180f)] public float autoFocusMaxWaitSeconds = 120f;

    [Header("Auto Threshold ROI")]
    [Tooltip("Use a fixed normalized ROI for transparent-percent evaluation. Keep off to use legacy center ROI.")]
    public bool autoUseFixedPercentRoi = false;
    [Range(0f, 1f)] public float autoPercentRoiXMin = 0.25f;
    [Range(0f, 1f)] public float autoPercentRoiYMin = 0.25f;
    [Range(0f, 1f)] public float autoPercentRoiXMax = 0.75f;
    [Range(0f, 1f)] public float autoPercentRoiYMax = 0.75f;

    [Header("Post Focus Edge Stabilization")]
    [Tooltip("After focus completes, stabilize only the outer border lines without changing global threshold.")]
    public bool enableEdgeLineStabilization = true;
    [Range(2, 64)] public int edgeStabilizationBandPixels = 12;
    [Range(0.005f, 0.3f)] public float edgeStabilizationTolerance = 0.04f;
    [Range(1, 20)] public int edgeStabilizationClipStep = 2;
    [Range(0, 80)] public int edgeStabilizationMaxExtraClip = 24;
    [Range(0.05f, 2f)] public float edgeStabilizationCheckInterval = 0.2f;

    [Header("Output Settings")]
    [Tooltip("Toggle camera rendering/visibility with a bool value.")]
    [SerializeField] private bool renderByBool = false;
    [Tooltip("Use chroma-key shader output. Turn off to show raw camera texture.")]
    [SerializeField] private bool useShaderOutput = true;

    public bool opaqueToBlack = true;
    [Range(1f, 10f)] public float edgeContrast = 1f;
    float noiseFilter = 0.0f;
    [Range(0f, 1f)] public float alphaCutoff = 0.4f;
    float midValueFilter = 0f;

    [Header("Left Clipping")]
    public int leftClipPixels = 0;
    public int rightClipPixels = 0;
    public int topClipPixels = 0;
    public int bottomClipPixels = 0;

    // 런타임 데이터 (인스펙터에 표시 안 됨)
    [System.NonSerialized] public WebCamTexture webcamTexture;
    [System.NonSerialized] public Material material;
    [System.NonSerialized] private RenderTexture _croppedTexture;
    [System.NonSerialized] private Texture2D _analysisReadbackTexture;

    private RawImage _targetRawImage;
    private Coroutine _autoKeyRoutine;
    private bool _autoKeyGoalReached;

    [System.NonSerialized] private float _autoFocusDeadlineTime = 0f;
    [System.NonSerialized] private bool _edgeBaselineReady = false;
    [System.NonSerialized] private float _edgeBaselineLeftRatio = 0f;
    [System.NonSerialized] private float _edgeBaselineRightRatio = 0f;
    [System.NonSerialized] private float _edgeBaselineTopRatio = 0f;
    [System.NonSerialized] private float _edgeBaselineBottomRatio = 0f;
    [System.NonSerialized] private int _edgeBaseLeftClip = 0;
    [System.NonSerialized] private int _edgeBaseRightClip = 0;
    [System.NonSerialized] private int _edgeBaseTopClip = 0;
    [System.NonSerialized] private int _edgeBaseBottomClip = 0;
    [System.NonSerialized] private float _edgeNextCheckTime = 0f;


    public Text GuideText;



    [Tooltip("Seconds between automatic threshold increments during auto key.")]
    public float autoThresholdStepInterval = 1.0f;
    public Direction CurrentDirection;

    CameraVisible _cameraVisible;


    JsonGenericUpData _genericData = new JsonGenericUpData();

    bool _cameraOnDelay = false;

    public bool CameraOnDelay
    {
        get { return _cameraOnDelay; }
    }


    bool _isRendering = false;

    public bool IsRendered
    {
        get { return _isRendering; }
        set
        {
            if (_isRendering == value) return;
            if (_isRendering == false && value == true)
            {
                StartCoroutine(DelayFalse());
            }
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

    public void SetSize()
    {
        if (_targetRawImage == null)
            return;

        // RectTransform rawRect = _targetRawImage.rectTransform;
        // RectTransform targetRect = targetRawImage.GetComponent<RectTransform>();
        // if (rawRect == null || targetRect == null)
        //     return;

        Vector2 targetSize;
        targetSize.x = 556f;
        targetSize.y = 536f;

        // if (targetSize.x <= 0f || targetSize.y <= 0f)
        // {
        //     targetSize = targetRect.sizeDelta;
        // }
        // if (targetSize.x <= 0f || targetSize.y <= 0f)
        //     return;

        float sourceAspect = 1f;
        if (webcamTexture != null && webcamTexture.width > 0 && webcamTexture.height > 0)
        {
            sourceAspect = (float)webcamTexture.width / webcamTexture.height;
        }
        else if (requestedWidth > 0 && requestedHeight > 0)
        {
            sourceAspect = (float)requestedWidth / requestedHeight;
        }

        float targetAspect = targetSize.x / targetSize.y;
        float width;
        float height;

        // Keep aspect ratio and scale to cover target bounds.
        if (sourceAspect >= targetAspect)
        {
            height = targetSize.y;
            width = height * sourceAspect;
        }
        else
        {
            width = targetSize.x;
            height = width / sourceAspect;
        }

        float finalWidth = Mathf.Max(width, targetSize.x);
        float finalHeight = Mathf.Max(height, targetSize.y);

        _targetRawImage.rectTransform.sizeDelta = new Vector2(finalWidth, finalHeight);

        float addedHeight = finalHeight - targetSize.y;
        Vector2 anchoredPos = _targetRawImage.rectTransform.anchoredPosition;
        anchoredPos.y = addedHeight * 0.5f;
        _targetRawImage.rectTransform.anchoredPosition = anchoredPos;
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
            ReleaseCroppedTexture();
            return;
        }

        if (webcamTexture != null)
        {
            if (!webcamTexture.isPlaying)
            {
                webcamTexture.Play();
            }
            Texture outputTexture = GetProcessedOutputTexture();
            _targetRawImage.texture = outputTexture;
            if (material != null)
            {
                material.mainTexture = outputTexture;
            }
        }
    }

    Texture GetProcessedOutputTexture()
    {
        if (webcamTexture == null || webcamTexture.width <= 0 || webcamTexture.height <= 0)
            return webcamTexture;

        int sourceWidth = webcamTexture.width;
        int sourceHeight = webcamTexture.height;

        int left = Mathf.Max(0, leftClipPixels);
        int right = Mathf.Max(0, rightClipPixels);
        int top = Mathf.Max(0, topClipPixels);
        int bottom = Mathf.Max(0, bottomClipPixels);

        int croppedWidth = sourceWidth - left - right;
        int croppedHeight = sourceHeight - top - bottom;

        if (croppedWidth <= 0 || croppedHeight <= 0)
        {
            ReleaseCroppedTexture();
            return webcamTexture;
        }

        bool needCrop = left > 0 || right > 0 || top > 0 || bottom > 0;
        if (!needCrop)
        {
            ReleaseCroppedTexture();
            return webcamTexture;
        }

        if (_croppedTexture == null || _croppedTexture.width != croppedWidth || _croppedTexture.height != croppedHeight)
        {
            ReleaseCroppedTexture();
            _croppedTexture = new RenderTexture(croppedWidth, croppedHeight, 0, RenderTextureFormat.ARGB32);
            _croppedTexture.filterMode = FilterMode.Bilinear;
            _croppedTexture.wrapMode = TextureWrapMode.Clamp;
            _croppedTexture.Create();
        }

        Vector2 scale = new Vector2(
            (float)croppedWidth / sourceWidth,
            (float)croppedHeight / sourceHeight
        );
        Vector2 offset = new Vector2(
            (float)left / sourceWidth,
            (float)bottom / sourceHeight
        );

        Graphics.Blit(webcamTexture, _croppedTexture, scale, offset);
        return _croppedTexture;
    }

    void ReleaseCroppedTexture()
    {
        if (_croppedTexture == null) return;
        _croppedTexture.Release();
        Destroy(_croppedTexture);
        _croppedTexture = null;
    }

    public void GuideTextOn()
    {
        if (GuideText != null)
        {
            GuideText.gameObject.SetActive(true);
        }
    }
    public void GuideTextOff()
    {
        if (GuideText != null)
        {
            GuideText.gameObject.SetActive(false);
        }
    }
    bool TryGetProcessingPixels(out Color32[] pixels, out int width, out int height)
    {
        pixels = null;
        width = 0;
        height = 0;

        if (webcamTexture == null || !webcamTexture.isPlaying || webcamTexture.width <= 16)
            return false;

        Texture processedTexture = GetProcessedOutputTexture();
        if (processedTexture == null)
            return false;

        if (processedTexture == webcamTexture)
        {
            try
            {
                pixels = webcamTexture.GetPixels32();
            }
            catch
            {
                return false;
            }

            width = webcamTexture.width;
            height = webcamTexture.height;
            return pixels != null && pixels.Length > 0;
        }

        RenderTexture rt = processedTexture as RenderTexture;
        if (rt == null || rt.width <= 0 || rt.height <= 0)
            return false;

        if (_analysisReadbackTexture == null || _analysisReadbackTexture.width != rt.width || _analysisReadbackTexture.height != rt.height)
        {
            if (_analysisReadbackTexture != null)
            {
                Destroy(_analysisReadbackTexture);
            }
            _analysisReadbackTexture = new Texture2D(rt.width, rt.height, TextureFormat.RGBA32, false);
        }

        RenderTexture previousActive = RenderTexture.active;
        RenderTexture.active = rt;
        _analysisReadbackTexture.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        _analysisReadbackTexture.Apply(false, false);
        RenderTexture.active = previousActive;

        pixels = _analysisReadbackTexture.GetPixels32();
        width = rt.width;
        height = rt.height;
        return pixels != null && pixels.Length > 0;
    }

    IEnumerator DelayFalse()
    {
        _cameraOnDelay = true;
        yield return new WaitForSeconds(1f);

        _cameraOnDelay = false;
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

        ReleaseCroppedTexture();
    }

    public void Start()
    {
        _cameraVisible = FindAnyObjectByType<CameraVisible>();
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

        // Clipping is already applied to the source texture before shader processing.
        material.SetFloat("_LeftClip", 0f);
        material.SetFloat("_RightClip", 0f);
        material.SetFloat("_TopClip", 0f);
        material.SetFloat("_BottomClip", 0f);
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

    public void CloseCamera()
    {
        if (CurrentDirection == Direction.Left)
            _cameraVisible.CameraOffLeft();
        else if (CurrentDirection == Direction.Right)
            _cameraVisible.CameraOffRight();
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
        if (_analysisReadbackTexture != null)
        {
            Destroy(_analysisReadbackTexture);
            _analysisReadbackTexture = null;
        }
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
        StartAutoKeyForSeconds(seconds, true);
    }

    public void StartAutoKeyForSeconds(float seconds, bool keepUntilGoal)
    {
        // 이전 오토가 돌고 있으면 먼저 정지
        if (_autoKeyRoutine != null)
        {
            StopCoroutine(_autoKeyRoutine);
            _autoKeyRoutine = null;
        }

        _autoKeyGoalReached = false;
        _edgeBaselineReady = false;

        float initialDuration = Mathf.Max(0.05f, seconds);
        float maxDuration = keepUntilGoal ? float.PositiveInfinity : initialDuration;
        _autoFocusDeadlineTime = keepUntilGoal ? float.PositiveInfinity : (Time.time + Mathf.Max(0.05f, maxDuration));

        _autoKeyRoutine = StartCoroutine(AutoPickKeyColorRoutine());
        string deadlineText = keepUntilGoal ? "INF" : maxDuration.ToString("F2");
        Debug.Log($"[CameraValue] Auto key color started for '{gameObject.name}'. Initial={initialDuration:F2}s, KeepUntilGoal={keepUntilGoal}, DeadlineIn={deadlineText}s");
    }

    IEnumerator AutoPickKeyColorRoutine()
    {
        SetWarningText("Auto picking key color...");
        float targetTransparentRatio = Mathf.Clamp01(autoThresholdTransparentTarget);
        float currentTransparentRatio = -1f;

        bool keyAcquired = false;
        while (!keyAcquired)
        {
            if (Time.time >= _autoFocusDeadlineTime)
            {
                break;
            }

            if (TryUpdateKeyAndThresholdFromWebcam(out Color sampledKeyColor, out float sampledThreshold, out bool thresholdGoalReached))
            {
                // Phase 1 is key acquisition; apply sampled key immediately.
                keyColor = sampledKeyColor;
                threshold = sampledThreshold;
                _autoKeyGoalReached = thresholdGoalReached;
                Debug.Log($"[CameraValue] Key acquired for {gameObject.name}: {keyColor}");
                keyAcquired = true;
            }

            if (!keyAcquired)
                yield return new WaitForSeconds(0.05f);
        }

        if (!keyAcquired)
        {
            OffWarningText();
            Debug.LogWarning($"[CameraValue] Auto key color stopped for '{gameObject.name}' because key acquisition timed out.");
            _autoKeyRoutine = null;
            yield break;
        }

        if (keyAcquired)
        {
            // Procedural phase 2: adjust threshold and compare black-pixel ratio against target.
            float currentThreshold = Mathf.Clamp01(threshold);
            float thresholdStep = Mathf.Max(0.0005f, autoThresholdStep);
            float stepInterval = Mathf.Max(0.05f, autoThresholdStepInterval);
            float maxThreshold = Mathf.Clamp01(autoMaxThreshold);

            threshold = currentThreshold;

            while (true)
            {
                if (!TryComputeTransparentRatioFromCurrentFrame(
                    out float transparentRatio,
                    out int transparentCount,
                    out int totalCount,
                    true,
                    false
                ))
                {
                    yield return new WaitForSeconds(0.05f);
                    continue;
                }

                currentTransparentRatio = transparentRatio;
                _autoKeyGoalReached = transparentRatio >= targetTransparentRatio;

                if (verboseDebug)
                {
                    Debug.Log($"[CameraValue] Auto threshold step '{gameObject.name}': Threshold={currentThreshold:F3}, Transparent={transparentRatio * 100f:F1}% ({transparentCount}/{totalCount}), Target={targetTransparentRatio * 100f:F1}%, Key=({keyColor.r:F3},{keyColor.g:F3},{keyColor.b:F3})");
                }

                if (_autoKeyGoalReached)
                {
                    break;
                }

                if (Time.time >= _autoFocusDeadlineTime)
                {
                    break;
                }

                // Move threshold in the direction that increases transparent ratio.
                float nextThreshold = Mathf.Min(maxThreshold, currentThreshold + thresholdStep);
                if (Mathf.Approximately(nextThreshold, currentThreshold))
                {
                    break;
                }

                currentThreshold = nextThreshold;
                threshold = currentThreshold;

                yield return new WaitForSeconds(stepInterval);

            }
        }

        bool stoppedByGoal = keyAcquired && _autoKeyGoalReached;
        bool stoppedByDeadline = Time.time >= _autoFocusDeadlineTime;
        float currentPercent = Mathf.Clamp01(currentTransparentRatio < 0f ? 0f : currentTransparentRatio) * 100f;
        float targetPercent = targetTransparentRatio * 100f;

        if (TryComputeEdgeBandRatios(out float leftRatio, out float rightRatio, out float topRatio, out float bottomRatio))
        {
            _edgeBaselineLeftRatio = leftRatio;
            _edgeBaselineRightRatio = rightRatio;
            _edgeBaselineTopRatio = topRatio;
            _edgeBaselineBottomRatio = bottomRatio;

            _edgeBaseLeftClip = leftClipPixels;
            _edgeBaseRightClip = rightClipPixels;
            _edgeBaseTopClip = topClipPixels;
            _edgeBaseBottomClip = bottomClipPixels;

            _edgeBaselineReady = true;
            _edgeNextCheckTime = Time.time + Mathf.Max(0.05f, edgeStabilizationCheckInterval);
        }

        // 오토 종료 시점의 최종 키/쓰레숄드 로그
        Debug.Log($"[CameraValue] Auto key color finished for '{gameObject.name}'. Final Key={keyColor}, Threshold={threshold:F3}, Transparent={currentPercent:F1}%, Target={targetPercent:F1}%, GoalReached={stoppedByGoal}, DeadlineReached={stoppedByDeadline}");

        if (stoppedByGoal)
        {
            if (CurrentDirection == Direction.Left)
            {
                _cameraVisible.CameraOffLeft();
            }
            else if (CurrentDirection == Direction.Right)
            {
                _cameraVisible.CameraOffRight();
            }
            //IsRendered = false;
            Debug.Log($"[CameraValue] Auto key goal reached. Camera rendering disabled for '{gameObject.name}'. Transparent={currentPercent:F1}% / Target={targetPercent:F1}%");
        }

        OffWarningText();
        Debug.Log($"[CameraValue] Auto key color stopped for '{gameObject.name}'.");

        // End this auto-key cycle strictly within the requested duration.
        _autoKeyRoutine = null;
    }

    bool TryUpdateKeyAndThresholdFromWebcam(out Color sampledKeyColor, out float sampledThreshold, out bool thresholdGoalReached)
    {
        sampledKeyColor = keyColor;
        sampledThreshold = threshold;
        thresholdGoalReached = false;

        if (!TryGetProcessingPixels(out Color32[] pixels, out int w, out int h))
        {
            return false;
        }

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

            // threshold는 일단 0에서 시작해서, Update()에서 단계적으로 올린다.
            sampledThreshold = 0f;
            thresholdGoalReached = false;
            return true;
        }

        return false;
    }

    bool TryComputeTransparentRatioFromCurrentFrame(
        out float transparentRatio,
        out int transparentCount,
        out int totalCount,
        bool useFullFrame = false,
        bool useCenterExclusion = true)
    {
        transparentRatio = 0f;
        transparentCount = 0;
        totalCount = 0;

        if (!TryGetProcessingPixels(out Color32[] pixels, out int w, out int h))
        {
            return false;
        }

        int x0 = useFullFrame ? 0 : (w / 4);
        int y0 = useFullFrame ? 0 : (h / 4);
        int x1 = useFullFrame ? w : ((w * 3) / 4);
        int y1 = useFullFrame ? h : ((h * 3) / 4);

        if (autoUseFixedPercentRoi)
        {
            float minX = Mathf.Clamp01(autoPercentRoiXMin);
            float minY = Mathf.Clamp01(autoPercentRoiYMin);
            float maxX = Mathf.Clamp01(autoPercentRoiXMax);
            float maxY = Mathf.Clamp01(autoPercentRoiYMax);

            if (maxX < minX)
            {
                float t = minX;
                minX = maxX;
                maxX = t;
            }
            if (maxY < minY)
            {
                float t = minY;
                minY = maxY;
                maxY = t;
            }

            x0 = Mathf.Clamp(Mathf.FloorToInt(minX * (w - 1)), 0, Mathf.Max(0, w - 1));
            y0 = Mathf.Clamp(Mathf.FloorToInt(minY * (h - 1)), 0, Mathf.Max(0, h - 1));
            x1 = Mathf.Clamp(Mathf.CeilToInt(maxX * (w - 1)) + 1, x0 + 1, w);
            y1 = Mathf.Clamp(Mathf.CeilToInt(maxY * (h - 1)) + 1, y0 + 1, h);
        }

        int stepX = useFullFrame ? 1 : Mathf.Max(1, (x1 - x0) / 96);
        int stepY = useFullFrame ? 1 : Mathf.Max(1, (y1 - y0) / 54);

        int cx0 = x0;
        int cy0 = y0;
        int cx1 = x1;
        int cy1 = y1;
        if (useCenterExclusion && autoUseEdgeSampling)
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

        if (x1 <= x0 || y1 <= y0)
        {
            return false;
        }

        Color.RGBToHSV(keyColor, out float keyH, out float keyS, out float keyV);
        float keySat = keyS;
        float keyVal = keyV;
        float wHue = 0.7f + (0.2f * keySat);
        float wSat = 0.35f;
        float wVal = Mathf.Lerp(0.45f, 0.12f, keySat);
        wVal += Mathf.Clamp01((0.2f - keyVal) / 0.2f) * 0.12f;
        float norm = Mathf.Sqrt((wHue * wHue) + (wSat * wSat) + (wVal * wVal));

        float thresholdLocal = Mathf.Clamp01(threshold);
        float smoothLocal = Mathf.Max(smoothness, 1e-5f);
        float edgeLocal = Mathf.Max(1f, edgeContrast);
        float midRange = Mathf.Clamp(midValueFilter, 0f, 0.49f);
        float midMin = 0.5f - midRange;
        float midMax = 0.5f + midRange;
        float alphaCutoffLocal = Mathf.Clamp01(alphaCutoff);

        for (int y = y0; y < y1; y += stepY)
        {
            int row = y * w;
            for (int x = x0; x < x1; x += stepX)
            {
                if (useCenterExclusion && autoUseEdgeSampling && x >= cx0 && x < cx1 && y >= cy0 && y < cy1)
                {
                    continue;
                }

                Color32 c = pixels[row + x];
                totalCount++;
                Color.RGBToHSV(new Color(c.r / 255f, c.g / 255f, c.b / 255f), out float hsvH, out float hsvS, out float hsvV);

                float hueDist = Mathf.Abs(hsvH - keyH);
                hueDist = Mathf.Min(hueDist, 1f - hueDist) * 2f;
                float satDist = Mathf.Abs(hsvS - keyS);
                float valDist = Mathf.Abs(hsvV - keyV);

                float dist = Mathf.Sqrt(
                    (hueDist * hueDist * wHue * wHue) +
                    (satDist * satDist * wSat * wSat) +
                    (valDist * valDist * wVal * wVal)
                );
                float keyDist = dist / Mathf.Max(norm, 1e-5f);

                float alpha = Mathf.Clamp01((keyDist - thresholdLocal) / smoothLocal);
                alpha = Mathf.Clamp01((alpha - 0.5f) * edgeLocal + 0.5f);
                if (alpha >= midMin && alpha <= midMax)
                {
                    alpha = 0f;
                }
                alpha = alpha >= alphaCutoffLocal ? 1f : 0f;

                if (alpha < 0.5f)
                {
                    transparentCount++;
                }
            }
        }

        if (totalCount <= 0)
        {
            return false;
        }

        transparentRatio = (float)transparentCount / totalCount;
        return true;
    }

    void Update()
    {
        if (_isRendering && webcamTexture != null && webcamTexture.isPlaying && _targetRawImage != null)
        {
            Texture outputTexture = GetProcessedOutputTexture();
            if (_targetRawImage.texture != outputTexture)
            {
                _targetRawImage.texture = outputTexture;
            }
            if (material != null && material.mainTexture != outputTexture)
            {
                material.mainTexture = outputTexture;
            }

            if (debugPercentEveryFrame && TryComputeTransparentRatioFromCurrentFrame(out float transparentRatio, out int transparentCount, out int totalCount))
            {
                Debug.Log($"[CameraValue] FramePercent '{gameObject.name}': Transparent={transparentRatio * 100f:F1}% ({transparentCount}/{totalCount}), Threshold={threshold:F3}, Key=({keyColor.r:F3},{keyColor.g:F3},{keyColor.b:F3})");
            }

            if (enableEdgeLineStabilization && _autoKeyRoutine == null && _edgeBaselineReady && Time.time >= _edgeNextCheckTime)
            {
                _edgeNextCheckTime = Time.time + Mathf.Max(0.05f, edgeStabilizationCheckInterval);

                if (TryComputeEdgeBandRatios(out float leftNow, out float rightNow, out float topNow, out float bottomNow))
                {
                    bool changed = false;
                    int step = Mathf.Max(1, edgeStabilizationClipStep);
                    int maxExtra = Mathf.Max(0, edgeStabilizationMaxExtraClip);
                    float tolerance = Mathf.Max(0.0001f, edgeStabilizationTolerance);

                    changed |= AdjustEdgeClipFromRatio(leftNow, _edgeBaselineLeftRatio, ref leftClipPixels, _edgeBaseLeftClip, step, maxExtra, tolerance);
                    changed |= AdjustEdgeClipFromRatio(rightNow, _edgeBaselineRightRatio, ref rightClipPixels, _edgeBaseRightClip, step, maxExtra, tolerance);
                    changed |= AdjustEdgeClipFromRatio(topNow, _edgeBaselineTopRatio, ref topClipPixels, _edgeBaseTopClip, step, maxExtra, tolerance);
                    changed |= AdjustEdgeClipFromRatio(bottomNow, _edgeBaselineBottomRatio, ref bottomClipPixels, _edgeBaseBottomClip, step, maxExtra, tolerance);

                    if (verboseDebug && changed)
                    {
                        Debug.Log($"[CameraValue] EdgeStabilize '{gameObject.name}': L={leftClipPixels}, R={rightClipPixels}, T={topClipPixels}, B={bottomClipPixels} | EdgeNow(LRTB)=({leftNow * 100f:F1},{rightNow * 100f:F1},{topNow * 100f:F1},{bottomNow * 100f:F1}) | Base=({_edgeBaselineLeftRatio * 100f:F1},{_edgeBaselineRightRatio * 100f:F1},{_edgeBaselineTopRatio * 100f:F1},{_edgeBaselineBottomRatio * 100f:F1})");
                    }
                }
            }
        }
    }

    bool AdjustEdgeClipFromRatio(float nowRatio, float baselineRatio, ref int clipPixels, int baseClip, int step, int maxExtra, float tolerance)
    {
        float diff = nowRatio - baselineRatio;
        int prev = clipPixels;
        int maxClip = baseClip + maxExtra;

        if (diff > tolerance)
        {
            clipPixels = Mathf.Min(maxClip, clipPixels + step);
        }
        else if (diff < (tolerance * 0.35f) && clipPixels > baseClip)
        {
            clipPixels = Mathf.Max(baseClip, clipPixels - step);
        }

        return prev != clipPixels;
    }

    bool TryComputeEdgeBandRatios(out float leftRatio, out float rightRatio, out float topRatio, out float bottomRatio)
    {
        leftRatio = 0f;
        rightRatio = 0f;
        topRatio = 0f;
        bottomRatio = 0f;

        if (!TryGetProcessingPixels(out Color32[] pixels, out int w, out int h))
        {
            return false;
        }

        if (w <= 1 || h <= 1)
        {
            return false;
        }

        int band = Mathf.Clamp(edgeStabilizationBandPixels, 1, Mathf.Min(w, h) / 2);
        if (band <= 0)
        {
            return false;
        }

        Color.RGBToHSV(keyColor, out float keyH, out float keyS, out float keyV);
        float keySat = keyS;
        float keyVal = keyV;
        float wHue = 0.7f + (0.2f * keySat);
        float wSat = 0.35f;
        float wVal = Mathf.Lerp(0.45f, 0.12f, keySat);
        wVal += Mathf.Clamp01((0.2f - keyVal) / 0.2f) * 0.12f;
        float norm = Mathf.Sqrt((wHue * wHue) + (wSat * wSat) + (wVal * wVal));

        float thresholdLocal = Mathf.Clamp01(threshold);
        float smoothLocal = Mathf.Max(smoothness, 1e-5f);
        float edgeLocal = Mathf.Max(1f, edgeContrast);
        float midRange = Mathf.Clamp(midValueFilter, 0f, 0.49f);
        float midMin = 0.5f - midRange;
        float midMax = 0.5f + midRange;
        float alphaCutoffLocal = Mathf.Clamp01(alphaCutoff);

        int leftTransparent = 0, leftTotal = 0;
        int rightTransparent = 0, rightTotal = 0;
        int topTransparent = 0, topTotal = 0;
        int bottomTransparent = 0, bottomTotal = 0;

        for (int y = 0; y < h; y++)
        {
            int row = y * w;
            bool inTop = y >= (h - band);
            bool inBottom = y < band;

            for (int x = 0; x < w; x++)
            {
                bool inLeft = x < band;
                bool inRight = x >= (w - band);

                if (!inLeft && !inRight && !inTop && !inBottom)
                {
                    continue;
                }

                Color32 c = pixels[row + x];
                Color.RGBToHSV(new Color(c.r / 255f, c.g / 255f, c.b / 255f), out float hsvH, out float hsvS, out float hsvV);

                float hueDist = Mathf.Abs(hsvH - keyH);
                hueDist = Mathf.Min(hueDist, 1f - hueDist) * 2f;
                float satDist = Mathf.Abs(hsvS - keyS);
                float valDist = Mathf.Abs(hsvV - keyV);

                float dist = Mathf.Sqrt(
                    (hueDist * hueDist * wHue * wHue) +
                    (satDist * satDist * wSat * wSat) +
                    (valDist * valDist * wVal * wVal)
                );
                float keyDist = dist / Mathf.Max(norm, 1e-5f);

                float alpha = Mathf.Clamp01((keyDist - thresholdLocal) / smoothLocal);
                alpha = Mathf.Clamp01((alpha - 0.5f) * edgeLocal + 0.5f);
                if (alpha >= midMin && alpha <= midMax)
                {
                    alpha = 0f;
                }
                alpha = alpha >= alphaCutoffLocal ? 1f : 0f;
                bool isTransparent = alpha < 0.5f;

                if (inLeft)
                {
                    leftTotal++;
                    if (isTransparent) leftTransparent++;
                }
                if (inRight)
                {
                    rightTotal++;
                    if (isTransparent) rightTransparent++;
                }
                if (inTop)
                {
                    topTotal++;
                    if (isTransparent) topTransparent++;
                }
                if (inBottom)
                {
                    bottomTotal++;
                    if (isTransparent) bottomTransparent++;
                }
            }
        }

        leftRatio = leftTotal > 0 ? (float)leftTransparent / leftTotal : 0f;
        rightRatio = rightTotal > 0 ? (float)rightTransparent / rightTotal : 0f;
        topRatio = topTotal > 0 ? (float)topTransparent / topTotal : 0f;
        bottomRatio = bottomTotal > 0 ? (float)bottomTransparent / bottomTotal : 0f;
        return leftTotal > 0 && rightTotal > 0 && topTotal > 0 && bottomTotal > 0;
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
        if (_autoKeyRoutine != null)
        {
            StopCoroutine(_autoKeyRoutine);
            _autoKeyRoutine = null;
        }
        _edgeBaselineReady = false;
    }
}
