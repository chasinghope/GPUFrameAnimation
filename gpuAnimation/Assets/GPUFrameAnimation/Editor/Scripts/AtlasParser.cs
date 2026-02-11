using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

namespace GPUAnimation.Editor
{
    /// <summary>
    /// 动画状态信息结构，存储解析后的动画数据
    /// </summary>
    public class AnimationStateInfo
    {
        /// <summary>
        /// 动画状态名称（如：hero4-initial-atk）
        /// </summary>
        public string stateName;

        /// <summary>
        /// 起始帧索引
        /// </summary>
        public int startFrameIndex;

        /// <summary>
        /// 动画总帧数
        /// </summary>
        public int frameCount;

        public AnimationStateInfo(string stateName, int startFrameIndex, int frameCount)
        {
            this.stateName = stateName;
            this.startFrameIndex = startFrameIndex;
            this.frameCount = frameCount;
        }

        public override string ToString()
        {
            return $"{stateName}@{startFrameIndex}@{frameCount}";
        }
    }

    /// <summary>
    /// .atlas 文件解析工具
    /// 用于解析纹理图集文件并提取动画状态信息
    /// </summary>
    public class AtlasParser
    {
        /// <summary>
        /// 需要过滤的行前缀列表
        /// </summary>
        private static readonly string[] FilteredPrefixes = { "bounds", "origin" };

        /// <summary>
        /// 通过文件路径解析 .atlas 文件
        /// </summary>
        /// <param name="filePath">.atlas 文件的绝对路径</param>
        /// <returns>解析后的动画状态信息列表</returns>
        public static List<AnimationStateInfo> ParseAtlasFile(string filePath)
        {
            List<AnimationStateInfo> result = new List<AnimationStateInfo>();

            if (!File.Exists(filePath))
            {
                Debug.LogError($"文件不存在: {filePath}");
                return result;
            }

            string[] lines = File.ReadAllLines(filePath);

            foreach (string line in lines)
            {
                string trimmedLine = line.Trim();

                // 跳过空行
                if (string.IsNullOrEmpty(trimmedLine))
                {
                    continue;
                }

                // 过滤以指定前缀开头的行
                if (ShouldFilterLine(trimmedLine))
                {
                    continue;
                }

                // 尝试解析动画状态信息
                AnimationStateInfo stateInfo = ParseAnimationState(trimmedLine);
                if (stateInfo != null)
                {
                    result.Add(stateInfo);
                }
            }

            return result;
        }

