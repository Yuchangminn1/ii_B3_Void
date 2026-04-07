using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SaveShadowTexture : MonoBehaviour
{
    Texture2D shadowTexture;
    RenderTexture _snapshotRt;

    public int CurrentIndex = 0;


    RawImage _rawImage;


    void Start()
    {
        _rawImage = GetComponent<RawImage>();
    }

    private void OnDestroy()
    {
        ReleaseSnapshotResources();
    }

    private void ReleaseSnapshotResources()
    {
        if (shadowTexture != null)
        {
            Destroy(shadowTexture);
            shadowTexture = null;
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

        Texture texture = sourceRawImage.texture;
        if (texture == null)
        {
            ReleaseSnapshotResources();
            targetRawImage.texture = null;
            return;
        }

        Vector2Int targetSize = GetRawImagePixelSize(sourceRawImage);
        int targetWidth = targetSize.x;
        int targetHeight = targetSize.y;
        if (targetWidth <= 0 || targetHeight <= 0)
        {
            return;
        }

        EnsureSnapshotTexture(targetWidth, targetHeight);
        EnsureSnapshotRenderTarget(targetWidth, targetHeight);

        int copyWidth = Mathf.Min(texture.width, targetWidth);
        int copyHeight = Mathf.Min(texture.height, targetHeight);
        Rect sourceRect = new Rect(0f, 0f, (float)copyWidth / texture.width, (float)copyHeight / texture.height);
        Rect drawRect = new Rect(0f, 0f, copyWidth, copyHeight);

        RenderTexture previous = RenderTexture.active;

        try
        {
            RenderTexture.active = _snapshotRt;
            GL.Clear(true, true, Color.clear);

            GL.PushMatrix();
            GL.LoadPixelMatrix(0, targetWidth, 0, targetHeight);
            Graphics.DrawTexture(drawRect, texture, sourceRect, 0, 0, 0, 0);
            GL.PopMatrix();

            shadowTexture.ReadPixels(new Rect(0, 0, targetWidth, targetHeight), 0, 0);
            shadowTexture.Apply(false, false);
        }
        finally
        {
            RenderTexture.active = previous;
        }

        targetRawImage.texture = shadowTexture;
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
        if (shadowTexture != null && shadowTexture.width == width && shadowTexture.height == height)
        {
            return;
        }

        if (shadowTexture != null)
        {
            Destroy(shadowTexture);
        }

        shadowTexture = new Texture2D(width, height, TextureFormat.RGBA32, false);
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
