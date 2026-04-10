using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SaveShadowTexture : MonoBehaviour
{
    RenderTexture _snapshotRt;

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
    }



    public void SetTexture(RawImage sourceRawImage)
    {
        RawImage targetRawImage = _rawImage != null ? _rawImage : GetComponent<RawImage>();
        _rawImage = targetRawImage;

        if (sourceRawImage == null || targetRawImage == null)
        {
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
            return;
        }

        Vector2Int targetSize = GetRawImagePixelSize(targetRawImage);
        int targetWidth = targetSize.x;
        int targetHeight = targetSize.y;
        if (targetWidth <= 0 || targetHeight <= 0)
        {
            return;
        }

        EnsureSnapshotTexture(targetWidth, targetHeight);
        EnsureSnapshotRenderTarget(targetWidth, targetHeight);

        if (cameraValue != null)
        {
            cameraValue.ApplyMaterialProperties();
        }

        int writeIndex = _currentTextureIndex % MaxTextureCount;
        Texture2D capturedTexture = textures[writeIndex] as Texture2D;
        if (capturedTexture == null)
        {
            capturedTexture = new Texture2D(targetWidth, targetHeight, TextureFormat.RGBA32, false);
            textures[writeIndex] = capturedTexture;
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

        Material sourceMaterial = sourceRawImage.material;

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
            capturedTexture.Apply(false, false);
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




}
