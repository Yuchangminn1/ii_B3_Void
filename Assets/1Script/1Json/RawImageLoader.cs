using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.IO;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class RawImageConfig
{
    public string streamingAssets_Path;         // StreamingAssets 기준 경로

    public CusVector3 local_position;


    public CusVector2 localSize;

}
public class CusVector2
{
    public CusVector2(float x, float y)
    {
        _x = x;
        _y = y;
    }
    public float _x, _y;
}
public class CusVector3
{
    public CusVector3(float x, float y, float z)
    {
        _x = x;
        _y = y;
        _z = z;
    }
    public float _x, _y, _z;
}

public class RawImageLoader : JsonLoaderBase<RawImageConfig, RawImage>
{
    private readonly Dictionary<string, Texture2D> _textureCache = new();

    public RawImageLoader(string jsonPath) : base(jsonPath) { }

    public override void ApplyLoadedData(string name, RawImage obj)
    {
        if (!data.TryGetValue(name, out var item))
            return;

        // 경로 조합
        string absPath = Path.Combine(Application.streamingAssetsPath, item.streamingAssets_Path).Replace("\\", "/");
        string url = GetPlatformURL(absPath);

        // 캐시 확인 후 로드
        CoroutineRunner.Instance.Run(LoadAndApplyTexture(url, item.streamingAssets_Path, obj, item));
    }

    private static string GetPlatformURL(string path)
    {
#if UNITY_ANDROID
        return "jar:file://" + path;
#elif UNITY_IOS
        return "file://" + path;
#else
        return "file:///" + path;
#endif
    }

    private IEnumerator LoadAndApplyTexture(string url, string cacheKey, RawImage target, RawImageConfig item)
    {

        // 캐시에 있으면 즉시 적용
        if (_textureCache.TryGetValue(cacheKey, out var cachedTex))
        {
            ApplyTexture(cachedTex, target, item);
            yield break;
        }
        if (item.streamingAssets_Path != "")
        {
            using (UnityWebRequest www = UnityWebRequestTexture.GetTexture(url))
            {
                yield return www.SendWebRequest();

#if UNITY_2020_3_OR_NEWER
                if (www.result != UnityWebRequest.Result.Success)
#else
            if (www.isNetworkError || www.isHttpError)
#endif
                {
                    Debug.LogError($"RawImage load failed: {www.error}\n{url}");
                    yield break;
                }

                Texture2D texture = DownloadHandlerTexture.GetContent(www);
                CompressIfSupported(texture);
                _textureCache[cacheKey] = texture;

                ApplyTexture(texture, target, item);
            }
        }
        else
        {
            ApplyTexture(target, item);
        }

    }

    private void CompressIfSupported(Texture2D texture)
    {
        if (SystemInfo.SupportsTextureFormat(TextureFormat.DXT1))
        {
            try
            {
                // 현재 텍스처 크기가 4의 배수인지 확인
                int newWidth = Mathf.CeilToInt(texture.width / 4f) * 4;
                int newHeight = Mathf.CeilToInt(texture.height / 4f) * 4;

                // 리사이즈가 필요한 경우 새 텍스처 생성 후 복사
                if (newWidth != texture.width || newHeight != texture.height)
                {
                    //          Debug.Log($"[RawImageLoader] Texture resized for compression: {texture.width}x{texture.height} → {newWidth}x{newHeight}");
                    texture = ResizeToMultipleOfFour(texture, newWidth, newHeight);
                }

                // DXT1 압축 시도
                texture.Compress(true);
                texture.Apply(true, true);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[RawImageLoader] Compression failed: {e.Message}");
            }
        }
        else
        {
            Debug.LogWarning("[RawImageLoader] DXT1 compression not supported on this platform.");
        }

    }
    private Texture2D ResizeToMultipleOfFour(Texture2D source, int width, int height)
    {
        RenderTexture rt = RenderTexture.GetTemporary(width, height);
        Graphics.Blit(source, rt);

        RenderTexture prev = RenderTexture.active;
        RenderTexture.active = rt;

        Texture2D result = new Texture2D(width, height, source.format, false);
        result.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        result.Apply();

        RenderTexture.active = prev;
        RenderTexture.ReleaseTemporary(rt);

        return result;
    }

    private void ApplyTexture(Texture2D texture, RawImage target, RawImageConfig item)
    {
        if (item.streamingAssets_Path != "")
            target.texture = texture;

        target.rectTransform.localPosition = new Vector3(item.local_position._x, item.local_position._y, 0f);
        target.rectTransform.localScale = Vector3.one;
        target.rectTransform.sizeDelta = new Vector2(item.localSize._x, item.localSize._y);


    }
    private void ApplyTexture(RawImage target, RawImageConfig item)
    {

        target.rectTransform.localPosition = new Vector3(item.local_position._x, item.local_position._y, 0f);
        target.rectTransform.localScale = Vector3.one;

        target.rectTransform.sizeDelta = new Vector2(item.localSize._x, item.localSize._y);

    }


    public override void JsonDataUpdate()
    {

        foreach (var kv in _target)
        {
            var raw = kv.Value;
            if (raw == null) continue;

            string fileName = "";


            if (raw.texture != null && raw.texture.name != "")
            {
                fileName = $"RawImages/{raw.texture.name}.png";
            }
            if (fileName == "")
            {
                if (data.TryGetValue(kv.Key, out var existingConfig) && existingConfig.streamingAssets_Path != null)
                    fileName = existingConfig.streamingAssets_Path;
            }

            if (raw.texture == null || raw.texture is RenderTexture)
            {
                fileName = "";
            }


            var cfg = new RawImageConfig
            {

                streamingAssets_Path = fileName,
                localSize = new CusVector2(raw.rectTransform.rect.width, raw.rectTransform.rect.height),
                local_position = new CusVector3(raw.rectTransform.localPosition.x, raw.rectTransform.localPosition.y, 1f),

            };

            data[kv.Key] = cfg;
        }

        // 이름순 정렬
        var ordered = new SortedDictionary<string, RawImageConfig>(data);
        data = new Dictionary<string, RawImageConfig>(ordered);

        Save();
    }
}
