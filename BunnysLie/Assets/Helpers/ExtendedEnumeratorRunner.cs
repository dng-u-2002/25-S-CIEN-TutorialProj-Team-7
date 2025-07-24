using System.Collections;
using UnityEngine;
using System.Collections.Generic;

namespace Helpers
{
    public class ExtendedEnumeratorRunner : MonoBehaviour
    {
        Dictionary<string, Coroutine> Coroutines = new Dictionary<string, Coroutine>();
#if UNITY_EDITOR
        [SerializeField] List<string> IDs = new List<string>();
#endif
        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }

        public void Run(ExtendedEnumerator coroutineInformation)
        {
            Coroutine coroutine = StartCoroutine(_Run(coroutineInformation.Enumerator, coroutineInformation.ID));
            Coroutines.Add(coroutineInformation.ID, coroutine);
#if UNITY_EDITOR
            IDs.Add(coroutineInformation.ID);
#endif
        }

        IEnumerator _Run(IEnumerator enumerator, string id)
        {
            while (enumerator.MoveNext())
                yield return enumerator.Current;

            Stop(id);
        }

        public bool Stop(string id)
        {
#if UNITY_EDITOR
            IDs.Remove(id);
#endif
            if (id != null)
            {
                if (Coroutines.TryGetValue(id, out Coroutine coroutine))
                {
                    StopCoroutine(coroutine);
                    Coroutines.Remove(id);
                    return true;
                }
            }
            return false;
        }
    }
}