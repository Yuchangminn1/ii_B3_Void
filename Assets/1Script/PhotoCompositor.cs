using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace My.Scripts.Utils
{
    [Serializable]
    public class CompositeSlot
    {
        public string fileSuffix;

        [Header("Position (Top-Left Pivot)")]
        [Tooltip("배경의 좌상단(0,0)을 기준으로, 사진의 좌상단이 위치할 좌표입니다.")]
        public Vector2 position;

        [Header("Scale")]
        public Vector2 scale = Vector2.one;
    }

    /// <summary> 
    /// 저장된 개별 플레이어 사진들을 지정된 프레임(틀) 이미지 위에 합성한 뒤,
    /// 로컬 디스크에 PNG로 저장하고 서버로 업로드하는 시퀀스를 관리합니다.
    /// </summary>
    public class PhotoCompositor : MonoBehaviour
    {
        [Header("Assets")]
        public Texture2D baseFrame;

        [Tooltip("배경 이미지(출력 캔버스)의 스케일입니다. 화질을 높이려면 값을 키워 해상도를 증가시킬 수 있습니다.")]
        public Vector2 baseFrameScale = Vector2.one;

        [Header("Config")]
        public string saveFolderName = "Pictures";
        public string outputFileName = "Composite";

        [Tooltip("서버 업로드 시 구분용 카운트 번호")]
        [Min(1)]
        public int uploadCount = 1;

        [Header("Canvas Capture")]
        [Tooltip("메인 화면과 다르게 별도로 저장/업로드할 캔버스 목록입니다.")]
        public List<Canvas> uploadCanvases = new List<Canvas>();

        [SerializeField] private Vector2Int defaultCanvasCaptureResolution = new Vector2Int(2250, 4000);

        [SerializeField] private bool releaseCachedTexturesAfterRun = true;

        [Header("API Retry Settings")]
        [SerializeField] private int maxRetries = 10;
        [SerializeField] private float retryDelay = 1.0f;
        [SerializeField] private float delayBetweenUploads = 0.5f;

        [Header("API Endpoints")]
        [SerializeField] private string uploadUrl = "http://192.168.0.252:8500/api/uploadFile.cfm";

        [Header("Layout")]
        public List<CompositeSlot> slots;

        [Header("Debug")]
        public string debugBaseName = "PlayerAPlayerB";

        public bool IsProcessing { get; private set; }

        private RenderTexture _cachedRenderTex;
        private Texture2D _cachedCanvasTex;
        private Texture2D _cachedScreenTex;

        [ContextMenu("Execute Composite Now")]
        public void DebugProcessAndSave()
        {
            // 컨텍스트 메뉴로 실행 시 isDebug를 true로 전달하여 로컬 PNG 저장만 수행
            ProcessAndSave(debugBaseName, true);
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.P) && !IsProcessing)
            {
                ProcessAndSave(debugBaseName, false);
            }
        }

        public void UpdatePlayerImage()
        {
            ProcessAndSave(debugBaseName, false);
        }

        private void OnDestroy()
        {
            ReleaseCachedTextures();
        }

        /// <summary> 
        /// 합성 로직을 실행합니다. 무거운 인코딩과 업로드는 비동기로 처리하여 프리징을 방지합니다.
        /// </summary>
        public void ProcessAndSave(string baseName, bool isDebug = false)
        {
            if (IsProcessing)
            {
                Debug.LogWarning("[PhotoCompositor] 이전 캡처/업로드 작업이 진행 중입니다. 중복 요청은 무시합니다.");
                return;
            }

            IsProcessing = true;

            string safeBaseName = string.IsNullOrEmpty(baseName) ? "" : baseName;
            string clean = safeBaseName.Replace("\n", "").Replace("\r", "").Trim();
            string invalidChars = Regex.Escape(new string(Path.GetInvalidFileNameChars()));
            string invalidRegStr = string.Format(@"([{0}]*\.+$)|([{0}]+)", invalidChars);
            string sanitizedName = Regex.Replace(clean, invalidRegStr, "");

            if (string.IsNullOrWhiteSpace(sanitizedName))
            {
                sanitizedName = "UnknownPlayers";
            }

            StartCoroutine(CaptureCanvasAndUploadRoutine(sanitizedName, isDebug));
        }

        /// <summary> 
        /// 지정된 캔버스를 캡처하거나, 별도 지정이 없으면 현재 화면을 캡처해 PNG로 저장/업로드합니다.
        /// </summary>
        private IEnumerator CaptureCanvasAndUploadRoutine(string sanitizedName, bool isDebug)
        {
            // UI(Canvas)까지 모두 반영된 최종 프레임을 캡처하기 위해 프레임 종료를 대기
            yield return new WaitForEndOfFrame();

            string rootPath = GetRootPath();
            int nextUploadCount = Mathf.Max(1, uploadCount);
            List<Canvas> canvasTargets = GetUploadCanvasTargets();
            bool hasCanvasTargets = canvasTargets.Count > 0;

            try
            {
                if (hasCanvasTargets)
                {
                    int savedCanvasCount = 0;

                    foreach (Canvas targetCanvas in canvasTargets)
                    {
                        if (targetCanvas == null)
                        {
                            continue;
                        }

                        Texture2D canvasTexture = CaptureCanvasToTexture(targetCanvas);
                        if (!canvasTexture)
                        {
                            Debug.LogWarning($"[PhotoCompositor] 캔버스 캡처 실패: {targetCanvas.name}");
                            continue;
                        }

                        Task<byte[]> encodeTask = EncodeTextureToPngAsync(canvasTexture);
                        yield return new WaitUntil(() => encodeTask.IsCompleted);

                        if (encodeTask.IsFaulted)
                        {
                            Debug.LogError($"[PhotoCompositor] PNG 인코딩 예외: {encodeTask.Exception?.GetBaseException().Message}");
                            continue;
                        }

                        byte[] pngBytes = encodeTask.Result;
                        if (pngBytes == null || pngBytes.Length == 0)
                        {
                            Debug.LogError($"[PhotoCompositor] PNG 인코딩 실패: {targetCanvas.name}");
                            continue;
                        }

                        // savedCanvasCount++;
                        // string suffix = $"{outputFileName}_{savedCanvasCount}";

                        int canvasIndex = savedCanvasCount + 1;
                        string suffix = $"{outputFileName}_{canvasIndex}";
                        string finalFileName = $"{sanitizedName}_{suffix}.png";
                        string finalFilePath = Path.Combine(rootPath, finalFileName);

                        Task writeTask = File.WriteAllBytesAsync(finalFilePath, pngBytes);
                        yield return new WaitUntil(() => writeTask.IsCompleted);

                        if (writeTask.IsFaulted)
                        {
                            Debug.LogError($"[PhotoCompositor] 파일 저장 예외: {writeTask.Exception?.GetBaseException().Message}");
                            continue;
                        }
                        savedCanvasCount = canvasIndex;
                        pngBytes = null;

                        if (!isDebug)
                        {
                            Task uploadTask = UploadImageAsync(
                                finalFilePath,
                                nextUploadCount,
                                canvasTexture.width,
                                canvasTexture.height);
                            yield return new WaitUntil(() => uploadTask.IsCompleted);

                            if (uploadTask.IsFaulted)
                            {
                                Debug.LogError($"[PhotoCompositor] 업로드 예외: {uploadTask.Exception?.GetBaseException().Message}");
                            }
                        }

                        nextUploadCount++;
                        if (delayBetweenUploads > 0f)
                        {
                            yield return new WaitForSeconds(delayBetweenUploads);
                        }
                        yield return null;
                    }

                    if (savedCanvasCount == 0)
                    {
                        Debug.LogError("[PhotoCompositor] 저장/업로드할 캔버스 이미지가 생성되지 않았습니다.");
                        yield break;
                    }

                    if (isDebug)
                    {
                        Debug.Log($"<color=cyan>[PhotoCompositor] 디버그 모드 완료: 캔버스 이미지 {savedCanvasCount}개 생성됨. 서버 업로드는 생략되었습니다.</color>");
                    }
                }
                else
                {
                    if (_cachedScreenTex != null) Destroy(_cachedScreenTex);
                    _cachedScreenTex = ScreenCapture.CaptureScreenshotAsTexture();
                    if (!_cachedScreenTex)
                    {
                        Debug.LogError("[PhotoCompositor] 화면 캡처 실패");
                        yield break;
                    }
                    Texture2D screenshotTex = _cachedScreenTex;

                    try
                    {
                        Task<byte[]> encodeTask = EncodeTextureToPngAsync(screenshotTex);
                        _cachedScreenTex = null;
                        Destroy(screenshotTex);
                        yield return new WaitUntil(() => encodeTask.IsCompleted);

                        if (encodeTask.IsFaulted)
                        {
                            Debug.LogError($"[PhotoCompositor] PNG 인코딩 예외: {encodeTask.Exception?.GetBaseException().Message}");
                            yield break;
                        }

                        byte[] pngBytes = encodeTask.Result;

                        if (pngBytes == null || pngBytes.Length == 0)
                        {
                            Debug.LogError("[PhotoCompositor] PNG 인코딩 실패");
                            yield break;
                        }

                        string finalFileName = $"{sanitizedName}_{outputFileName}.png";
                        string finalFilePath = Path.Combine(rootPath, finalFileName);

                        Task writeTask = File.WriteAllBytesAsync(finalFilePath, pngBytes);
                        yield return new WaitUntil(() => writeTask.IsCompleted);

                        if (writeTask.IsFaulted)
                        {
                            Debug.LogError($"[PhotoCompositor] 파일 저장 예외: {writeTask.Exception?.GetBaseException().Message}");
                            yield break;
                        }

                        pngBytes = null;

                        if (!isDebug)
                        {
                            Task uploadTask = UploadImageAsync(
                                finalFilePath,
                                nextUploadCount,
                                screenshotTex.width,
                                screenshotTex.height);
                            yield return new WaitUntil(() => uploadTask.IsCompleted);

                            if (uploadTask.IsFaulted)
                            {
                                Debug.LogError($"[PhotoCompositor] 업로드 예외: {uploadTask.Exception?.GetBaseException().Message}");
                            }
                        }
                        else
                        {
                            Debug.Log($"<color=cyan>[PhotoCompositor] 디버그 모드 완료: {finalFileName} (PNG) 생성됨. 서버 업로드는 생략되었습니다.</color>");
                        }
                    }
                    finally
                    {
                        if (_cachedScreenTex != null)
                        {
                            Destroy(_cachedScreenTex);
                            _cachedScreenTex = null;
                        }
                    }
                }
            }
            finally
            {
                if (releaseCachedTexturesAfterRun)
                {
                    ReleaseCachedTextures();
                }

                IsProcessing = false;
            }
        }

        private List<Canvas> GetUploadCanvasTargets()
        {
            List<Canvas> targets = new List<Canvas>();

            if (uploadCanvases != null)
            {
                foreach (Canvas canvas in uploadCanvases)
                {
                    if (canvas != null)
                    {
                        targets.Add(canvas);
                    }
                }
            }

            if (targets.Count > 0)
            {
                return targets;
            }

            Canvas[] childCanvases = GetComponentsInChildren<Canvas>(true);
            foreach (Canvas canvas in childCanvases)
            {
                if (canvas != null && canvas.transform != transform && canvas.transform.IsChildOf(transform))
                {
                    targets.Add(canvas);
                }
            }

            return targets;
        }

        private Task<byte[]> EncodeTextureToPngAsync(Texture2D texture)
        {
            byte[] rawData = texture.GetRawTextureData();
            int texWidth = texture.width;
            int texHeight = texture.height;
            GraphicsFormat format = texture.graphicsFormat;

            return Task.Run(() =>
                ImageConversion.EncodeArrayToPNG(rawData, format, (uint)texWidth, (uint)texHeight));
        }

        private Texture2D CaptureCanvasToTexture(Canvas targetCanvas)
        {
            if (!targetCanvas.isActiveAndEnabled)
            {
                Debug.LogWarning($"[PhotoCompositor] 비활성 캔버스는 캡처할 수 없습니다: {targetCanvas.name}");
                return null;
            }

            if (targetCanvas.renderMode == RenderMode.WorldSpace)
            {
                Debug.LogError($"[PhotoCompositor] World Space Canvas는 현재 캡처 대상에서 지원하지 않습니다: {targetCanvas.name}");
                return null;
            }

            Vector2Int canvasSize = GetCanvasCaptureSize(targetCanvas);
            int captureWidth = canvasSize.x;
            int captureHeight = canvasSize.y;
            if (_cachedRenderTex == null || _cachedRenderTex.width != captureWidth || _cachedRenderTex.height != captureHeight)
            {
                if (_cachedRenderTex != null) { _cachedRenderTex.Release(); Destroy(_cachedRenderTex); }
                _cachedRenderTex = new RenderTexture(captureWidth, captureHeight, 0, RenderTextureFormat.ARGB32);
            }
            RenderTexture renderTexture = _cachedRenderTex;
            GameObject cameraObject = new GameObject("PhotoCompositorCanvasCaptureCamera");
            cameraObject.hideFlags = HideFlags.HideAndDontSave;

            Camera captureCamera = cameraObject.AddComponent<Camera>();
            captureCamera.enabled = false;
            captureCamera.clearFlags = CameraClearFlags.SolidColor;
            captureCamera.backgroundColor = Color.clear;
            captureCamera.cullingMask = ~0;
            captureCamera.nearClipPlane = 0.01f;
            captureCamera.farClipPlane = 10f;
            captureCamera.transform.position = new Vector3(0f, 0f, -5f);
            captureCamera.targetTexture = renderTexture;

            RenderMode originalRenderMode = targetCanvas.renderMode;
            Camera originalWorldCamera = targetCanvas.worldCamera;
            float originalPlaneDistance = targetCanvas.planeDistance;
            int originalTargetDisplay = targetCanvas.targetDisplay;
            RenderTexture previousActive = RenderTexture.active;

            try
            {
                renderTexture.Create();
                targetCanvas.renderMode = RenderMode.ScreenSpaceCamera;
                targetCanvas.worldCamera = captureCamera;
                targetCanvas.planeDistance = 1f;
                targetCanvas.targetDisplay = 0;

                Canvas.ForceUpdateCanvases();

                // 드라이버/플랫폼에 따라 이전 RT 내용이 남는 경우를 방지하기 위해 명시적으로 클리어합니다.
                RenderTexture.active = renderTexture;
                GL.Clear(true, true, Color.clear);
                RenderTexture.active = previousActive;

                captureCamera.Render();

                RenderTexture.active = renderTexture;
                if (_cachedCanvasTex == null || _cachedCanvasTex.width != captureWidth || _cachedCanvasTex.height != captureHeight)
                {
                    if (_cachedCanvasTex != null) Destroy(_cachedCanvasTex);
                    _cachedCanvasTex = new Texture2D(captureWidth, captureHeight, TextureFormat.RGBA32, false);
                }
                Texture2D capturedTexture = _cachedCanvasTex;
                capturedTexture.ReadPixels(new Rect(0, 0, captureWidth, captureHeight), 0, 0);
                capturedTexture.Apply();
                return capturedTexture;
            }
            finally
            {
                RenderTexture.active = previousActive;
                targetCanvas.renderMode = originalRenderMode;
                targetCanvas.worldCamera = originalWorldCamera;
                targetCanvas.planeDistance = originalPlaneDistance;
                targetCanvas.targetDisplay = originalTargetDisplay;
                captureCamera.targetTexture = null;
                Destroy(cameraObject);
                Canvas.ForceUpdateCanvases();
            }
        }

        private Vector2Int GetCanvasCaptureSize(Canvas targetCanvas)
        {
            int fallbackWidth = Mathf.Max(1, defaultCanvasCaptureResolution.x);
            int fallbackHeight = Mathf.Max(1, defaultCanvasCaptureResolution.y);

            RawImage[] rawImages = targetCanvas.GetComponentsInChildren<RawImage>(true);
            int maxWidth = 0;
            int maxHeight = 0;
            float maxArea = 0f;

            foreach (RawImage rawImage in rawImages)
            {
                if (rawImage == null || !rawImage.isActiveAndEnabled)
                {
                    continue;
                }

                RectTransform rawRectTransform = rawImage.rectTransform;
                Vector2 rawRectSize = rawRectTransform.rect.size;
                Vector3 lossyScale = rawRectTransform.lossyScale;
                float rawScaleFactor = Mathf.Max(0.01f, targetCanvas.scaleFactor);

                int widthFromRaw = Mathf.Max(1, Mathf.RoundToInt(Mathf.Abs(rawRectSize.x * lossyScale.x) * rawScaleFactor));
                int heightFromRaw = Mathf.Max(1, Mathf.RoundToInt(Mathf.Abs(rawRectSize.y * lossyScale.y) * rawScaleFactor));
                float area = widthFromRaw * heightFromRaw;

                if (area > maxArea)
                {
                    maxArea = area;
                    maxWidth = widthFromRaw;
                    maxHeight = heightFromRaw;
                }
            }

            if (maxWidth > 1 && maxHeight > 1)
            {
                return new Vector2Int(maxWidth, maxHeight);
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

        private void ReleaseCachedTextures()
        {
            if (_cachedRenderTex != null)
            {
                _cachedRenderTex.Release();
                Destroy(_cachedRenderTex);
                _cachedRenderTex = null;
            }

            if (_cachedCanvasTex != null)
            {
                Destroy(_cachedCanvasTex);
                _cachedCanvasTex = null;
            }

            if (_cachedScreenTex != null)
            {
                Destroy(_cachedScreenTex);
                _cachedScreenTex = null;
            }
        }

        /// <summary> 
        /// 서버 업로드를 Task 기반으로 처리합니다. 
        /// </summary>
        private async Task UploadImageAsync(string filePath, int requestCount, int width, int height)
        {
            if (UserDataManager.Instance.IsUser() == false)
            {
                Debug.LogWarning("[PhotoCompositor] 업로드 실패: 사용자 정보가 없습니다.");
                return;
            }

            if (File.Exists(filePath) == false)
            {
                Debug.LogError($"[PhotoCompositor] 업로드 실패: 파일을 찾을 수 없습니다. {filePath}");
                return;
            }

            string idxUser = UserDataManager.Instance.FindValue("IDX_USER");
            string uid = ResolveUploadUid();
            if (string.IsNullOrWhiteSpace(uid))
            {
                Debug.LogError("[PhotoCompositor] 업로드 실패: UID를 확인할 수 없습니다.");
                return;
            }

            string code = UnityWebRequest.EscapeURL(ServerData.Instance.Code);
            int safeUploadCount = Mathf.Max(1, requestCount);
            int safeWidth = Mathf.Max(1, width);
            int safeHeight = Mathf.Max(1, height);

            // 파라미터 type=png, count, width, height 사용
            string requestUrl =
                $"{uploadUrl}?idx_user={idxUser}&uid={uid}&code={code}&type=png&count={safeUploadCount}&width={safeWidth}&height={safeHeight}";

            Debug.Log("UploadImageRequest URL: " + requestUrl);

            // 전역 변수로 설정된 횟수와 딜레이 사용
            for (int attempt = 0; attempt < maxRetries; attempt++)
            {
                using (UnityWebRequest webRequest = new UnityWebRequest(requestUrl, UnityWebRequest.kHttpVerbPOST))
                {
                    webRequest.uploadHandler = new UploadHandlerFile(filePath);
                    webRequest.SetRequestHeader("Content-Type", "image/png");
                    webRequest.downloadHandler = new DownloadHandlerBuffer();
                    webRequest.timeout = 15;

                    await SendWebRequestAsync(webRequest);

                    if (webRequest.result == UnityWebRequest.Result.Success)
                    {
                        Debug.Log($"[PhotoCompositor] 업로드 성공: {webRequest.responseCode}");
                        return; // 성공 시 루프 종료
                    }

                    if (attempt < maxRetries - 1)
                    {
                        Debug.LogWarning($"[PhotoCompositor] 업로드 실패 ({attempt + 1}/{maxRetries}): {webRequest.error}. {retryDelay}초 후 재시도...");
                        await Task.Delay(TimeSpan.FromSeconds(retryDelay));
                    }
                    else
                    {
                        Debug.LogError($"[PhotoCompositor] 업로드 최종 실패: {webRequest.error}");
                    }
                }
            }
        }

        private string ResolveUploadUid()
        {
            string uid = UserDataManager.Instance.FindValue("UID_LEFT");
            if (!string.IsNullOrWhiteSpace(uid))
            {
                return uid;
            }

            uid = UserDataManager.Instance.FindValue("UID_LEFT");
            if (!string.IsNullOrWhiteSpace(uid))
            {
                return uid;
            }

            uid = UserDataManager.Instance.FindValue("UID_RIGHT");
            return uid;
        }

        private Task SendWebRequestAsync(UnityWebRequest request)
        {
            var tcs = new TaskCompletionSource<bool>();
            request.SendWebRequest().completed += _ => tcs.SetResult(true);
            return tcs.Task;
        }

        private Texture2D LoadTextureFromFile(string path)
        {
            try
            {
                byte[] bytes = File.ReadAllBytes(path);
                Texture2D tex = new Texture2D(2, 2);
                tex.LoadImage(bytes);
                return tex;
            }
            catch { return null; }
        }

        private string GetRootPath()
        {
            string dataPath = Application.dataPath;
            DirectoryInfo parentDir = Directory.GetParent(dataPath);
            string rootPath = parentDir != null ? parentDir.FullName : dataPath;
            string savePath = Path.Combine(rootPath, saveFolderName, DateTime.Now.ToString("yyyy-MM-dd"));
            Directory.CreateDirectory(savePath);
            return savePath;
        }
    }
}