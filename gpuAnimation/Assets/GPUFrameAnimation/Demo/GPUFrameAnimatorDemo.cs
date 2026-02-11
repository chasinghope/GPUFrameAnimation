using GPUAnimation;
using UnityEngine;
using UnityEngine.UI;

namespace GPUAnimation.Demo
{
    /// <summary>
    /// GPUFrameAnimator 功能演示脚本
    /// 展示动画播放、事件监听、自动播放等功能
    /// </summary>
    public class GPUFrameAnimatorDemo : MonoBehaviour
    {
        [Header("组件引用")]
        [SerializeField] private GPUFrameAnimator animator;

        [Header("UI 控件")]
        [SerializeField] private Dropdown animationDropdown;
        [SerializeField] private Button playButton;
        [SerializeField] private Toggle loopPlayToggle;
        [SerializeField] private Text statusText;
        [SerializeField] private Text eventLogText;

        [Header("设置")]
        [SerializeField] private float logDisplayDuration = 3f;
        [SerializeField] private int maxLogLines = 5;

        private string[] _animationNames;
        private float _logClearTimer;

        private void Awake()
        {
            Application.runInBackground = true;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
        }

        private void Start()
        {
            if (animator == null)
            {
                Debug.LogError("请设置 GPUFrameAnimator 引用！");
                return;
            }

            SetupAnimations();
            SetupUI();
            SubscribeEvents();

            UpdateStatus("就绪 - 请选择动画播放");
        }

        private void Update()
        {
            // 自动清理过期的日志显示
            if (_logClearTimer > 0)
            {
                _logClearTimer -= Time.deltaTime;
                if (_logClearTimer <= 0)
                {
                    eventLogText.text = "";
                }
            }
        }

        private void OnDestroy()
        {
            UnsubscribeEvents();
        }

        /// <summary>
        /// 设置动画列表
        /// </summary>
        private void SetupAnimations()
        {
            // 获取所有可用的动画名称
            var animCount = animator.transform.childCount;
            _animationNames = new string[animCount];

            for (int i = 0; i < animCount; i++)
            {
                _animationNames[i] = animator.transform.GetChild(i).name;
            }

            // 更新下拉菜单选项
            if (animationDropdown != null)
            {
                animationDropdown.ClearOptions();
                animationDropdown.AddOptions(new System.Collections.Generic.List<string>(_animationNames));
            }
        }

        /// <summary>
        /// 设置 UI 事件监听
        /// </summary>
        private void SetupUI()
        {
            if (playButton != null)
            {
                playButton.onClick.AddListener(OnPlayButtonClick);
            }

            if (animationDropdown != null)
            {
                animationDropdown.onValueChanged.AddListener(OnAnimationSelected);
            }
        }

        /// <summary>
        /// 订阅动画事件
        /// </summary>
        private void SubscribeEvents()
        {
            animator.EOnAnimStart += OnAnimationStart;
            animator.EOnAnimEnd += OnAnimationEnd;
        }

        /// <summary>
        /// 取消订阅动画事件
        /// </summary>
        private void UnsubscribeEvents()
        {
            if (animator != null)
            {
                animator.EOnAnimStart -= OnAnimationStart;
                animator.EOnAnimEnd -= OnAnimationEnd;
            }
        }

        /// <summary>
        /// 播放按钮点击事件
        /// </summary>
        private void OnPlayButtonClick()
        {
            if (animationDropdown == null || _animationNames == null)
                return;

            int selectedIndex = animationDropdown.value;
            if (selectedIndex >= 0 && selectedIndex < _animationNames.Length)
            {
                string animName = _animationNames[selectedIndex];
                PlayAnimation(animName);
            }
        }

        /// <summary>
        /// 动画选择改变事件
        /// </summary>
        private void OnAnimationSelected(int index)
        {
            if (index >= 0 && index < _animationNames.Length)
            {
                UpdateStatus($"已选择: {_animationNames[index]}");
            }
        }

        /// <summary>
        /// 播放指定动画
        /// </summary>
        public void PlayAnimation(string animName)
        {
            if (string.IsNullOrEmpty(animName))
            {
                LogEvent("错误: 动画名称为空");
                return;
            }

            animator.Play(animName);
            LogEvent($"播放: {animName}");
            UpdateStatus($"正在播放: {animName}");
        }

        /// <summary>
        /// 动画开始事件回调
        /// </summary>
        private void OnAnimationStart(string animName)
        {
            LogEvent($"[开始] {animName}");
            UpdateStatus($"播放中: {animName}");
        }

        /// <summary>
        /// 动画结束事件回调
        /// </summary>
        private void OnAnimationEnd(string animName)
        {
            LogEvent($"[结束] {animName}");

            // 如果启用了循环播放，自动重新播放
            if (loopPlayToggle != null && loopPlayToggle.isOn)
            {
                animator.Play(animName);
            }
            else
            {
                UpdateStatus($"已完成: {animName}");
            }
        }

        /// <summary>
        /// 记录事件日志
        /// </summary>
        private void LogEvent(string message)
        {
            if (eventLogText != null)
            {
                string timestamp = System.DateTime.Now.ToString("HH:mm:ss");
                string logLine = $"[{timestamp}] {message}\n";

                // 添加新日志
                eventLogText.text = logLine + eventLogText.text;

                // 重置清理计时器
                _logClearTimer = logDisplayDuration;

                // 限制日志行数
                var lines = eventLogText.text.Split('\n');
                if (lines.Length > maxLogLines)
                {
                    System.Text.StringBuilder sb = new System.Text.StringBuilder();
                    for (int i = 0; i < maxLogLines; i++)
                    {
                        sb.AppendLine(lines[i]);
                    }
                    eventLogText.text = sb.ToString().TrimEnd('\n');
                }
            }

            Debug.Log($"[GPUFrameAnimatorDemo] {message}");
        }

        /// <summary>
        /// 更新状态显示
        /// </summary>
        private void UpdateStatus(string status)
        {
            if (statusText != null)
            {
                statusText.text = $"状态: {status}";
            }
        }

        /// <summary>
        /// 通过索引播放动画（供按钮/键盘调用）
        /// </summary>
        public void PlayAnimationByIndex(int index)
        {
            if (_animationNames != null && index >= 0 && index < _animationNames.Length)
            {
                PlayAnimation(_animationNames[index]);
            }
        }

        /// <summary>
        /// 播放下一个动画
        /// </summary>
        public void PlayNextAnimation()
        {
            if (animationDropdown != null && _animationNames != null)
            {
                int nextIndex = (animationDropdown.value + 1) % _animationNames.Length;
                animationDropdown.value = nextIndex;
                PlayAnimation(_animationNames[nextIndex]);
            }
        }

        /// <summary>
        /// 播放上一个动画
        /// </summary>
        public void PlayPreviousAnimation()
        {
            if (animationDropdown != null && _animationNames != null)
            {
                int prevIndex = (animationDropdown.value - 1 + _animationNames.Length) % _animationNames.Length;
                animationDropdown.value = prevIndex;
                PlayAnimation(_animationNames[prevIndex]);
            }
        }
    }
}
