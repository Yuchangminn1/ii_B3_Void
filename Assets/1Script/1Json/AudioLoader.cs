using UnityEngine;
using System.Collections;
using UnityEngine.Networking;
using System.IO;

[System.Serializable]
public class AudioJson
{
    public float soundValue;
    public bool isLoop;
    public string fileName; // 실제 파일명 (.mp3, .wav 등)
}

public class AudioLoader : JsonLoaderBase<AudioJson, AudioSource>
{
    public AudioLoader(string jsonPath) : base(jsonPath) { }

    public override void ApplyLoadedData(string name, AudioSource obj)
    {
        if (!data.TryGetValue(name, out var item))
            return;

        // 기본 설정 적용
        obj.loop = item.isLoop;
        obj.volume = Mathf.Clamp01(item.soundValue);

        // 파일 경로
        string filePath = Path.Combine(Application.streamingAssetsPath, item.fileName);
        string url = filePath;
#if UNITY_ANDROID
        url = "jar:file://" + filePath;
#elif UNITY_IOS
        url = "file://" + filePath;
#elif UNITY_STANDALONE || UNITY_EDITOR
        url = "file:///" + filePath;
#endif

        // 확장자 기반으로 AudioType 판별
        AudioType audioType = GetAudioTypeByExtension(Path.GetExtension(item.fileName));

        // AudioClip 비동기 로드
        CoroutineRunner.Instance.Run(LoadAndApplyClip(url, audioType, obj));
    }

    private IEnumerator LoadAndApplyClip(string url, AudioType audioType, AudioSource audioSource)
    {
        using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(url, audioType))
        {
            yield return www.SendWebRequest();

#if UNITY_2020_3_OR_NEWER
            if (www.result != UnityWebRequest.Result.Success)
#else
            if (www.isNetworkError || www.isHttpError)
#endif
            {
                Debug.LogError($"Audio Load Failed ({url}): {www.error}");
                yield break;
            }

            AudioClip clip = DownloadHandlerAudioClip.GetContent(www);
            audioSource.clip = clip;
            // audioSource.Play();
        }
    }

    private AudioType GetAudioTypeByExtension(string extension)
    {
        extension = extension.ToLowerInvariant();
        return extension switch
        {
            ".wav" => AudioType.WAV,
            ".mp3" => AudioType.MPEG,
            ".ogg" => AudioType.OGGVORBIS,
            ".aiff" or ".aif" => AudioType.AIFF,
            ".flac" => AudioType.UNKNOWN, // 일부 Unity 버전 미지원
            _ => AudioType.UNKNOWN
        };
    }



    public override void JsonDataUpdate()
    {
        Save();
    }
}
