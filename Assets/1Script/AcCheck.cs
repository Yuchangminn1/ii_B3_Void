using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class AcCheck : MonoBehaviour
{
    [Header("Base")]
    [Header("기준이 되는 RawImage. 비워두면 같은 오브젝트에서 자동 검색합니다.")]
    public RawImage targetRawImage;

    [Header("Overlay")]
    [Header("기준 이미지 안에 들어와야 하는 타겟 RawImage")]
    public RawImage overlayRawImage;

    public Direction CurrentDirection;

    [Header("Threshold")]
    [Header("기준 텍스처에서 이 값보다 투명하면 무시")]
    [SerializeField, Range(0f, 1f)] float baseMinAlpha = 0.01f;
    [Header("겹쳐진 텍스처에서 이 값보다 투명하면 덮이지 않은 것으로 처리")]
    [SerializeField, Range(0f, 1f)] float overlayMinAlpha = 0.01f;
    [Header("타겟(overlay)에서 검은 영역만 계산에 사용")]
    [SerializeField] bool overlayBlackOnly = true;
    [Header("검정 판정 밝기 임계값. 낮을수록 더 어두운 픽셀만 통과")]
    [SerializeField, Range(0f, 0.5f)] float blackLumaThreshold = 0.15f;
    [Header("검정 RGB 임계값. luma 대신 RGB 상한으로도 통과 판정")]
    [SerializeField, Range(0, 255)] int blackRgbThreshold = 70;

    [Header("Update")]
    [Header("분석 주기(초)")]
    [SerializeField, Range(0.5f, 1f)] float updateInterval = 0.5f;
    [Header("샘플링 간격. 1이면 더 정확하지만 무거울 수 있습니다.")]
    [SerializeField, Range(1, 16)] int sampleStep = 2;
    [Header("체크 반경 (타겟 텍스처 기준 픽셀)")]
    [Tooltip("타겟 텍스처의 기준 픽셀 단위 반경. 0이면 단일 픽만 검사. 이 값에 따라 오버레이의 주변 픽을 허용하여 약간의 위치 오차를 보정합니다.")]
    [SerializeField, Range(0, 64)] int checkRadius = 0;

    [Header("Output")]
    [Header("타겟 텍스처의 유효 알파 영역 중 기준 이미지 안에 들어온 비율(%)")]
    [SerializeField, Range(0f, 100f)] float detectedPercent;
    [Header("분모로 계산된 타겟 픽셀 수")]
    [SerializeField] int debugTotalPixels;
    [Header("기준 이미지 안에 들어온 타겟 픽셀 수")]
    [SerializeField] int debugMatchedPixels;
    public enum DetectionMode { MatchedPercent, ProtrusionOverTarget, ProtrusionOverOverlap }
    [Tooltip("MatchedPercent: 기존 방식(matched/target). ProtrusionOverTarget: 삐져나온 픽 기준(target). ProtrusionOverOverlap: 삐져나온 픽을 겹친 픽 기준으로 계산.")]
    public DetectionMode detectionMode = DetectionMode.MatchedPercent;

    //public ShadowMaskContainer shadowMaskContainer;

    [Header("결과를 표시할 UI Text (선택)")]
    [SerializeField] protected Text outputText;
    [Header("텍스트 포맷 예시: {0:F1}%")]
    [SerializeField] string outputFormat = "{0:F1}%";

    CameraValue _cameraValue = null;

    //영역 채우기 임계값
    const int MatchedPercentThreshold = 50;
    //영역 들어오기 임계값
    //const int ProtrusionOverTargetThreshold = 93;


    protected float matchedAdjustPercent = 0f;
    //protected float protrusionAdjustPercent = 0f;

    public const float CheckDelay = 0.1f;

    float modifier = 0f;

    int ansCount = 0;

    Coroutine _returnCheckCoroutine = null;





    float _nextUpdateTime;
    Texture2D _baseReadbackTexture;
    Texture2D _overlayReadbackTexture;

    static readonly int ShaderPropTargetMaskEnabled = Shader.PropertyToID("_TargetMaskEnabled");
    static readonly int ShaderPropTargetMaskTex = Shader.PropertyToID("_TargetMaskTex");
    static readonly int ShaderPropTargetWorldToLocal = Shader.PropertyToID("_TargetWorldToLocal");
    static readonly int ShaderPropTargetRectMinMax = Shader.PropertyToID("_TargetRectMinMax");
    static readonly int ShaderPropTargetUvRect = Shader.PropertyToID("_TargetUvRect");
    static readonly int ShaderPropTargetMaskMinAlpha = Shader.PropertyToID("_TargetMaskMinAlpha");

    public float DetectedPercent => detectedPercent;
    public int DebugTotalPixels => debugTotalPixels;
    public int DebugMatchedPixels => debugMatchedPixels;

    protected bool _isClear = false;

    protected bool _isCheck = false;

    protected Action onClear;

    void Awake()
    {
        if (targetRawImage == null)
        {
            targetRawImage = GetComponent<RawImage>();
        }
    }

    protected virtual void Start()
    {
        PageController.Instance.OnReset += Reset;

        // foreach (ShadowMaskContainer tmp in FindObjectsOfType<ShadowMaskContainer>())
        // {
        //     if (tmp.CurrentDirection == CurrentDirection)
        //     {
        //         shadowMaskContainer = tmp;
        //         break;
        //     }

        // }

        foreach (CameraValue tmp in FindObjectsOfType<CameraValue>())
        {
            if (tmp.CurrentDirection == CurrentDirection)
            {
                _cameraValue = tmp;
                break;
            }
        }
    }

    IEnumerator ResetAnsCount()
    {
        yield return CoroutineReturnManager.GetWaitForSeconds(1.5f);
        ansCount = 0;
    }

    public void Reset()
    {
        _isCheck = false;
        _isClear = false;
        ClearOverlayShaderMask();


        if (outputText != null)
        {
            outputText.text = string.Format(outputFormat, detectedPercent);
            FadeManager.Instance.SetAlphaZero(outputText);
        }
        else
        {
            // outputText is null, skip fading it
        }

        FadeManager.Instance.SetAlphaZero(targetRawImage);
    }

    protected virtual void Update()
    {
        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            if (CurrentDirection == Direction.Left)
            {
                DebugClear();
            }
        }
        else if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            if (CurrentDirection == Direction.Right)
            {
                DebugClear();
            }
        }

        if (_isCheck == false)
            return;

        ApplyOverlayShaderMaskFromTarget();

        if (Time.unscaledTime < _nextUpdateTime) return;

        _nextUpdateTime = Time.unscaledTime + Mathf.Max(0.5f, updateInterval);
        UpdateColorPercent();
    }



    public virtual void StartCheck()
    {
        StartCoroutine(DelayToCheckStart());
        _isClear = false;
        ApplyOverlayShaderMaskFromTarget();

        _cameraValue.GuideTextOn();

        Debug.Log("AcCheck - StartCheck: " + CurrentDirection);


        //shadowMaskContainer.CurrentIndex++;

        if (outputText != null)
        {
            FadeManager.Instance.SetAlphaOne(outputText);
        }

        FadeManager.Instance.SetAlphaOne(targetRawImage);

    }

    protected IEnumerator DelayToCheckStart()
    {
        yield return CoroutineReturnManager.GetWaitForSeconds(2f);
        _isCheck = true;

    }


    public virtual void StopCheck()
    {

        _isCheck = false;
        _cameraValue?.CloseCamera();
        _cameraValue.GuideTextOff();

        ClearOverlayShaderMask();
        //shadowMaskContainer.HideShadowMasks();


        if (outputText != null)
        {
            FadeManager.Instance.SetAlphaZero(outputText);
        }

    }

    public void SetTargetRawImage(RawImage rawImage)
    {
        //shadowMaskContainer.CurrentIndex++;

        targetRawImage = rawImage;
        StartCheck();
    }


    void OnDestroy()
    {
        ClearOverlayShaderMask();

        if (_baseReadbackTexture != null)
        {
            Destroy(_baseReadbackTexture);
            _baseReadbackTexture = null;
        }

        if (_overlayReadbackTexture != null)
        {
            Destroy(_overlayReadbackTexture);
            _overlayReadbackTexture = null;
        }
    }
    public void AddOnClearListener(Action listener)
    {
        if (onClear != null && onClear.GetInvocationList().Contains(listener))
        {
            return;
        }
        onClear += listener;
    }

    public void RemoveOnClearListener(Action listener)
    {
        if (onClear == null || !onClear.GetInvocationList().Contains(listener))
        {
            return;
        }
        onClear -= listener;
    }

    //public void 

    public void UpdateColorPercent()
    {
        ApplyOverlayShaderMaskFromTarget();

        if (targetRawImage == null || overlayRawImage == null)
        {
            ResetResult();
            return;
        }

        // if (cameraValue == null)
        // {
        //     cameraValue = overlayRawImage.GetComponent<CameraValue>();
        // }

        if (_cameraValue.CameraOnDelay)
        {
            return;
        }

        // 디버그: 이 AcCheck 인스턴스가 어떤 타겟/오버레이를 보고 있는지 확인

        Color32[] basePixels;
        int baseWidth;
        int baseHeight;
        if (!TryGetPixelsFromRawImage(targetRawImage, ref _baseReadbackTexture, out basePixels, out baseWidth, out baseHeight))
        {
            ResetResult();
            return;
        }

        Color32[] overlayPixels;
        int overlayWidth;
        int overlayHeight;
        if (!TryGetPixelsFromRawImage(overlayRawImage, ref _overlayReadbackTexture, out overlayPixels, out overlayWidth, out overlayHeight))
        {
            ResetResult();
            return;
        }

        GameManager.Instance.GoToIdleCheck();
        // RectTransform 정보 가져오기
        RectTransform targetRectTransform = targetRawImage.rectTransform;
        RectTransform overlayRectTransform = overlayRawImage.rectTransform;
        Rect targetRect = targetRectTransform.rect;
        Rect overlayRect = overlayRectTransform.rect;

        // Overlay 텍스처의 UV 경계
        int overlayXMin;
        int overlayXMax;
        int overlayYMin;
        int overlayYMax;
        GetTextureBounds(overlayRawImage.uvRect, overlayWidth, overlayHeight, out overlayXMin, out overlayXMax, out overlayYMin, out overlayYMax);

        // Base 텍스처의 UV 경계
        int baseXMin;
        int baseXMax;
        int baseYMin;
        int baseYMax;
        GetTextureBounds(targetRawImage.uvRect, baseWidth, baseHeight, out baseXMin, out baseXMax, out baseYMin, out baseYMax);

        int total = 0;
        int matched = 0;
        int step = Mathf.Max(1, sampleStep);

        // Helper: bilinear sample from overlay pixels (u,v in 0..1)
        Color SampleOverlayBilinear(Color32[] pix, int w, int h, float u, float v)
        {
            if (w <= 1 || h <= 1) return Color.clear;
            float fx = Mathf.Clamp01(u) * (w - 1);
            float fy = Mathf.Clamp01(v) * (h - 1);
            int x0 = Mathf.Clamp(Mathf.FloorToInt(fx), 0, w - 1);
            int x1 = Mathf.Clamp(x0 + 1, 0, w - 1);
            int y0 = Mathf.Clamp(Mathf.FloorToInt(fy), 0, h - 1);
            int y1 = Mathf.Clamp(y0 + 1, 0, h - 1);
            float sx = fx - x0;
            float sy = fy - y0;

            Color c00 = new Color(pix[y0 * w + x0].r / 255f, pix[y0 * w + x0].g / 255f, pix[y0 * w + x0].b / 255f, pix[y0 * w + x0].a / 255f);
            Color c10 = new Color(pix[y0 * w + x1].r / 255f, pix[y0 * w + x1].g / 255f, pix[y0 * w + x1].b / 255f, pix[y0 * w + x1].a / 255f);
            Color c01 = new Color(pix[y1 * w + x0].r / 255f, pix[y1 * w + x0].g / 255f, pix[y1 * w + x0].b / 255f, pix[y1 * w + x0].a / 255f);
            Color c11 = new Color(pix[y1 * w + x1].r / 255f, pix[y1 * w + x1].g / 255f, pix[y1 * w + x1].b / 255f, pix[y1 * w + x1].a / 255f);

            Color cx0 = Color.Lerp(c00, c10, sx);
            Color cx1 = Color.Lerp(c01, c11, sx);
            return Color.Lerp(cx0, cx1, sy);
        }

        // Iterate target (base) pixels - more stable denominator
        for (int by = baseYMin; by <= baseYMax; by += step)
        {
            int baseRow = by * baseWidth;
            for (int bx = baseXMin; bx <= baseXMax; bx += step)
            {
                Color32 basePixel = basePixels[baseRow + bx];
                if (!HasVisibleAlpha(basePixel, baseMinAlpha)) continue;

                total++;

                // Base 픽 좌표 -> 정규화 UV (0..1)
                float baseUV_X = (baseWidth > 1) ? (bx / (float)(baseWidth - 1)) : 0f;
                float baseUV_Y = (baseHeight > 1) ? (by / (float)(baseHeight - 1)) : 0f;

                // Base 로컬 좌표
                float baseLocalX = Mathf.Lerp(targetRect.xMin, targetRect.xMax, baseUV_X);
                float baseLocalY = Mathf.Lerp(targetRect.yMin, targetRect.yMax, baseUV_Y);
                Vector3 worldPoint = targetRectTransform.TransformPoint(new Vector3(baseLocalX, baseLocalY, 0f));

                // 월드 -> Overlay 로컬
                Vector2 overlayLocalPoint = overlayRectTransform.InverseTransformPoint(worldPoint);
                if (!overlayRect.Contains(overlayLocalPoint)) continue;

                // Overlay 정규화 좌표
                float overlayNormalizedX = Mathf.InverseLerp(overlayRect.xMin, overlayRect.xMax, overlayLocalPoint.x);
                float overlayNormalizedY = Mathf.InverseLerp(overlayRect.yMin, overlayRect.yMax, overlayLocalPoint.y);

                // UV rect mapping
                float overlayUV_X = Mathf.Lerp(overlayRawImage.uvRect.xMin, overlayRawImage.uvRect.xMax, overlayNormalizedX);
                float overlayUV_Y = Mathf.Lerp(overlayRawImage.uvRect.yMin, overlayRawImage.uvRect.yMax, overlayNormalizedY);

                // Sample overlay bilinearly
                Color sampled = SampleOverlayBilinear(overlayPixels, overlayWidth, overlayHeight, overlayUV_X, overlayUV_Y);
                Color32 sampled32 = new Color32((byte)(Mathf.Clamp01(sampled.r) * 255f), (byte)(Mathf.Clamp01(sampled.g) * 255f), (byte)(Mathf.Clamp01(sampled.b) * 255f), (byte)(Mathf.Clamp01(sampled.a) * 255f));

                bool anyMatch = false;
                if (checkRadius <= 0)
                {
                    // Single bilinear sample
                    anyMatch = IsOverlayTargetPixel(sampled32);
                }
                else
                {
                    // Convert checkRadius (target pixels) to overlay pixel radius
                    int radiusOverlay = Mathf.Clamp(Mathf.RoundToInt(checkRadius * ((float)overlayWidth / (float)baseWidth)), 1, Mathf.Max(1, Mathf.Max(overlayWidth, overlayHeight)));
                    float fx = Mathf.Clamp01(overlayUV_X) * (overlayWidth - 1);
                    float fy = Mathf.Clamp01(overlayUV_Y) * (overlayHeight - 1);
                    int cx = Mathf.RoundToInt(fx);
                    int cy = Mathf.RoundToInt(fy);

                    int stepR = Mathf.Max(1, sampleStep);
                    int x0 = Mathf.Clamp(cx - radiusOverlay, 0, overlayWidth - 1);
                    int x1 = Mathf.Clamp(cx + radiusOverlay, 0, overlayWidth - 1);
                    int y0 = Mathf.Clamp(cy - radiusOverlay, 0, overlayHeight - 1);
                    int y1 = Mathf.Clamp(cy + radiusOverlay, 0, overlayHeight - 1);

                    for (int oy2 = y0; oy2 <= y1 && !anyMatch; oy2 += stepR)
                    {
                        int rowO2 = oy2 * overlayWidth;
                        for (int ox2 = x0; ox2 <= x1; ox2 += stepR)
                        {
                            Color32 oc = overlayPixels[rowO2 + ox2];
                            if (IsOverlayTargetPixel(oc))
                            {
                                anyMatch = true;
                                break;
                            }
                        }
                    }
                }

                if (anyMatch)
                {
                    matched++;
                }
            }
        }

        // Count protruding: overlay pixels that are visible and map to transparent target pixels
        int protruding = 0;
        // iterate overlay pixels (sample step) and map into target
        int stepO = Mathf.Max(1, sampleStep);
        for (int oy = overlayYMin; oy <= overlayYMax; oy += stepO)
        {
            int rowO = oy * overlayWidth;
            for (int ox = overlayXMin; ox <= overlayXMax; ox += stepO)
            {
                Color32 op = overlayPixels[rowO + ox];
                if (!IsOverlayTargetPixel(op)) continue;

                // overlay pixel -> local overlay coords
                float overlayUV_X = ox / (float)(overlayWidth - 1);
                float overlayUV_Y = oy / (float)(overlayHeight - 1);
                float overlayLocalX = Mathf.Lerp(overlayRect.xMin, overlayRect.xMax, overlayUV_X);
                float overlayLocalY = Mathf.Lerp(overlayRect.yMin, overlayRect.yMax, overlayUV_Y);
                Vector3 worldPoint = overlayRectTransform.TransformPoint(new Vector3(overlayLocalX, overlayLocalY, 0f));

                Vector2 targetLocalPoint = targetRectTransform.InverseTransformPoint(worldPoint);
                if (!targetRect.Contains(targetLocalPoint)) continue;

                float targetNormalizedX = Mathf.InverseLerp(targetRect.xMin, targetRect.xMax, targetLocalPoint.x);
                float targetNormalizedY = Mathf.InverseLerp(targetRect.yMin, targetRect.yMax, targetLocalPoint.y);

                float baseUV_X = Mathf.Lerp(targetRawImage.uvRect.xMin, targetRawImage.uvRect.xMax, targetNormalizedX);
                float baseUV_Y = Mathf.Lerp(targetRawImage.uvRect.yMin, targetRawImage.uvRect.yMax, targetNormalizedY);

                int baseTexX = Mathf.Clamp(Mathf.RoundToInt(baseUV_X * (baseWidth - 1)), 0, baseWidth - 1);
                int baseTexY = Mathf.Clamp(Mathf.RoundToInt(baseUV_Y * (baseHeight - 1)), 0, baseHeight - 1);

                Color32 sampledBasePixel = basePixels[baseTexY * baseWidth + baseTexX];
                // if base is transparent here -> protruding
                if (!HasVisibleAlpha(sampledBasePixel, baseMinAlpha)) protruding++;
            }
        }

        debugTotalPixels = total;
        debugMatchedPixels = matched;

        // Decide detectedPercent based on selected detection mode
        switch (detectionMode)
        {
            case DetectionMode.MatchedPercent:
                detectedPercent = total > 0 ? (matched * 100f / total) + modifier : 0f;
                break;
            case DetectionMode.ProtrusionOverTarget:
                detectedPercent = total > 0 ? (Mathf.Clamp01(1f - ((float)protruding / (float)total)) * 100f) + modifier : 0f;
                break;
            case DetectionMode.ProtrusionOverOverlap:
                if (matched == 0)
                {
                    detectedPercent = (protruding > 0) ? 0f : 100f;
                }
                else
                {
                    detectedPercent = (Mathf.Clamp01(1f - ((float)protruding / (float)matched)) * 100f) + modifier;
                }
                break;
            default:
                detectedPercent = total > 0 ? (matched * 100f / total) + modifier : 0f;
                break;
        }

        // Compute combined progress percentage from two adjusted goals and display single percent
        float matchedPercent = total > 0 ? (matched * 100f / total) : 0f;
        float protrusionOverTargetPercent = total > 0 ? (Mathf.Clamp01(1f - ((float)protruding / (float)total)) * 100f) : 0f;

        // Effective thresholds include adjustments
        float effMatchedThreshold = Mathf.Max(0f, MatchedPercentThreshold - matchedAdjustPercent);
        //float effProtrusionThreshold = Mathf.Clamp(ProtrusionOverTargetThreshold - protrusionAdjustPercent, 0f, 100f);

        float matchedRatio = effMatchedThreshold > 0f ? Mathf.Clamp01(matchedPercent / effMatchedThreshold) : 1f;

        Debug.LogWarning($"matchedRatio = {matchedRatio}");
        //float protrusionRatio = effProtrusionThreshold > 0f ? Mathf.Clamp01(protrusionOverTargetPercent / effProtrusionThreshold) : 1f;

        // Combine ratios with higher weight on matchedRatio (inside coverage).
        // matchedRatio 비중을 3배로, protrusionRatio 비중을 1배로 줌.
        //float combinedRatio = (matchedRatio * 3f + protrusionRatio * 1f) / 4f;
        float combinedPercent = (100f / (MatchedPercentThreshold - matchedAdjustPercent)) * matchedRatio * 100f;



        // Trigger only when BOTH adjusted conditions met:
        // 1) MatchedPercent (matched / target) >= (40 - matchedAdjustPercent)
        // 2) ProtrusionOverTarget >= (100 - protrusionAdjustPercent)
        float matchedPercentFinal = total > 0 ? (matched * 100f / total) : 0f;
        //float protrusionOverTargetPercentFinal = total > 0 ? (Mathf.Clamp01(1f - ((float)protruding / (float)total)) * 100f) : 0f;
        float effMatchedThresholdFinal = Mathf.Max(0f, MatchedPercentThreshold - matchedAdjustPercent);
        //float effProtrusionThresholdFinal = Mathf.Clamp(ProtrusionOverTargetThreshold - protrusionAdjustPercent, 0f, 100f);

        // if (outputText != null)
        // {
        //     outputText.text = $"{matchedPercentFinal:F1} / {effMatchedThresholdFinal:F1}, {protrusionOverTargetPercentFinal:F1} / {effProtrusionThresholdFinal:F1}";
        // }
        if (outputText != null)
        {
            if (matchedPercent >= 5f)
            {
                if (combinedPercent > 99f)
                {
                    combinedPercent = 99f;
                }
                outputText.text = string.Format("{0:F0}%", combinedPercent);
            }
            else
            {
                outputText.text = string.Format("{0:F0}%", 0f);
            }
        }
        //if (matchedPercent >= effMatchedThresholdFinal && protrusionOverTargetPercentFinal >= effProtrusionThresholdFinal)

        if (matchedPercent >= effMatchedThresholdFinal)
        {
            CheckOn(matchedPercent, effMatchedThresholdFinal);

        }
    }

    protected virtual void CheckOn(float matchedPercent, float effMatchedThresholdFinal)
    {
        ansCount++;
        if (ansCount > 2)
        {
            ansCount = 0;
            _returnCheckCoroutine = StartCoroutine(ResetAnsCount());
            //Debug.Log($"[AcCheck] Combined trigger met: matched={matchedPercent:F1}% (need {effMatchedThresholdFinal}%), protrusionOverTarget={protrusionOverTargetPercentFinal:F1}% (need {effProtrusionThresholdFinal}%)");
            Debug.Log($"[AcCheck] Combined trigger met: matched={matchedPercent:F1}% (need {effMatchedThresholdFinal}%),");

            StopCheck();
            StartCoroutine(DelayOnClear());
        }
        else
        {
            if (_returnCheckCoroutine != null)
            {
                StopCoroutine(_returnCheckCoroutine);
            }
        }
    }

    protected virtual IEnumerator DelayOnClear()
    {
        _isClear = true;
        _isCheck = false;
        if (outputText != null)
        {
            outputText.text = string.Format("{0:F0}%", 100f);

        }

        yield return CoroutineReturnManager.GetWaitForSeconds(CheckDelay);
        onClear?.Invoke();
    }



    public void DebugClear()
    {
        StopCheck();

        detectedPercent = 100f;
        if (outputText != null)
        {
            outputText.text = string.Format(outputFormat, detectedPercent);
        }

        StartCoroutine(DelayOnClear());

    }

    void ResetResult()
    {
        detectedPercent = 0f;
        debugTotalPixels = 0;
        debugMatchedPixels = 0;

        if (outputText != null)
        {
            outputText.text = string.Format(outputFormat, detectedPercent);
        }
    }

    bool HasVisibleAlpha(Color32 pixel, float minAlpha)
    {
        if (pixel.a / 255f < minAlpha) return false;
        return true;
    }

    bool IsOverlayTargetPixel(Color32 pixel)
    {
        if (!HasVisibleAlpha(pixel, overlayMinAlpha)) return false;
        if (!overlayBlackOnly) return true;

        float red = pixel.r / 255f;
        float green = pixel.g / 255f;
        float blue = pixel.b / 255f;
        float luma = red * 0.299f + green * 0.587f + blue * 0.114f;

        bool passByLuma = luma <= blackLumaThreshold;
        bool passByRgb = pixel.r <= blackRgbThreshold && pixel.g <= blackRgbThreshold && pixel.b <= blackRgbThreshold;
        return passByLuma || passByRgb;
    }

    bool TryGetPixelsFromRawImage(RawImage rawImage, ref Texture2D readbackCache, out Color32[] pixels, out int width, out int height)
    {
        pixels = null;
        width = 0;
        height = 0;

        if (rawImage == null) return false;

        // If RawImage has a material (shader), render material output into RT so readback follows shader result.
        bool canUseMaterialReadback = rawImage.material != null;

        if (canUseMaterialReadback)
        {
            Texture src = GetRawImageTexture(rawImage) ?? rawImage.material.mainTexture;
            if (src == null) return false;

            // Determine target readback size. Prefer source texture size when available, otherwise use rect size.
            int w = 0, h = 0;
            if (src is Texture2D t2) { w = t2.width; h = t2.height; }
            else if (src is WebCamTexture wct) { w = wct.width; h = wct.height; }
            else if (src is RenderTexture rts) { w = rts.width; h = rts.height; }
            else { w = Mathf.Max(1, Mathf.RoundToInt(rawImage.rectTransform.rect.width)); h = Mathf.Max(1, Mathf.RoundToInt(rawImage.rectTransform.rect.height)); }

            if (w <= 0 || h <= 0) return false;

            // Ensure readback cache matches needed size
            if (readbackCache == null || readbackCache.width != w || readbackCache.height != h)
            {
                if (readbackCache != null) Destroy(readbackCache);
                readbackCache = new Texture2D(w, h, TextureFormat.RGBA32, false);
                readbackCache.wrapMode = TextureWrapMode.Clamp;
            }

            RenderTexture rt = new RenderTexture(w, h, 0, RenderTextureFormat.ARGB32);
            RenderTexture prev = RenderTexture.active;
            bool hasTargetMaskProp = rawImage.material.HasProperty(ShaderPropTargetMaskEnabled);
            float originalTargetMaskEnabled = hasTargetMaskProp ? rawImage.material.GetFloat(ShaderPropTargetMaskEnabled) : 0f;
            try
            {
                rt.Create();
                if (hasTargetMaskProp)
                {
                    // Graphics.Blit does not preserve UI mesh-local coordinates used by target-mask clipping.
                    // Disable only mask clipping during readback; geometric overlap is handled in CPU matching logic.
                    rawImage.material.SetFloat(ShaderPropTargetMaskEnabled, 0f);
                }

                // Blit using the RawImage's material so shader output is rendered into RT
                Graphics.Blit(src, rt, rawImage.material);

                RenderTexture.active = rt;
                readbackCache.ReadPixels(new Rect(0, 0, w, h), 0, 0);
                readbackCache.Apply();

                pixels = readbackCache.GetPixels32();
                width = w;
                height = h;
                return pixels != null && pixels.Length > 0;
            }
            finally
            {
                if (hasTargetMaskProp)
                {
                    rawImage.material.SetFloat(ShaderPropTargetMaskEnabled, originalTargetMaskEnabled);
                }

                RenderTexture.active = prev;

                // Safety guard: never release an RT while it is still active.
                if (RenderTexture.active == rt)
                {
                    RenderTexture.active = null;
                }

                if (rt != null)
                {
                    rt.Release();
                    Destroy(rt);
                }
            }
        }

        Texture texture = GetRawImageTexture(rawImage);
        if (texture == null) return false;

        return TryGetPixels(texture, ref readbackCache, out pixels, out width, out height);
    }

    void GetTextureBounds(Rect uvRect, int width, int height, out int xMin, out int xMax, out int yMin, out int yMax)
    {
        float u0 = Mathf.Clamp01(Mathf.Min(uvRect.xMin, uvRect.xMax));
        float u1 = Mathf.Clamp01(Mathf.Max(uvRect.xMin, uvRect.xMax));
        float v0 = Mathf.Clamp01(Mathf.Min(uvRect.yMin, uvRect.yMax));
        float v1 = Mathf.Clamp01(Mathf.Max(uvRect.yMin, uvRect.yMax));

        xMin = Mathf.Clamp(Mathf.FloorToInt(u0 * (width - 1)), 0, width - 1);
        xMax = Mathf.Clamp(Mathf.CeilToInt(u1 * (width - 1)), 0, width - 1);
        yMin = Mathf.Clamp(Mathf.FloorToInt(v0 * (height - 1)), 0, height - 1);
        yMax = Mathf.Clamp(Mathf.CeilToInt(v1 * (height - 1)), 0, height - 1);
    }

    Texture GetRawImageTexture(RawImage rawImage)
    {
        if (rawImage == null) return null;
        if (rawImage.texture != null) return rawImage.texture;
        if (rawImage.material != null) return rawImage.material.mainTexture;
        return null;
    }

    bool TryGetPixels(Texture texture, ref Texture2D readbackCache, out Color32[] pixels, out int width, out int height)
    {
        pixels = null;
        width = 0;
        height = 0;

        if (texture == null) return false;

        if (texture is WebCamTexture webcam)
        {
            width = webcam.width;
            height = webcam.height;
            if (width <= 16 || height <= 16 || !webcam.isPlaying) return false;

            try
            {
                pixels = webcam.GetPixels32();
                return pixels != null && pixels.Length > 0;
            }
            catch
            {
                return false;
            }
        }

        if (texture is Texture2D texture2D)
        {
            width = texture2D.width;
            height = texture2D.height;
            if (width <= 0 || height <= 0) return false;

            try
            {
                pixels = texture2D.GetPixels32();
                return pixels != null && pixels.Length > 0;
            }
            catch
            {
                return TryReadByBlit(texture, ref readbackCache, out pixels, out width, out height);
            }
        }

        if (texture is RenderTexture renderTexture)
        {
            return TryReadRenderTexture(renderTexture, ref readbackCache, out pixels, out width, out height);
        }

        return TryReadByBlit(texture, ref readbackCache, out pixels, out width, out height);
    }

    bool TryReadByBlit(Texture source, ref Texture2D readbackCache, out Color32[] pixels, out int width, out int height)
    {
        pixels = null;
        width = source.width;
        height = source.height;
        if (width <= 0 || height <= 0) return false;

        RenderTexture temp = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32);
        try
        {
            Graphics.Blit(source, temp);
            return TryReadRenderTexture(temp, ref readbackCache, out pixels, out width, out height);
        }
        finally
        {
            RenderTexture.ReleaseTemporary(temp);
        }
    }

    bool TryReadRenderTexture(RenderTexture renderTexture, ref Texture2D readbackCache, out Color32[] pixels, out int width, out int height)
    {
        pixels = null;
        width = renderTexture.width;
        height = renderTexture.height;
        if (width <= 0 || height <= 0) return false;

        if (readbackCache == null || readbackCache.width != width || readbackCache.height != height)
        {
            if (readbackCache != null) Destroy(readbackCache);
            readbackCache = new Texture2D(width, height, TextureFormat.RGBA32, false);
        }

        RenderTexture previous = RenderTexture.active;
        try
        {
            RenderTexture.active = renderTexture;
            readbackCache.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            readbackCache.Apply(false, false);
            pixels = readbackCache.GetPixels32();
            return pixels != null && pixels.Length > 0;
        }
        catch
        {
            return false;
        }
        finally
        {
            RenderTexture.active = previous;
        }
    }

    void ApplyOverlayShaderMaskFromTarget()
    {
        if (targetRawImage == null || overlayRawImage == null) return;

        Material overlayMaterial = overlayRawImage.material;
        if (overlayMaterial == null) return;

        if (!overlayMaterial.HasProperty(ShaderPropTargetMaskEnabled)) return;

        Texture targetTexture = GetRawImageTexture(targetRawImage);
        if (targetTexture == null)
        {
            overlayMaterial.SetFloat(ShaderPropTargetMaskEnabled, 0f);
            return;
        }

        RectTransform targetRectTransform = targetRawImage.rectTransform;
        Rect targetRect = targetRectTransform.rect;
        Rect uvRect = targetRawImage.uvRect;

        overlayMaterial.SetTexture(ShaderPropTargetMaskTex, targetTexture);
        overlayMaterial.SetFloat(ShaderPropTargetMaskEnabled, 1f);
        overlayMaterial.SetFloat(ShaderPropTargetMaskMinAlpha, baseMinAlpha);
        overlayMaterial.SetMatrix(ShaderPropTargetWorldToLocal, targetRectTransform.worldToLocalMatrix);
        overlayMaterial.SetVector(ShaderPropTargetRectMinMax, new Vector4(targetRect.xMin, targetRect.yMin, targetRect.xMax, targetRect.yMax));
        overlayMaterial.SetVector(ShaderPropTargetUvRect, new Vector4(uvRect.xMin, uvRect.yMin, uvRect.xMax, uvRect.yMax));
    }

    void ClearOverlayShaderMask()
    {
        if (overlayRawImage == null) return;

        Material overlayMaterial = overlayRawImage.material;
        if (overlayMaterial == null) return;

        if (!overlayMaterial.HasProperty(ShaderPropTargetMaskEnabled)) return;

        //_cameraValue?.CloseCamera();


        overlayMaterial.SetFloat(ShaderPropTargetMaskEnabled, 0f);


    }
}
