
using System;
using System.Collections;
using UnityEngine;

namespace Helpers
{
    public class DelayedFunctionHelper : MonoBehaviour
    {
        static DelayedFunctionHelper Instance;
        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }
        public static void InvokeDelayed(Action action, float delay)
        {
            Instance.StartCoroutine(Instance.InvokeAfterDelay(action, delay));
        }

        IEnumerator InvokeAfterDelay(Action action, float delay)
        {
            yield return new WaitForSeconds(delay);
            action?.Invoke();
        }
    }
}