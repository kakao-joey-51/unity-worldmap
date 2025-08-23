using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections;
using CAU;

/// <summary>
/// Unity Editor에서 RAW 파일들을 Terrain 에셋으로 일괄 변환하는 툴
/// 전세계 지형 데이터 648개 타일을 TerrainData 에셋으로 변환하여 저장
/// </summary>
public class TerrainBatchConverter : EditorWindow
    {
        [Header("Conversion Settings")]
        private string sourcePath = "Assets/Terrains/";
        private string targetPath = "Assets/Resources/TerrainData/";
        private bool convertToAssets = true;
        private bool createPrefabs = false;
        private int maxConcurrentConversions = 4;
        
        [Header("Progress")]
        private bool isConverting = false;
        private int totalFiles = 0;
        private int convertedFiles = 0;
        private string currentFile = "";
        
        private TerrainLoader terrainLoader;
        
        [MenuItem("Window/Terrain Batch Converter")]
        static void ShowWindow()
        {
            TerrainBatchConverter window = GetWindow<TerrainBatchConverter>("Terrain Batch Converter");
            window.minSize = new Vector2(400, 350);
            window.Show();
        }
        
        void OnEnable()
        {
            // TerrainLoader 인스턴스 생성
            GameObject tempGO = new GameObject("TerrainLoaderTemp");
            tempGO.hideFlags = HideFlags.HideAndDontSave;
            terrainLoader = tempGO.AddComponent<TerrainLoader>();
        }
        
        void OnDisable()
        {
            if (terrainLoader != null && terrainLoader.gameObject != null)
            {
                DestroyImmediate(terrainLoader.gameObject);
            }
        }
        
        void OnGUI()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Terrain Batch Converter", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("전세계 지형 RAW 파일들을 Unity Terrain 에셋으로 일괄 변환합니다.", MessageType.Info);
            
            EditorGUILayout.Space();
            
            // 경로 설정
            EditorGUILayout.LabelField("Path Settings", EditorStyles.boldLabel);
            sourcePath = EditorGUILayout.TextField("Source Path (RAW files):", sourcePath);
            targetPath = EditorGUILayout.TextField("Target Path (Assets):", targetPath);
            
            EditorGUILayout.Space();
            
            // 변환 옵션
            EditorGUILayout.LabelField("Conversion Options", EditorStyles.boldLabel);
            convertToAssets = EditorGUILayout.Toggle("Convert to TerrainData Assets", convertToAssets);
            createPrefabs = EditorGUILayout.Toggle("Create Terrain Prefabs", createPrefabs);
            maxConcurrentConversions = EditorGUILayout.IntSlider("Max Concurrent Conversions", maxConcurrentConversions, 1, 8);
            
            EditorGUILayout.Space();
            
            // 파일 정보 표시
            DisplayFileInfo();
            
            EditorGUILayout.Space();
            
            // 변환 버튼
            EditorGUI.BeginDisabledGroup(isConverting);
            
            if (GUILayout.Button("Convert All RAW Files", GUILayout.Height(30)))
            {
                StartConversion();
            }
            
            if (GUILayout.Button("Convert Selected Range"))
            {
                ShowRangeSelectionDialog();
            }
            
            EditorGUI.EndDisabledGroup();
            
            // 진행 상황 표시
            if (isConverting)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Progress", EditorStyles.boldLabel);
                
                float progress = totalFiles > 0 ? (float)convertedFiles / totalFiles : 0f;
                EditorGUI.ProgressBar(EditorGUILayout.GetControlRect(), progress, $"{convertedFiles}/{totalFiles}");
                
                EditorGUILayout.LabelField("Current File:", currentFile);
                
                if (GUILayout.Button("Cancel Conversion"))
                {
                    StopConversion();
                }
                
                Repaint();
            }
        }
        
        private void DisplayFileInfo()
        {
            string fullSourcePath = Application.dataPath + "/" + sourcePath.Replace("Assets/", "");
            
            if (Directory.Exists(fullSourcePath))
            {
                string[] rawFiles = Directory.GetFiles(fullSourcePath, "*.raw", SearchOption.TopDirectoryOnly);
                EditorGUILayout.LabelField($"Found RAW Files: {rawFiles.Length}");
                
                if (rawFiles.Length > 0)
                {
                    long totalSize = 0;
                    foreach (string file in rawFiles)
                    {
                        totalSize += new FileInfo(file).Length;
                    }
                    
                    EditorGUILayout.LabelField($"Total Size: {totalSize / (1024 * 1024)} MB");
                }
            }
            else
            {
                EditorGUILayout.HelpBox($"Source directory not found: {fullSourcePath}", MessageType.Warning);
            }
        }
        
        private void StartConversion()
        {
            string fullSourcePath = Application.dataPath + "/" + sourcePath.Replace("Assets/", "");
            
            if (!Directory.Exists(fullSourcePath))
            {
                EditorUtility.DisplayDialog("Error", $"Source directory not found: {fullSourcePath}", "OK");
                return;
            }
            
            // 대상 폴더 생성
            string fullTargetPath = Application.dataPath + "/" + targetPath.Replace("Assets/", "");
            if (!Directory.Exists(fullTargetPath))
            {
                Directory.CreateDirectory(fullTargetPath);
            }
            
            string[] rawFiles = Directory.GetFiles(fullSourcePath, "*.raw", SearchOption.TopDirectoryOnly);
            
            if (rawFiles.Length == 0)
            {
                EditorUtility.DisplayDialog("Warning", "No RAW files found in source directory", "OK");
                return;
            }
            
            isConverting = true;
            totalFiles = rawFiles.Length;
            convertedFiles = 0;
            
            EditorApplication.update += UpdateConversion;
            
            ConvertFilesSync(rawFiles);
        }
        
        private void StopConversion()
        {
            isConverting = false;
            EditorApplication.update -= UpdateConversion;
            
            EditorUtility.ClearProgressBar();
        }
        
        private void UpdateConversion()
        {
            if (!isConverting)
            {
                EditorApplication.update -= UpdateConversion;
            }
        }
        
        private void ConvertFilesSync(string[] rawFiles)
        {
            for (int i = 0; i < rawFiles.Length; i++)
            {
                if (!isConverting) break;
                
                string rawFile = rawFiles[i];
                currentFile = Path.GetFileName(rawFile);
                
                // 타일 인덱스 파싱
                if (ParseTileIndicesFromFileName(currentFile, out int tileX, out int tileY))
                {
                    ConvertSingleFileSync(rawFile, tileX, tileY);
                }
                else
                {
                    Debug.LogWarning($"파일명에서 타일 인덱스를 파싱할 수 없습니다: {currentFile}");
                }
                
                convertedFiles++;
                
                // 주기적으로 에디터 업데이트
                if (i % 5 == 0)
                {
                    EditorUtility.DisplayProgressBar(
                        "Converting Terrain Files",
                        $"Processing {currentFile} ({convertedFiles}/{totalFiles})",
                        (float)convertedFiles / totalFiles
                    );
                }
            }
            
            // 변환 완료
            EditorUtility.ClearProgressBar();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            isConverting = false;
            
            EditorUtility.DisplayDialog(
                "Conversion Complete",
                $"Successfully converted {convertedFiles} terrain files!",
                "OK"
            );
        }
        
        private void ConvertSingleFileSync(string rawFilePath, int tileX, int tileY)
        {
            try
            {
                // TerrainData 생성
                TerrainData terrainData = CreateTerrainDataFromRAW(rawFilePath, tileX, tileY);
                
                if (terrainData != null && convertToAssets)
                {
                    // 에셋으로 저장
                    string assetPath = $"{targetPath}/TerrainData_{tileX}_{tileY}.asset";
                    AssetDatabase.CreateAsset(terrainData, assetPath);
                    
                    // 프리팹 생성 옵션
                    if (createPrefabs)
                    {
                        CreateTerrainPrefabSync(terrainData, tileX, tileY);
                    }
                    
                    Debug.Log($"Terrain 변환 완료: tile_{tileX}_{tileY}");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Terrain 변환 실패 {Path.GetFileName(rawFilePath)}: {ex.Message}");
            }
        }
        
        private void CreateTerrainPrefabSync(TerrainData terrainData, int tileX, int tileY)
        {
            GameObject terrainGO = Terrain.CreateTerrainGameObject(terrainData);
            terrainGO.name = $"Terrain_{tileX}_{tileY}";
            
            // 프리팹으로 저장
            string prefabPath = $"{targetPath}/Prefabs/Terrain_{tileX}_{tileY}.prefab";
            
            // Prefabs 폴더 생성
            string prefabDirectory = Path.GetDirectoryName(Application.dataPath + "/" + prefabPath.Replace("Assets/", ""));
            if (!Directory.Exists(prefabDirectory))
            {
                Directory.CreateDirectory(prefabDirectory);
            }
            
            PrefabUtility.SaveAsPrefabAsset(terrainGO, prefabPath);
            
            // 임시 GameObject 삭제
            DestroyImmediate(terrainGO);
        }
        
        private TerrainData CreateTerrainDataFromRAW(string rawFilePath, int tileX, int tileY)
        {
            if (terrainLoader == null) return null;
            
            // 임시 GameObject 생성하여 TerrainLoader 사용
            GameObject tempTerrain = terrainLoader.LoadTerrainFromRAW(rawFilePath, tileX, tileY);
            
            if (tempTerrain != null)
            {
                Terrain terrain = tempTerrain.GetComponent<Terrain>();
                TerrainData terrainData = terrain.terrainData;
                
                // 에셋으로 저장하기 위해 복사본 생성
                TerrainData assetTerrainData = Object.Instantiate(terrainData);
                assetTerrainData.name = $"TerrainData_{tileX}_{tileY}";
                
                // 임시 GameObject 삭제
                DestroyImmediate(tempTerrain);
                
                return assetTerrainData;
            }
            
            return null;
        }
        
        private bool ParseTileIndicesFromFileName(string fileName, out int tileX, out int tileY)
        {
            tileX = tileY = -1;
            
            // 파일명 형식: tile_X_Y.raw
            string nameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
            string[] parts = nameWithoutExtension.Split('_');
            
            if (parts.Length >= 3 && parts[0] == "tile")
            {
                return int.TryParse(parts[1], out tileX) && int.TryParse(parts[2], out tileY);
            }
            
            return false;
        }
        
        private void ShowRangeSelectionDialog()
        {
            RangeSelectionDialog.ShowWindow(this);
        }
        
        public void ConvertTileRange(int startX, int startY, int endX, int endY)
        {
            // 범위 변환 구현
            Debug.Log($"Converting tile range: ({startX},{startY}) to ({endX},{endY})");
            // TODO: 범위 변환 로직 구현
        }
    
/// <summary>
/// 타일 범위 선택 다이얼로그
/// </summary>
public class RangeSelectionDialog : EditorWindow
    {
        private int startX = 0, startY = 0;
        private int endX = 35, endY = 17;
        private TerrainBatchConverter parentWindow;
        
        public static void ShowWindow(TerrainBatchConverter parent)
        {
            RangeSelectionDialog window = GetWindow<RangeSelectionDialog>("Select Tile Range");
            window.parentWindow = parent;
            window.minSize = new Vector2(300, 200);
            window.Show();
        }
        
        void OnGUI()
        {
            EditorGUILayout.LabelField("Select Tile Range to Convert", EditorStyles.boldLabel);
            
            EditorGUILayout.Space();
            
            EditorGUILayout.LabelField("Start Tile:");
            EditorGUILayout.BeginHorizontal();
            startX = EditorGUILayout.IntSlider("X", startX, 0, 35);
            startY = EditorGUILayout.IntSlider("Y", startY, 0, 17);
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space();
            
            EditorGUILayout.LabelField("End Tile:");
            EditorGUILayout.BeginHorizontal();
            endX = EditorGUILayout.IntSlider("X", endX, 0, 35);
            endY = EditorGUILayout.IntSlider("Y", endY, 0, 17);
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space();
            
            int tileCount = (endX - startX + 1) * (endY - startY + 1);
            EditorGUILayout.LabelField($"Total Tiles: {tileCount}");
            
            EditorGUILayout.Space();
            
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Convert Range"))
            {
                parentWindow?.ConvertTileRange(startX, startY, endX, endY);
                Close();
            }
            
            if (GUILayout.Button("Cancel"))
            {
                Close();
            }
            
            EditorGUILayout.EndHorizontal();
        }
    }
}