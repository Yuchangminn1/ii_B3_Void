using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Networking;
using UnityEngine.UI;

public class StopMotionCreateSingle : MonoBehaviour
{
    public static Action OnCreateFinished;

    private const int BackgroundChangeInterval = 10;
    private const int TargetFrameCount = 100;

    [Header("Frame Sources")]
    [SerializeField] private List<RawImage> backgroundImages = new List<RawImage>();
    [SerializeField] private SaveShadowTextureContainer leftFrameContainer;
    [SerializeField] private SaveShadowTextureContainer rightFrameContainer;

    [Header("Composite Display")]
    [SerializeField] private Canvas captureCanvas;
    [SerializeField] private RawImage backgroundDisplayImage;
    [SerializeField] private RawImage leftDisplayImage;
    [SerializeField] private RawImage rightDisplayImage;
    [SerializeField] private Vector2Int defaultCanvasCaptureResolution = new Vector2Int(2250, 4000);

    [Header("Output")]
    private string saveFolderName = "Pictures";
    private string outputName = "StopMotionSingle";
    [SerializeField] private int outputFps = 12;
    [SerializeField] private bool saveVideoToComputer = true;

    [Header("Video Encode")]
    string ffmpegExecutablePath = @"C:\ProgramData\chocolatey\bin\ffmpeg.exe";
    [SerializeField] private bool deletePngSequenceAfterMp4 = true;

    [Header("Upload")]
    [SerializeField] private string uploadUrl = "http://192.168.0.252:8500/api/uploadFile.cfm";
    [SerializeField] private int maxRetries = 10;
    [SerializeField] private float retryDelay = 1.0f;

    public bool IsProcessing { get; private set; }

    private GameObject _captureCameraObject;
    private Camera _captureCamera;
    private RenderTexture _captureRenderTexture;
    private Texture2D _capturedCanvasTexture;
    private readonly List<SaveShadowTextureContainer.SaveShadowCapturedFrame> _resolvedLeftFrames = new List<SaveShadowTextureContainer.SaveShadowCapturedFrame>(TargetFrameCount);
    private readonly List<SaveShadowTextureContainer.SaveShadowCapturedFrame> _resolvedRightFrames = new List<SaveShadowTextureContainer.SaveShadowCapturedFrame>(TargetFrameCount);

    private enum SingleOutputSide
    {
        Left,
        Right
    }

    [ContextMenu("Create Left And Right StopMotion")]
    public void CreateNow()
    {
        if (IsProcessing)
        {
            Debug.LogWarning("[StopMotionCreateSingle] 이전 작업이 아직 진행 중입니다.");
            return;
        }

        StartCoroutine(CreateStopMotionRoutine());
    }

    private IEnumerator CreateStopMotionRoutine()
    {
        IsProcessing = true;

        bool originalLeftEnabled = leftDisplayImage != null && leftDisplayImage.enabled;
        bool originalRightEnabled = rightDisplayImage != null && rightDisplayImage.enabled;

        try
        {
            if (!ValidateInputs(out int frameCount))
            {
                yield break;
            }

            if (!HasFfmpeg())
            {
                Debug.LogError($"[StopMotionCreateSingle] ffmpeg 경로가 유효하지 않습니다: {ffmpegExecutablePath}");
                yield break;
            }

            string rootPath = GetRootPath();
            Debug.Log($"[StopMotionCreateSingle] 파일 저장 경로: {rootPath}");

            yield return CaptureSideSequence(rootPath, SingleOutputSide.Left, _resolvedLeftFrames, frameCount);
            yield return CaptureSideSequence(rootPath, SingleOutputSide.Right, _resolvedRightFrames, frameCount);
        }
        finally
        {
            if (leftDisplayImage != null)
            {
                leftDisplayImage.texture = null;
                leftDisplayImage.enabled = originalLeftEnabled;
            }

            if (rightDisplayImage != null)
            {
                rightDisplayImage.texture = null;
                rightDisplayImage.enabled = originalRightEnabled;
            }

            ReleaseCaptureResources();
            IsProcessing = false;

            try
            {
                OnCreateFinished?.Invoke();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[StopMotionCreateSingle] OnCreateFinished 실행 예외: {ex.Message}");
            }
        }
    }

