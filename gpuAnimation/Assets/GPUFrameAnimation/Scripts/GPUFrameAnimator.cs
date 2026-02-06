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

        private bool isBindEvent = false;
        
        private void Awake()
        {
            gpuAnimationDict.Clear();
            for (int i = 0; i < gpuAnimationParamList.Count; i++)
            {
                GPUAnimationParam param = gpuAnimationParamList[i];
                gpuAnimationDict.Add(param.AnimName, param.GpuAnim);
            }
        }


        private void OnEnable()
        {
            if (!isBindEvent)
            {
                isBindEvent = true;
                foreach (var gpuAnimation in gpuAnimationParamList)
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


        private void Reset()
        {
            gpuAnimationParamList.Clear();
            for (int i = 0; i < transform.childCount; i++)
            {
                Transform s = transform.GetChild(i);
                GPUAnimationParam param = new()
                {
                    AnimName = s.gameObject.name,
                    GpuAnim = s.GetComponent<GPUInstancedAnimation>()
                };
                gpuAnimationParamList.Add(param);
            }
        }

        
        public void Play(string rAnimName)
        {
            if (!gpuAnimationDict.TryGetValue(rAnimName, out GPUInstancedAnimation anim))
            {
                Debug.Log($"不存在动画{rAnimName}");
                return;
            }

            for (int i = 0; i < gpuAnimationParamList.Count; i++)
            {
                gpuAnimationParamList[i].GpuAnim.gameObject.SetActive(false);
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