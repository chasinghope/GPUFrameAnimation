using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GPUAnimation.Demo
{
    public class GPUInstanceTest : MonoBehaviour
    {
        [SerializeField]
        private List<GameObject> prefabs;
        [SerializeField]
        private int count = 1000;


        private IEnumerator GenerateOverTime()
        {
            int index = 0;
            for (int i = 0; i < count; i++)
            {
                Vector3 pos = Vector3.zero;
                pos.x = Random.Range(-1.6f, 1.6f);
                pos.y = Random.Range(-4f, 4f);

                int prefabIndex = i % prefabs.Count;
                GameObject prefab = prefabs[prefabIndex];
                GameObject go = Instantiate(prefab, pos, Quaternion.identity);

                index++;
                if (index > 30)
                {
                    yield return new WaitForSeconds(1f / 60f);
                }

            }
        }


        public void Create(int num)
        {
            count = num;
            StartCoroutine(GenerateOverTime());
        }
    }
}