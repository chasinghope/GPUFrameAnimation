using System;
using System.Collections.Generic;
using UnityEngine;

namespace GPUAnimation
{
    [System.Serializable]
    public class GPUAnimationParam
    {
        public string AnimName;
        public GPUInstancedAnimation GpuAnim;
    }
    
    public class GPUFrameAnimator : MonoBehaviour
    {
        [SerializeField]
        private List<GPUAnimationParam> gpuAnimationParamList = new();

        private Dictionary<string, GPUInstancedAnimation> gpuAnimationDict = new();

        [SerializeField]
        private string defaultAnimationName;

        [SerializeField]
        private bool autoPlay;

        public event Action<string> EOnAnimStart, EOnAnimEnd;

        private void Awake()
        {
            gpuAnimationDict.Clear();
            for (int i = 0; i < gpuAnimationParamList.Count; i++)
            {
                GPUAnimationParam param = gpuAnimationParamList[i];
                if (param.GpuAnim != null)
                {
                    if (!gpuAnimationDict.ContainsKey(param.AnimName))
                    {
                        gpuAnimationDict.Add(param.AnimName, param.GpuAnim);
                    }
                    else
                    {
                        Debug.LogWarning($"重复的动画名称: {param.AnimName}");
                    }
                }
                else
                {
                    Debug.LogWarning($"动画参数中GpuAnim为空: {param.AnimName}");
                }
            }
        }


        private void OnEnable()
        {
            foreach (var gpuAnimation in gpuAnimationParamList)
            {
                if(gpuAnimation.GpuAnim != null)
                {
                    gpuAnimation.GpuAnim.OnPlayStart += OnAnimStartCall;
                    gpuAnimation.GpuAnim.OnPlayFinished += OnAnimEndCall;
                }
            }

            if (autoPlay && gpuAnimationDict.ContainsKey(defaultAnimationName))
            {
                Play(defaultAnimationName);
            }
        }

        private void OnDisable()
        {
            foreach (var gpuAnimation in gpuAnimationParamList)
            {
                if(gpuAnimation.GpuAnim != null)
                {
                    gpuAnimation.GpuAnim.OnPlayStart -= OnAnimStartCall;
                    gpuAnimation.GpuAnim.OnPlayFinished -= OnAnimEndCall;
                }
            }
        }


        private void Reset()
        {
            gpuAnimationParamList.Clear();
            for (int i = 0; i < transform.childCount; i++)
            {
                Transform s = transform.GetChild(i);
                GPUInstancedAnimation anim = s.GetComponent<GPUInstancedAnimation>();
                if (anim == null)
                {
                    Debug.LogWarning($"{s.name} gpuInstancedAnimation cannot be null.");
                }
                GPUAnimationParam param = new()
                {
                    AnimName = s.gameObject.name,
                    GpuAnim = anim
                };
                if(anim != null)
                {
                    gpuAnimationParamList.Add(param);
                }
            }
        }

        
        public void Play(string rAnimName)
        {
            if (!gpuAnimationDict.TryGetValue(rAnimName, out GPUInstancedAnimation anim))
            {
                Debug.LogWarning($"不存在动画{rAnimName}");
                return;
            }

            for (int i = 0; i < gpuAnimationParamList.Count; i++)
            {
                if(gpuAnimationParamList[i].GpuAnim != null)
                {
                    gpuAnimationParamList[i].GpuAnim.gameObject.SetActive(false);
                }
            }

            anim.gameObject.SetActive(true);
            anim.Play();
        }


        private void OnAnimStartCall(GPUInstancedAnimation rGpuAnim)
        {
            foreach (var param in gpuAnimationParamList)
            {
                if (param.GpuAnim == rGpuAnim)
                {
                    EOnAnimStart?.Invoke(param.AnimName);
                    break;
                }
            }
        }
        
        
        private void OnAnimEndCall(GPUInstancedAnimation rGpuAnim)
        {
            foreach (var param in gpuAnimationParamList)
            {
                if (param.GpuAnim == rGpuAnim)
                {
                    EOnAnimEnd?.Invoke(param.AnimName);
                    break;
                }
            }
        }
    }
}