    private IEnumerator CaptureSideSequence(string rootPath, SingleOutputSide side, List<SaveShadowTextureContainer.SaveShadowCapturedFrame> frames, int frameCount)
    {
        string sideName = side == SingleOutputSide.Left ? "Left" : "Right";
        RawImage activeDisplay = side == SingleOutputSide.Left ? leftDisplayImage : rightDisplayImage;
        RawImage inactiveDisplay = side == SingleOutputSide.Left ? rightDisplayImage : leftDisplayImage;

        if (activeDisplay == null)
        {
            Debug.LogError($"[StopMotionCreateSingle] {sideName} 표시용 RawImage가 없습니다.");
            yield break;
        }

        string sideRootPath = Path.Combine(rootPath, sideName);
        string framesFolder = Path.Combine(sideRootPath, "Frames");
        Directory.CreateDirectory(framesFolder);

        Debug.Log($"[StopMotionCreateSingle] {sideName} 저장 경로: {framesFolder}");

        List<string> pngPaths = new List<string>(frameCount);
        // RawImage currentBackgroundImage = null;

        if (inactiveDisplay != null)
        {
            inactiveDisplay.texture = null;
            inactiveDisplay.enabled = false;
        }

        activeDisplay.enabled = true;

        try
        {
            for (int i = 0; i < frameCount; i++)
            {
                // RawImage backgroundImage = GetBackgroundForFrame(i);
                SaveShadowTextureContainer.SaveShadowCapturedFrame frame = frames[i];
                Texture2D texture = frame.Texture as Texture2D;

                if (texture == null)
                {
                    Debug.LogError($"[StopMotionCreateSingle] {sideName} null 프레임 소스 발견: frame={i} (작업 중단)");
                    yield break;
                }

                // if (backgroundImage != currentBackgroundImage)
                // {
                //     ApplyBackgroundImage(backgroundImage);
                //     currentBackgroundImage = backgroundImage;
                // }

                // ApplyCapturedFrameLayout(activeDisplay, frame);
                activeDisplay.texture = texture;
                Canvas.ForceUpdateCanvases();

                Texture2D compositeFrame = CaptureCanvasToTexture(captureCanvas);
                if (compositeFrame == null)
                {
                    Debug.LogError($"[StopMotionCreateSingle] {sideName} 캔버스 캡처 실패: frame={i} (작업 중단)");
                    yield break;
                }

                string framePath = Path.Combine(framesFolder, $"{outputName}_{sideName}_{i:D3}.png");

                byte[] pngBytes = null;
                try
                {
                    pngBytes = compositeFrame.EncodeToPNG();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[StopMotionCreateSingle] {sideName} PNG 인코딩 실패: frame={i}, Exception: {ex.Message}\n{ex.StackTrace}");
                    continue;
                }

                if (pngBytes == null || pngBytes.Length == 0)
                {
                    Debug.LogError($"[StopMotionCreateSingle] {sideName} PNG 인코딩 실패: frame={i} (빈 바이트 배열)");
                    continue;
                }

                Task writeTask = File.WriteAllBytesAsync(framePath, pngBytes);
                yield return new WaitUntil(() => writeTask.IsCompleted);

                if (writeTask.IsFaulted)
                {
                    Debug.LogError($"[StopMotionCreateSingle] {sideName} 파일 저장 실패: frame={i}, 경로={framePath}, Exception: {writeTask.Exception?.InnerException?.Message}");
                    continue;
                }

                pngPaths.Add(framePath);
                yield return null;
            }
        }
        finally
        {
            activeDisplay.texture = null;
            activeDisplay.enabled = true;
            if (inactiveDisplay != null)
            {
                inactiveDisplay.enabled = true;
            }
        }

        if (pngPaths.Count == 0)
        {
            Debug.LogError($"[StopMotionCreateSingle] {sideName} 생성된 프레임이 없습니다.");
            yield break;
        }

        Debug.Log($"[StopMotionCreateSingle] {sideName} 총 {pngPaths.Count}개 프레임 저장 완료. 경로: {framesFolder}");

        string inputPattern = Path.Combine(framesFolder, $"{outputName}_{sideName}_%03d.png");
        string mp4Path = Path.Combine(sideRootPath, $"{outputName}_{sideName}.mp4");

        Task<bool> encodeVideoTask = EncodeMp4Async(inputPattern, mp4Path, outputFps);
        yield return new WaitUntil(() => encodeVideoTask.IsCompleted);

        if (!encodeVideoTask.Result)
        {
            yield break;
        }

        Debug.Log($"[StopMotionCreateSingle] {sideName} MP4 생성 완료: {mp4Path}");

        Task uploadVideoTask = UploadVideoAsync(mp4Path, side);
        yield return new WaitUntil(() => uploadVideoTask.IsCompleted);

        if (uploadVideoTask.IsFaulted)
        {
            Debug.LogError($"[StopMotionCreateSingle] {sideName} 업로드 예외: {uploadVideoTask.Exception?.GetBaseException().Message}");
        }

        if (deletePngSequenceAfterMp4)
        {
            DeleteGeneratedFiles(pngPaths);
        }
    }

