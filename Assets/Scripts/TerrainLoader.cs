using UnityEngine;
using System.IO;

namespace CAU
{
    /// <summary>
    /// RAW 파일로부터 Unity Terrain을 생성하는 클래스
    /// 전세계 지형 데이터(1025x1025 Uint16 RAW 파일)를 Unity Terrain으로 변환
    /// </summary>
    public class TerrainLoader : MonoBehaviour
    {
        [Header("Terrain Settings")]
        public int heightmapResolution = 1025;
        
        [Header("Real World Scale")]
        float terrainWidthKm = 1113f;  // 경도 10도 (적도 기준)  
        float terrainHeightKm = 1113f; // 위도 10도 - X축과 동일하게 설정하여 정사각형 타일
        public float maxElevationM = 8000f; // 최대 고도 (미터) - 더 부드러운 지형을 위해 줄임
        
        // Unity 내부 스케일 (실제 크기를 Unity 단위로 변환)
        private float unityScaleFactor = 0.001f; // 1km = 1 Unity unit
        
        private Vector3 GetTerrainSize()
        {
            return new Vector3(
                terrainWidthKm * unityScaleFactor,
                maxElevationM * 0.001f, // 미터를 킬로미터로
                terrainHeightKm * unityScaleFactor
            );
        }
        
        /// <summary>
        /// RAW 파일로부터 Terrain GameObject 생성
        /// </summary>
        /// <param name="rawFilePath">RAW 파일 경로</param>
        /// <param name="tileX">타일 X 좌표 (0-35)</param>
        /// <param name="tileY">타일 Y 좌표 (0-17)</param>
        /// <returns>생성된 Terrain GameObject</returns>
        public GameObject LoadTerrainFromRAW(string rawFilePath, int tileX, int tileY)
        {
            if (!File.Exists(rawFilePath))
            {
                Debug.LogError($"RAW 파일을 찾을 수 없습니다: {rawFilePath}");
                return null;
            }
            
            // 1. TerrainData 생성
            TerrainData terrainData = new TerrainData();
            terrainData.heightmapResolution = heightmapResolution;
            terrainData.size = GetTerrainSize();
            
            // 2. RAW 파일 읽기 (Uint16, Little Endian)
            byte[] rawBytes = File.ReadAllBytes(rawFilePath);
            
            if (rawBytes.Length != heightmapResolution * heightmapResolution * 2)
            {
                Debug.LogError($"RAW 파일 크기가 예상과 다릅니다: {rawBytes.Length} bytes, 예상: {heightmapResolution * heightmapResolution * 2} bytes");
                return null;
            }
            
            // 3. 높이 데이터 배열 생성
            float[,] heights = new float[heightmapResolution, heightmapResolution];
            
            // 4. Uint16 데이터를 Unity float 높이로 변환 (90도 회전 + Flip Vertically 적용)
            for (int y = 0; y < heightmapResolution; y++)
            {
                for (int x = 0; x < heightmapResolution; x++)
                {
                    int index = (y * heightmapResolution + x) * 2;
                    
                    // Little Endian으로 Uint16 읽기
                    ushort heightValue = (ushort)(rawBytes[index] | (rawBytes[index + 1] << 8));
                    
                    // 0-65535 범위를 0-1 범위로 정규화
                    // Y축 뒤집기: 위아래 반전하여 올바른 시점으로 보이도록 수정
                    heights[heightmapResolution - 1 - y, x] = heightValue / 65535.0f;
                }
            }
            
            // 5. Terrain에 높이 데이터 적용
            terrainData.SetHeights(0, 0, heights);
            
            // 6. GameObject에 Terrain 컴포넌트 추가
            GameObject terrainGO = Terrain.CreateTerrainGameObject(terrainData);
            terrainGO.name = $"Terrain_{tileX}_{tileY}";
            
            // 7. 실제 지리적 위치에 배치
            Vector3 worldPosition = GetWorldPositionFromTile(tileX, tileY);
            terrainGO.transform.position = worldPosition;
            
            // 8. Terrain 설정
            Terrain terrain = terrainGO.GetComponent<Terrain>();
            terrain.materialTemplate = Resources.Load<Material>("DefaultTerrain"); // 필요시 생성
            
            Debug.Log($"Terrain 로드 완료: {terrainGO.name} at {worldPosition}");
            
            return terrainGO;
        }
        
        /// <summary>
        /// 타일 좌표를 월드 좌표로 변환
        /// </summary>
        private Vector3 GetWorldPositionFromTile(int tileX, int tileY)
        {
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
        
        /// <summary>
        /// 월드 좌표에서 해당하는 타일 인덱스 계산
        /// GetWorldPositionFromTile()의 역변환 로직
        /// </summary>
        /// <param name="worldPosition">Unity 월드 좌표</param>
        /// <returns>타일 인덱스 (x, y)</returns>
        public Vector2Int GetTileFromWorldPosition(Vector3 worldPosition)
        {
            // Unity 월드 좌표를 지리적 좌표로 역변환
            float longitude = (worldPosition.x / (terrainWidthKm * unityScaleFactor)) * 10f;
            float latitude = (worldPosition.z / (terrainHeightKm * unityScaleFactor)) * 10f;
            
            // 지리적 좌표를 타일 인덱스로 변환
            int tileX = Mathf.FloorToInt((longitude + 180f) / 10f);
            int tileY = Mathf.FloorToInt((90f - latitude) / 10f);
            
            // 유효 범위로 클램프
            tileX = Mathf.Clamp(tileX, 0, 35);
            tileY = Mathf.Clamp(tileY, 0, 17);
            
            return new Vector2Int(tileX, tileY);
        }
        
        /// <summary>
        /// 지리적 좌표(경도, 위도)에서 타일 인덱스 계산
        /// </summary>
        public Vector2Int GetTileFromGeographicCoordinate(float longitude, float latitude)
        {
            int tileX = Mathf.FloorToInt((longitude + 180f) / 10f);
            int tileY = Mathf.FloorToInt((90f - latitude) / 10f);
            
            tileX = Mathf.Clamp(tileX, 0, 35);
            tileY = Mathf.Clamp(tileY, 0, 17);
            
            return new Vector2Int(tileX, tileY);
        }
    }
}