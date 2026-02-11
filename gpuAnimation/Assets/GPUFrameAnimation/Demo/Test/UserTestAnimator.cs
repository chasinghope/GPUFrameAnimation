using GPUAnimation;
using UnityEngine;
using UnityEngine.UI;

namespace GPUAnimation.Demo
{
    public class UserTestAnimator : MonoBehaviour
    {
        [SerializeField] private InputField inputField;
        [SerializeField] private Button changeButton;
        [SerializeField] private GPUFrameAnimator animator;


        private void Awake()
        {
            Application.runInBackground = true;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
        }

        private void OnEnable()
        {
            changeButton.onClick.AddListener(() =>
            {
                string animName = inputField.text;
                animator.Play(animName);
            });
        }
    }
}