    private bool ValidateInputs(out int frameCount)
    {
        frameCount = 0;
        ResolveFrameSources();

        // if (backgroundImages == null || backgroundImages.Count == 0)
        // {
        //     Debug.LogError("[StopMotionCreateSingle] 배경 RawImage가 비어 있습니다.");
        //     return false;
        // }

        if (leftFrameContainer == null || rightFrameContainer == null)
        {
            Debug.LogError("[StopMotionCreateSingle] 좌/우 SaveShadowTextureContainer가 필요합니다.");
            return false;
        }

        if (captureCanvas == null || leftDisplayImage == null || rightDisplayImage == null)
        {
            Debug.LogError("[StopMotionCreateSingle] 캡처용 Canvas와 표시용 RawImage 3개가 필요합니다.");
            return false;
        }

        if (_resolvedLeftFrames.Count == 0 || _resolvedRightFrames.Count == 0)
        {
            Debug.LogError("[StopMotionCreateSingle] 좌/우 컨테이너에서 가져온 텍스처가 비어 있습니다.");
            return false;
        }

        // if (backgroundImages[0] == null || backgroundImages[0].texture == null)
        // {
        //     Debug.LogError("[StopMotionCreateSingle] 첫 배경 RawImage 또는 Texture가 null입니다.");
        //     return false;
        // }

        frameCount = Mathf.Min(TargetFrameCount, _resolvedLeftFrames.Count, _resolvedRightFrames.Count);
        if (frameCount <= 0)
        {
            Debug.LogError("[StopMotionCreateSingle] 유효한 프레임 수가 없습니다.");
            return false;
        }

        return true;
    }

    private void ResolveFrameSources()
    {
        _resolvedLeftFrames.Clear();
        _resolvedRightFrames.Clear();

        TryAddFramesFromContainer(leftFrameContainer, _resolvedLeftFrames);
        TryAddFramesFromContainer(rightFrameContainer, _resolvedRightFrames);
        _resolvedRightFrames.Reverse();
    }

    private static bool TryAddFramesFromContainer(SaveShadowTextureContainer container, List<SaveShadowTextureContainer.SaveShadowCapturedFrame> destination)
    {
        if (container == null)
        {
            return false;
        }

        List<SaveShadowTextureContainer.SaveShadowCapturedFrame> captured = container.GetCapturedFrames();
        if (captured == null || captured.Count == 0)
        {
            return false;
        }

        for (int i = 0; i < captured.Count; i++)
        {
            SaveShadowTextureContainer.SaveShadowCapturedFrame frame = captured[i];
            if (frame.Texture != null)
            {
                destination.Add(frame);
            }
        }

        return true;
    }

