using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CameraVV : MonoBehaviour
{
    [System.Serializable]
    public class CameraDisplayRoute
    {
        [Tooltip("WebCamTexture.devices의 카메라 인덱스")]
        public int cameraIndex = 0;
        [Tooltip("매핑할 디스플레이 인덱스(0=메인)")]
        public int displayIndex = 0;
    }

    // --------------------------------------------
    // 웹캠 + 컬러키(특정 색 투명 처리) 컴포넌트
    // - 이 스크립트를 UI의 RawImage 또는 MeshRenderer가 달린 오브젝트에 붙여서 사용합니다.
    // - 내부에서 "Custom/WebcamChromaKey" 셰이더를 찾아 머티리얼을 생성/적용하고,
    //   웹캠 텍스처를 실시간으로 출력합니다.
    // - 인스펙터에서 투명 처리할 키 컬러(임의 색), 임계값, 부드러움을 조절하세요.
    // --------------------------------------------

    [Header("Webcam Settings")]
    public string deviceName = "";
    public int requestedWidth = 1280;
    public int requestedHeight = 720;
    public int requestedFPS = 30;
    public bool autoPlay = true;
    public bool mirrorHorizontal = true;
    public bool mirrorVertical = false;

    [Header("Color Key Settings")]
    public Color keyColor = Color.green;
    [Range(0f, 1f)] public float threshold = 0.4f;
    [Range(0f, 1f)] public float smoothness = 0.1f;
    [Range(0f, 1f)] public float spillReduction = 0.2f; // 단순 컬러키에서는 사용하지 않음
    [Tooltip("웹캠 화면에서 주기적으로 키 컬러를 자동 추출합니다.")]
    public bool autoKeyColor = true;
    [Tooltip("키 컬러 자동 추출 간격(초)")]
    [Range(1f, 120f)] public float autoKeyInterval = 5f;
    [Header("Output Settings")]
    public bool opaqueToBlack = true; // 전경(불투명) 영역을 검은색으로 출력
    [Range(1f, 10f)] public float edgeContrast = 1f; // 경계 선명도(콘트라스트)

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
    [Tooltip("예: cameraIndex=0, displayIndex=1 => 0번 카메라는 1번 디스플레이로 라우팅")]
    public List<CameraDisplayRoute> cameraDisplayRoutes = new List<CameraDisplayRoute>
    {
        new CameraDisplayRoute { cameraIndex = 0, displayIndex = 1 }
    };

    WebCamTexture _webcam;
    Material _material;
    Renderer _targetRenderer;
    RawImage _targetRawImage;
    Coroutine _autoKeyRoutine;
    static readonly HashSet<int> _activatedDisplays = new HashSet<int>();
    int _selectedCameraIndex = -1;

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
        // 출력 대상 컴포넌트 캐시: MeshRenderer 또는 RawImage
        _targetRenderer = GetComponent<Renderer>();
        _targetRawImage = GetComponent<RawImage>();

        var shader = ResolveRuntimeShader();
        if (shader == null)
        {
            Debug.LogWarning("[CameraVV] No shader available. Running without chroma-key material (webcam texture only).");
            return;
        }
        _material = new Material(shader);

        // 대상 렌더러/RawImage에 머티리얼 적용
        if (_targetRenderer != null)
        {
            _targetRenderer.material = _material;
        }
        if (_targetRawImage != null)
        {
            _targetRawImage.material = _material;
        }
    }

    void Start()
    {
        ApplyDisplayRouting();

        // 자동 재생 옵션이 켜져 있으면 시작 시 웹캠을 실행
        if (autoPlay)
        {
            StartWebcam();
        }
    }

    void Update()
    {
        // 웹캠이 아직 준비되지 않았으면 리턴
        if (_webcam == null) return;

        // 셰이더 프로퍼티 갱신(인스펙터에서 실시간 조절 시 반영)
        if (_material != null)
        {
            if (_material.HasProperty("_KeyColor")) _material.SetColor("_KeyColor", keyColor);
            if (_material.HasProperty("_Threshold")) _material.SetFloat("_Threshold", threshold);
            if (_material.HasProperty("_Smooth")) _material.SetFloat("_Smooth", smoothness);
            if (_material.HasProperty("_Spill")) _material.SetFloat("_Spill", spillReduction);
            if (_material.HasProperty("_Mirror")) _material.SetFloat("_Mirror", mirrorHorizontal ? 1f : 0f);
            if (_material.HasProperty("_VFlip")) _material.SetFloat("_VFlip", mirrorVertical ^ _webcam.videoVerticallyMirrored ? 1f : 0f);
            if (_material.HasProperty("_OpaqueToBlack")) _material.SetFloat("_OpaqueToBlack", opaqueToBlack ? 1f : 0f);
            if (_material.HasProperty("_EdgeContrast")) _material.SetFloat("_EdgeContrast", edgeContrast);
        }

        // (옵션) 웹캠의 회전 각도에 맞춰 RawImage를 회전
        if (_targetRawImage != null)
        {
            var rt = _targetRawImage.rectTransform;
            rt.localEulerAngles = new Vector3(0f, 0f, -_webcam.videoRotationAngle);
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            isSetUp = !isSetUp;
        }
    }

    public void StartWebcam()
    {
        // 기존 웹캠 정지 후 새로 시작
        StopWebcam();
        _selectedCameraIndex = -1;

        // 연결 가능한 웹캠 목록에서 선택
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

        WebCamDevice selected = default;
        bool found = false;
        if (!string.IsNullOrEmpty(deviceName))
        {
            for (int i = 0; i < devices.Length; i++)
            {
                var d = devices[i];
                if (d.name.Contains(deviceName))
                {
                    selected = d;
                    _selectedCameraIndex = i;
                    found = true;
                    break;
                }
            }
        }
        if (!found && devices.Length > 0)
        {
            selected = devices[0];
            _selectedCameraIndex = 0;
            found = true;
        }

        if (!found)
        {
            Debug.LogWarning("No webcam devices found.");
            return;
        }

        // 요청한 해상도/FPS로 웹캠 텍스처 생성
        _webcam = new WebCamTexture(selected.name, requestedWidth, requestedHeight, requestedFPS);
        if (_material != null)
        {
            // 머티리얼의 메인 텍스처로 웹캠 설정
            _material.mainTexture = _webcam;
        }
        if (_targetRawImage != null)
        {
            // UI 출력용으로 RawImage에도 텍스처 적용
            _targetRawImage.texture = _webcam;
        }
        // 재생 시작
        _webcam.Play();

        // 선택된 카메라 인덱스 기준으로 디스플레이 라우팅 재적용
        ApplyDisplayRouting();

        // 키 컬러 자동 선택 코루틴 시작
        if (autoKeyColor)
        {
            _autoKeyRoutine = StartCoroutine(AutoPickKeyColorRoutine());
        }
    }

    public void StopWebcam()
    {
        // 웹캠 정지 및 참조 해제
        if (_webcam != null)
        {
            if (_webcam.isPlaying) _webcam.Stop();
            _webcam = null;
        }

        // 자동 키 컬러 코루틴 중지
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
        // 파괴 시 웹캠/머티리얼 정리
        StopWebcam();
        if (_material != null)
        {
            Destroy(_material);
            _material = null;
        }
    }

    IEnumerator AutoPickKeyColorRoutine()
    {
        // 웹캠이 준비될 때까지 대기
        while (_webcam != null && (_webcam.width <= 16 || _webcam.height <= 16))
        {
            yield return null; // 다음 프레임까지 대기
        }

        // 주기적으로 키 컬러를 추출
        var wait = new WaitForSeconds(Mathf.Max(1f, autoKeyInterval));
        while (_webcam != null)
        {
            if (isSetUp == false)
                TryUpdateKeyColorFromWebcam();
            yield return wait;
        }
    }

    void TryUpdateKeyColorFromWebcam()
    {
        Debug.Log("Auto picking key color from webcam...");
        if (_webcam == null || !_webcam.isPlaying) return;
        int w = _webcam.width;
        int h = _webcam.height;
        if (w <= 0 || h <= 0) return;

        // 픽셀 데이터 가져오기
        Color32[] pixels;
        try
        {
            pixels = _webcam.GetPixels32();
        }
        catch
        {
            return; // 드문 플랫폼 이슈로 실패 시 건너뜀
        }
        if (pixels == null || pixels.Length == 0) return;

        // 중앙 영역(50%)만 샘플링해서 평균 색을 구합니다.
        int x0 = w / 4;
        int y0 = h / 4;
        int x1 = (w * 3) / 4;
        int y1 = (h * 3) / 4;

        // 그리드 샘플 간격(너무 많은 샘플을 피해서 성능 확보)
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
            // 알파는 사용하지 않으므로 1로 고정
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

    void ApplyDisplayRouting()
    {
        if (!routeToDisplay) return;

        int rawDisplayIndex = targetDisplay;
        if (useCameraDisplayRoutes && _selectedCameraIndex >= 0)
        {
            for (int i = 0; i < cameraDisplayRoutes.Count; i++)
            {
                var route = cameraDisplayRoutes[i];
                if (route != null && route.cameraIndex == _selectedCameraIndex)
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

        if (_selectedCameraIndex >= 0)
        {
            Debug.Log($"Camera->Display route: camera {_selectedCameraIndex} -> display {displayIndex}");
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
}
