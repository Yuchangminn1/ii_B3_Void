# CameraVV 사용 안내 (한국어)

이 컴포넌트는 웹캠 영상을 받아 특정 색을 투명으로 처리(컬러키)하여 UI RawImage 또는 MeshRenderer에 출력합니다.

## 설치 및 준비

- 본 리포지토리에는 셰이더 파일이 포함되어 있습니다: `Assets/Shaders/WebcamChromaKey.shader`
- `CameraVV` 스크립트를 출력 대상 오브젝트에 붙입니다.
  - UI 경로: `Canvas` → `RawImage` 생성 → `CameraVV` 추가
  - 3D 경로: `Quad` 등 MeshRenderer가 있는 오브젝트 생성 → `CameraVV` 추가

## 주요 옵션 설명

- `deviceName`: 사용할 웹캠 이름(부분 문자열 허용). 비워두면 첫 번째 기기를 사용합니다.
- `requestedWidth`/`requestedHeight`/`requestedFPS`: 요청 해상도 및 프레임레이트.
- `autoPlay`: 시작 시 자동으로 웹캠을 실행합니다.
- `mirrorHorizontal`/`mirrorVertical`: 좌우/상하 반전 옵션. 일부 기기에서 상하 반전은 자동 감지와 합쳐져 적용됩니다.

### 컬러키 파라미터

- `keyColor`: 투명 처리하고 싶은 기준 색상.
- `threshold`: 키 색과의 거리 기준(0~1). 값이 낮을수록 정확히 일치하는 색만 투명.
- `smoothness`: 경계 부드러움(0~1). 경계 영역의 알파를 점진적으로 변화.
- `spillReduction`: 단순 컬러키에서는 사용하지 않음.

### 출력 옵션

- `opaqueToBlack`: 투명하지 않은(전경) 영역을 검은색으로 출력합니다. 알파는 그대로 유지되므로 합성 시 전경 실루엣만 검게 표시됩니다.
- `edgeContrast`: 경계의 선명도를 높입니다(값이 클수록 또렷). 기본값 1은 원래와 동일하며 UI에서 `SetEdgeContrast`로 제어할 수 있습니다.

## 사용 방법

1. 오브젝트에 `CameraVV`를 붙인 뒤 플레이하면(또는 `autoPlay`가 켜져 있으면) 웹캠이 시작됩니다.
2. 인스펙터에서 `keyColor`, `threshold`, `smoothness`를 조절하고 필요하면 `opaqueToBlack`을 켜거나 `edgeContrast`를 올려 경계를 또렷하게 만듭니다.
3. UI `RawImage`는 스크립트가 자동으로 회전 각도를 맞추며, 셰이더의 `mirror/flip` 옵션도 동작합니다.

## 트러블슈팅

- 셰이더를 찾지 못할 경우 Unlit/Texture로 대체되어 투명 처리(컬러키)가 적용되지 않습니다.
  - `Assets/Shaders/WebcamChromaKey.shader`가 프로젝트에 존재하는지 확인하세요.
- 웹캠 목록이 비어 있으면 `No webcam devices found.` 경고가 출력됩니다. 기기 연결 여부를 확인하세요.
- 일부 카메라 드라이버에서는 수직 반전(`videoVerticallyMirrored`) 보고가 달라질 수 있으니 `mirrorVertical`을 조정해 보세요.

## 확장 아이디어

- 백그라운드 교체: 컬러키로 투명 처리된 영역 뒤에 다른 비디오/이미지를 배치하여 합성 연출.
- UI 슬라이더 연결: `SetThreshold`, `SetSmoothness`, `SetSpill` 메서드에 슬라이더를 바인딩해 실시간 제어.
