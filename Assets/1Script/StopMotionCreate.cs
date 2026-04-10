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

public class StopMotionCreate : MonoBehaviour
{
    private const int BackgroundChangeInterval = 10;
    private const int TargetFrameCount = 100;

    [Header("Frame Sources")]
    [Tooltip("배경 RawImage 10개. 10프레임마다 다음 배경으로 교체됩니다.")]
    [SerializeField] private List<RawImage> backgroundImages = new List<RawImage>();

    [Tooltip("좌측 스톱모션 프레임을 보관하는 컨테이너")]
    [SerializeField] private SaveShadowTextureContainer leftFrameContainer;

    [Tooltip("우측 스톱모션 프레임을 보관하는 컨테이너")]
    [SerializeField] private SaveShadowTextureContainer rightFrameContainer;

    [Header("Composite Display")]
    [SerializeField] private Canvas captureCanvas;
    [SerializeField] private RawImage backgroundDisplayImage;
    [SerializeField] private RawImage leftDisplayImage;
    [SerializeField] private RawImage rightDisplayImage;
    [SerializeField] private Vector2Int defaultCanvasCaptureResolution = new Vector2Int(2250, 4000);

    [Header("Output")]
    [SerializeField] private string saveFolderName = "Pictures";
    [SerializeField] private string outputName = "StopMotion";
    [SerializeField] private int outputFps = 12;

    [Header("Video Encode (Editor Only)")]
    [Tooltip("로컬 ffmpeg 경로. Unity Editor에서만 MP4 생성/저장에 사용됩니다.")]
    private string ffmpegExecutablePath = @"C:\ProgramData\chocolatey\bin\ffmpeg.exe";

    [SerializeField] private bool createMp4 = true;
    [SerializeField] private bool forceMp4UploadOnly = true;
    [SerializeField] private bool deletePngSequenceAfterMp4 = true;
    [SerializeField] private bool deleteMp4AfterUpload = false;

    [Header("Upload")]
    [SerializeField] private bool uploadToServer = false;
    [SerializeField] private int uploadCount = 1;
    // Current upload endpoint: http://192.168.0.252:8500/api/uploadFile.cfm
    [SerializeField] private string uploadUrl = "http://192.168.0.252:8500/api/uploadFile.cfm";
    [SerializeField] private int maxRetries = 10;
    [SerializeField] private float retryDelay = 1f;
    [SerializeField] private float delayBetweenUploads = 0.3f;

    [Header("Upload Complete Callback")]
    [SerializeField] private SequenceScript[] sequenceScripts;
    [SerializeField] private float triggerDelayAfterUploadSeconds = 5f;

    public bool IsProcessing { get; private set; }

    SequenceScript[] sequenceScript;

    private RenderTexture _captureRenderTexture;
    private Texture2D _capturedCanvasTexture;
    private Coroutine _triggerAfterUploadCoroutine;
    private readonly List<SaveShadowTextureContainer.SaveShadowCapturedFrame> _resolvedLeftFrames = new List<SaveShadowTextureContainer.SaveShadowCapturedFrame>(TargetFrameCount);
    private readonly List<SaveShadowTextureContainer.SaveShadowCapturedFrame> _resolvedRightFrames = new List<SaveShadowTextureContainer.SaveShadowCapturedFrame>(TargetFrameCount);

