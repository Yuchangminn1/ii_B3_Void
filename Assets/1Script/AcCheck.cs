using UnityEngine;
using UnityEngine.UI;

public class AcCheck : MonoBehaviour
{
    [Header("Target")]
    [Tooltip("분석할 RawImage. 비워두면 같은 오브젝트에서 자동 검색합니다.")]
    public RawImage targetRawImage;

    [Header("Area Mask")]
    [Tooltip("검사 영역을 제한할 마스크 RawImage (예: 하트). 비워두면 전체 영역 검사")]
    public RawImage areaMaskRawImage;
    [Tooltip("검사 영역을 제한할 마스크 Image(Sprite). RawImage 대신 사용할 수 있습니다.")]
    public Image areaMaskImage;
    [Tooltip("마스크에서 유효 영역으로 인정할 최소 알파값")]
    [Range(0f, 1f)] public float maskMinAlpha = 0.1f;

    [Header("Color Check")]
    [Tooltip("검출 기준 색상")]
    public Color targetColor = Color.green;
    [Tooltip("색상 허용 오차(0=완전 동일, 1=매우 넓음)")]
    [Range(0f, 1f)] public float tolerance = 0.1f;
    [Tooltip("투명 픽셀을 제외할 최소 알파값")]
    [Range(0f, 1f)] public float minAlpha = 0.01f;
    [Tooltip("체크 시 검은색 전용 모드 사용")]
    public bool checkBlackOnly = true;
    [Tooltip("검은색 판정 밝기 임계값. 낮을수록 더 어두운 픽셀만 검정으로 인식")]
    [Range(0f, 0.5f)] public float blackLumaThreshold = 0.1f;

    [Header("Update")]
    [Tooltip("분석 주기(초)")]
    [Range(0.02f, 1f)] public float updateInterval = 0.1f;
    [Tooltip("샘플링 간격. 1이면 모든 픽셀 검사")]
    [Range(1, 16)] public int sampleStep = 2;

    [Header("Startup Calibration")]
    [Tooltip("시작 시 현재 화면 색을 자동 캘리브레이션해서 초기 100% 기준색으로 설정")]
    public bool autoCalibrateOnStart = true;
    [Tooltip("기준색 설정 후 실제 계산 시작까지 대기 시간(초)")]
    [Range(0f, 10f)] public float measureStartDelay = 2f;

    [Header("Output")]
    [Tooltip("검출된 비율(%)")]
    [Range(0f, 100f)] public float detectedPercent;
    [Tooltip("실제로 계산에 사용된 유효 픽셀 수")]
    public int debugTotalPixels;
    [Tooltip("조건에 일치한 픽셀 수")]
    public int debugMatchedPixels;
    [Tooltip("결과를 표시할 UI Text (선택)")]
    public Text outputText;
    [Tooltip("텍스트 포맷 예시: {0:F1}%")]
    public string outputFormat = "{0:F1}%";

    float _nextUpdateTime;
    float _measureStartTime;
    bool _startupCalibrated;
    Texture2D _readbackTexture;
    Texture2D _maskReadbackTexture;

    void Awake()
    {
        if (targetRawImage == null)
        {
            targetRawImage = GetComponent<RawImage>();
        }

        _startupCalibrated = !autoCalibrateOnStart;
        _measureStartTime = Time.unscaledTime;

        if (autoCalibrateOnStart)
        {
            detectedPercent = 100f;
            if (outputText != null) outputText.text = string.Format(outputFormat, detectedPercent);
        }
    }

    void Update()
    {
        if (!_startupCalibrated)
        {
            if (TryCalibrateTargetColor())
            {
                _startupCalibrated = true;
                _measureStartTime = Time.unscaledTime + Mathf.Max(0f, measureStartDelay);
                detectedPercent = 100f;
                if (outputText != null) outputText.text = string.Format(outputFormat, detectedPercent);
            }

            return;
        }

        if (Time.unscaledTime < _measureStartTime)
        {
            detectedPercent = 100f;
            if (outputText != null) outputText.text = string.Format(outputFormat, detectedPercent);
            return;
        }

        if (Time.unscaledTime < _nextUpdateTime) return;
        _nextUpdateTime = Time.unscaledTime + Mathf.Max(0.02f, updateInterval);

        UpdateColorPercent();
    }

