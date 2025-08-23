using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Collections;

namespace CAU
{
    /// <summary>
    /// 플레이어 위치에 따라 지형 타일을 동적으로 로드/언로드하는 스트리밍 시스템
    /// 메모리 효율을 위해 필요한 지형만 로드하여 전세계 항해를 가능하게 함
    /// </summary>
    public class TerrainStreaming : MonoBehaviour, IController
    {
        [Header("Player Reference")]
        public Transform player; // ShipController Transform
        
        [Header("Streaming Settings")]
        [Range(1, 5)]
        public int loadRadius = 1; // 플레이어 주변 몇 개 타일을 로드할지
        
        [Range(0.5f, 5.0f)]
        public float updateInterval = 1.0f; // 타일 업데이트 주기 (초)
        
        [Header("Performance Settings")]
        public int maxTilesPerFrame = 1; // 프레임당 최대 로드 타일 수
        public bool useAsyncLoading = true; // 비동기 로딩 사용
        
        [Header("Terrain Size Settings")]
        [Range(0.1f, 10.0f)]
        public float terrainHeight = 1.0f; // 지형 높이 (Unity 단위)
        
        
        // 프라이빗 변수들
        private TerrainLoader terrainLoader;
        private Dictionary<Vector2Int, GameObject> loadedTiles = new Dictionary<Vector2Int, GameObject>();
        private Queue<Vector2Int> loadQueue = new Queue<Vector2Int>();
        private Queue<Vector2Int> unloadQueue = new Queue<Vector2Int>();
        
        private Vector2Int currentTile = new Vector2Int(-1, -1);
        private Vector2Int lastUpdateTile = new Vector2Int(-1, -1);
        
        private Coroutine streamingCoroutine;
        private bool isInitialized = false;
        
        #region IController Implementation
        
        public void Initialize(System.Action initCompleteCallback)
        {
            // TerrainLoader 컴포넌트 가져오기 또는 추가
            terrainLoader = GetComponent<TerrainLoader>();
            if (terrainLoader == null)
            {
                terrainLoader = gameObject.AddComponent<TerrainLoader>();
            }
            
            // 플레이어 자동 찾기 (지정되지 않은 경우)
            if (player == null)
            {
                ShipController shipController = FindObjectOfType<ShipController>();
                if (shipController != null)
                {
                    player = shipController.transform;
                    Debug.Log("플레이어가 자동으로 설정되었습니다: " + player.name);
                }
                else
                {
                    Debug.LogWarning("플레이어를 찾을 수 없습니다. 수동으로 설정해주세요.");
                }
            }
            
            
            isInitialized = true;
            
            // 스트리밍 코루틴 시작
            if (streamingCoroutine != null)
            {
                StopCoroutine(streamingCoroutine);
            }
            streamingCoroutine = StartCoroutine(StreamingUpdateCoroutine());
            
            Debug.Log("TerrainStreaming 초기화 완료");
            initCompleteCallback?.Invoke();
        }
        
        public void Release()
        {
            // 모든 로드된 타일 제거
            UnloadAllTiles();
            
            // 코루틴 정지
            if (streamingCoroutine != null)
            {
                StopCoroutine(streamingCoroutine);
                streamingCoroutine = null;
            }
            
            // 큐 초기화
            loadQueue.Clear();
            unloadQueue.Clear();
            
            isInitialized = false;
            
            Debug.Log("TerrainStreaming 해제 완료");
        }
        
        #endregion
        
        #region Unity Lifecycle
        
        void Start()
        {
            Initialize(null);
        }
        
        void OnDestroy()
        {
            Release();
        }
        
        #endregion
        
        #region 스트리밍 로직
        
        private IEnumerator StreamingUpdateCoroutine()
        {
            while (isInitialized)
            {
                if (player != null)
                {
                    UpdateTerrainStreaming();
                    ProcessLoadUnloadQueues();
                }
                
                yield return new WaitForSeconds(updateInterval);
            }
        }
        
        private void UpdateTerrainStreaming()
        {
            Vector2Int newTile = terrainLoader.GetTileFromWorldPosition(player.position);
            
            // 현재 타일이 변경된 경우에만 업데이트
            if (newTile != lastUpdateTile)
            {
                currentTile = newTile;
                lastUpdateTile = newTile;
                
                Debug.Log($"플레이어 위치: Tile({currentTile.x}, {currentTile.y}) - World({player.position})");
                
                UpdateRequiredTiles();
            }
        }
        
