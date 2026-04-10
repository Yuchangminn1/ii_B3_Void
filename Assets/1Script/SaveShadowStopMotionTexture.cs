using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SaveShadowStopMotionTexture : MonoBehaviour
{
    public int MaxCaptureCount = 100;
    public CameraValue cameraValue;

    [Tooltip("Leave empty to use RawImage on this GameObject")]
    public RawImage targetRawImage;

    public int CurrentCaptureIndex = 0;

    [Tooltip("Logs estimated capture memory for two instances (2 x MaxCaptureCount)")]
    public bool LogMemoryEstimate = true;

    readonly List<Texture2D> _capturedTextures = new List<Texture2D>();
    RenderTexture _snapshotRt;
    RawImage _selfRawImage;
    RectTransform _rectTransform;
    int _nextWriteIndex = 0;
    int _captureCount = 0;
    int _captureWidth = 0;
    int _captureHeight = 0;
    bool _hasLoggedMemoryEstimate = false;

    void Start()
    {
        _selfRawImage = GetComponent<RawImage>();
        if (_selfRawImage != null)
        {
            _rectTransform = _selfRawImage.rectTransform;
        }

        if (targetRawImage == null)
        {
            targetRawImage = _selfRawImage;
        }
    }

    void OnDestroy()
    {
        ReleaseSnapshotRenderTarget();
        ClearCapturedTextures();
    }

    public void CaptureTexture(RawImage sourceRawImage)
    {
        RawImage target = targetRawImage != null ? targetRawImage : _selfRawImage;
        if (sourceRawImage == null || target == null)
        {
            return;
        }

        RectTransform sourceRectTransform = sourceRawImage.rectTransform;
        RectTransform targetRectTransform = target.rectTransform;
        if (_rectTransform != null && sourceRectTransform != null && targetRectTransform != null)
        {
            targetRectTransform.localRotation = _rectTransform.localRotation * sourceRectTransform.localRotation;
        }

        Vector2Int targetSize = GetRawImagePixelSize(target);
        if (targetSize.x <= 0 || targetSize.y <= 0)
        {
            return;
        }

        EnsureSnapshotRenderTarget(targetSize.x, targetSize.y);
        EnsureCaptureBuffer(targetSize.x, targetSize.y);

        if (cameraValue != null)
        {
            cameraValue.ApplyMaterialProperties();
        }

        Material sourceMaterial = sourceRawImage.material;
        Texture texture = sourceRawImage.texture;
        if (texture == null)
        {
            return;
        }

        Texture2D captured = _capturedTextures[_nextWriteIndex];
        if (captured == null)
        {
            captured = new Texture2D(targetSize.x, targetSize.y, TextureFormat.RGBA32, false);
            _capturedTextures[_nextWriteIndex] = captured;
        }

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

            captured.ReadPixels(new Rect(0, 0, targetSize.x, targetSize.y), 0, 0);
            captured.Apply(false, false);
        }
        finally
        {
            RenderTexture.active = previous;
        }

        RegisterCapturedIndex();
        SetDisplayTexture(CurrentCaptureIndex);
    }

    public void SetTexture(RawImage sourceRawImage)
    {
        CaptureTexture(sourceRawImage);
    }

    public void SetDisplayTexture(int index)
    {
        RawImage target = targetRawImage != null ? targetRawImage : _selfRawImage;
        if (target == null)
        {
            return;
        }

        if (_captureCount <= 0)
        {
            target.texture = null;
            return;
        }

        int clampedIndex = Mathf.Clamp(index, 0, _captureCount - 1);
        CurrentCaptureIndex = clampedIndex;
        target.texture = _capturedTextures[clampedIndex];
    }

    public void ClearCapturedTextures()
    {
        for (int i = 0; i < _capturedTextures.Count; i++)
        {
            if (_capturedTextures[i] != null)
            {
                Destroy(_capturedTextures[i]);
            }
        }

        _capturedTextures.Clear();
        _nextWriteIndex = 0;
        _captureCount = 0;
        _captureWidth = 0;
        _captureHeight = 0;
        _hasLoggedMemoryEstimate = false;
        CurrentCaptureIndex = 0;

        RawImage target = targetRawImage != null ? targetRawImage : _selfRawImage;
        if (target != null)
        {
            target.texture = null;
        }
    }

    private void EnsureCaptureBuffer(int width, int height)
    {
        int safeMax = Mathf.Max(1, MaxCaptureCount);
        bool needsResize = _capturedTextures.Count != safeMax;
        bool sizeChanged = _captureWidth != width || _captureHeight != height;

        if (!needsResize && !sizeChanged)
        {
            return;
        }

        ClearCapturedTextures();
        _capturedTextures.Capacity = safeMax;
        for (int i = 0; i < safeMax; i++)
        {
            _capturedTextures.Add(null);
        }

        _captureWidth = width;
        _captureHeight = height;
        TryLogEstimatedMemoryUsage(width, height, safeMax);
    }

    private void RegisterCapturedIndex()
    {
        if (_capturedTextures.Count == 0)
        {
            return;
        }

        if (_captureCount < _capturedTextures.Count)
        {
            _captureCount++;
        }

        CurrentCaptureIndex = _nextWriteIndex;
        _nextWriteIndex = (_nextWriteIndex + 1) % _capturedTextures.Count;
    }

    private void TryLogEstimatedMemoryUsage(int width, int height, int perInstanceCount)
    {
        if (!LogMemoryEstimate || _hasLoggedMemoryEstimate)
        {
            return;
        }

        long bytesPerFrame = (long)width * height * 4L;
        long oneInstanceBytes = bytesPerFrame * perInstanceCount;
        long twoInstanceBytes = oneInstanceBytes * 2L;

        float oneInstanceMb = oneInstanceBytes / (1024f * 1024f);
        float twoInstanceMb = twoInstanceBytes / (1024f * 1024f);
        Debug.Log($"[SaveShadowStopMotionTexture] Estimated Texture2D memory: 1 instance ({perInstanceCount} frames) ~= {oneInstanceMb:F2} MB, 2 instances (200 frames when per-instance is 100) ~= {twoInstanceMb:F2} MB at {width}x{height}.");

        _hasLoggedMemoryEstimate = true;
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

    private void EnsureSnapshotRenderTarget(int width, int height)
    {
        if (_snapshotRt != null && _snapshotRt.width == width && _snapshotRt.height == height)
        {
            return;
        }

        ReleaseSnapshotRenderTarget();

        _snapshotRt = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32);
        _snapshotRt.Create();
    }

    private void ReleaseSnapshotRenderTarget()
    {
        if (_snapshotRt == null)
        {
            return;
        }

        _snapshotRt.Release();
        Destroy(_snapshotRt);
        _snapshotRt = null;
    }
}
