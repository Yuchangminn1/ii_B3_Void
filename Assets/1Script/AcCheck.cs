using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class AcCheck : MonoBehaviour
{
    [Header("Base")]
    [Tooltip("기준이 되는 RawImage. 비워두면 같은 오브젝트에서 자동 검색합니다.")]
    public RawImage targetRawImage;

    [Header("Overlay")]
    [Tooltip("기준 이미지 안에 들어와야 하는 타겟 RawImage")]
    public RawImage overlayRawImage;

    public Direction CurrentDirection;

    [Header("Threshold")]
    [Tooltip("기준 텍스처에서 이 값보다 투명하면 무시")]
    [SerializeField, Range(0f, 1f)] float baseMinAlpha = 0.01f;
    [Tooltip("겹쳐진 텍스처에서 이 값보다 투명하면 덮이지 않은 것으로 처리")]
    [SerializeField, Range(0f, 1f)] float overlayMinAlpha = 0.01f;
    [Tooltip("타겟(overlay)에서 검은 영역만 계산에 사용")]
    [SerializeField] bool overlayBlackOnly = true;
    [Tooltip("검정 판정 밝기 임계값. 낮을수록 더 어두운 픽셀만 통과")]
    [SerializeField, Range(0f, 0.5f)] float blackLumaThreshold = 0.15f;
    [Tooltip("검정 RGB 임계값. luma 대신 RGB 상한으로도 통과 판정")]
    [SerializeField, Range(0, 255)] int blackRgbThreshold = 70;

    [Header("Update")]
    [Tooltip("분석 주기(초)")]
    [SerializeField, Range(0.5f, 1f)] float updateInterval = 0.5f;
    [Tooltip("샘플링 간격. 1이면 더 정확하지만 무거울 수 있습니다.")]
    [SerializeField, Range(1, 16)] int sampleStep = 2;

    [Header("Output")]
    [Tooltip("타겟 텍스처의 유효 알파 영역 중 기준 이미지 안에 들어온 비율(%)")]
    [SerializeField, Range(0f, 100f)] float detectedPercent;
    [Tooltip("분모로 계산된 타겟 픽셀 수")]
    [SerializeField] int debugTotalPixels;
    [Tooltip("기준 이미지 안에 들어온 타겟 픽셀 수")]
    [SerializeField] int debugMatchedPixels;
    [Tooltip("결과를 표시할 UI Text (선택)")]
    [SerializeField] Text outputText;
    [Tooltip("텍스트 포맷 예시: {0:F1}%")]
    [SerializeField] string outputFormat = "{0:F1}%";


    float modifier = 0f;





    float _nextUpdateTime;
    Texture2D _baseReadbackTexture;
    Texture2D _overlayReadbackTexture;

    public float DetectedPercent => detectedPercent;
    public int DebugTotalPixels => debugTotalPixels;
    public int DebugMatchedPixels => debugMatchedPixels;

    bool _isCheck = false;

    Action onClear;

    void Awake()
    {
        if (targetRawImage == null)
        {
            targetRawImage = GetComponent<RawImage>();
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            if (CurrentDirection == Direction.Left)
            {
                DebugClear();
                FadeManager.Instance.SetAlphaZero(outputText);
            }
        }
        else if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            if (CurrentDirection == Direction.Right)
            {
                DebugClear();
                FadeManager.Instance.SetAlphaZero(outputText);
            }
        }

        if (_isCheck == false)
            return;
        if (Time.unscaledTime < _nextUpdateTime) return;

        _nextUpdateTime = Time.unscaledTime + Mathf.Max(0.5f, updateInterval);
        UpdateColorPercent();
    }

    public void StartCheck()
    {
        _isCheck = true;
        FadeManager.Instance.SetAlphaOne(outputText);
        Debug.Log($"{name}Start Check");
    }

    public void StopCheck()
    {
        _isCheck = false;
        FadeManager.Instance.SetAlphaZero(outputText);
        Debug.Log($"{name}Stop Check");


    }

    public void SetTargetRawImage(RawImage rawImage)
    {
        targetRawImage = rawImage;
    }


    void OnDestroy()
    {
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

    public void UpdateColorPercent()
    {
        if (targetRawImage == null || overlayRawImage == null)
        {
            ResetResult();
            return;
        }

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

        int overlayXMin;
        int overlayXMax;
        int overlayYMin;
        int overlayYMax;
        GetTextureBounds(overlayRawImage.uvRect, overlayWidth, overlayHeight, out overlayXMin, out overlayXMax, out overlayYMin, out overlayYMax);

        int total = 0;
        int matched = 0;
        int step = Mathf.Max(1, sampleStep);
        int regionWidth = Mathf.Max(1, overlayXMax - overlayXMin);
        int regionHeight = Mathf.Max(1, overlayYMax - overlayYMin);
        RectTransform overlayRectTransform = overlayRawImage.rectTransform;
        Rect overlayRect = overlayRectTransform.rect;

        for (int y = overlayYMin; y <= overlayYMax; y += step)
        {
            int row = y * overlayWidth;
            float normalizedY = (y - overlayYMin) / (float)regionHeight;
            float localY = Mathf.Lerp(overlayRect.yMin, overlayRect.yMax, normalizedY);

            for (int x = overlayXMin; x <= overlayXMax; x += step)
            {
                Color32 overlayPixel = overlayPixels[row + x];
                if (!IsOverlayTargetPixel(overlayPixel)) continue;

                total++;

                float normalizedX = (x - overlayXMin) / (float)regionWidth;
                float localX = Mathf.Lerp(overlayRect.xMin, overlayRect.xMax, normalizedX);
                Vector3 worldPoint = overlayRectTransform.TransformPoint(new Vector3(localX, localY, 0f));

                Color32 sampledBasePixel;
                if (!TrySampleRawImageAtWorldPoint(targetRawImage, basePixels, baseWidth, baseHeight, worldPoint, out sampledBasePixel)) continue;
                if (!HasVisibleAlpha(sampledBasePixel, baseMinAlpha)) continue;

                matched++;
            }
        }

        detectedPercent = total > 0 ? (matched * 100f / total) + modifier : 0f;
        debugTotalPixels = total;
        debugMatchedPixels = matched;

        if (outputText != null)
        {
            outputText.text = string.Format(outputFormat, detectedPercent);
        }

        if (detectedPercent >= 100f)
        {
            StopCheck();

            StartCoroutine(DelayOnClear());
        }
    }

    IEnumerator DelayOnClear()
    {
        yield return CoroutineReturnManager.GetWaitForSeconds(1f);
        onClear?.Invoke();
    }



    public void DebugClear()
    {
        StopCheck();
        detectedPercent = 100f;
        outputText.text = string.Format(outputFormat, detectedPercent);
        onClear?.Invoke();
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

    bool TrySampleRawImageAtWorldPoint(RawImage rawImage, Color32[] pixels, int width, int height, Vector3 worldPoint, out Color32 pixel)
    {
        pixel = default;

        if (rawImage == null || pixels == null || pixels.Length == 0 || width <= 0 || height <= 0)
        {
            return false;
        }

        RectTransform rectTransform = rawImage.rectTransform;
        Vector2 localPoint = rectTransform.InverseTransformPoint(worldPoint);
        Rect rect = rectTransform.rect;
        if (!rect.Contains(localPoint)) return false;

        float normalizedX = Mathf.InverseLerp(rect.xMin, rect.xMax, localPoint.x);
        float normalizedY = Mathf.InverseLerp(rect.yMin, rect.yMax, localPoint.y);

        Rect uvRect = rawImage.uvRect;
        float u = Mathf.Lerp(uvRect.xMin, uvRect.xMax, normalizedX);
        float v = Mathf.Lerp(uvRect.yMin, uvRect.yMax, normalizedY);

        int x = Mathf.Clamp(Mathf.RoundToInt(u * (width - 1)), 0, width - 1);
        int y = Mathf.Clamp(Mathf.RoundToInt(v * (height - 1)), 0, height - 1);
        pixel = pixels[y * width + x];
        return true;
    }

    bool TryGetPixelsFromRawImage(RawImage rawImage, ref Texture2D readbackCache, out Color32[] pixels, out int width, out int height)
    {
        pixels = null;
        width = 0;
        height = 0;

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
}
