using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CameraVV2 : MonoBehaviour
{
    [System.Serializable]
    public class CameraDisplayRoute
    {
        [Header("Camera Display Route")]
        [Tooltip("WebCamTexture.devices의 카메라 인덱스")]
        public int cameraIndex = 0;
        [Tooltip("매핑할 디스플레이 인덱스(0=메인)")]
        public int displayIndex = 0;
    }

    [Header("UI Outputs")]
    [Tooltip("첫 번째 카메라 화면을 출력할 RawImage")]
    public RawImage firstRawImage;
    [Tooltip("두 번째 카메라 화면을 출력할 RawImage")]
    public RawImage secondRawImage;
    [Tooltip("첫 번째 RawImage에 연결할 카메라 인덱스")]
    public int firstCameraIndex = 0;
    [Tooltip("두 번째 RawImage에 연결할 카메라 인덱스")]
    public int secondCameraIndex = 1;
    [Tooltip("첫 번째 RawImage에 연결할 카메라 이름. 비워두면 firstCameraIndex를 사용합니다.")]
    public string firstDeviceName = "";
    [Tooltip("두 번째 RawImage에 연결할 카메라 이름. 비워두면 secondCameraIndex를 사용합니다.")]
    public string secondDeviceName = "";

    // --------------------------------------------
    // 웹캠 + 컬러키(특정 색 투명 처리) 컴포넌트
    // - 이 스크립트를 매니저 오브젝트에 붙이고 RawImage 2개를 연결해서 사용합니다.
    // - firstRawImage, secondRawImage를 비워두면 현재 오브젝트의 RawImage 또는 Renderer를 첫 번째 출력으로 사용합니다.
    // - 내부에서 "Custom/WebcamChromaKey" 셰이더를 찾아 머티리얼을 생성/적용하고,
    //   웹캠 텍스처를 실시간으로 출력합니다.
    // - 인스펙터에서 투명 처리할 키 컬러(임의 색), 임계값, 부드러움을 조절하세요.
    // --------------------------------------------

    [Header("Webcam Settings")]
    [Tooltip("기본으로 사용할 웹캠 이름. 비워두면 카메라 인덱스 기준으로 선택합니다.")]
    public string deviceName = "";
    [Tooltip("요청 웹캠 가로 해상도")]
    public int requestedWidth = 1280;
    [Tooltip("요청 웹캠 세로 해상도")]
    public int requestedHeight = 720;
    [Tooltip("요청 웹캠 FPS")]
    public int requestedFPS = 30;
    [Tooltip("시작 시 자동으로 웹캠 재생")]
    public bool autoPlay = true;
    [Tooltip("좌우 반전 출력")]
    public bool mirrorHorizontal = true;
    [Tooltip("상하 반전 출력")]
    public bool mirrorVertical = false;

    [Header("Color Key Settings")]
    [Tooltip("이 색의 밝기를 기준으로 검정/투명을 분리합니다.")]
    public Color keyColor = Color.green;
    [Tooltip("기준 보정값(현재 이진화 모드에서는 영향이 적을 수 있음)")]
    [Range(0f, 1f)] public float threshold = 0.4f;
    [Tooltip("경계 완화값(현재 이진화 모드에서는 영향이 적을 수 있음)")]
    [Range(0f, 1f)] public float smoothness = 0.1f;
    [Tooltip("스필 감소 값(현재 셰이더에서는 사용하지 않음)")]
    [Range(0f, 1f)] public float spillReduction = 0.2f; // 단순 컬러키에서는 사용하지 않음

    /*
    [Header("Shadow / Second Key Settings")]
    public bool useSecondKey = false;
    public Color keyColor2 = Color.black;
    [Range(0f, 1f)] public float threshold2 = 0.4f;
    [Range(0f, 1f)] public float smoothness2 = 0.1f;
    */

    [Tooltip("웹캠 화면에서 주기적으로 키 컬러를 자동 추출합니다.")]
    public bool autoKeyColor = true;
    [Tooltip("키 컬러 자동 추출 간격(초)")]
    [Range(1f, 120f)] public float autoKeyInterval = 5f;

    [Header("Output Settings")]
    [Tooltip("전경 영역을 검정으로 강제 출력합니다.")]
    public bool opaqueToBlack = true; // 전경(불투명) 영역을 검은색으로 출력
    [Tooltip("경계 대비값(현재 이진화 모드에서는 영향이 적을 수 있음)")]
    [Range(1f, 10f)] public float edgeContrast = 1f; // 경계 선명도(콘트라스트)
    [Tooltip("3x3 기반 점 노이즈 필터 강도. 높을수록 자글자글한 노이즈가 줄어듭니다.")]
    [Range(0f, 1f)] public float noiseFilter = 1f; // 3x3 기반 점 노이즈 필터 강도

    [Header("Shader Settings")]
    [Tooltip("빌드에서 스트립되지 않도록 인스펙터에 직접 할당 권장 (예: Assets/WebcamChromaKey.shader)")]
    public Shader chromaKeyShader;
    [Tooltip("크로마키 셰이더를 못 찾을 때 사용할 안전한 대체 셰이더")]
    public Shader fallbackShader;

    [Header("Display Routing")]
    [Tooltip("true면 이 오브젝트의 출력 대상을 지정한 디스플레이로 라우팅합니다.")]
    public bool routeToDisplay = true;
    [Tooltip("0=메인 디스플레이, 1=두 번째 디스플레이 ...")]
    [Range(0, 7)] public int targetDisplay = 0;
    [Tooltip("추가 디스플레이(1 이상)를 자동 활성화합니다.")]
    public bool activateDisplayOnStart = true;
    [Tooltip("카메라 인덱스별 디스플레이 매핑을 사용합니다.")]
    public bool useCameraDisplayRoutes = true;

    [Tooltip("설정 상태를 표시할 UI 텍스트 배열")]
    public Text[] SettingTexts;
    [Tooltip("예: cameraIndex=0, displayIndex=1 => 0번 카메라는 1번 디스플레이로 라우팅")]
    public List<CameraDisplayRoute> cameraDisplayRoutes = new List<CameraDisplayRoute>
    {
        new CameraDisplayRoute { cameraIndex = 0, displayIndex = 1 }
    };

    [Header("Debug")]
    [Tooltip("체크 시 0번 카메라를 무시합니다.")]
    public bool debugIgnoreCameraZero = false;
    [Tooltip("이 문자열이 포함된 이름의 카메라는 선택에서 제외합니다.")]
    public string ignoreDeviceName = "Mac";

    WebCamTexture _firstWebcam;
    WebCamTexture _secondWebcam;
    Material _firstMaterial;
    Material _secondMaterial;
    Renderer _targetRenderer;
    RawImage _selfRawImage;
    Coroutine _autoKeyRoutine;
    static readonly HashSet<int> _activatedDisplays = new HashSet<int>();
    int _selectedPrimaryCameraIndex = -1;

    bool isSetUp = false;

    Shader GetSupportedShader(Shader shader)
    {
        if (shader == null) return null;
        if (!shader.isSupported)
        {
            Debug.LogWarning($"[CameraVV] Shader not supported on this platform: {shader.name}");
            return null;
        }
        return shader;
    }

    IEnumerator AutoKeyStop()
    {
        yield return new WaitForSeconds(10f);
        autoKeyColor = false;
        autoPlay = false;
    }

    Shader ResolveRuntimeShader()
    {
        // 1순위: 인스펙터에 직접 할당된 셰이더 (빌드 스트립 방지에 가장 확실)
        var shader = GetSupportedShader(chromaKeyShader);
        if (shader != null) return shader;

        // 2순위: Resources 폴더에 있으면 로드 (빌드에 확실히 포함됨)
        var resShader = Resources.Load<Shader>("WebcamChromaKey");
        shader = GetSupportedShader(resShader);
        if (shader != null) return shader;

        // 3순위: Shader.Find — 빌드에서 스트립되지 않은 경우에만 동작
        shader = GetSupportedShader(Shader.Find("Custom/WebcamChromaKey"));
        if (shader != null) return shader;

        shader = GetSupportedShader(fallbackShader);
        if (shader != null) return shader;

        // 최후 수단: 크로마키 없이 웹캠 텍스처만 출력
        Debug.LogWarning("[CameraVV] Custom/WebcamChromaKey 셰이더를 찾지 못했습니다. " +
            "인스펙터의 Chroma Key Shader 필드에 셰이더를 직접 할당하거나, " +
            "셰이더를 Assets/Resources/ 폴더로 복사하세요.");
        return GetSupportedShader(Shader.Find("Unlit/Texture"));
    }

    void Awake()
    {
        // 첫 번째 출력의 레거시 fallback 대상
        _targetRenderer = GetComponent<Renderer>();
        _selfRawImage = GetComponent<RawImage>();
        if (firstRawImage == null)
        {
            firstRawImage = _selfRawImage;
        }

        var shader = ResolveRuntimeShader();
        if (shader == null)
        {
            Debug.LogWarning("[CameraVV] No shader available. Running without chroma-key material (webcam texture only).");
            return;
        }
        _firstMaterial = new Material(shader);
        _secondMaterial = new Material(shader);

        if (firstRawImage != null)
        {
            firstRawImage.material = _firstMaterial;
        }
        else if (_targetRenderer != null)
        {
            _targetRenderer.material = _firstMaterial;
        }

        if (secondRawImage != null)
        {
            secondRawImage.material = _secondMaterial;
        }
    }

    void Start()
    {
        foreach (var text in SettingTexts)
        {
            FadeManager.Instance.SetAlphaOne(text);
        }
        ApplyDisplayRouting();

        // 자동 재생 옵션이 켜져 있으면 시작 시 웹캠을 실행
        if (autoPlay)
        {
            StartWebcam();
        }

        StartCoroutine(StopAutoKeyAfterDelay(10f));
    }

    IEnumerator StopAutoKeyAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        autoKeyColor = false;
        foreach (var text in SettingTexts)
        {
            FadeManager.Instance.SetAlphaZero(text);
        }
        Debug.Log($"[CameraVV] Auto key color stopped after {delay} seconds.");
    }

    void Update()
    {
        if (_firstWebcam == null && _secondWebcam == null) return;

        // 키보드 입력으로 Threshold, Smoothness 조절
        if (Input.GetKeyDown(KeyCode.Q))
        {
            threshold = Mathf.Clamp01(threshold + 0.01f);
            Debug.Log($"Threshold: {threshold}");
        }
        if (Input.GetKeyDown(KeyCode.W))
        {
            threshold = Mathf.Clamp01(threshold - 0.01f);
            Debug.Log($"Threshold: {threshold}");
        }
        if (Input.GetKeyDown(KeyCode.A))
        {
            smoothness = Mathf.Clamp01(smoothness + 0.01f);
            Debug.Log($"Smoothness: {smoothness}");
        }
        if (Input.GetKeyDown(KeyCode.S))
        {
            smoothness = Mathf.Clamp01(smoothness - 0.01f);
            Debug.Log($"Smoothness: {smoothness}");
        }

        ApplyMaterialProperties(_firstMaterial, _firstWebcam);
        ApplyMaterialProperties(_secondMaterial, _secondWebcam);
        ApplyRawImageRotation(firstRawImage, _firstWebcam);
        ApplyRawImageRotation(secondRawImage, _secondWebcam);

        if (Input.GetKeyDown(KeyCode.Space))
        {
            isSetUp = !isSetUp;
        }
    }

    public void StartWebcam()
    {
        StopWebcam();
        _selectedPrimaryCameraIndex = -1;

        var devices = WebCamTexture.devices;

        Debug.Log($"Available webcam devices ({devices.Length}):");
        for (int i = 0; i < devices.Length; i++)
        {
            Debug.Log($"  {i}: {devices[i].name}");
        }
        Debug.Log($"Available displays ({Display.displays.Length}):");
        for (int i = 0; i < Display.displays.Length; i++)
        {
            Debug.Log($"  {i}: {Display.displays[i].systemWidth}x{Display.displays[i].systemHeight}");
        }

        if (devices.Length == 0)
        {
            Debug.LogWarning("No webcam devices found.");
            return;
        }

        StartOutputWebcam(
            devices,
            firstCameraIndex,
            string.IsNullOrEmpty(firstDeviceName) ? deviceName : firstDeviceName,
            firstRawImage,
            _targetRenderer,
            _firstMaterial,
            allowFallbackToFirstDevice: true,
            slotLabel: "First",
            disallowCameraIndex: -1,
            out _firstWebcam,
            out _selectedPrimaryCameraIndex);

        StartOutputWebcam(
            devices,
            secondCameraIndex,
            secondDeviceName,
            secondRawImage,
            null,
            _secondMaterial,
            allowFallbackToFirstDevice: false,
            slotLabel: "Second",
            disallowCameraIndex: _selectedPrimaryCameraIndex,
            out _secondWebcam,
            out _);

        if (_firstWebcam == null && _secondWebcam == null)
        {
            return;
        }

        ApplyDisplayRouting();

        if (autoKeyColor)
        {
            _autoKeyRoutine = StartCoroutine(AutoPickKeyColorRoutine());

        }
    }

    public void StopWebcam()
    {
        StopSingleWebcam(ref _firstWebcam, firstRawImage);
        StopSingleWebcam(ref _secondWebcam, secondRawImage);

        if (_autoKeyRoutine != null)
        {
            StopCoroutine(_autoKeyRoutine);
            _autoKeyRoutine = null;
        }
    }

    void OnDisable()
    {
        // 비활성화 시 웹캠 정지
        StopWebcam();
    }

    void OnDestroy()
    {
        StopWebcam();
        if (_firstMaterial != null)
        {
            Destroy(_firstMaterial);
            _firstMaterial = null;
        }
        if (_secondMaterial != null)
        {
            Destroy(_secondMaterial);
            _secondMaterial = null;
        }
    }

    IEnumerator AutoPickKeyColorRoutine()
    {
        if (autoKeyColor == false)
        {
            yield break;
        }
        while (autoKeyColor)
        {
            var sourceWebcam = GetAutoKeySourceWebcam();
            if (sourceWebcam == null)
            {
                yield break;
            }

            if (sourceWebcam.width > 16 && sourceWebcam.height > 16)
            {
                break;
            }

            yield return null;
        }

        var wait = new WaitForSeconds(Mathf.Max(1f, autoKeyInterval));

        Debug.Log($"Auto Color Value: {keyColor}");
        while (GetAutoKeySourceWebcam() != null && autoKeyColor)
        {
            if (isSetUp == false)
                TryUpdateKeyColorFromWebcam();
            yield return wait;
        }
    }

    void TryUpdateKeyColorFromWebcam()
    {
        Debug.Log("Auto picking key color from webcam...");
        var sourceWebcam = GetAutoKeySourceWebcam();
        if (sourceWebcam == null || !sourceWebcam.isPlaying) return;
        int w = sourceWebcam.width;
        int h = sourceWebcam.height;
        if (w <= 0 || h <= 0) return;

        Color32[] pixels;
        try
        {
            pixels = sourceWebcam.GetPixels32();
        }
        catch
        {
            return;
        }
        if (pixels == null || pixels.Length == 0) return;

        int x0 = w / 4;
        int y0 = h / 4;
        int x1 = (w * 3) / 4;
        int y1 = (h * 3) / 4;

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
            float r = (float)sr / (255f * count);
            float g = (float)sg / (255f * count);
            float b = (float)sb / (255f * count);
            keyColor = new Color(r, g, b, 1f);
        }
    }

    // Convenience setters for runtime UI bindings
    // 런타임 UI 바인딩을 위한 간단한 세터들
    public void SetKeyColor(Color c) { keyColor = c; }
    public void SetThreshold(float t) { threshold = t; }
    public void SetSmoothness(float s) { smoothness = s; }
    public void SetSpill(float v) { spillReduction = v; }
    public void SetOpaqueToBlack(bool v) { opaqueToBlack = v; }
    public void SetEdgeContrast(float v) { edgeContrast = v; }
    public void SetNoiseFilter(float v) { noiseFilter = Mathf.Clamp01(v); }

    void ApplyDisplayRouting()
    {
        if (!routeToDisplay) return;

        int rawDisplayIndex = targetDisplay;
        if (useCameraDisplayRoutes && _selectedPrimaryCameraIndex >= 0)
        {
            for (int i = 0; i < cameraDisplayRoutes.Count; i++)
            {
                var route = cameraDisplayRoutes[i];
                if (route != null && route.cameraIndex == _selectedPrimaryCameraIndex)
                {
                    rawDisplayIndex = route.displayIndex;
                    break;
                }
            }
        }

        int displayIndex = Mathf.Clamp(rawDisplayIndex, 0, Mathf.Max(0, Display.displays.Length - 1));

        if (activateDisplayOnStart && displayIndex > 0 && !_activatedDisplays.Contains(displayIndex))
        {
            Display.displays[displayIndex].Activate();
            _activatedDisplays.Add(displayIndex);
            Debug.Log($"Activated display {displayIndex}: {Display.displays[displayIndex].systemWidth}x{Display.displays[displayIndex].systemHeight}");
        }

        if (_selectedPrimaryCameraIndex >= 0)
        {
            Debug.Log($"Camera->Display route: camera {_selectedPrimaryCameraIndex} -> display {displayIndex}");
        }

        var cam = GetComponent<Camera>();
        if (cam != null)
        {
            cam.targetDisplay = displayIndex;
        }

        var canvas = GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            canvas.targetDisplay = displayIndex;
        }
    }

    void ApplyMaterialProperties(Material material, WebCamTexture webcam)
    {
        if (material == null || webcam == null) return;

        if (material.HasProperty("_KeyColor")) material.SetColor("_KeyColor", keyColor);
        if (material.HasProperty("_Threshold")) material.SetFloat("_Threshold", threshold);
        if (material.HasProperty("_Smooth")) material.SetFloat("_Smooth", smoothness);

        /*
        if (material.HasProperty("_KeyColor2")) material.SetColor("_KeyColor2", keyColor2);
        if (material.HasProperty("_Threshold2")) material.SetFloat("_Threshold2", threshold2);
        if (material.HasProperty("_Smooth2")) material.SetFloat("_Smooth2", smoothness2);
        if (material.HasProperty("_UseSecondKey")) material.SetFloat("_UseSecondKey", useSecondKey ? 1f : 0f);
        */

        if (material.HasProperty("_Spill")) material.SetFloat("_Spill", spillReduction);
        if (material.HasProperty("_Mirror")) material.SetFloat("_Mirror", mirrorHorizontal ? 1f : 0f);
        if (material.HasProperty("_VFlip")) material.SetFloat("_VFlip", mirrorVertical ^ webcam.videoVerticallyMirrored ? 1f : 0f);
        if (material.HasProperty("_OpaqueToBlack")) material.SetFloat("_OpaqueToBlack", opaqueToBlack ? 1f : 0f);
        if (material.HasProperty("_EdgeContrast")) material.SetFloat("_EdgeContrast", edgeContrast);
        if (material.HasProperty("_NoiseFilter")) material.SetFloat("_NoiseFilter", noiseFilter);
    }

    void ApplyRawImageRotation(RawImage targetRawImage, WebCamTexture webcam)
    {
        if (targetRawImage == null || webcam == null) return;

        // var rt = targetRawImage.rectTransform;
        // rt.localEulerAngles = new Vector3(0f, 0f, -webcam.videoRotationAngle);
    }

    void StartOutputWebcam(
        WebCamDevice[] devices,
        int requestedCameraIndex,
        string preferredDeviceName,
        RawImage targetRawImage,
        Renderer targetRenderer,
        Material targetMaterial,
        bool allowFallbackToFirstDevice,
        string slotLabel,
        int disallowCameraIndex,
        out WebCamTexture webcam,
        out int selectedCameraIndex)
    {
        webcam = null;
        selectedCameraIndex = -1;

        if (targetRawImage == null && targetRenderer == null)
        {
            return;
        }

        int resolvedIndex = FindDeviceIndex(devices, preferredDeviceName, requestedCameraIndex, allowFallbackToFirstDevice, disallowCameraIndex);
        if (resolvedIndex < 0 || resolvedIndex >= devices.Length)
        {
            Debug.LogWarning($"[CameraVV] {slotLabel} output could not resolve a webcam. Requested index: {requestedCameraIndex}, device name: {preferredDeviceName}");
            return;
        }

        var selected = devices[resolvedIndex];
        var candidateWebcam = new WebCamTexture(selected.name, requestedWidth, requestedHeight, requestedFPS);

        try
        {
            candidateWebcam.Play();
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[CameraVV] {slotLabel} output failed to start camera {resolvedIndex} ({selected.name}). {ex.Message}");
            return;
        }

        if (!candidateWebcam.isPlaying)
        {
            Debug.LogWarning($"[CameraVV] {slotLabel} output camera {resolvedIndex} ({selected.name}) did not start. Another app may already be using this camera.");
            return;
        }

        webcam = candidateWebcam;
        selectedCameraIndex = resolvedIndex;

        if (targetMaterial != null)
        {
            targetMaterial.mainTexture = webcam;
        }

        if (targetRawImage != null)
        {
            targetRawImage.texture = webcam;
        }

        if (targetRenderer != null)
        {
            if (targetMaterial != null)
            {
                targetRenderer.material = targetMaterial;
            }
            else
            {
                targetRenderer.material.mainTexture = webcam;
            }
        }

        Debug.Log($"[CameraVV] {slotLabel} output -> camera {resolvedIndex}: {selected.name}");
    }

    int FindDeviceIndex(WebCamDevice[] devices, string preferredDeviceName, int requestedCameraIndex, bool allowFallbackToFirstDevice, int disallowCameraIndex)
    {
        // 내부 헬퍼: 사용 가능한지 체크
        bool IsUsable(int index, string name)
        {
            if (index == disallowCameraIndex) return false;

            // 디버그 옵션이 켜져 있을 때만 예외 처리
            if (debugIgnoreCameraZero && index == 0) return false;

            // 이름으로 거르기
            if (!string.IsNullOrEmpty(ignoreDeviceName) && name.Contains(ignoreDeviceName))
            {
                return false;
            }

            return true;
        }

        // 1. 선호 이름(preferredDeviceName) 검색
        if (!string.IsNullOrEmpty(preferredDeviceName))
        {
            for (int i = 0; i < devices.Length; i++)
            {
                if (!IsUsable(i, devices[i].name)) continue;

                if (devices[i].name.Contains(preferredDeviceName))
                {
                    return i;
                }
            }
        }

        // 2. 요청 인덱스(requestedCameraIndex) 확인
        //    요청된 인덱스라 하더라도 무시 조건(MacBook Pro 등)에 걸리면 건너뛰고 Fallback으로 감
        if (requestedCameraIndex >= 0 && requestedCameraIndex < devices.Length)
        {
            if (IsUsable(requestedCameraIndex, devices[requestedCameraIndex].name))
            {
                return requestedCameraIndex;
            }
        }

        // 3. Fallback: 조건에 맞는 아무 카메라나 앞에서부터 찾기
        if (allowFallbackToFirstDevice && devices.Length > 0)
        {
            for (int i = 0; i < devices.Length; i++)
            {
                if (IsUsable(i, devices[i].name))
                {
                    return i;
                }
            }
        }

        return -1;
    }

    void StopSingleWebcam(ref WebCamTexture webcam, RawImage targetRawImage)
    {
        if (webcam != null)
        {
            if (webcam.isPlaying) webcam.Stop();
            if (targetRawImage != null && targetRawImage.texture == webcam)
            {
                targetRawImage.texture = null;
            }
            webcam = null;
        }
    }

    WebCamTexture GetAutoKeySourceWebcam()
    {
        if (_firstWebcam != null) return _firstWebcam;
        if (_secondWebcam != null) return _secondWebcam;
        return null;
    }
}
