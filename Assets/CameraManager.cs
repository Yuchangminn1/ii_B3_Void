using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 기존 CameraInstanceSettings는 CameraValue.cs로 대체되었으므로 삭제

public class CameraManager : MonoBehaviour
{
    [Header("Camera Control")]
    [Tooltip("관리할 카메라 인스턴스 (자동으로 할당됨)")]
    public List<CameraValue> cameraInstances = new List<CameraValue>();

    [Tooltip("시작 시 자동으로 모든 카메라 재생")]
    public bool autoPlay = true;

    [Header("Global Shader Settings")]
    [Tooltip("빌드에서 스트립되지 않도록 인스펙터에 직접 할당 권장")]
    public Shader chromaKeyShader;
    [Tooltip("크로마키 셰이더를 못 찾을 때 사용할 안전한 대체 셰이더")]
    public Shader fallbackShader;

    [Header("UI & Control")]
    [Tooltip("현재 선택된 카메라의 설정을 표시할 UI 텍스트")]

    public CameraVisible cameraVisible;

    private int _currentlyControlledIndex = 0;

    const int defaultFocusTime = 20;

    public Text SelectCameraText;

    Shader GetSupportedShader(Shader shader)
    {
        if (shader == null) return null;
        if (!shader.isSupported)
        {
            Debug.LogWarning($"[CameraManager] Shader not supported on this platform: {shader.name}");
            return null;
        }
        return shader;
    }

    Shader ResolveRuntimeShader()
    {
        var shader = GetSupportedShader(chromaKeyShader);
        if (shader != null) return shader;
        var resShader = Resources.Load<Shader>("WebcamChromaKey");
        shader = GetSupportedShader(resShader);
        if (shader != null) return shader;
        shader = GetSupportedShader(Shader.Find("Custom/WebcamChromaKey"));
        if (shader != null) return shader;
        shader = GetSupportedShader(fallbackShader);
        if (shader != null) return shader;
        Debug.LogWarning("[CameraManager] Custom/WebcamChromaKey 셰이더를 찾지 못했습니다.");
        return GetSupportedShader(Shader.Find("Unlit/Texture"));
    }

    void Start()
    {
        var shader = ResolveRuntimeShader();
        if (shader == null)
        {
            Debug.LogError("[CameraManager] No available shader found. Aborting setup.");
            return;
        }

        foreach (var instance in cameraInstances)
        {
            if (instance != null)
            {
                instance.Initialize(shader);
            }
        }

        if (autoPlay)
        {
            StartAllWebcams();
        }
        UpdateSelectionText();
        StartFocusCheck();
    }

    void Update()
    {
        if (cameraInstances.Count == 0) return;

        HandleCameraSwitching();
        HandleSettingsInput();

        // 모든 활성 카메라 업데이트 호출
        foreach (var instance in cameraInstances)
        {
            if (instance != null)
            {
                instance.ApplyMaterialProperties();
            }
        }
    }

    void HandleCameraSwitching()
    {
        if (Input.GetKeyDown(KeyCode.B))
        {
            _currentlyControlledIndex--;
            if (_currentlyControlledIndex < 0)
            {
                _currentlyControlledIndex = cameraInstances.Count - 1;
            }
            UpdateSelectionText();
            Debug.Log($"Controlling Camera: {cameraInstances[_currentlyControlledIndex].name}");
        }

        if (Input.GetKeyDown(KeyCode.N))
        {
            _currentlyControlledIndex++;
            if (_currentlyControlledIndex >= cameraInstances.Count)
            {
                _currentlyControlledIndex = 0;
            }
            UpdateSelectionText();
            Debug.Log($"Controlling Camera: {cameraInstances[_currentlyControlledIndex].name}");
        }
    }

    void HandleSettingsInput()
    {
        if (_currentlyControlledIndex < 0 || _currentlyControlledIndex >= cameraInstances.Count) return;

        var current = cameraInstances[_currentlyControlledIndex];
        if (current == null) return;

        if (Input.GetKeyDown(KeyCode.Q))
        {
            current.threshold = Mathf.Clamp01(current.threshold + 0.01f);
            current.StopAutoThreshold();
            Debug.Log($"[{current.name}] Threshold: {current.threshold:F2}");
        }
        if (Input.GetKeyDown(KeyCode.W))
        {
            current.threshold = Mathf.Clamp01(current.threshold - 0.01f);
            current.StopAutoThreshold();
            Debug.Log($"[{current.name}] Threshold: {current.threshold:F2}");
        }

        if (Input.GetKeyDown(KeyCode.E))
        {
            current.smoothness = Mathf.Clamp01(current.smoothness + 0.01f);
            Debug.Log($"[{current.name}] Smoothness: {current.smoothness:F2}");
        }
        if (Input.GetKeyDown(KeyCode.R))
        {
            current.smoothness = Mathf.Clamp01(current.smoothness - 0.01f);
            Debug.Log($"[{current.name}] Smoothness: {current.smoothness:F2}");
        }

        if (Input.GetKeyDown(KeyCode.T))
        {
            current.opaqueToBlack = !current.opaqueToBlack;
            Debug.Log($"[{current.name}] OpaqueToBlack: {current.opaqueToBlack}");
        }

        if (Input.GetKeyDown(KeyCode.Y))
        {
            current.RenderByBool = !current.RenderByBool;
            Debug.Log($"[{current.name}] RenderByBool: {current.RenderByBool}");
        }

        if (Input.GetKeyDown(KeyCode.U))
        {
            current.UseShaderOutput = !current.UseShaderOutput;
            Debug.Log($"[{current.name}] UseShaderOutput: {current.UseShaderOutput}");
        }

        if (Input.GetKeyDown(KeyCode.O))
        {
            AdjustSelectedClipPixels(0, 10, 0, 0);
            Debug.Log($"[{current.name}] Left Clip Pixels: {current.leftClipPixels}");
        }
        if (Input.GetKeyDown(KeyCode.P))
        {
            AdjustSelectedClipPixels(0, -10, 0, 0);
            Debug.Log($"[{current.name}] Left Clip Pixels: {current.leftClipPixels}");
        }

        if (Input.GetKeyDown(KeyCode.A))
        {
            current.StartAutoKeyForSeconds(defaultFocusTime);
            Debug.Log($"[{current.name}] Auto Key Started for {defaultFocusTime} seconds");
        }
    }

