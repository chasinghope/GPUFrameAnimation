using System;
using System.Collections.Generic;
using UnityEngine;

namespace GPUAnimation
{
    public class GPUAnimManager : MonoBehaviour
    {
        private static GPUAnimManager _instance;
        private static readonly object _lock = new object();
        private static readonly int UnscaledTime = Shader.PropertyToID("_UnscaledTime");

        public static GPUAnimManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            GameObject managerObj = new GameObject("GPUAnimManager");
                            _instance = managerObj.AddComponent<GPUAnimManager>();
                            DontDestroyOnLoad(managerObj);
                        }
                    }
                }
                return _instance;
            }
        }

        [Tooltip("基础材质球，需使用支持 Instancing 的 Shader")]
        public Material baseMaterial;

        /// <summary>
        /// 材质缓存池：Key 为贴图，Value 为生成的唯一材质
        /// </summary>
        private Dictionary<Texture2D, Material> _materialPool = new Dictionary<Texture2D, Material>();

        public Material GetSharedMaterial(Texture2D tex)
        {
            if (tex == null) 
            {
                Debug.LogError("传入的纹理不能为空");
                return null;
            }

            if (!_materialPool.TryGetValue(tex, out Material mat))
            {
                // 如果池子里没有，基于基础材质创建一个新的
                if (baseMaterial == null)
                {
                    // 如果没有手动指定基础材质，则尝试加载默认路径
                    baseMaterial = Resources.Load<Material>("Materials/Mat_GPUAnim");
                    if (baseMaterial == null) 
                    {
                        Debug.LogError("无法加载基础材质，请确保 Resources/Materials/Mat_GPUAnim 存在");
                        return null;
                    }
                }

                mat = new Material(baseMaterial);
                mat.name = $"Mat_GPU_{tex.name}";
                mat.mainTexture = tex;
                mat.enableInstancing = true; // 强制开启 Instancing
                _materialPool.Add(tex, mat);
            }
            return mat;
        }

        private void OnDestroy()
        {
            // 销毁时清理材质缓存以释放内存
            foreach (var material in _materialPool.Values)
            {
                if (material != null)
                {
#if UNITY_EDITOR
                    if (Application.isEditor && !Application.isPlaying)
                    {
                        DestroyImmediate(material);
                    }
                    else
                    {
                        Destroy(material);
                    }
#else
                    Destroy(material);
#endif
                }
            }
            _materialPool.Clear();
        }

        private void Update()
        {
            // 模拟 Unity 的 _Time 结构： (t/20, t, t*2, t*3)
            // 这样你在 Shader 里就能用 _UnscaledTime.y 了
            float t = Time.unscaledTime;
            Shader.SetGlobalVector(UnscaledTime, new Vector4(t / 20f, t, t * 2f, t * 3f));
        }
    }
}
