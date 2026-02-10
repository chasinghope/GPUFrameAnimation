using UnityEngine;
using UnityEditor;

namespace GPUAnimation.Editor
{
    public class GPUAnimationCreator
    {
        // 配置预制体在项目中的路径
        private const string PREFAB_PATH = "Prefabs/GPUAnimEntity";
        [MenuItem("GameObject/GPUFrameAnimation/GPU Animation Entity", false, 10)]
        private static void CreateFromTemplate(MenuCommand menuCommand)
        {
            // 1. 从路径加载预制体资源
            GameObject template = Resources.Load<GameObject>(PREFAB_PATH);

            if (template == null)
            {
                Debug.LogError($"未找到预制体模板，请确认路径是否正确: {PREFAB_PATH}");
                return;
            }

            // 2. 实例化预制体
            // GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(template);
            GameObject instance = Object.Instantiate(template);
            instance.name = "GPUAnim_";

            // 3. 设置父子层级并重置坐标
            GameObjectUtility.SetParentAndAlign(instance, menuCommand.context as GameObject);

            // 4. 注册撤销操作并选中
            Undo.RegisterCreatedObjectUndo(instance, "Create GPU Animation From Template");
            Selection.activeObject = instance;
        }
    }
}