    public CameraValue GetSelectedCameraValue()
    {
        if (_currentlyControlledIndex < 0 || _currentlyControlledIndex >= cameraInstances.Count) return null;
        return cameraInstances[_currentlyControlledIndex];
    }

    public void SetSelectedClipPixels(int left, int right, int top, int bottom)
    {
        var current = GetSelectedCameraValue();
        if (current == null) return;
        current.SetClipPixels(left, right, top, bottom);
    }

    public void AdjustSelectedClipPixels(int leftDelta, int rightDelta, int topDelta, int bottomDelta)
    {
        var current = GetSelectedCameraValue();
        if (current == null) return;
        current.AddClipPixels(leftDelta, rightDelta, topDelta, bottomDelta);
    }

    public IEnumerator StartFocusCheckCoroutine()
    {
        yield return CoroutineReturnManager.GetWaitForSeconds(5f);

        while (GameManager.Instance.IsStarted == false)
        {
            yield return CoroutineReturnManager.GetWaitForSeconds(0.5f);
        }
        cameraVisible.CameraOn();
        yield return CoroutineReturnManager.GetWaitForSeconds(3f);
        foreach (var instance in cameraInstances)
        {
            if (instance != null)
            {
                instance.StartAutoKeyForSeconds(defaultFocusTime);
            }
        }




    }

    public void StartFocusCheck()
    {

        StartCoroutine(StartFocusCheckCoroutine());
    }

    void UpdateSelectionText()
    {
        if (cameraInstances[_currentlyControlledIndex] != null && cameraInstances.Count > 0)
        {
            FadeManager.Instance.SetAlphaOne(SelectCameraText);
            SelectCameraText.text = $"Selected: {cameraInstances[_currentlyControlledIndex].name}";

        }
    }

    public void StartAllWebcams()
    {
        var devices = WebCamTexture.devices;
        if (devices.Length == 0)
        {
            Debug.LogWarning("No webcam devices found.");
            return;
        }

        foreach (var instance in cameraInstances)
        {
            if (instance != null)
            {
                instance.StartWebcam(devices);
            }
        }
    }

    public void StopAllWebcams()
    {
        foreach (var instance in cameraInstances)
        {
            if (instance != null)
            {
                instance.StopWebcam();
            }
        }
    }

    void OnDestroy()
    {
        StopAllWebcams();
    }

    [ContextMenu("Find and Assign Cameras")]
    void FindAndAssignCameras()
    {
        Debug.Log("[CameraManager] Finding RawImages and assigning cameras...");

        // 씬에서 모든 활성 RawImage 찾기
        var rawImages = FindObjectsOfType<RawImage>();
        if (rawImages == null || rawImages.Length == 0)
        {
            Debug.LogWarning("[CameraManager] No active RawImage components found in the scene.");
            return;
        }

        // 사용 가능한 웹캠 목록 가져오기
        var devices = WebCamTexture.devices;
        if (devices.Length == 0)
        {
            Debug.LogWarning("[CameraManager] No webcams found.");
        }

        // 기존 목록 지우기
        cameraInstances.Clear();

        // RawImage를 이름순으로 정렬하여 일관된 순서 보장
        Array.Sort(rawImages, (a, b) => a.name.CompareTo(b.name));

        for (int i = 0; i < rawImages.Length; i++)
        {
            // 각 RawImage 오브젝트에 CameraValue 컴포넌트가 없으면 추가
            var cv = rawImages[i].GetComponent<CameraValue>();
            if (cv == null)
            {
                cv = rawImages[i].gameObject.AddComponent<CameraValue>();
            }

            // 웹캠 할당 (이름 또는 인덱스)
            if (devices.Length > 0)
            {
                int deviceIndex = i % devices.Length;
                cv.cameraIndex = deviceIndex;
                cv.deviceName = devices[deviceIndex].name;
            }
            else
            {
                cv.cameraIndex = i;
            }

            cameraInstances.Add(cv);
            Debug.Log($"Assigned Camera Index {cv.cameraIndex} ({cv.deviceName}) to RawImage '{rawImages[i].name}'");
        }

        Debug.Log($"[CameraManager] Successfully assigned {cameraInstances.Count} cameras.");
    }
}
