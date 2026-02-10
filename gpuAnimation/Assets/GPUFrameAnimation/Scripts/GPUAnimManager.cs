using System;
using System.Collections.Generic;
using UnityEngine;

namespace GPUAnimation
{
    public class GPUAnimManager : MonoBehaviour
    {
        private static GPUAnimManager _instance;
        private static readonly int UnscaledTime = Shader.PropertyToID("_UnscaledTime");

        public static GPUAnimManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new GameObject("GPUAnimManager").AddComponent<GPUAnimManager>();
                    DontDestroyOnLoad(_instance.gameObject);
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
            if (tex == null) return null;

            if (!_materialPool.TryGetValue(tex, out Material mat))
            {
                // 如果池子里没有，基于基础材质创建一个新的
                if (baseMaterial == null)
                {
                    // 如果没有手动指定基础材质，则尝试加载默认路径
                    baseMaterial = Resources.Load<Material>("Materials/Mat_GPUAnim");
                    if (baseMaterial == null) return null;
                }

                mat = new Material(baseMaterial);
                mat.name = $"Mat_GPU_{tex.name}";
                mat.mainTexture = tex;
                mat.enableInstancing = true; // 强制开启 Instancing
                _materialPool.Add(tex, mat);
            }
            return mat;
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