        private void UpdateRequiredTiles()
        {
            // 필요한 타일 목록 생성
            HashSet<Vector2Int> requiredTiles = new HashSet<Vector2Int>();
            
            for (int x = currentTile.x - loadRadius; x <= currentTile.x + loadRadius; x++)
            {
                for (int y = currentTile.y - loadRadius; y <= currentTile.y + loadRadius; y++)
                {
                    if (x >= 0 && x < 36 && y >= 0 && y < 18)
                    {
                        requiredTiles.Add(new Vector2Int(x, y));
                    }
                }
            }
            
            // 불필요한 타일을 언로드 큐에 추가
            foreach (var tile in loadedTiles.Keys)
            {
                if (!requiredTiles.Contains(tile))
                {
                    unloadQueue.Enqueue(tile);
                }
            }
            
            // 새 타일을 로드 큐에 추가
            foreach (var tile in requiredTiles)
            {
                if (!loadedTiles.ContainsKey(tile) && !loadQueue.Contains(tile))
                {
                    loadQueue.Enqueue(tile);
                }
            }
        }
        
        private void ProcessLoadUnloadQueues()
        {
            int processedThisFrame = 0;
            
            // 언로드 처리 (빠르게 처리)
            while (unloadQueue.Count > 0 && processedThisFrame < maxTilesPerFrame * 2)
            {
                Vector2Int tileToUnload = unloadQueue.Dequeue();
                UnloadTile(tileToUnload);
                processedThisFrame++;
            }
            
            // 로드 처리 (제한적으로 처리)
            processedThisFrame = 0;
            while (loadQueue.Count > 0 && processedThisFrame < maxTilesPerFrame)
            {
                Vector2Int tileToLoad = loadQueue.Dequeue();
                
                if (useAsyncLoading)
                {
                    StartCoroutine(LoadTileAsync(tileToLoad));
                }
                else
                {
                    LoadTile(tileToLoad);
                }
                
                processedThisFrame++;
            }
        }
        
        private void LoadTile(Vector2Int tileIndex)
        {
            GameObject terrainGO = null;
            
            // 1차: TerrainData asset에서 로드 시도
            string assetName = $"TerrainData_{tileIndex.x}_{tileIndex.y}";
            string resourcesPath = $"TerrainData/{assetName}";
            TerrainData terrainData = Resources.Load<TerrainData>(resourcesPath);
            
            if (terrainData != null)
            {
                // TerrainData asset으로부터 로드
                Vector3 newSize = new Vector3(1.113f, terrainHeight, 1.113f);
                terrainData.size = newSize;
                
                terrainGO = Terrain.CreateTerrainGameObject(terrainData);
                terrainGO.name = $"Terrain_{tileIndex.x}_{tileIndex.y}";
                Debug.Log($"Asset에서 타일 로드: {assetName}");
            }
            else
            {
                // 2차: RAW 파일에서 로드 시도
                string rawFileName = $"tile_{tileIndex.x}_{tileIndex.y}.raw";
                string rawFilePath = Path.Combine(Application.dataPath, "Terrains", rawFileName);
                
                if (File.Exists(rawFilePath))
                {
                    terrainGO = terrainLoader.LoadTerrainFromRAW(rawFilePath, tileIndex.x, tileIndex.y);
                    
                    if (terrainGO != null)
                    {
                        // Terrain 크기 조정
                        Terrain terrain = terrainGO.GetComponent<Terrain>();
                        if (terrain != null && terrain.terrainData != null)
                        {
                            Vector3 newSize = new Vector3(1.113f, terrainHeight, 1.113f);
                            terrain.terrainData.size = newSize;
                        }
                        Debug.Log($"RAW에서 타일 로드: {rawFileName}");
                    }
                }
            }
            
            // 공통 처리
            if (terrainGO != null)
            {
                // 실제 지리적 위치에 배치
                Vector3 worldPosition = GetWorldPositionFromTile(tileIndex.x, tileIndex.y);
                
                // 해수면 높이 보정
                float seaLevelOffset = (32768f / 65535f) * terrainHeight;
                worldPosition.y = -seaLevelOffset;
                
                terrainGO.transform.position = worldPosition;
                loadedTiles[tileIndex] = terrainGO;
                Debug.Log($"타일 로드 완료: Tile({tileIndex.x}, {tileIndex.y})");
            }
            else
            {
                Debug.LogWarning($"타일을 로드할 수 없습니다: Tile({tileIndex.x}, {tileIndex.y})");
            }
        }
        
