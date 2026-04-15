using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class TutorialAcCheck : AcCheck
{

    [Header("타겟 텍스처")]
    [Tooltip("타겟 마스크 텍스처. 알파가 임계값(threshold)보다 큰 픽을 채워진 영역(도형)으로 간주합니다. 임포트 설정에서 알파 포함 및 Read/Write 활성화 필요.")]
    public Texture2D _defaultTexture;

    [Tooltip("클리어 시 표시할 선택적 텍스처입니다. 보통 투명하거나 빈 텍스처를 사용합니다.")]
    public Texture2D _clearTexture;

    [Header("돌출(삐져나옴) 설정")]
    [Tooltip("평가를 수행하기 위한 최소 겹침 비율(overlapOnTarget / targetFilled). 예: 0.2 = 20%. 이 값보다 낮으면 평가를 실패로 처리합니다.")]
    public float minOverlapRatio = 0.2f;

    RawImage _rawImage;

    public CameraValue _CameraValue;


    void Awake()
    {
        _rawImage = GetComponent<RawImage>();
    }

    // public void ShowCheckTexture()
    // {
    //     _rawImage.texture = _defaultTexture;
    //     FadeManager.Instance.SetAlphaOne(_rawImage);
    // }

    public void DisableRawImage()
    {
        FadeManager.Instance.SetAlphaZero(_rawImage);
    }




    public override void StartCheck()
    {
        _rawImage.texture = _defaultTexture;

        StartCoroutine(DelayToCheckStart());

        _isClear = false;

        Debug.Log("AcCheck - StartCheck: " + CurrentDirection);

        if (shadowMaskContainer != null)
        {
            shadowMaskContainer.CurrentIndex = 0;
        }

        if (outputText != null)
        {
            FadeManager.Instance.SetAlphaOne(outputText);
        }

        FadeManager.Instance.SetAlphaOne(targetRawImage);

    }


    protected override IEnumerator DelayOnClear()
    {

        _rawImage.texture = _clearTexture;

        yield return base.DelayOnClear();

    }


    // Compute how much the overlay (e.g. webcam/result) protrudes outside the filled area of targetMask.
    // Return values:
    //  -1 => target has no filled pixels
    //  -2 => overlap ratio below minOverlapRatio (insufficient overlap to evaluate)
    //  -3 => overlay texture not available or not Texture2D
    //  0..1 => score (1 == no protrusion, 0 == fully protruding)
    public float ComputeProtrusionScore(Texture2D targetMask, Texture2D overlay, float alphaThreshold = 0.1f, float minOverlapRatio = 0.2f, bool useBilinear = true, float minLuma = 0.02f, bool verbose = true)
    {
        if (targetMask == null) return -1f;
        if (overlay == null) return -3f;

        int tw = targetMask.width;
        int th = targetMask.height;
        if (tw == 0 || th == 0) return -1f;

        Color32[] tPixels = targetMask.GetPixels32();

        int targetFilled = 0;
        int overlapOnTarget = 0;
        int protruding = 0;

        // Use bilinear sampling from overlay when requested to avoid integer mapping errors
        for (int y = 0; y < th; y++)
        {
            int rowT = y * tw;
            float v = (y + 0.5f) / (float)th;
            for (int x = 0; x < tw; x++)
            {
                int idxT = rowT + x;
                Color32 ct = tPixels[idxT];
                bool tFilled = (ct.a / 255f) > alphaThreshold;

                Color co;
                if (useBilinear)
                {
                    float u = (x + 0.5f) / (float)tw;
                    co = overlay.GetPixelBilinear(u, v);
                }
                else
                {
                    int ox = Mathf.Clamp(Mathf.FloorToInt(x * ((float)overlay.width / tw)), 0, overlay.width - 1);
                    int oy = Mathf.Clamp(Mathf.FloorToInt(y * ((float)overlay.height / th)), 0, overlay.height - 1);
                    Color32 oc = overlay.GetPixels32()[oy * overlay.width + ox];
                    co = new Color(oc.r / 255f, oc.g / 255f, oc.b / 255f, oc.a / 255f);
                }

                // Visibility: require both alpha and some luminance to count as visible
                float luma = 0.299f * co.r + 0.587f * co.g + 0.114f * co.b;
                // Use OR: visible if either alpha present or sufficient luminance (covers WebCamTexture with no alpha)
                bool oVisible = (co.a > alphaThreshold) || (luma > minLuma);

                if (tFilled)
                {
                    targetFilled++;
                    if (oVisible) overlapOnTarget++;
                }
                else
                {
                    if (oVisible) protruding++;
                }
            }
        }

        if (verbose) Debug.Log($"[TutorialAcCheck] Protrusion calc: targetFilled={targetFilled}, overlap={overlapOnTarget}, protruding={protruding}, target={tw}x{th}, overlay={overlay.width}x{overlay.height}");

        if (targetFilled == 0) return -1f;
        float overlapRatio = (float)overlapOnTarget / (float)targetFilled;
        if (overlapRatio < minOverlapRatio)
        {
            if (verbose) Debug.LogWarning($"[TutorialAcCheck] Insufficient overlap (ratio={overlapRatio:F3}) < minOverlapRatio={minOverlapRatio}. Treating as fail.");
            return 0f; // fail: not enough overlap to evaluate reliably
        }

        if (overlapOnTarget == 0)
        {
            // If there are no overlapping pixels inside the target but there are protruding pixels,
            // treat as bad (0). If neither overlap nor protrusion, treat as perfect (1).
            return (protruding > 0) ? 0f : 1f;
        }

        float score = 1f - ((float)protruding / (float)overlapOnTarget);
        return Mathf.Clamp01(score);
    }

    // Helper: use _defaultTexture as target mask and the current RawImage.texture as overlay (supports Texture2D, WebCamTexture, RenderTexture)
    public float ComputeProtrusionScoreUsingRawDefault(float alphaThreshold = 0.1f, bool useBilinear = true, float minLuma = 0.02f, bool verbose = true)
    {
        if (_defaultTexture == null)
        {
            if (verbose) Debug.LogWarning("ComputeProtrusionScoreUsingRawDefault: _defaultTexture is null");
            return -1f;
        }
        if (_rawImage == null || _rawImage.texture == null)
        {
            if (verbose) Debug.LogWarning("ComputeProtrusionScoreUsingRawDefault: RawImage or its texture is null");
            return -3f;
        }

        Texture overlayTex = _rawImage.texture;
        Texture2D temp = null;
        Texture2D overlay = null;

        if (overlayTex is Texture2D t2)
        {
            overlay = t2;
        }
        else if (overlayTex is WebCamTexture wct)
        {
            if (wct.width <= 16 || wct.height <= 16)
            {
                if (verbose) Debug.LogWarning("ComputeProtrusion: WebCamTexture not ready (small size)");
                return -3f;
            }
            try
            {
                Color32[] px = wct.GetPixels32();
                temp = new Texture2D(wct.width, wct.height, TextureFormat.RGBA32, false);
                temp.SetPixels32(px);
                temp.Apply();
                overlay = temp;
            }
            catch (System.Exception ex)
            {
                if (verbose) Debug.LogWarning("ComputeProtrusion: failed to copy WebCamTexture: " + ex.Message);
                if (temp != null) Destroy(temp);
                return -3f;
            }
        }
        else if (overlayTex is RenderTexture rt)
        {
            temp = new Texture2D(rt.width, rt.height, TextureFormat.RGBA32, false);
            RenderTexture prev = RenderTexture.active;
            RenderTexture.active = rt;
            temp.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            temp.Apply();
            RenderTexture.active = prev;
            overlay = temp;
        }
        else
        {
            if (verbose) Debug.LogWarning("ComputeProtrusionScoreUsingRawDefault: RawImage.texture type not supported: " + overlayTex.GetType());
            return -3f;
        }

        float result = ComputeProtrusionScore(_defaultTexture, overlay, alphaThreshold, this.minOverlapRatio, useBilinear, minLuma, verbose);

        if (temp != null) Destroy(temp);
        return result;
    }

}