    // private static void ApplyCapturedFrameLayout(RawImage target, SaveShadowTextureContainer.SaveShadowCapturedFrame frame)
    // {
    //     if (target == null)
    //     {
    //         return;
    //     }
    //
    //     RectTransform targetRect = target.rectTransform;
    //     if (targetRect == null)
    //     {
    //         return;
    //     }
    //
    //     targetRect.localPosition = frame.LocalPosition;
    //     targetRect.sizeDelta = frame.SizeDelta;
    //     targetRect.localRotation = frame.LocalRotation;
    //     targetRect.localScale = frame.LocalScale;
    // }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            CreateNow();
        }
    }

    private RawImage GetBackgroundForFrame(int frameIndex)
    {
        int bgCount = backgroundImages.Count;
        if (bgCount == 0)
        {
            return null;
        }

        int bgIndex = (frameIndex / BackgroundChangeInterval) % bgCount;
        return backgroundImages[bgIndex];
    }

    private void ApplyBackgroundImage(RawImage source)
    {
        if (source == null || backgroundDisplayImage == null)
        {
            return;
        }

        backgroundDisplayImage.texture = source.texture;
        backgroundDisplayImage.material = source.material;
        backgroundDisplayImage.color = source.color;
        backgroundDisplayImage.uvRect = source.uvRect;

        RectTransform sourceRect = source.rectTransform;
        RectTransform targetRect = backgroundDisplayImage.rectTransform;
        targetRect.anchorMin = sourceRect.anchorMin;
        targetRect.anchorMax = sourceRect.anchorMax;
        targetRect.pivot = sourceRect.pivot;
        targetRect.anchoredPosition = sourceRect.anchoredPosition;
        targetRect.sizeDelta = sourceRect.sizeDelta;
        targetRect.localRotation = sourceRect.localRotation;
        targetRect.localScale = sourceRect.localScale;
    }

    private Texture2D CaptureCanvasToTexture(Canvas targetCanvas)
    {
        if (targetCanvas == null || !targetCanvas.isActiveAndEnabled)
        {
            return null;
        }

        if (targetCanvas.renderMode == RenderMode.WorldSpace)
        {
            Debug.LogError($"[StopMotionCreateSingle] World Space Canvas는 캡처 대상에서 지원하지 않습니다: {targetCanvas.name}");
            return null;
        }

        Vector2Int canvasSize = GetCanvasCaptureSize(targetCanvas);
        EnsureCaptureResources(canvasSize.x, canvasSize.y);
        EnsureCaptureCamera();
        _captureCamera.targetTexture = _captureRenderTexture;

        RenderMode originalRenderMode = targetCanvas.renderMode;
        Camera originalWorldCamera = targetCanvas.worldCamera;
        float originalPlaneDistance = targetCanvas.planeDistance;
        int originalTargetDisplay = targetCanvas.targetDisplay;
        RenderTexture previous = RenderTexture.active;
        try
        {
            _captureRenderTexture.Create();
            targetCanvas.renderMode = RenderMode.ScreenSpaceCamera;
            targetCanvas.worldCamera = _captureCamera;
            targetCanvas.planeDistance = 1f;
            targetCanvas.targetDisplay = 0;

            Canvas.ForceUpdateCanvases();

            RenderTexture.active = _captureRenderTexture;
            GL.Clear(true, true, Color.clear);
            _captureCamera.Render();

            _capturedCanvasTexture.ReadPixels(new Rect(0, 0, canvasSize.x, canvasSize.y), 0, 0);
            _capturedCanvasTexture.Apply(false, false);
            return _capturedCanvasTexture;
        }
        finally
        {
            RenderTexture.active = previous;
            targetCanvas.renderMode = originalRenderMode;
            targetCanvas.worldCamera = originalWorldCamera;
            targetCanvas.planeDistance = originalPlaneDistance;
            targetCanvas.targetDisplay = originalTargetDisplay;
            _captureCamera.targetTexture = null;
            Canvas.ForceUpdateCanvases();
        }
    }

    private void EnsureCaptureCamera()
    {
        if (_captureCamera != null)
        {
            return;
        }

        _captureCameraObject = new GameObject("StopMotionSingleCanvasCaptureCamera");
        _captureCameraObject.hideFlags = HideFlags.HideAndDontSave;

        _captureCamera = _captureCameraObject.AddComponent<Camera>();
        _captureCamera.enabled = false;
        _captureCamera.clearFlags = CameraClearFlags.SolidColor;
        _captureCamera.backgroundColor = Color.clear;
        _captureCamera.cullingMask = ~0;
        _captureCamera.nearClipPlane = 0.01f;
        _captureCamera.farClipPlane = 10f;
        _captureCamera.transform.position = new Vector3(0f, 0f, -5f);
    }

    private Vector2Int GetCanvasCaptureSize(Canvas targetCanvas)
    {
        int fallbackWidth = Mathf.Max(1, defaultCanvasCaptureResolution.x);
        int fallbackHeight = Mathf.Max(1, defaultCanvasCaptureResolution.y);
        if (targetCanvas == null)
        {
            return new Vector2Int(fallbackWidth, fallbackHeight);
        }

        RectTransform rectTransform = targetCanvas.GetComponent<RectTransform>();
        if (rectTransform == null)
        {
            return new Vector2Int(fallbackWidth, fallbackHeight);
        }

        Vector2 rectSize = rectTransform.rect.size;
        float scaleFactor = Mathf.Max(0.01f, targetCanvas.scaleFactor);
        int width = Mathf.Max(1, Mathf.RoundToInt(Mathf.Abs(rectSize.x) * scaleFactor));
        int height = Mathf.Max(1, Mathf.RoundToInt(Mathf.Abs(rectSize.y) * scaleFactor));
        if (width <= 1 || height <= 1)
        {
            return new Vector2Int(fallbackWidth, fallbackHeight);
        }

        return new Vector2Int(width, height);
    }

    private void EnsureCaptureResources(int width, int height)
    {
        if (_captureRenderTexture != null && _captureRenderTexture.width == width && _captureRenderTexture.height == height)
        {
            if (_capturedCanvasTexture != null && _capturedCanvasTexture.width == width && _capturedCanvasTexture.height == height)
            {
                return;
            }
        }

        if (_captureRenderTexture != null)
        {
            _captureRenderTexture.Release();
            Destroy(_captureRenderTexture);
        }

        _captureRenderTexture = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32);
        _captureRenderTexture.Create();

        if (_capturedCanvasTexture != null)
        {
            Destroy(_capturedCanvasTexture);
        }

        _capturedCanvasTexture = new Texture2D(width, height, TextureFormat.RGBA32, false);
    }

    private void ReleaseCaptureResources()
    {
        if (_captureRenderTexture != null)
        {
            _captureRenderTexture.Release();
            Destroy(_captureRenderTexture);
            _captureRenderTexture = null;
        }

        if (_capturedCanvasTexture != null)
        {
            Destroy(_capturedCanvasTexture);
            _capturedCanvasTexture = null;
        }

        if (_captureCameraObject != null)
        {
            Destroy(_captureCameraObject);
            _captureCameraObject = null;
            _captureCamera = null;
        }
    }

    private bool HasFfmpeg()
    {
        bool exists = !string.IsNullOrWhiteSpace(ffmpegExecutablePath) && File.Exists(ffmpegExecutablePath);
        Debug.Log($"[StopMotionCreateSingle] ffmpeg 경로 확인 - 경로: {ffmpegExecutablePath}, 존재: {exists}");
        return exists;
    }

    private Task<bool> EncodeMp4Async(string inputPattern, string outputPath, int fps)
    {
        return Task.Run(() =>
        {
            try
            {
                System.Diagnostics.ProcessStartInfo psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = ffmpegExecutablePath,
                    Arguments = $"-y -framerate {Mathf.Max(1, fps)} -i \"{inputPattern}\" -c:v libx264 -pix_fmt yuv420p \"{outputPath}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using (System.Diagnostics.Process process = System.Diagnostics.Process.Start(psi))
                {
                    if (process == null)
                    {
                        return false;
                    }

                    StringBuilder errors = new StringBuilder();
                    process.ErrorDataReceived += (_, e) =>
                    {
                        if (!string.IsNullOrEmpty(e.Data))
                        {
                            errors.AppendLine(e.Data);
                        }
                    };
                    process.BeginErrorReadLine();
                    process.WaitForExit();

                    if (process.ExitCode != 0)
                    {
                        Debug.LogError($"[StopMotionCreateSingle] ffmpeg 실패(ExitCode={process.ExitCode}): {errors}");
                        return false;
                    }

                    return File.Exists(outputPath);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[StopMotionCreateSingle] MP4 인코딩 예외: {ex.Message}");
                return false;
            }
        });
    }

    private async Task UploadVideoAsync(string filePath, SingleOutputSide side)
    {
        if (!UserDataManager.Instance.IsUser())
        {
            Debug.LogWarning("[StopMotionCreateSingle] 업로드 실패: 사용자 정보가 없습니다.");
            return;
        }

        if (!File.Exists(filePath))
        {
            Debug.LogError($"[StopMotionCreateSingle] 업로드 실패: 파일을 찾을 수 없습니다. {filePath}");
            return;
        }

        string uidKey = side == SingleOutputSide.Left ? "UID_LEFT" : "UID_RIGHT";
        string uid = UserDataManager.Instance.FindValue(uidKey);
        if (string.IsNullOrWhiteSpace(uid))
        {
            Debug.LogError($"[StopMotionCreateSingle] 업로드 실패: {uidKey}를 확인할 수 없습니다.");
            return;
        }

        string idxUser = UserDataManager.Instance.FindValue("IDX_USER");
        string code = UnityWebRequest.EscapeURL(ServerData.Instance.Code);
        string requestUrl = $"{uploadUrl}?idx_user={idxUser}&uid={uid}&code={code}&type=mp4";

        Debug.Log($"[StopMotionCreateSingle] {side} 업로드 URL: {requestUrl}");

        for (int attempt = 0; attempt < maxRetries; attempt++)
        {
            using (UnityWebRequest webRequest = new UnityWebRequest(requestUrl, UnityWebRequest.kHttpVerbPOST))
            {
                webRequest.uploadHandler = new UploadHandlerFile(filePath);
                webRequest.SetRequestHeader("Content-Type", "video/mp4");
                webRequest.downloadHandler = new DownloadHandlerBuffer();
                webRequest.timeout = 60;

                await SendWebRequestAsync(webRequest);

                if (webRequest.result == UnityWebRequest.Result.Success)
                {
                    Debug.Log($"[StopMotionCreateSingle] {side} 업로드 성공: {webRequest.responseCode}");
                    return;
                }

                if (attempt < maxRetries - 1)
                {
                    Debug.LogWarning($"[StopMotionCreateSingle] {side} 업로드 실패 ({attempt + 1}/{maxRetries}): {webRequest.error}. {retryDelay}초 후 재시도...");
                    await Task.Delay(TimeSpan.FromSeconds(retryDelay));
                }
                else
                {
                    Debug.LogError($"[StopMotionCreateSingle] {side} 업로드 최종 실패: {webRequest.error}");
                }
            }
        }
    }

    private Task SendWebRequestAsync(UnityWebRequest request)
    {
        var tcs = new TaskCompletionSource<bool>();
        request.SendWebRequest().completed += _ => tcs.SetResult(true);
        return tcs.Task;
    }

    private static void DeleteGeneratedFiles(List<string> paths)
    {
        if (paths == null)
        {
            return;
        }

        for (int i = 0; i < paths.Count; i++)
        {
            TryDeleteFile(paths[i]);
        }
    }

    private static void TryDeleteFile(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            return;
        }

        try
        {
            File.Delete(path);
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[StopMotionCreateSingle] 파일 삭제 실패: {path}, {ex.Message}");
        }
    }

    private string GetRootPath()
    {
        string rootPath = saveVideoToComputer ? GetComputerSaveRootPath() : GetTemporarySaveRootPath();
        string savePath = Path.Combine(rootPath, DateTime.Now.ToString("yyyy-MM-dd"), outputName);
        Directory.CreateDirectory(savePath);
        Debug.Log($"[StopMotionCreateSingle] 저장 모드: {(saveVideoToComputer ? "PC 저장" : "임시 저장")}");
        return savePath;
    }

    private string GetComputerSaveRootPath()
    {
        string dataPath = Application.dataPath;
        DirectoryInfo parentDir = Directory.GetParent(dataPath);
        string rootPath = parentDir != null ? parentDir.FullName : dataPath;
        return Path.Combine(rootPath, saveFolderName);
    }

    private string GetTemporarySaveRootPath()
    {
        return Path.Combine(Application.temporaryCachePath, outputName);
    }
}
