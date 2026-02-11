using UnityEngine;
using UnityEngine.UI;

namespace GPUAnimation.Demo
{
    public class UserTest : MonoBehaviour
    {
        [SerializeField] private InputField inputField;
        [SerializeField] private Button gpuBtn;
        [SerializeField] private Button cpuBtn;
        [SerializeField] private GPUInstanceTest gpuTest;
        [SerializeField] private GPUInstanceTest cpuTest;


        private void Awake()
        {
            Application.runInBackground = true;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
        }

        private void OnEnable()
        {
            gpuBtn.onClick.AddListener(() =>
            {
                if (int.TryParse(inputField.text, out int count) && count > 0)
                {
                    gpuTest.Create(count);
                }
                else
                {
                    Debug.LogWarning("请输入有效的正整数");
                }
            });

            cpuBtn.onClick.AddListener(() =>
            {
                if (int.TryParse(inputField.text, out int count) && count > 0)
                {
                    cpuTest.Create(count);
                }
                else
                {
                    Debug.LogWarning("请输入有效的正整数");
                }
            });
        }
    }
}
