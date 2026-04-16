using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SaveShadowTexture : MonoBehaviour
{
    static readonly int ShaderPropTargetMaskEnabled = Shader.PropertyToID("_TargetMaskEnabled");

    RenderTexture _snapshotRt;
    Material _snapshotMaterial;
    Texture2D _maskReadbackTexture;

    const int MaxTextureCount = 10;

    int _currentTextureIndex = 0;
    int _capturedCount = 0;

    public Texture[] textures = new Texture[MaxTextureCount];
    readonly Vector3[] _capturedLocalPositions = new Vector3[MaxTextureCount];
    readonly Vector2[] _capturedSizeDeltas = new Vector2[MaxTextureCount];
    readonly Quaternion[] _capturedLocalRotations = new Quaternion[MaxTextureCount];
    readonly Vector3[] _capturedLocalScales = new Vector3[MaxTextureCount];

    public int CurrentIndex = 0;
    public CameraValue cameraValue;

    Quaternion _rectTransformRotation;

    RawImage _rawImage;


    void Start()
    {
        _rawImage = GetComponent<RawImage>();
        _rectTransformRotation = _rawImage.rectTransform.localRotation;

        PageController.Instance.OnReset += Reset;

    }

    public void Reset()
    {
        _currentTextureIndex = 0;
        _capturedCount = 0;
        ReleaseSnapshotResources();

        for (int i = 0; i < MaxTextureCount; i++)
        {
            _capturedLocalPositions[i] = Vector3.zero;
            _capturedSizeDeltas[i] = Vector2.zero;
            _capturedLocalRotations[i] = Quaternion.identity;
            _capturedLocalScales[i] = Vector3.one;
        }
    }

    private void OnDestroy()
    {
        if (PageController.Instance != null)
        {
            PageController.Instance.OnReset -= Reset;
        }

        ReleaseSnapshotResources();
    }

    private void ReleaseSnapshotResources()
    {
        for (int i = 0; i < textures.Length; i++)
        {
            if (textures[i] != null)
            {
                Destroy(textures[i]);
                textures[i] = null;
            }
        }

        if (_snapshotRt != null)
        {
            _snapshotRt.Release();
            Destroy(_snapshotRt);
            _snapshotRt = null;
        }

        if (_snapshotMaterial != null)
        {
            Destroy(_snapshotMaterial);
            _snapshotMaterial = null;
        }

        if (_maskReadbackTexture != null)
        {
            Destroy(_maskReadbackTexture);
            _maskReadbackTexture = null;
        }
    }



    public void SetTexture(RawImage sourceRawImage)
    {
        SetTexture(sourceRawImage, null, 0.01f);
    }

    public void SetTexture(RawImage sourceRawImage, RawImage maskTargetRawImage, float maskMinAlpha)
    {
        RawImage targetRawImage = _rawImage != null ? _rawImage : GetComponent<RawImage>();
        _rawImage = targetRawImage;


        if (sourceRawImage == null || targetRawImage == null)
        {
            Debug.LogError($"1텍스처 재사용: ");

            return;
        }

        RectTransform sourceRectTransform = sourceRawImage.rectTransform;
        RectTransform targetRectTransform = targetRawImage.rectTransform;
        if (sourceRectTransform != null && targetRectTransform != null)
        {
            targetRectTransform.localRotation = _rectTransformRotation * sourceRectTransform.localRotation;
        }

        Texture texture = sourceRawImage.texture;
        if (texture == null)
        {
            ReleaseSnapshotResources();
            targetRawImage.texture = null;
            Debug.LogError($"2텍스처 재사용: ");

            return;
        }

        Vector2Int targetSize = GetRawImagePixelSize(targetRawImage);
        int targetWidth = targetSize.x;
        int targetHeight = targetSize.y;
        if (targetWidth <= 0 || targetHeight <= 0)
        {
            Debug.LogError($"3텍스처 재사용: ");

            return;
        }

        EnsureSnapshotTexture(targetWidth, targetHeight);
        EnsureSnapshotRenderTarget(targetWidth, targetHeight);

        if (cameraValue != null)
        {
            cameraValue.ApplyMaterialProperties();
        }
        int writeIndex = _currentTextureIndex;
        //% MaxTextureCount;
        Texture2D capturedTexture = textures[writeIndex] as Texture2D;
        if (capturedTexture == null)
        {
            capturedTexture = new Texture2D(targetWidth, targetHeight, TextureFormat.RGBA32, false);
            textures[writeIndex] = capturedTexture;
            Debug.LogError($"텍스처 생성: {writeIndex}");
        }
        else
        {

            Debug.LogError($"텍스처 재사용: {writeIndex}");
        }

        if (targetRectTransform != null)
        {
            _capturedLocalPositions[writeIndex] = targetRectTransform.localPosition;
            _capturedSizeDeltas[writeIndex] = targetRectTransform.sizeDelta;
            _capturedLocalRotations[writeIndex] = targetRectTransform.localRotation;
            _capturedLocalScales[writeIndex] = targetRectTransform.localScale;
        }
        else
        {
            _capturedLocalPositions[writeIndex] = Vector3.zero;
            _capturedSizeDeltas[writeIndex] = Vector2.zero;
            _capturedLocalRotations[writeIndex] = Quaternion.identity;
            _capturedLocalScales[writeIndex] = Vector3.one;
        }

        Material sourceMaterial = GetSnapshotMaterial(sourceRawImage.material);

        RenderTexture previous = RenderTexture.active;

        try
        {
            RenderTexture.active = _snapshotRt;
            GL.Clear(true, true, Color.clear);

            if (sourceMaterial != null)
            {
                Graphics.Blit(texture, _snapshotRt, sourceMaterial);
            }
            else
            {
                Graphics.Blit(texture, _snapshotRt);
            }

            capturedTexture.ReadPixels(new Rect(0, 0, targetWidth, targetHeight), 0, 0);

            RawImage resolvedMaskTarget = maskTargetRawImage;
            float resolvedMaskMinAlpha = Mathf.Clamp01(maskMinAlpha);
            if (resolvedMaskTarget == null)
            {
                TryResolveMaskTarget(sourceRawImage, out resolvedMaskTarget, out resolvedMaskMinAlpha);
            }

            if (resolvedMaskTarget != null)
            {
                ApplyTargetMask(capturedTexture, sourceRawImage, resolvedMaskTarget, resolvedMaskMinAlpha);
            }
            else
            {
                capturedTexture.Apply(false, false);
            }
        }
        finally
        {
            RenderTexture.active = previous;
        }

        targetRawImage.texture = capturedTexture;
        _currentTextureIndex++;
        _capturedCount = Mathf.Min(_capturedCount + 1, MaxTextureCount);
    }

    public int CapturedCount
    {
        get { return _capturedCount; }
    }

    public Texture GetCapturedTexture(int index)
    {
        if (index < 0 || index >= _capturedCount)
        {
            return null;
        }

        return textures[index];
    }

    public bool TryGetCapturedTransform(int index, out Vector3 localPosition, out Vector2 sizeDelta, out Quaternion localRotation, out Vector3 localScale)
    {
        if (index < 0 || index >= _capturedCount)
        {
            localPosition = Vector3.zero;
            sizeDelta = Vector2.zero;
            localRotation = Quaternion.identity;
            localScale = Vector3.one;
            return false;
        }

        localPosition = _capturedLocalPositions[index];
        sizeDelta = _capturedSizeDeltas[index];
        localRotation = _capturedLocalRotations[index];
        localScale = _capturedLocalScales[index];
        return true;
    }

    public bool TryGetCurrentRectTransform(out Vector3 localPosition, out Vector2 sizeDelta, out Quaternion localRotation, out Vector3 localScale)
    {
        RawImage targetRawImage = _rawImage != null ? _rawImage : GetComponent<RawImage>();
        _rawImage = targetRawImage;
        if (targetRawImage == null || targetRawImage.rectTransform == null)
        {
            localPosition = Vector3.zero;
            sizeDelta = Vector2.zero;
            localRotation = Quaternion.identity;
            localScale = Vector3.one;
            return false;
        }

        RectTransform rectTransform = targetRawImage.rectTransform;
        localPosition = rectTransform.localPosition;
        sizeDelta = rectTransform.sizeDelta;
        localRotation = rectTransform.localRotation;
        localScale = rectTransform.localScale;
        return true;
    }

    private Vector2Int GetRawImagePixelSize(RawImage rawImage)
    {
        RectTransform rectTransform = rawImage.rectTransform;
        if (rectTransform == null)
        {
            return new Vector2Int(0, 0);
        }

        Vector2 size = rectTransform.rect.size;
        Vector3 lossyScale = rectTransform.lossyScale;
        Canvas canvas = rawImage.canvas;
        float canvasScale = canvas != null ? Mathf.Max(0.01f, canvas.scaleFactor) : 1f;

        int width = Mathf.Max(1, Mathf.RoundToInt(Mathf.Abs(size.x * lossyScale.x) * canvasScale));
        int height = Mathf.Max(1, Mathf.RoundToInt(Mathf.Abs(size.y * lossyScale.y) * canvasScale));
        return new Vector2Int(width, height);
    }

    private void EnsureSnapshotTexture(int width, int height)
    {
        for (int i = 0; i < textures.Length; i++)
        {
            Texture2D texture = textures[i] as Texture2D;
            if (texture != null && (texture.width != width || texture.height != height))
            {
                Destroy(texture);
                textures[i] = null;
            }
        }
    }

    private void EnsureSnapshotRenderTarget(int width, int height)
    {
        if (_snapshotRt != null && _snapshotRt.width == width && _snapshotRt.height == height)
        {
            return;
        }

        if (_snapshotRt != null)
        {
            _snapshotRt.Release();
            Destroy(_snapshotRt);
        }

        _snapshotRt = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32);
        _snapshotRt.Create();
    }

    private bool CanUseFastGpuCopy(Texture2D source, Texture2D destination)
    {
        if (!SystemInfo.copyTextureSupport.Equals(UnityEngine.Rendering.CopyTextureSupport.None)
            && source != null
            && destination != null
            && source.width == destination.width
            && source.height == destination.height
            && source.format == destination.format)
        {
            return true;
        }

        return false;
    }

    private void ApplyTargetMask(Texture2D capturedTexture, RawImage sourceRawImage, RawImage maskTargetRawImage, float maskMinAlpha)
    {
        if (capturedTexture == null || sourceRawImage == null || maskTargetRawImage == null)
        {
            capturedTexture?.Apply(false, false);
            return;
        }

        RectTransform sourceRectTransform = sourceRawImage.rectTransform;
        RectTransform maskRectTransform = maskTargetRawImage.rectTransform;
        if (sourceRectTransform == null || maskRectTransform == null)
        {
            capturedTexture.Apply(false, false);
            return;
        }

        Texture maskTexture = GetRawImageTexture(maskTargetRawImage);
        if (!TryGetPixels(maskTexture, ref _maskReadbackTexture, out Color32[] maskPixels, out int maskWidth, out int maskHeight))
        {
            capturedTexture.Apply(false, false);
            return;
        }

        Rect sourceRect = sourceRectTransform.rect;
        Rect maskRect = maskRectTransform.rect;
        if (Mathf.Abs(sourceRect.width) <= Mathf.Epsilon || Mathf.Abs(sourceRect.height) <= Mathf.Epsilon || Mathf.Abs(maskRect.width) <= Mathf.Epsilon || Mathf.Abs(maskRect.height) <= Mathf.Epsilon)
        {
            capturedTexture.Apply(false, false);
            return;
        }

        Rect sourceUvRect = sourceRawImage.uvRect;
        Rect maskUvRect = maskTargetRawImage.uvRect;
        Color32[] capturedPixels = capturedTexture.GetPixels32();
        int capturedWidth = capturedTexture.width;
        int capturedHeight = capturedTexture.height;

        for (int y = 0; y < capturedHeight; y++)
        {
            float sourceV = capturedHeight > 1 ? (float)y / (capturedHeight - 1) : 0.5f;
            float sourceLocalY = Mathf.Lerp(sourceRect.yMin, sourceRect.yMax, sourceV);

            for (int x = 0; x < capturedWidth; x++)
            {
                int pixelIndex = y * capturedWidth + x;
                Color32 capturedPixel = capturedPixels[pixelIndex];
                if (capturedPixel.a == 0)
                {
                    continue;
                }

                float sourceU = capturedWidth > 1 ? (float)x / (capturedWidth - 1) : 0.5f;
                float sourceLocalX = Mathf.Lerp(sourceRect.xMin, sourceRect.xMax, sourceU);

                Vector3 worldPoint = sourceRectTransform.TransformPoint(new Vector3(sourceLocalX, sourceLocalY, 0f));
                Vector3 maskLocalPoint = maskRectTransform.InverseTransformPoint(worldPoint);

                if (maskLocalPoint.x < maskRect.xMin || maskLocalPoint.x > maskRect.xMax || maskLocalPoint.y < maskRect.yMin || maskLocalPoint.y > maskRect.yMax)
                {
                    capturedPixels[pixelIndex].a = 0;
                    continue;
                }

                float maskU = Mathf.InverseLerp(maskRect.xMin, maskRect.xMax, maskLocalPoint.x);
                float maskV = Mathf.InverseLerp(maskRect.yMin, maskRect.yMax, maskLocalPoint.y);
                float sampledU = Mathf.Lerp(maskUvRect.xMin, maskUvRect.xMax, maskU);
                float sampledV = Mathf.Lerp(maskUvRect.yMin, maskUvRect.yMax, maskV);

                if (SampleAlpha(maskPixels, maskWidth, maskHeight, sampledU, sampledV) < maskMinAlpha)
                {
                    capturedPixels[pixelIndex].a = 0;
                }
            }
        }

        capturedTexture.SetPixels32(capturedPixels);
        capturedTexture.Apply(false, false);
    }

    private bool TryResolveMaskTarget(RawImage sourceRawImage, out RawImage maskTargetRawImage, out float maskMinAlpha)
    {
        maskTargetRawImage = null;
        maskMinAlpha = 0.01f;

        if (sourceRawImage == null)
        {
            return false;
        }

        AcCheck[] checks = FindObjectsOfType<AcCheck>(true);
        for (int i = 0; i < checks.Length; i++)
        {
            AcCheck check = checks[i];
            if (check == null || check.overlayRawImage != sourceRawImage || check.targetRawImage == null)
            {
                continue;
            }

            maskTargetRawImage = check.targetRawImage;
            return true;
        }

        return false;
    }

    private Texture GetRawImageTexture(RawImage rawImage)
    {
        if (rawImage == null)
        {
            return null;
        }

        if (rawImage.texture != null)
        {
            return rawImage.texture;
        }

        if (rawImage.material != null)
        {
            return rawImage.material.mainTexture;
        }

        return null;
    }

    private float SampleAlpha(Color32[] pixels, int width, int height, float u, float v)
    {
        if (pixels == null || pixels.Length == 0 || width <= 0 || height <= 0)
        {
            return 0f;
        }

        int x = Mathf.Clamp(Mathf.RoundToInt(Mathf.Clamp01(u) * (width - 1)), 0, width - 1);
        int y = Mathf.Clamp(Mathf.RoundToInt(Mathf.Clamp01(v) * (height - 1)), 0, height - 1);
        return pixels[y * width + x].a / 255f;
    }

    private bool TryGetPixels(Texture texture, ref Texture2D readbackCache, out Color32[] pixels, out int width, out int height)
    {
        pixels = null;
        width = 0;
        height = 0;

        if (texture == null)
        {
            return false;
        }

        if (texture is Texture2D texture2D)
        {
            width = texture2D.width;
            height = texture2D.height;
            if (width <= 0 || height <= 0)
            {
                return false;
            }

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

    private bool TryReadByBlit(Texture source, ref Texture2D readbackCache, out Color32[] pixels, out int width, out int height)
    {
        pixels = null;
        width = source.width;
        height = source.height;
        if (width <= 0 || height <= 0)
        {
            return false;
        }

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

    private bool TryReadRenderTexture(RenderTexture renderTexture, ref Texture2D readbackCache, out Color32[] pixels, out int width, out int height)
    {
        pixels = null;
        width = renderTexture.width;
        height = renderTexture.height;
        if (width <= 0 || height <= 0)
        {
            return false;
        }

        if (readbackCache == null || readbackCache.width != width || readbackCache.height != height)
        {
            if (readbackCache != null)
            {
                Destroy(readbackCache);
            }

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

    private Material GetSnapshotMaterial(Material sourceMaterial)
    {
        if (sourceMaterial == null)
        {
            if (_snapshotMaterial != null)
            {
                Destroy(_snapshotMaterial);
                _snapshotMaterial = null;
            }

            return null;
        }

        if (_snapshotMaterial == null || _snapshotMaterial.shader != sourceMaterial.shader)
        {
            if (_snapshotMaterial != null)
            {
                Destroy(_snapshotMaterial);
            }

            _snapshotMaterial = new Material(sourceMaterial.shader);
        }

        _snapshotMaterial.CopyPropertiesFromMaterial(sourceMaterial);

        if (_snapshotMaterial.HasProperty(ShaderPropTargetMaskEnabled))
        {
            _snapshotMaterial.SetFloat(ShaderPropTargetMaskEnabled, 0f);
        }

        return _snapshotMaterial;
    }




}