    void OnDestroy()
    {
        if (_readbackTexture != null)
        {
            Destroy(_readbackTexture);
            _readbackTexture = null;
        }
        if (_maskReadbackTexture != null)
        {
            Destroy(_maskReadbackTexture);
            _maskReadbackTexture = null;
        }
    }

    public void UpdateColorPercent()
    {
        if (targetRawImage == null)
        {
            detectedPercent = 0f;
            debugTotalPixels = 0;
            debugMatchedPixels = 0;
            return;
        }

        Texture sourceTexture = GetRawImageTexture(targetRawImage);
        if (sourceTexture == null)
        {
            detectedPercent = 0f;
            debugTotalPixels = 0;
            debugMatchedPixels = 0;
            if (outputText != null) outputText.text = string.Format(outputFormat, detectedPercent);
            return;
        }

        Color32[] pixels = null;
        int width = 0;
        int height = 0;

        if (!TryGetPixels(sourceTexture, ref _readbackTexture, out pixels, out width, out height))
        {
            detectedPercent = 0f;
            debugTotalPixels = 0;
            debugMatchedPixels = 0;
            if (outputText != null) outputText.text = string.Format(outputFormat, detectedPercent);
            return;
        }

        if (pixels == null || pixels.Length == 0 || width <= 0 || height <= 0)
        {
            detectedPercent = 0f;
            debugTotalPixels = 0;
            debugMatchedPixels = 0;
            if (outputText != null) outputText.text = string.Format(outputFormat, detectedPercent);
            return;
        }

        Color32[] maskPixels = null;
        int maskWidth = 0;
        int maskHeight = 0;
        int maskXMin = 0;
        int maskXMax = 0;
        int maskYMin = 0;
        int maskYMax = 0;
        bool useMask = TryGetMaskPixels(out maskPixels, out maskWidth, out maskHeight, out maskXMin, out maskXMax, out maskYMin, out maskYMax);

        Rect uvRect = targetRawImage.uvRect;
        float u0 = Mathf.Clamp01(Mathf.Min(uvRect.xMin, uvRect.xMax));
        float u1 = Mathf.Clamp01(Mathf.Max(uvRect.xMin, uvRect.xMax));
        float v0 = Mathf.Clamp01(Mathf.Min(uvRect.yMin, uvRect.yMax));
        float v1 = Mathf.Clamp01(Mathf.Max(uvRect.yMin, uvRect.yMax));

        int xMin = Mathf.Clamp(Mathf.FloorToInt(u0 * (width - 1)), 0, width - 1);
        int xMax = Mathf.Clamp(Mathf.CeilToInt(u1 * (width - 1)), 0, width - 1);
        int yMin = Mathf.Clamp(Mathf.FloorToInt(v0 * (height - 1)), 0, height - 1);
        int yMax = Mathf.Clamp(Mathf.CeilToInt(v1 * (height - 1)), 0, height - 1);

        float tr = targetColor.r;
        float tg = targetColor.g;
        float tb = targetColor.b;
        float toleranceSqr = tolerance * tolerance;
        int regionWidth = Mathf.Max(1, xMax - xMin);
        int regionHeight = Mathf.Max(1, yMax - yMin);
        int maskRegionWidth = Mathf.Max(1, maskXMax - maskXMin);
        int maskRegionHeight = Mathf.Max(1, maskYMax - maskYMin);

        int total = 0;
        int matched = 0;
        int step = Mathf.Max(1, sampleStep);

        for (int y = yMin; y <= yMax; y += step)
        {
            int row = y * width;
            for (int x = xMin; x <= xMax; x += step)
            {
                if (useMask)
                {
                    float un = (x - xMin) / (float)regionWidth;
                    float vn = (y - yMin) / (float)regionHeight;
                    int mx = Mathf.Clamp(maskXMin + Mathf.RoundToInt(maskRegionWidth * un), 0, maskWidth - 1);
                    int my = Mathf.Clamp(maskYMin + Mathf.RoundToInt(maskRegionHeight * vn), 0, maskHeight - 1);
                    Color32 mp = maskPixels[my * maskWidth + mx];
                    float ma = mp.a / 255f;
                    if (ma < maskMinAlpha) continue;
                }

                Color32 p = pixels[row + x];
                float a = p.a / 255f;
                if (a < minAlpha) continue;

                total++;

                float pr = p.r / 255f;
                float pg = p.g / 255f;
                float pb = p.b / 255f;

                if (checkBlackOnly)
                {
                    float luma = pr * 0.299f + pg * 0.587f + pb * 0.114f;
                    if (luma <= blackLumaThreshold)
                    {
                        matched++;
                    }
                }
                else
                {
                    float dr = pr - tr;
                    float dg = pg - tg;
                    float db = pb - tb;
                    float distSqr = dr * dr + dg * dg + db * db;

                    if (distSqr <= toleranceSqr)
                    {
                        matched++;
                    }
                }
            }
        }

        detectedPercent = total > 0 ? (matched * 100f) / total : 0f;
        debugTotalPixels = total;
        debugMatchedPixels = matched;

        if (outputText != null)
        {
            outputText.text = string.Format(outputFormat, detectedPercent);
        }
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

        if (texture is Texture2D tex2D)
        {
            width = tex2D.width;
            height = tex2D.height;
            if (width <= 0 || height <= 0) return false;

            try
            {
                pixels = tex2D.GetPixels32();
                return pixels != null && pixels.Length > 0;
            }
            catch
            {
                return TryReadByBlit(texture, ref readbackCache, out pixels, out width, out height);
            }
        }

        if (texture is RenderTexture rt)
        {
            return TryReadRenderTexture(rt, ref readbackCache, out pixels, out width, out height);
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

    bool TryReadRenderTexture(RenderTexture rt, ref Texture2D readbackCache, out Color32[] pixels, out int width, out int height)
    {
        pixels = null;
        width = rt.width;
        height = rt.height;
        if (width <= 0 || height <= 0) return false;

        if (readbackCache == null || readbackCache.width != width || readbackCache.height != height)
        {
            if (readbackCache != null) Destroy(readbackCache);
            readbackCache = new Texture2D(width, height, TextureFormat.RGBA32, false);
        }

        RenderTexture prev = RenderTexture.active;
        try
        {
            RenderTexture.active = rt;
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
            RenderTexture.active = prev;
        }
    }

    bool TryCalibrateTargetColor()
    {
        Texture sourceTexture = GetRawImageTexture(targetRawImage);
        if (targetRawImage == null || sourceTexture == null) return false;

        Color32[] pixels;
        int width;
        int height;
        if (!TryGetPixels(sourceTexture, ref _readbackTexture, out pixels, out width, out height)) return false;

        Color32[] maskPixels = null;
        int maskWidth = 0;
        int maskHeight = 0;
        int maskXMin = 0;
        int maskXMax = 0;
        int maskYMin = 0;
        int maskYMax = 0;
        bool useMask = TryGetMaskPixels(out maskPixels, out maskWidth, out maskHeight, out maskXMin, out maskXMax, out maskYMin, out maskYMax);

        Rect uvRect = targetRawImage.uvRect;
        float u0 = Mathf.Clamp01(Mathf.Min(uvRect.xMin, uvRect.xMax));
        float u1 = Mathf.Clamp01(Mathf.Max(uvRect.xMin, uvRect.xMax));
        float v0 = Mathf.Clamp01(Mathf.Min(uvRect.yMin, uvRect.yMax));
        float v1 = Mathf.Clamp01(Mathf.Max(uvRect.yMin, uvRect.yMax));

        int xMin = Mathf.Clamp(Mathf.FloorToInt(u0 * (width - 1)), 0, width - 1);
        int xMax = Mathf.Clamp(Mathf.CeilToInt(u1 * (width - 1)), 0, width - 1);
        int yMin = Mathf.Clamp(Mathf.FloorToInt(v0 * (height - 1)), 0, height - 1);
        int yMax = Mathf.Clamp(Mathf.CeilToInt(v1 * (height - 1)), 0, height - 1);

        int regionWidth = Mathf.Max(1, xMax - xMin);
        int regionHeight = Mathf.Max(1, yMax - yMin);
        int maskRegionWidth = Mathf.Max(1, maskXMax - maskXMin);
        int maskRegionHeight = Mathf.Max(1, maskYMax - maskYMin);

        int step = Mathf.Max(1, sampleStep);
        float sumR = 0f;
        float sumG = 0f;
        float sumB = 0f;
        int count = 0;

        for (int y = yMin; y <= yMax; y += step)
        {
            int row = y * width;
            for (int x = xMin; x <= xMax; x += step)
            {
                if (useMask)
                {
                    float un = (x - xMin) / (float)regionWidth;
                    float vn = (y - yMin) / (float)regionHeight;
                    int mx = Mathf.Clamp(maskXMin + Mathf.RoundToInt(maskRegionWidth * un), 0, maskWidth - 1);
                    int my = Mathf.Clamp(maskYMin + Mathf.RoundToInt(maskRegionHeight * vn), 0, maskHeight - 1);
                    Color32 mp = maskPixels[my * maskWidth + mx];
                    if (mp.a / 255f < maskMinAlpha) continue;
                }

                Color32 p = pixels[row + x];
                if (p.a / 255f < minAlpha) continue;

                sumR += p.r / 255f;
                sumG += p.g / 255f;
                sumB += p.b / 255f;
                count++;
            }
        }

        if (count <= 0) return false;

        targetColor = new Color(sumR / count, sumG / count, sumB / count, 1f);
        return true;
    }

    Texture GetRawImageTexture(RawImage rawImage)
    {
        if (rawImage == null) return null;
        if (rawImage.texture != null) return rawImage.texture;
        if (rawImage.material != null) return rawImage.material.mainTexture;
        return null;
    }

    bool TryGetMaskPixels(out Color32[] maskPixels, out int maskWidth, out int maskHeight, out int maskXMin, out int maskXMax, out int maskYMin, out int maskYMax)
    {
        maskPixels = null;
        maskWidth = 0;
        maskHeight = 0;
        maskXMin = 0;
        maskXMax = 0;
        maskYMin = 0;
        maskYMax = 0;

        Texture rawMaskTexture = GetRawImageTexture(areaMaskRawImage);
        if (areaMaskRawImage != null && rawMaskTexture != null)
        {
            if (!TryGetPixels(rawMaskTexture, ref _maskReadbackTexture, out maskPixels, out maskWidth, out maskHeight)) return false;

            Rect uvRect = areaMaskRawImage.uvRect;
            float u0 = Mathf.Clamp01(Mathf.Min(uvRect.xMin, uvRect.xMax));
            float u1 = Mathf.Clamp01(Mathf.Max(uvRect.xMin, uvRect.xMax));
            float v0 = Mathf.Clamp01(Mathf.Min(uvRect.yMin, uvRect.yMax));
            float v1 = Mathf.Clamp01(Mathf.Max(uvRect.yMin, uvRect.yMax));

            maskXMin = Mathf.Clamp(Mathf.FloorToInt(u0 * (maskWidth - 1)), 0, maskWidth - 1);
            maskXMax = Mathf.Clamp(Mathf.CeilToInt(u1 * (maskWidth - 1)), 0, maskWidth - 1);
            maskYMin = Mathf.Clamp(Mathf.FloorToInt(v0 * (maskHeight - 1)), 0, maskHeight - 1);
            maskYMax = Mathf.Clamp(Mathf.CeilToInt(v1 * (maskHeight - 1)), 0, maskHeight - 1);
            return true;
        }

        if (areaMaskImage != null && areaMaskImage.sprite != null)
        {
            Sprite sprite = areaMaskImage.sprite;
            if (!TryGetPixels(sprite.texture, ref _maskReadbackTexture, out maskPixels, out maskWidth, out maskHeight)) return false;

            Rect textureRect = sprite.textureRect;
            maskXMin = Mathf.Clamp(Mathf.FloorToInt(textureRect.xMin), 0, maskWidth - 1);
            maskXMax = Mathf.Clamp(Mathf.CeilToInt(textureRect.xMax) - 1, 0, maskWidth - 1);
            maskYMin = Mathf.Clamp(Mathf.FloorToInt(textureRect.yMin), 0, maskHeight - 1);
            maskYMax = Mathf.Clamp(Mathf.CeilToInt(textureRect.yMax) - 1, 0, maskHeight - 1);
            return true;
        }

        return false;
    }
}
