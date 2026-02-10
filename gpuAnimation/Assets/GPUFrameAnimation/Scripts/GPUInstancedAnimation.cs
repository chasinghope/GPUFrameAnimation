using UnityEngine;
using System;

namespace GPUAnimation
{
    public class GPUInstancedAnimation : MonoBehaviour
    {
        [Header("Resources")]
        public Texture2D mainTexture;
        public float pixelsPerUnit = 100f;
        public Vector2 Pivot = new Vector2(0.5f, 0.5f);
        public Color tintColor = Color.white;
        
        [Header("Layout Settings")]
        public int rows = 8;
        public int columns = 8;
        public int startFrame = 0; // 暴露起始帧设置
        public int totalFrames = 64;
        public float fps = 30f;
        public bool isLoop = true;

        // --- C# 标准事件接口 ---
        public event Action<GPUInstancedAnimation> OnPlayStart;    
        public event Action<GPUInstancedAnimation> OnPlayFinished; 

        public bool IsPlaying { get; private set; }

        private Transform child;
        private MaterialPropertyBlock _propBlock;
        private MeshRenderer _renderer;
        private float _duration;
        private float _timer;

        private static readonly int ID_Columns = Shader.PropertyToID("_Columns");
        private static readonly int ID_Rows = Shader.PropertyToID("_Rows");
        private static readonly int ID_StartFrame = Shader.PropertyToID("_StartFrame");
        private static readonly int ID_TotalFrames = Shader.PropertyToID("_TotalFrames");
        private static readonly int ID_FPS = Shader.PropertyToID("_FPS");
        private static readonly int ID_Loop = Shader.PropertyToID("_Loop");
        private static readonly int ID_StartTime = Shader.PropertyToID("_StartTime");
        private static readonly int ID_PivotOffset = Shader.PropertyToID("_PivotOffset");
        private static readonly int ID_Color = Shader.PropertyToID("_Color");
        
        

        private void Awake()
        {
            child = transform.GetChild(0);
            if (child == null)
            {
                Debug.LogError($"没有发现渲染子物体");
                return;
            }
            _renderer = child.GetComponent<MeshRenderer>();
            _propBlock = new MaterialPropertyBlock();
            
            InitAnimation();
        }

        private void OnEnable()
        {
            if (Application.isPlaying) Play();
        }

        // 核心修改：初始化时向中控索要材质
        public void InitAnimation()
        {
            if (mainTexture != null)
            {
                Material sharedMat = GPUAnimManager.Instance.GetSharedMaterial(mainTexture);
                if (sharedMat != null)
                {
                    ChangeAnimationCategory(sharedMat);
                }
            }
            UpdateProperties(Time.timeSinceLevelLoad);
        }

        [ContextMenu("Play")]
        public void Play()
        {
            IsPlaying = true;
            _duration = (fps > 0) ? (totalFrames / fps) : 0;
            _timer = 0f;

            UpdateProperties(Application.isPlaying ? Time.timeSinceLevelLoad : 0f);
            OnPlayStart?.Invoke(this);
        }

        private void Update()
        {
            if (!IsPlaying || isLoop) return;

            _timer += Time.deltaTime;
            if (_timer >= _duration)
            {
                IsPlaying = false;
                OnPlayFinished?.Invoke(this);
            }
        }

        public void ChangeAnimationCategory(Material newCategoryMat)
        {
            // 关键点：使用 sharedMaterial 实现同类合批
            _renderer.sharedMaterial = newCategoryMat;
        
            if (newCategoryMat.mainTexture != null)
            {
                float pW = (float)mainTexture.width / Mathf.Max(1, columns);
                float pH = (float)mainTexture.height / Mathf.Max(1, rows);
                
                // 换算为 Unity 单位：Pixel / PPU
                Vector3 targetScale = new Vector3(
                    (pW / pixelsPerUnit), 
                    (pH / pixelsPerUnit), 
                    1f
                );
                child.localScale = targetScale;
            }
        }

        public void UpdateProperties(float startTime)
        {
            if (_renderer == null) return;
            if (_propBlock == null) _propBlock = new MaterialPropertyBlock();

            _renderer.GetPropertyBlock(_propBlock);

            Vector4 pivotOffset = new Vector4(Pivot.x - 0.5f, Pivot.y - 0.5f, 0, 0);
            
            // 仅提交数值，不提交 Texture 以维持合批
            _propBlock.SetColor(ID_Color, tintColor);
            _propBlock.SetVector(ID_PivotOffset, pivotOffset);
            _propBlock.SetFloat(ID_Columns, columns);
            _propBlock.SetFloat(ID_Rows, rows);
            _propBlock.SetFloat(ID_StartFrame, startFrame);
            _propBlock.SetFloat(ID_TotalFrames, totalFrames);
            _propBlock.SetFloat(ID_FPS, fps);
            _propBlock.SetFloat(ID_Loop, isLoop ? 1f : 0f);
            _propBlock.SetFloat(ID_StartTime, startTime); 
        
            _renderer.SetPropertyBlock(_propBlock);
        }
        
        public void SetTintColor(Color color)
        {
            this.tintColor = color;
            
            // 立即应用修改
            if (_renderer == null) return;
            if (_propBlock == null) _propBlock = new MaterialPropertyBlock();

            _renderer.GetPropertyBlock(_propBlock);
            _propBlock.SetColor(ID_Color, tintColor); // 提交颜色到 PropertyBlock
            _renderer.SetPropertyBlock(_propBlock);
        }

        private void SyncPreview()
        {
            UpdateProperties(0);
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
            if (!Application.isPlaying) UnityEditor.SceneView.RepaintAll();
#endif
        }
        
        
#if UNITY_EDITOR
        [ContextMenu("Sync PPU from Texture")]
        public void SyncPPUFromTexture()
        {
            if (mainTexture == null) return;
            string path = UnityEditor.AssetDatabase.GetAssetPath(mainTexture);
            var importer = UnityEditor.AssetImporter.GetAtPath(path) as UnityEditor.TextureImporter;
            if (importer != null)
            {
                pixelsPerUnit = importer.spritePixelsPerUnit;
                UnityEditor.EditorUtility.SetDirty(this);
                SyncPreview();
            }
        }
#endif
        
    }
}
