using UnityEngine;
using System;
using UnityEngine.Serialization;

namespace GPUAnimation
{
    [RequireComponent(typeof(MeshRenderer), typeof(MeshFilter))]
    public class GPUInstancedAnimation : MonoBehaviour
    {
        [Header("Resources")]
        public Texture2D mainTexture;
        
        [Header("Layout Settings")]
        public int columns = 8;
        public int rows = 8;
        public int totalFrames = 64;
        public float fps = 30f;
        public bool isLoop = true;

        [Header("Transform Settings")]
        public bool autoScale = true; 
        public float baseScale = 1.0f;

        public Vector2 Pivot = new Vector2(0.5f, 0.5f);

        // --- C# 标准事件接口 ---
        public event Action<GPUInstancedAnimation> OnPlayStart;    
        public event Action<GPUInstancedAnimation> OnPlayFinished; 

        public bool IsPlaying { get; private set; }

        private MaterialPropertyBlock _propBlock;
        private MeshRenderer _renderer;
        private float _duration;
        private float _timer;

        private static readonly int ID_Columns = Shader.PropertyToID("_Columns");
        private static readonly int ID_Rows = Shader.PropertyToID("_Rows");
        private static readonly int ID_TotalFrames = Shader.PropertyToID("_TotalFrames");
        private static readonly int ID_FPS = Shader.PropertyToID("_FPS");
        private static readonly int ID_Loop = Shader.PropertyToID("_Loop");
        private static readonly int ID_StartTime = Shader.PropertyToID("_StartTime");
        private static readonly int ID_PivotOffset = Shader.PropertyToID("_PivotOffset");

        private void Reset() => SetupMeshAndMaterial();

        private void OnValidate()
        {
            if (_renderer == null) _renderer = GetComponent<MeshRenderer>();
            SyncPreview();
        }

        private void Awake()
        {
            _renderer = GetComponent<MeshRenderer>();
            _propBlock = new MaterialPropertyBlock();
            
            if (Application.isPlaying)
            {
                InitAnimation();
            }
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
            if (_renderer == null) _renderer = GetComponent<MeshRenderer>();
            
            // 关键点：使用 sharedMaterial 实现同类合批
            _renderer.sharedMaterial = newCategoryMat;
        
            if (autoScale && newCategoryMat.mainTexture != null)
            {
                float pW = (float)mainTexture.width / Mathf.Max(1, columns);
                float pH = (float)mainTexture.height / Mathf.Max(1, rows);

                // 换算为 Unity 单位：Pixel / PPU
                Vector3 targetScale = new Vector3(
                    (pW / 100f) * baseScale, 
                    (pH / 100f) * baseScale, 
                    1f
                );
                transform.localScale = targetScale;
            }
        }

        public void UpdateProperties(float startTime)
        {
            if (_renderer == null) return;
            if (_propBlock == null) _propBlock = new MaterialPropertyBlock();

            _renderer.GetPropertyBlock(_propBlock);

            Vector4 pivotOffset = new Vector4(Pivot.x - 0.5f, Pivot.y - 0.5f, 0, 0);
            
            // 仅提交数值，不提交 Texture 以维持合批
            _propBlock.SetVector(ID_PivotOffset, pivotOffset);
            _propBlock.SetFloat(ID_Columns, columns);
            _propBlock.SetFloat(ID_Rows, rows);
            _propBlock.SetFloat(ID_TotalFrames, totalFrames);
            _propBlock.SetFloat(ID_FPS, fps);
            _propBlock.SetFloat(ID_Loop, isLoop ? 1f : 0f);
            _propBlock.SetFloat(ID_StartTime, startTime); 
        
            _renderer.SetPropertyBlock(_propBlock);
        }

        // 辅助方法：确保 Editor 模式下有材质显示
        private void SetupMeshAndMaterial()
        {
            MeshFilter mf = GetComponent<MeshFilter>();
            if (mf.sharedMesh == null)
            {
                GameObject temp = GameObject.CreatePrimitive(PrimitiveType.Quad);
                mf.sharedMesh = temp.GetComponent<MeshFilter>().sharedMesh;
                DestroyImmediate(temp);
            }

            _renderer = GetComponent<MeshRenderer>();
            if (_renderer.sharedMaterial == null)
            {
    #if UNITY_EDITOR
                Material loadedMat = Resources.Load<Material>("Materials/Mat_GPUAnim");
                if (loadedMat != null) _renderer.sharedMaterial = loadedMat;
    #endif
            }
        }

        private void SyncPreview()
        {
            UpdateProperties(0);
    #if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
            if (!Application.isPlaying) UnityEditor.SceneView.RepaintAll();
    #endif
        }
    }
}