    [ContextMenu("Create StopMotion And Upload")]
    public void CreateNow()
    {
        if (IsProcessing)
        {
            Debug.LogWarning("[StopMotionCreate] 이전 작업이 아직 진행 중입니다.");
            return;
        }

        StartCoroutine(CreateStopMotionRoutine());
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            CreateNow();
        }
    }

    private IEnumerator CreateStopMotionRoutine()
    {
        IsProcessing = true;
        int nextUploadCount = Mathf.Max(1, uploadCount);
        // MP4를 에디터 뿐 아니라 빌드에서도 생성 가능하게 변경
        bool canCreateMp4 = createMp4;

        try
        {
            if (!ValidateInputs(out int frameCount))
            {
                yield break;
            }

            string rootPath = GetRootPath();
            string framesFolder = Path.Combine(rootPath, "Frames");
            Directory.CreateDirectory(framesFolder);

            float pipelineStartTime = Time.realtimeSinceStartup;
            float uploadStartTime = -1f;

            Debug.Log($"[StopMotionCreate] 파일 저장 경로: {framesFolder}");

            List<string> pngPaths = new List<string>(frameCount);
            Vector2Int outputSize = GetCanvasCaptureSize(captureCanvas);
            RawImage currentBackgroundImage = null;

            try
            {
                for (int i = 0; i < frameCount; i++)
                {
                    RawImage backgroundImage = GetBackgroundForFrame(i);
                    SaveShadowTextureContainer.SaveShadowCapturedFrame leftFrame = _resolvedLeftFrames[i];
                    SaveShadowTextureContainer.SaveShadowCapturedFrame rightFrame = _resolvedRightFrames[i];
                    Texture2D leftTexture = leftFrame.Texture as Texture2D;
                    Texture2D rightTexture = rightFrame.Texture as Texture2D;

                    if (backgroundImage == null || leftTexture == null || rightTexture == null)
                    {
                        Debug.LogError($"[StopMotionCreate] null 프레임 소스 발견: frame={i} (작업 중단)");
                        yield break;
                    }

                    if (backgroundImage != currentBackgroundImage)
                    {
                        ApplyBackgroundImage(backgroundImage);
                        currentBackgroundImage = backgroundImage;
                    }

                    // 프레임별로 저장된 localPosition/size/rotation/scale을 항상 반영
                    ApplyCapturedFrameLayout(leftDisplayImage, leftFrame);
                    ApplyCapturedFrameLayout(rightDisplayImage, rightFrame);

                    leftDisplayImage.texture = leftTexture;
                    rightDisplayImage.texture = rightTexture;
                    Canvas.ForceUpdateCanvases();

                    Texture2D compositeFrame = CaptureCanvasToTexture(captureCanvas);

                    if (compositeFrame == null)
                    {
                        Debug.LogError($"[StopMotionCreate] 캔버스 캡처 실패: frame={i} (작업 중단)");
                        yield break;
                    }

                    outputSize = new Vector2Int(compositeFrame.width, compositeFrame.height);
                    string framePath = Path.Combine(framesFolder, $"{outputName}_{i:D3}.png");

                    byte[] pngBytes = null;
                    try
                    {
                        // EncodeToPNG()는 메인 스레드에서만 안전합니다 - Task.Run 사용 금지
                        pngBytes = compositeFrame.EncodeToPNG();
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[StopMotionCreate] PNG 인코딩 실패: frame={i}, Exception: {ex.Message}\n{ex.StackTrace}");
                        Destroy(compositeFrame);
                        continue;
                    }

                    if (pngBytes == null || pngBytes.Length == 0)
                    {
                        Debug.LogError($"[StopMotionCreate] PNG 인코딩 실패: frame={i} (빈 바이트 배열)");
                        Destroy(compositeFrame);
                        continue;
                    }

                    Task writeTask = File.WriteAllBytesAsync(framePath, pngBytes);
                    yield return new WaitUntil(() => writeTask.IsCompleted);
                    GameManager.Instance.GoToIdleCheck();


                    if (writeTask.IsFaulted)
                    {
                        Debug.LogError($"[StopMotionCreate] 파일 저장 실패: frame={i}, 경로={framePath}, Exception: {writeTask.Exception?.InnerException?.Message}");
                        Destroy(compositeFrame);
                        continue;
                    }

                    Debug.Log($"[StopMotionCreate] 저장완료: {framePath}");
                    pngPaths.Add(framePath);
                    pngBytes = null;

                    Destroy(compositeFrame);


                    yield return null;
                }
            }
            finally
            {
                leftDisplayImage.texture = null;
                rightDisplayImage.texture = null;
            }

            if (pngPaths.Count == 0)
            {
                Debug.LogError("[StopMotionCreate] 생성된 프레임이 없습니다.");
                yield break;
            }

            Debug.Log($"[StopMotionCreate] 총 {pngPaths.Count}개 프레임 저장 완료. 경로: {framesFolder}");
            Debug.Log($"[StopMotionCreate]  프레임 추출/저장 소요: {(Time.realtimeSinceStartup - pipelineStartTime):F2}초");

            string mp4Path = null;
            if (canCreateMp4 && HasFfmpeg())
            {
                string inputPattern = Path.Combine(framesFolder, $"{outputName}_%03d.png");
                mp4Path = Path.Combine(framesFolder, $"{outputName}.mp4");

                Task<bool> encodeVideoTask = EncodeMp4Async(inputPattern, mp4Path, outputFps);
                yield return new WaitUntil(() => encodeVideoTask.IsCompleted);
                GameManager.Instance.GoToIdleCheck();

                if (!encodeVideoTask.Result)
                {
                    mp4Path = null;
                }
                else if (deletePngSequenceAfterMp4)
                {
                    DeleteGeneratedFiles(pngPaths);
                }
            }
            else if (createMp4)
            {
                Debug.LogWarning("[StopMotionCreate] ffmpeg 경로가 유효하지 않아 MP4 생성을 건너뜁니다.");
            }

            int generatedCount = pngPaths.Count;

            if (!uploadToServer)
            {
                Debug.Log($"[StopMotionCreate] 생성 완료 (업로드 비활성): {generatedCount}장 - 경로: {framesFolder}");
                Debug.Log($"[StopMotionCreate] 디버그 - 총 소요(추출 시작~종료, 업로드 없음): {(Time.realtimeSinceStartup - pipelineStartTime):F2}초");
                yield break;
            }

            uploadStartTime = Time.realtimeSinceStartup;

            if (!TryGetUploadIdentity(out string idxUser, out string uid))
            {
                Debug.LogError("[StopMotionCreate] 업로드 실패: 사용자 식별값을 확인할 수 없습니다.");
                yield break;
            }

            if (forceMp4UploadOnly && canCreateMp4)
            {
                if (string.IsNullOrEmpty(mp4Path) || !File.Exists(mp4Path))
                {
                    Debug.LogError("[StopMotionCreate] MP4 업로드 전용 모드인데 MP4 생성에 실패했습니다. ffmpeg 경로/권한을 확인하세요.");
                    yield break;
                }

                Task<bool> uploadTask = UploadFileAsync(mp4Path, "mp4", nextUploadCount++, outputSize.x, outputSize.y, "left", idxUser, uid);
                yield return new WaitUntil(() => uploadTask.IsCompleted);

                if (uploadTask.Result)
                {
                    StartTriggerAfterUploadCallback();
                    // 업로드 완료 후 약 30초 뒤에 해당 폴더 내 파일들 삭제
                    StartCoroutine(DeleteFolderFilesAfterDelay(framesFolder, 10f));
                }

                if (delayBetweenUploads > 0f)
                {
                    yield return new WaitForSeconds(delayBetweenUploads);
                }

                if (deleteMp4AfterUpload)
                {
                    TryDeleteFile(mp4Path);
                }

                pngPaths.Clear();
                Debug.Log($"[StopMotionCreate] 완료(MP4 전용): {generatedCount}장");
                Debug.Log($"[StopMotionCreate] 업로드 소요: {(Time.realtimeSinceStartup - uploadStartTime):F2}초");
                Debug.Log($"[StopMotionCreate]  총 소요(추출 시작~업로드 완료): {(Time.realtimeSinceStartup - pipelineStartTime):F2}초");
                yield break;
            }

            if (string.IsNullOrEmpty(mp4Path) || !File.Exists(mp4Path))
            {
                Debug.LogError("[StopMotionCreate] MP4 파일이 없어서 업로드를 중단합니다. 이미지 업로드는 비활성화되어 있습니다.");
                yield break;
            }

            Task<bool> finalMp4UploadTask = UploadFileAsync(mp4Path, "mp4", nextUploadCount++, outputSize.x, outputSize.y, "left", idxUser, uid);
            yield return new WaitUntil(() => finalMp4UploadTask.IsCompleted);

            if (finalMp4UploadTask.Result)
            {
                StartTriggerAfterUploadCallback();
                // 업로드 완료 후 약 30초 뒤에 해당 폴더 내 파일들 삭제
                StartCoroutine(DeleteFolderFilesAfterDelay(framesFolder, 10f));
            }

            if (delayBetweenUploads > 0f)
            {
                yield return new WaitForSeconds(delayBetweenUploads);
            }

            if (deleteMp4AfterUpload)
            {
                TryDeleteFile(mp4Path);
            }

            pngPaths.Clear();
            Debug.Log($"[StopMotionCreate] 완료: {generatedCount}장");
            Debug.Log($"[StopMotionCreate] 디버그 - 업로드 소요: {(Time.realtimeSinceStartup - uploadStartTime):F2}초");
            Debug.Log($"[StopMotionCreate] 디버그 - 총 소요(추출 시작~업로드 완료): {(Time.realtimeSinceStartup - pipelineStartTime):F2}초");
        }
        finally
        {
            ReleaseCaptureResources();
            IsProcessing = false;
        }
    }

    private IEnumerator DeleteFolderFilesAfterDelay(string folderPath, float delaySeconds)
    {
        yield return new WaitForSeconds(Mathf.Max(0f, delaySeconds));

        if (string.IsNullOrWhiteSpace(folderPath) || !Directory.Exists(folderPath))
            yield break;

        string[] files;
        try
        {
            files = Directory.GetFiles(folderPath);
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[StopMotionCreate] 폴더 파일 나열 실패: {folderPath}, {ex.Message}");
            yield break;
        }

        foreach (string file in files)
        {
            TryDeleteFile(file);
        }
    }

    private bool ValidateInputs(out int frameCount)
    {
        frameCount = 0;

        ResolveFrameSources();

        if (backgroundImages == null || backgroundImages.Count == 0)
        {
            Debug.LogError("[StopMotionCreate] 배경 RawImage가 비어 있습니다.");
            return false;
        }

        if (leftFrameContainer == null || rightFrameContainer == null)
        {
            Debug.LogError("[StopMotionCreate] 좌/우 SaveShadowTextureContainer가 필요합니다.");
            return false;
        }

        if (captureCanvas == null || backgroundDisplayImage == null || leftDisplayImage == null || rightDisplayImage == null)
        {
            Debug.LogError("[StopMotionCreate] 캡처용 Canvas와 표시용 RawImage 3개가 필요합니다.");
            return false;
        }

        if (_resolvedLeftFrames.Count == 0 || _resolvedRightFrames.Count == 0)
        {
            Debug.LogError("[StopMotionCreate] 좌/우 컨테이너에서 가져온 텍스처가 비어 있습니다.");
            return false;
        }

        if (backgroundImages[0] == null || backgroundImages[0].texture == null)
        {
            Debug.LogError("[StopMotionCreate] 첫 배경 RawImage 또는 Texture가 null입니다.");
            return false;
        }

        frameCount = Mathf.Min(TargetFrameCount, _resolvedLeftFrames.Count, _resolvedRightFrames.Count);
        if (frameCount <= 0)
        {
            Debug.LogError("[StopMotionCreate] 유효한 프레임 수가 없습니다.");
            return false;
        }

        if (_resolvedLeftFrames.Count < TargetFrameCount || _resolvedRightFrames.Count < TargetFrameCount)
        {
            Debug.LogWarning($"[StopMotionCreate] 요청 프레임(100장)보다 소스가 적어 {frameCount}장만 생성합니다.");
        }

        return true;
    }

    private void ResolveFrameSources()
    {
        _resolvedLeftFrames.Clear();
        _resolvedRightFrames.Clear();

        TryAddFramesFromContainer(leftFrameContainer, _resolvedLeftFrames);
        TryAddFramesFromContainer(rightFrameContainer, _resolvedRightFrames);

        // 우측 프레임은 max 인덱스부터 감소(-- ) 순서로 사용
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

    private static void ApplyCapturedFrameLayout(RawImage target, SaveShadowTextureContainer.SaveShadowCapturedFrame frame)
    {
        if (target == null)
        {
            return;
        }

        RectTransform targetRect = target.rectTransform;
        if (targetRect == null)
        {
            return;
        }

        targetRect.localPosition = frame.LocalPosition;

        targetRect.sizeDelta = frame.SizeDelta;
        targetRect.localRotation = frame.LocalRotation;
        targetRect.localScale = frame.LocalScale;
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
            Debug.LogError($"[StopMotionCreate] World Space Canvas는 캡처 대상에서 지원하지 않습니다: {targetCanvas.name}");
            return null;
        }

        Vector2Int canvasSize = GetCanvasCaptureSize(targetCanvas);
        EnsureCaptureResources(canvasSize.x, canvasSize.y);

        GameObject cameraObject = new GameObject("StopMotionCanvasCaptureCamera");
        cameraObject.hideFlags = HideFlags.HideAndDontSave;

        Camera captureCamera = cameraObject.AddComponent<Camera>();
        captureCamera.enabled = false;
        captureCamera.clearFlags = CameraClearFlags.SolidColor;
        captureCamera.backgroundColor = Color.clear;
        captureCamera.cullingMask = ~0;
        captureCamera.nearClipPlane = 0.01f;
        captureCamera.farClipPlane = 10f;
        captureCamera.transform.position = new Vector3(0f, 0f, -5f);
        captureCamera.targetTexture = _captureRenderTexture;

        RenderMode originalRenderMode = targetCanvas.renderMode;
        Camera originalWorldCamera = targetCanvas.worldCamera;
        float originalPlaneDistance = targetCanvas.planeDistance;
        int originalTargetDisplay = targetCanvas.targetDisplay;
        RenderTexture previous = RenderTexture.active;
        try
        {
            _captureRenderTexture.Create();
            targetCanvas.renderMode = RenderMode.ScreenSpaceCamera;
            targetCanvas.worldCamera = captureCamera;
            targetCanvas.planeDistance = 1f;
            targetCanvas.targetDisplay = 0;

            Canvas.ForceUpdateCanvases();

            RenderTexture.active = _captureRenderTexture;
            GL.Clear(true, true, Color.clear);
            captureCamera.Render();

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
            captureCamera.targetTexture = null;
            Destroy(cameraObject);
            Canvas.ForceUpdateCanvases();
        }
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
        if (_captureRenderTexture == null)
        {
            return;
        }

        _captureRenderTexture.Release();
        Destroy(_captureRenderTexture);
        _captureRenderTexture = null;

        if (_capturedCanvasTexture != null)
        {
            Destroy(_capturedCanvasTexture);
            _capturedCanvasTexture = null;
        }
    }

    private bool HasFfmpeg()
    {
        bool exists = !string.IsNullOrWhiteSpace(ffmpegExecutablePath) && File.Exists(ffmpegExecutablePath);
        Debug.Log($"[StopMotionCreate] ffmpeg 경로 확인 - 경로: {ffmpegExecutablePath}, 존재: {exists}");
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
                        Debug.LogError($"[StopMotionCreate] ffmpeg 실패(ExitCode={process.ExitCode}): {errors}");
                        return false;
                    }

                    return File.Exists(outputPath);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[StopMotionCreate] MP4 인코딩 예외: {ex.Message}");
                return false;
            }
        });
    }

    private bool TryGetUploadIdentity(out string idxUser, out string uid)
    {
        idxUser = null;
        uid = null;

        if (UserDataManager.Instance == null || !UserDataManager.Instance.IsUser())
        {
            return false;
        }

        idxUser = UserDataManager.Instance.FindValue("IDX_USER");
        uid = UserDataManager.Instance.FindValue("UID_LEFT");
        return !string.IsNullOrWhiteSpace(idxUser) && !string.IsNullOrWhiteSpace(uid);
    }

    private async Task<bool> UploadFileAsync(string filePath, string type, int requestCount, int width, int height, string side, string idxUser, string uid)
    {
        if (!File.Exists(filePath))
        {
            Debug.LogError($"[StopMotionCreate] 업로드 실패: 파일 없음 {filePath}");
            return false;
        }

        string code = UnityWebRequest.EscapeURL(ServerData.Instance.Code);
        string safeType = UnityWebRequest.EscapeURL(string.IsNullOrWhiteSpace(type) ? "png" : type);
        string safeSide = UnityWebRequest.EscapeURL(string.IsNullOrWhiteSpace(side) ? "unknown" : side);

        string requestUrl =
            $"{uploadUrl}?idx_user={idxUser}&uid={uid}&code={code}&type={safeType}&side={safeSide}";
        // $"{uploadUrl}?idx_user={idxUser}&uid={uid}&code={code}&type={safeType}&count={Mathf.Max(1, requestCount)}&width={Mathf.Max(1, width)}&height={Mathf.Max(1, height)}&side={safeSide}";

        for (int attempt = 0; attempt < Mathf.Max(1, maxRetries); attempt++)
        {
            using (UnityWebRequest webRequest = new UnityWebRequest(requestUrl, UnityWebRequest.kHttpVerbPOST))
            {
                webRequest.uploadHandler = new UploadHandlerFile(filePath);
                webRequest.downloadHandler = new DownloadHandlerBuffer();
                webRequest.timeout = 30;

                if (safeType == "mp4")
                {
                    webRequest.SetRequestHeader("Content-Type", "video/mp4");
                }
                else
                {
                    webRequest.SetRequestHeader("Content-Type", "image/png");
                }

                await SendWebRequestAsync(webRequest);
                if (webRequest.result == UnityWebRequest.Result.Success)
                {
                    Debug.Log($"[StopMotionCreate] 업로드 성공 ({safeType}, {safeSide}): {Path.GetFileName(filePath)}");
                    return true;
                }

                if (attempt < maxRetries - 1)
                {
                    Debug.LogWarning($"[StopMotionCreate] 업로드 실패({attempt + 1}/{maxRetries}): {webRequest.error}");
                    await Task.Delay(TimeSpan.FromSeconds(Mathf.Max(0f, retryDelay)));
                }
                else
                {
                    Debug.LogError($"[StopMotionCreate] 업로드 최종 실패: {webRequest.error}");
                }
            }
        }

        return false;
    }

    private void StartTriggerAfterUploadCallback()
    {
        if (_triggerAfterUploadCoroutine != null)
        {
            StopCoroutine(_triggerAfterUploadCoroutine);
        }

        _triggerAfterUploadCoroutine = StartCoroutine(TriggerAfterUploadCoroutine());
    }

    private IEnumerator TriggerAfterUploadCoroutine()
    {
        yield return new WaitForSeconds(Mathf.Max(0f, triggerDelayAfterUploadSeconds));

        if (sequenceScripts == null || sequenceScripts.Length == 0)
        {
            _triggerAfterUploadCoroutine = null;
            yield break;
        }

        foreach (SequenceScript sequenceScript in sequenceScripts)
        {
            if (sequenceScript == null)
            {
                continue;
            }

            sequenceScript.TriggerOn();
        }

        _triggerAfterUploadCoroutine = null;
    }

    private Task SendWebRequestAsync(UnityWebRequest request)
    {
        TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>();
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
            Debug.LogWarning($"[StopMotionCreate] 파일 삭제 실패: {path}, {ex.Message}");
        }
    }

    private string GetRootPath()
    {
        string dataPath = Application.dataPath;
        DirectoryInfo parentDir = Directory.GetParent(dataPath);
        string rootPath = parentDir != null ? parentDir.FullName : dataPath;
        string savePath = Path.Combine(rootPath, saveFolderName, DateTime.Now.ToString("yyyy-MM-dd"), outputName);
        Directory.CreateDirectory(savePath);
        return savePath;
    }
}
