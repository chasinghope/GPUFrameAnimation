using UnityEngine;
using System.Collections;

namespace GPUAnimation
{
    [RequireComponent(typeof(GPUInstancedAnimation))]
    public class GPUAnimFlash : MonoBehaviour
    {
        [Header("Flash Settings")]
        public Gradient flashGradient; // 在编辑器中配置渐变色
        public float defaultDuration = 0.3f;

        private GPUInstancedAnimation _anim;
        private Coroutine _flashRoutine;

        private void Awake()
        {
            _anim = GetComponent<GPUInstancedAnimation>();
        }

        /// <summary>
        /// 触发受击闪烁
        /// </summary>
        public void PlayFlash(float duration = -1f)
        {
            float d = duration > 0 ? duration : defaultDuration;
            if (_flashRoutine != null) StopCoroutine(_flashRoutine);
            _flashRoutine = StartCoroutine(DoFlash(d));
        }

        private IEnumerator DoFlash(float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float normalizedTime = elapsed / duration;
                
                // 从渐变色中采样颜色
                Color currentColor = flashGradient.Evaluate(normalizedTime);
                
                // 应用到渲染器
                _anim.SetTintColor(currentColor);
                yield return null;
            }

            // 恢复初始状态（通常渐变最后一位应设为白色）
            _anim.SetTintColor(Color.white);
            _flashRoutine = null;
        }
    }
}