        /// <summary>
        /// 判断是否应该过滤该行
        /// </summary>
        /// <param name="line">文件行内容</param>
        /// <returns>是否需要过滤</returns>
        private static bool ShouldFilterLine(string line)
        {
            foreach (string prefix in FilteredPrefixes)
            {
                if (line.StartsWith(prefix))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 解析单行动画状态信息
        /// 根据最后一个下划线分割该行内容，解析格式：状态名@起始索引@长度
        /// </summary>
        /// <param name="line">文件行内容</param>
        /// <returns>解析后的动画状态信息，解析失败返回 null</returns>
        private static AnimationStateInfo ParseAnimationState(string line)
        {
            // 找到最后一个下划线的位置
            int lastUnderscoreIndex = line.LastIndexOf('_');

            if (lastUnderscoreIndex == -1 || lastUnderscoreIndex == line.Length - 1)
            {
                // 没有下划线或下划线是最后一个字符，无法解析
                return null;
            }

            // 提取状态名（最后一个下划线之前的部分）
            string stateName = line.Substring(0, lastUnderscoreIndex);

            // 提取帧索引（最后一个下划线之后的部分）
            string framePart = line.Substring(lastUnderscoreIndex + 1);

            // 尝试解析帧索引
            if (!int.TryParse(framePart, out int frameIndex))
            {
                return null;
            }

            // 返回包含状态名和帧索引的信息
            // 注意：这里只记录单帧信息，需要后续合并成动画状态
            return new AnimationStateInfo(stateName, frameIndex, 1);
        }

        /// <summary>
        /// 将解析的帧信息合并为完整的动画状态
        /// 同一状态名的帧会被合并，计算起始索引和总帧数
        /// </summary>
        /// <param name="frameInfos">帧信息列表</param>
        /// <returns>合并后的动画状态列表</returns>
        public static List<AnimationStateInfo> MergeFrameInfos(List<AnimationStateInfo> frameInfos)
        {
            Dictionary<string, List<int>> stateFrames = new Dictionary<string, List<int>>();

            // 按状态名分组帧索引
            foreach (AnimationStateInfo frameInfo in frameInfos)
            {
                if (!stateFrames.ContainsKey(frameInfo.stateName))
                {
                    stateFrames[frameInfo.stateName] = new List<int>();
                }
                stateFrames[frameInfo.stateName].Add(frameInfo.startFrameIndex);
            }

            // 构建合并后的动画状态信息
            List<AnimationStateInfo> result = new List<AnimationStateInfo>();
            foreach (var kvp in stateFrames)
            {
                string stateName = kvp.Key;
                List<int> frames = kvp.Value;

                // 排序帧索引并获取起始帧和总帧数
                frames.Sort();
                int startFrame = frames[0];
                int frameCount = frames.Count;

                result.Add(new AnimationStateInfo(stateName, startFrame, frameCount));
            }

            return result;
        }
    }

    /// <summary>
    /// Atlas 文件解析器的 Editor 窗口
    /// 提供图形界面选择并解析 .atlas 文件
    /// </summary>
    public class AtlasParserWindow : EditorWindow
    {
        /// <summary>
        /// 选中的 .atlas 文件路径
        /// </summary>
        private string selectedAtlasPath = string.Empty;

        /// <summary>
        /// 解析结果列表
        /// </summary>
        private List<AnimationStateInfo> parseResults = new List<AnimationStateInfo>();

        /// <summary>
        /// 滚动视图位置
        /// </summary>
        private Vector2 scrollPosition;

        [MenuItem("Tools/GPUFrameAnimation/Atlas Parser")]
        private static void ShowWindow()
        {
            GetWindow<AtlasParserWindow>("Atlas Parser");
        }

        /// <summary>
        /// 在 Project 窗口右键菜单中添加选项
        /// </summary>
        [MenuItem("Assets/GPUFrameAnimation/Parse Atlas", false, 10)]
        private static void ParseSelectedAtlas()
        {
            string selectedPath = AssetDatabase.GetAssetPath(Selection.activeObject);

            if (string.IsNullOrEmpty(selectedPath))
            {
                Debug.LogError("未选择任何文件");
                return;
            }

            if (!selectedPath.EndsWith(".atlas") && !selectedPath.EndsWith(".atlas.txt"))
            {
                Debug.LogError($"选中的文件不是 .atlas 或 .txt 文件: {selectedPath}");
                return;
            }

            string fullPath = Path.GetFullPath(selectedPath);
            ParseAndDisplay(fullPath);
        }

        /// <summary>
        /// 验证菜单项是否可用
        /// </summary>
        [MenuItem("Assets/GPUFrameAnimation/Parse Atlas", true)]
        private static bool ValidateParseSelectedAtlas()
        {
            if (Selection.activeObject == null)
            {
                return false;
            }

            string path = AssetDatabase.GetAssetPath(Selection.activeObject);
            return path.EndsWith(".atlas") || path.EndsWith(".atlas.txt");
        }

        /// <summary>
        /// 批量解析选中的所有 .atlas/.txt 文件
        /// </summary>
        [MenuItem("Assets/GPUFrameAnimation/Parse All Selected Atlas", false, 11)]
        private static void ParseAllSelectedAtlas()
        {
            // 获取所有选中的对象并过滤出 .atlas 和 .txt 文件
            UnityEngine.Object[] selectedObjects = Selection.GetFiltered(typeof(UnityEngine.Object), SelectionMode.Assets);

            if (selectedObjects == null || selectedObjects.Length == 0)
            {
                Debug.LogError("未选择任何文件");
                return;
            }

            List<string> validPaths = new List<string>();

            foreach (UnityEngine.Object obj in selectedObjects)
            {
                string assetPath = AssetDatabase.GetAssetPath(obj);

                if (string.IsNullOrEmpty(assetPath))
                {
                    continue;
                }

                if (assetPath.EndsWith(".atlas") || assetPath.EndsWith(".atlas.txt"))
                {
                    string fullPath = Path.GetFullPath(assetPath);
                    if (File.Exists(fullPath))
                    {
                        validPaths.Add(fullPath);
                    }
                }
            }

            if (validPaths.Count == 0)
            {
                Debug.LogError("选中的文件中没有有效的 .atlas 或 .atlas.txt 文件");
                return;
            }

            // 批量解析所有文件
            BatchParseAndDisplay(validPaths);
        }

        /// <summary>
        /// 验证批量解析菜单项是否可用
        /// </summary>
        [MenuItem("Assets/GPUFrameAnimation/Parse All Selected Atlas", true)]
        private static bool ValidateParseAllSelectedAtlas()
        {
            UnityEngine.Object[] selectedObjects = Selection.GetFiltered(typeof(UnityEngine.Object), SelectionMode.Assets);

            if (selectedObjects == null || selectedObjects.Length == 0)
            {
                return false;
            }

            foreach (UnityEngine.Object obj in selectedObjects)
            {
                string path = AssetDatabase.GetAssetPath(obj);
                if (!string.IsNullOrEmpty(path) && (path.EndsWith(".atlas") || path.EndsWith(".atlas.txt")))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 批量解析多个文件并显示结果
        /// </summary>
        /// <param name="filePathList">要解析的文件路径列表</param>
        private static void BatchParseAndDisplay(List<string> filePathList)
        {
            if (filePathList == null || filePathList.Count == 0)
            {
                return;
            }

            Debug.Log($"========================================");
            Debug.Log($"开始批量解析 {filePathList.Count} 个 Atlas 文件");
            Debug.Log($"========================================\n");

            int totalFiles = 0;
            int successFiles = 0;
            int totalStates = 0;

            foreach (string filePath in filePathList)
            {
                totalFiles++;

                // 解析原始帧信息
                List<AnimationStateInfo> frameInfos = AtlasParser.ParseAtlasFile(filePath);

                if (frameInfos.Count == 0)
                {
                    Debug.LogWarning($"[文件 {totalFiles}/{filePathList.Count}] {Path.GetFileName(filePath)} - 未找到有效的动画帧信息");
                    continue;
                }

                // 合并为完整的动画状态
                List<AnimationStateInfo> mergedStates = AtlasParser.MergeFrameInfos(frameInfos);

                successFiles++;
                totalStates += mergedStates.Count;

                // 输出单个文件解析结果
                Debug.Log($"--- [文件 {totalFiles}/{filePathList.Count}] {Path.GetFileName(filePath)} ---");
                Debug.Log($"解析出 {mergedStates.Count} 个动画状态:");

                foreach (AnimationStateInfo state in mergedStates)
                {
                    Debug.Log($"  {state}");
                }

                Debug.Log(string.Empty);
            }

            // 输出汇总信息
            Debug.Log($"========================================");
            Debug.Log($"批量解析完成!");
            Debug.Log($"处理文件: {totalFiles} | 成功: {successFiles} | 失败: {totalFiles - successFiles}");
            Debug.Log($"总动画状态数: {totalStates}");
            Debug.Log($"========================================");
        }

        private void OnGUI()
        {
            GUILayout.Label(".atlas 文件解析工具", EditorStyles.boldLabel);

            // 文件选择区域
            EditorGUILayout.Space();
            GUILayout.Label("选择 .atlas 文件:", EditorStyles.label);

            if (GUILayout.Button("浏览文件", GUILayout.Height(30)))
            {
                BrowseAtlasFile();
            }

            // 显示选中文件路径
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("文件路径:", GUILayout.Width(60));
            selectedAtlasPath = GUILayout.TextField(selectedAtlasPath);
            EditorGUILayout.EndHorizontal();

            // 拖拽区域支持
            Rect dropArea = GUILayoutUtility.GetRect(0, 50, GUILayout.ExpandWidth(true));
            GUI.Box(dropArea, "拖拽 .atlas 文件到此处");
            HandleDragAndDrop(dropArea);

            EditorGUILayout.Space();

            // 解析按钮
            EditorGUI.BeginDisabledGroup(string.IsNullOrEmpty(selectedAtlasPath) || !File.Exists(selectedAtlasPath));
            if (GUILayout.Button("解析文件", GUILayout.Height(35)))
            {
                ParseSelectedFile();
            }
            EditorGUI.EndDisabledGroup();

            // 显示解析结果
            DisplayParseResults();
        }

        /// <summary>
        /// 处理拖拽文件操作
        /// </summary>
        private void HandleDragAndDrop(Rect dropArea)
        {
            Event currentEvent = Event.current;

            if (currentEvent.type == EventType.DragUpdated)
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                currentEvent.Use();
            }
            else if (currentEvent.type == EventType.DragPerform)
            {
                if (dropArea.Contains(currentEvent.mousePosition))
                {
                    DragAndDrop.AcceptDrag();

                    foreach (string draggedPath in DragAndDrop.paths)
                    {
                        if (draggedPath.EndsWith(".atlas") || draggedPath.EndsWith(".txt"))
                        {
                            selectedAtlasPath = draggedPath;
                            break;
                        }
                    }

                    currentEvent.Use();
                }
            }
        }

        /// <summary>
        /// 浏览并选择 .atlas 文件
        /// </summary>
        private void BrowseAtlasFile()
        {
            string path = EditorUtility.OpenFilePanel("选择 .atlas 文件", "Assets", "atlas;txt");

            if (!string.IsNullOrEmpty(path))
            {
                selectedAtlasPath = path;
            }
        }

        /// <summary>
        /// 解析选中的文件
        /// </summary>
        private void ParseSelectedFile()
        {
            if (string.IsNullOrEmpty(selectedAtlasPath) || !File.Exists(selectedAtlasPath))
            {
                Debug.LogError("请选择有效的 .atlas 文件");
                return;
            }

            ParseAndDisplay(selectedAtlasPath);
        }

        /// <summary>
        /// 解析并显示结果
        /// </summary>
        private static void ParseAndDisplay(string filePath)
        {
            // 解析原始帧信息
            List<AnimationStateInfo> frameInfos = AtlasParser.ParseAtlasFile(filePath);

            if (frameInfos.Count == 0)
            {
                Debug.LogWarning($"文件 {filePath} 中未找到有效的动画帧信息");
                return;
            }

            // 合并为完整的动画状态
            List<AnimationStateInfo> mergedStates = AtlasParser.MergeFrameInfos(frameInfos);

            // 输出到 Console
            Debug.Log($"=== Atlas 文件解析完成: {Path.GetFileName(filePath)} ===");
            Debug.Log($"共解析出 {mergedStates.Count} 个动画状态:\n");

            foreach (AnimationStateInfo state in mergedStates)
            {
                Debug.Log($"[动画状态] {state}");
            }
        }

        /// <summary>
        /// 显示解析结果
        /// </summary>
        private void DisplayParseResults()
        {
            if (parseResults.Count == 0)
            {
                return;
            }

            EditorGUILayout.Space();
            GUILayout.Label("解析结果:", EditorStyles.boldLabel);

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(200));

            foreach (AnimationStateInfo stateInfo in parseResults)
            {
                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                GUILayout.Label(stateInfo.stateName, GUILayout.Width(150));
                GUILayout.Label($"起始: {stateInfo.startFrameIndex}", GUILayout.Width(80));
                GUILayout.Label($"帧数: {stateInfo.frameCount}", GUILayout.Width(80));
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();
        }
    }
}
