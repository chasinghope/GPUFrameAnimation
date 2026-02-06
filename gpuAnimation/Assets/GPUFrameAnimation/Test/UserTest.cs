using UnityEngine;
using UnityEngine.UI;

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
            int count = int.Parse(inputField.text);
            gpuTest.Create(count);
        });
        
        cpuBtn.onClick.AddListener(() =>
        {
            int count = int.Parse(inputField.text);
            cpuTest.Create(count);
        });
    }
}
