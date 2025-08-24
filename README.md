# Unity WorldMap

Unity 3D 기반의 세계 지도 시뮬레이션 프로젝트입니다. 실제 지형 데이터를 사용하여 대규모 지형을 렌더링하고, 선박 네비게이션 시스템을 제공합니다.

## 📋 프로젝트 개요

- **Unity 버전**: 2022.3.62f1
- **렌더링 파이프라인**: Universal Render Pipeline (URP)
- **주요 기능**: 
  - 실시간 지형 스트리밍
  - 물리 기반 선박 제어
  - 3인칭 카메라 시스템
  - 지형 충돌 감지

## 🛠️ 지형 데이터 설정

### 1. 지형 데이터 준비
1. 지형 RAW 파일을 다운로드합니다 
https://drive.google.com/file/d/1jBdXH5W5_f_jECI4VH5u6gsTqo55cCOj/view?usp=drive_link
2. `Assets/Terrains` 폴더를 생성합니다
3. RAW 파일을 압축 해제하여 배치합니다
   ```
   Assets/Terrains/
   ├── tile_0_0.raw
   ├── tile_0_1.raw
   └── ...
   ```

### 2. TerrainData 에셋 변환
1. `Assets/Resources/Terrains` 폴더를 생성합니다
2. Unity 에디터에서 **Window > Terrain Batch Converter**를 실행합니다
3. 변환 완료 후 다음과 같은 에셋이 생성됩니다:
   ```
   Assets/Resources/Terrains/
   ├── Terrain_0_0.asset
   ├── Terrain_0_1.asset
   └── ...
   ```

## 🎮 사용법

1. Unity 에디터에서 `Assets/World.unity` 씬을 엽니다
2. Play 버튼을 눌러 시뮬레이션을 시작합니다
3. **WASD** 키로 선박을 조종합니다
4. 카메라가 선박을 자동으로 추적합니다

## 🏗️ 주요 컴포넌트

- **TerrainStreaming**: 플레이어 위치 기반 동적 지형 로딩
- **ShipController**: Rigidbody 기반 선박 물리 시스템
- **SimpleCameraFollower**: 부드러운 카메라 추적 시스템