        private IEnumerator LoadTileAsync(Vector2Int tileIndex)
        {
            // 비동기 로딩 구현 (프레임 분산)
            yield return null; // 한 프레임 대기
            
            LoadTile(tileIndex);
            
            yield return null; // 로드 후 한 프레임 더 대기
        }
        
        private void UnloadTile(Vector2Int tileIndex)
        {
            if (loadedTiles.TryGetValue(tileIndex, out GameObject terrainGO))
            {
                if (terrainGO != null)
                {
                    DestroyImmediate(terrainGO);
                }
                
                loadedTiles.Remove(tileIndex);
                Debug.Log($"타일 언로드 완료: tile_{tileIndex.x}_{tileIndex.y}");
            }
        }
        
        private void UnloadAllTiles()
        {
            foreach (var terrainGO in loadedTiles.Values)
            {
                if (terrainGO != null)
                {
                    DestroyImmediate(terrainGO);
                }
            }
            
            loadedTiles.Clear();
            Debug.Log("모든 타일 언로드 완료");
        }
        
        #endregion
        
        #region 공개 메서드
        
        /// <summary>
        /// 현재 로드된 타일 개수 반환
        /// </summary>
        public int GetLoadedTileCount()
        {
            return loadedTiles.Count;
        }
        
        /// <summary>
        /// 로드 대기열 크기 반환
        /// </summary>
        public int GetLoadQueueSize()
        {
            return loadQueue.Count;
        }
        
        /// <summary>
        /// 언로드 대기열 크기 반환
        /// </summary>
        public int GetUnloadQueueSize()
        {
            return unloadQueue.Count;
        }
        
        /// <summary>
        /// 현재 플레이어가 있는 타일 반환
        /// </summary>
        public Vector2Int GetCurrentTile()
        {
            return currentTile;
        }
        
        /// <summary>
        /// 즉시 모든 타일 새로고침 (디버깅용)
        /// </summary>
        [ContextMenu("Force Refresh All Tiles")]
        public void ForceRefreshTiles()
        {
            if (isInitialized)
            {
                lastUpdateTile = new Vector2Int(-1, -1); // 강제 업데이트
                UpdateTerrainStreaming();
                
                // 큐에 있는 모든 작업 즉시 처리
                while (unloadQueue.Count > 0)
                {
                    UnloadTile(unloadQueue.Dequeue());
                }
                
                while (loadQueue.Count > 0)
                {
                    LoadTile(loadQueue.Dequeue());
                }
            }
        }
        
        #endregion
        
        #region 지리적 좌표 변환 (TerrainLoader에서 복사)
        
        /// <summary>
        /// 타일 좌표를 월드 좌표로 변환
        /// TerrainLoader와 동일한 로직 사용
        /// </summary>
        private Vector3 GetWorldPositionFromTile(int tileX, int tileY)
        {
            float terrainWidthKm = 1113f;  // 경도 10도 (적도 기준)  
            float terrainHeightKm = 1113f; // 위도 10도 - X축과 동일하게 설정하여 정사각형 타일
            float unityScaleFactor = 0.001f; // 1km = 1 Unity unit
            
            // WGS84 좌표계 기준
            // tile_0_0: -180°, 90° (서쪽 끝, 북극)
            // 각 타일은 10도씩 커버
            
            float longitude = -180f + (tileX * 10f);
            float latitude = 90f - (tileY * 10f);
            
            // Unity 월드 좌표로 변환 (중앙을 0,0으로)
            float worldX = (longitude / 10f) * terrainWidthKm * unityScaleFactor;
            float worldZ = (latitude / 10f) * terrainHeightKm * unityScaleFactor;
            
            return new Vector3(worldX, 0, worldZ);
        }
        
        #endregion
        
        #region 디버깅
        
        void OnGUI()
        {
            if (!isInitialized) return;
            
            GUILayout.BeginArea(new Rect(10, 10, 300, 200));
            GUILayout.Label("=== Terrain Streaming Debug ===");
            GUILayout.Label($"Current Tile: ({currentTile.x}, {currentTile.y})");
            GUILayout.Label($"Loaded Tiles: {GetLoadedTileCount()}");
            GUILayout.Label($"Load Queue: {GetLoadQueueSize()}");
            GUILayout.Label($"Unload Queue: {GetUnloadQueueSize()}");
            
            if (player != null)
            {
                GUILayout.Label($"Player Pos: {player.position}");
            }
            
            GUILayout.EndArea();
        }
        
        #endregion
    }
}