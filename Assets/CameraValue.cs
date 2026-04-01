using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CameraValue : MonoBehaviour, IJsonGenericTarget
{
    [Header("Webcam Settings")]
    public int cameraIndex = 0;
    public string deviceName = "";
    [Header("Device Filters")]
    [Tooltip("If non-empty and the selected device's name contains this substring, this CameraValue will skip starting the webcam.")]
    public string ignoreDeviceNameContains = "";
    public int requestedWidth = 1280;
    public int requestedHeight = 720;
    public int requestedFPS = 30;
    public bool mirrorHorizontal = true;
    public bool mirrorVertical = false;

    public Text CameraSetUpText;

    [Header("Debug")]
    [Tooltip("Enable verbose debug logs for CameraValue operations.")]
    public bool verboseDebug = false;

    [Header("Color Key Settings")]
    public Color keyColor = Color.green;
    [Range(0f, 1f)] public float threshold = 0.4f;
    [Range(0f, 1f)] public float smoothness = 0.1f;

    [Header("Output Settings")]
    public bool opaqueToBlack = true;
    [Range(1f, 10f)] public float edgeContrast = 1f;
    [Range(0f, 1f)] public float noiseFilter = 1f;
    [Range(0f, 1f)] public float alphaCutoff = 0.5f;
    [Range(0f, 0.49f)] public float midValueFilter = 0.15f;

    [Header("Left Clipping")]
    public int leftClipPixels = 0;
    public int rightClipPixels = 0;
    public int topClipPixels = 0;
    public int bottomClipPixels = 0;

    // 런타임 데이터 (인스펙터에 표시 안 됨)
    [System.NonSerialized] public WebCamTexture webcamTexture;
    [System.NonSerialized] public Material material;

    private RawImage _targetRawImage;
    private Coroutine _autoKeyRoutine;
    private Coroutine _autoKeyTimeoutRoutine;

    JsonGenericUpData _genericData = new JsonGenericUpData();


    public void Initialize(Shader shader)
    {
        _targetRawImage = GetComponent<RawImage>();
        if (_targetRawImage == null)
        {
            Debug.LogError($"[CameraValue] No RawImage found on {gameObject.name}");
            return;
        }

        if (material == null && shader != null)
        {
            material = new Material(shader);
            _targetRawImage.material = material;
        }
    }

    public void StartWebcam(WebCamDevice[] devices)
    {
        if (_targetRawImage == null) return;
        StopWebcam();

        // Build a filtered list of devices excluding any that match the ignore substring
        List<WebCamDevice> filtered = new List<WebCamDevice>();
        for (int i = 0; i < devices.Length; i++)
        {
            var d = devices[i];
            if (!string.IsNullOrEmpty(ignoreDeviceNameContains) && d.name != null && d.name.Contains(ignoreDeviceNameContains))
            {
                if (verboseDebug) Debug.Log($"[CameraValue] Ignoring device '{d.name}' because it contains '{ignoreDeviceNameContains}'");
                continue;
            }
            filtered.Add(d);
        }

        if (filtered.Count == 0)
        {
            Debug.LogWarning($"[CameraValue] No available cameras for '{gameObject.name}' after applying ignore filter '{ignoreDeviceNameContains}'.");
            return;
        }

        WebCamDevice device = default(WebCamDevice);
        bool found = false;

        // 1) Try exact name match within filtered devices
        if (!string.IsNullOrEmpty(deviceName))
        {
            for (int i = 0; i < filtered.Count; i++)
            {
                if (filtered[i].name == deviceName)
                {
                    device = filtered[i];
                    found = true;
                    break;
                }
            }
        }

        // 2) Try contains name match
        if (!found && !string.IsNullOrEmpty(deviceName))
        {
            for (int i = 0; i < filtered.Count; i++)
            {
                if (filtered[i].name.Contains(deviceName))
                {
                    device = filtered[i];
                    found = true;
                    break;
                }
            }
        }

        // 3) Use cameraIndex within filtered list
        if (!found)
        {
            int idx = Mathf.Clamp(cameraIndex, 0, filtered.Count - 1);
            device = filtered[idx];
            found = true;
        }

        if (!found)
        {
            Debug.LogWarning($"[CameraValue] Could not select a camera for '{gameObject.name}'.");
            return;
        }

        webcamTexture = new WebCamTexture(device.name, requestedWidth, requestedHeight, requestedFPS);
        webcamTexture.Play();

        _targetRawImage.texture = webcamTexture;
        if (material != null)
        {
            material.mainTexture = webcamTexture;
        }
        Debug.Log($"[CameraValue] Started '{gameObject.name}' with device '{device.name}'");
    }

    public void StopWebcam()
    {
        if (webcamTexture != null)
        {
            if (webcamTexture.isPlaying) webcamTexture.Stop();
            if (_targetRawImage != null) _targetRawImage.texture = null;
            Destroy(webcamTexture);
            webcamTexture = null;
        }
    }

    public void SetWarningText(string message)
    {
        CameraSetUpText.text = message;
        FadeManager.Instance.SetAlphaOne(CameraSetUpText);
    }

    public void OffWarningText()
    {
        FadeManager.Instance.SetAlphaZero(CameraSetUpText);
    }

    public void ApplyMaterialProperties()
    {
        if (material == null) return;

        material.SetColor("_KeyColor", keyColor);
        material.SetFloat("_Threshold", threshold);
        material.SetFloat("_Smooth", smoothness);
        material.SetFloat("_Mirror", mirrorHorizontal ? 1f : 0f);

        bool vflip = mirrorVertical;
        if (webcamTexture != null)
        {
            vflip ^= webcamTexture.videoVerticallyMirrored;
        }
        material.SetFloat("_VFlip", vflip ? 1f : 0f);

        material.SetFloat("_OpaqueToBlack", opaqueToBlack ? 1f : 0f);
        material.SetFloat("_EdgeContrast", edgeContrast);
        material.SetFloat("_NoiseFilter", noiseFilter);
        material.SetFloat("_AlphaCutoff", alphaCutoff);
        material.SetFloat("_MidValueFilter", midValueFilter);

        float clipAmount = 0f;
        if (webcamTexture != null && webcamTexture.width > 0)
        {
            clipAmount = (float)leftClipPixels / webcamTexture.width;
        }
        material.SetFloat("_LeftClip", Mathf.Clamp01(clipAmount));

        float rightClipAmount = 0f;
        if (webcamTexture != null && webcamTexture.width > 0)
        {
            rightClipAmount = (float)rightClipPixels / webcamTexture.width;
        }
        material.SetFloat("_RightClip", Mathf.Clamp01(rightClipAmount));

        float topClipAmount = 0f;
        if (webcamTexture != null && webcamTexture.height > 0)
        {
            topClipAmount = (float)topClipPixels / webcamTexture.height;
        }
        material.SetFloat("_TopClip", Mathf.Clamp01(topClipAmount));

        float bottomClipAmount = 0f;
        if (webcamTexture != null && webcamTexture.height > 0)
        {
            bottomClipAmount = (float)bottomClipPixels / webcamTexture.height;
        }
        material.SetFloat("_BottomClip", Mathf.Clamp01(bottomClipAmount));
    }

    public void SetClipPixels(int left, int right, int top, int bottom)
    {
        leftClipPixels = Mathf.Max(0, left);
        rightClipPixels = Mathf.Max(0, right);
        topClipPixels = Mathf.Max(0, top);
        bottomClipPixels = Mathf.Max(0, bottom);
    }

    public void AddClipPixels(int leftDelta, int rightDelta, int topDelta, int bottomDelta)
    {
        leftClipPixels = Mathf.Max(0, leftClipPixels + leftDelta);
        rightClipPixels = Mathf.Max(0, rightClipPixels + rightDelta);
        topClipPixels = Mathf.Max(0, topClipPixels + topDelta);
        bottomClipPixels = Mathf.Max(0, bottomClipPixels + bottomDelta);
    }

    void OnValidate()
    {
        leftClipPixels = Mathf.Max(0, leftClipPixels);
        rightClipPixels = Mathf.Max(0, rightClipPixels);
        topClipPixels = Mathf.Max(0, topClipPixels);
        bottomClipPixels = Mathf.Max(0, bottomClipPixels);
    }

    void OnDestroy()
    {
        StopWebcam();
        if (material != null)
        {
            Destroy(material);
        }
    }

    public void AutoFocus()
    {
        StartAutoKeyForSeconds(2f);
    }

    // --- Auto Key Color Logic ---
    public void StartAutoKeyForSeconds(float seconds)
    {
        if (_autoKeyRoutine != null) StopCoroutine(_autoKeyRoutine);
        if (_autoKeyTimeoutRoutine != null) StopCoroutine(_autoKeyTimeoutRoutine);

        _autoKeyRoutine = StartCoroutine(AutoPickKeyColorRoutine());
        _autoKeyTimeoutRoutine = StartCoroutine(StopAutoKeyAfterDelay(seconds));
        Debug.Log($"[CameraValue] Auto key color started for '{gameObject.name}' for {seconds} seconds.");
    }

    IEnumerator StopAutoKeyAfterDelay(float delay)
    {
        SetWarningText("Auto picking key color...");
        yield return new WaitForSeconds(delay);
        if (_autoKeyRoutine != null)
        {
            StopCoroutine(_autoKeyRoutine);
            _autoKeyRoutine = null;
        }
        _autoKeyTimeoutRoutine = null;
        OffWarningText();
        Debug.Log($"[CameraValue] Auto key color stopped for '{gameObject.name}'.");
    }

    IEnumerator AutoPickKeyColorRoutine()
    {
        var wait = new WaitForSeconds(1f);
        while (true)
        {
            if (TryUpdateKeyColorFromWebcam())
            {
                Debug.Log($"Auto Color Value for {gameObject.name}: {keyColor}");
            }
            yield return wait;
        }
    }

    bool TryUpdateKeyColorFromWebcam()
    {
        if (webcamTexture == null || !webcamTexture.isPlaying || webcamTexture.width <= 16) return false;

        Color32[] pixels;
        try
        {
            pixels = webcamTexture.GetPixels32();
        }
        catch { return false; }

        int w = webcamTexture.width;
        int h = webcamTexture.height;
        int x0 = w / 4, y0 = h / 4;
        int x1 = (w * 3) / 4, y1 = (h * 3) / 4;
        int stepX = Mathf.Max(1, (x1 - x0) / 32);
        int stepY = Mathf.Max(1, (y1 - y0) / 18);

        long sr = 0, sg = 0, sb = 0;
        int count = 0;
        for (int y = y0; y < y1; y += stepY)
        {
            int row = y * w;
            for (int x = x0; x < x1; x += stepX)
            {
                Color32 c = pixels[row + x];
                sr += c.r;
                sg += c.g;
                sb += c.b;
                count++;
            }
        }

        if (count > 0)
        {
            keyColor = new Color((float)sr / (255f * count), (float)sg / (255f * count), (float)sb / (255f * count), 1f);
            return true;
        }
        return false;
    }

    public void Initialize(JsonGenericUpData data)
    {
        _genericData = data;

        data.floatParams.TryGetValue("threshold", out threshold);
        data.floatParams.TryGetValue("smoothness", out smoothness);
        if (data.floatParams.TryGetValue("alphaCutoff", out float loadedAlphaCutoff)) alphaCutoff = loadedAlphaCutoff;
        if (data.floatParams.TryGetValue("midValueFilter", out float loadedMidValueFilter)) midValueFilter = loadedMidValueFilter;
        if (data.intParams != null)
        {
            if (data.intParams.TryGetValue("leftClipPixels", out int loadedLeftClip)) leftClipPixels = Mathf.Max(0, loadedLeftClip);
            if (data.intParams.TryGetValue("rightClipPixels", out int loadedRightClip)) rightClipPixels = Mathf.Max(0, loadedRightClip);
            if (data.intParams.TryGetValue("topClipPixels", out int loadedTopClip)) topClipPixels = Mathf.Max(0, loadedTopClip);
            if (data.intParams.TryGetValue("bottomClipPixels", out int loadedBottomClip)) bottomClipPixels = Mathf.Max(0, loadedBottomClip);
        }
    }

    public JsonGenericUpData Data()
    {
        _genericData.intParams = new Dictionary<string, int>();
        _genericData.floatParams = new Dictionary<string, float>();
        _genericData.boolParams = new Dictionary<string, bool>();

        _genericData.floatParams["threshold"] = threshold;
        _genericData.floatParams["smoothness"] = smoothness;
        _genericData.floatParams["alphaCutoff"] = alphaCutoff;
        _genericData.floatParams["midValueFilter"] = midValueFilter;
        _genericData.intParams["leftClipPixels"] = Mathf.Max(0, leftClipPixels);
        _genericData.intParams["rightClipPixels"] = Mathf.Max(0, rightClipPixels);
        _genericData.intParams["topClipPixels"] = Mathf.Max(0, topClipPixels);
        _genericData.intParams["bottomClipPixels"] = Mathf.Max(0, bottomClipPixels);
        return _genericData;
    }
}
