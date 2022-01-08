using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace Mesh_Generator.Scripts
{
    public class ThreadedDataRequester : MonoBehaviour
    {
        private static ThreadedDataRequester Instance;
        private Queue<ThreadInfo> DataQueue = new Queue<ThreadInfo>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(Instance);
            }
        
            Instance = FindObjectOfType<ThreadedDataRequester>();
        }

        public static void RequestData(Func<object> generateData, Action<object> callback)
        {
            ThreadStart threadStart = delegate { Instance.DataThread(generateData, callback); };
        
            new Thread(threadStart).Start();
        }

        void DataThread(Func<object> generateData, Action<object> callback)
        {
            object data = generateData();
            lock (DataQueue) DataQueue.Enqueue(new ThreadInfo(callback, data));
        }

        private void Update()
        {
            if (DataQueue.Count > 0)
            {
                for (int i = 0; i < DataQueue.Count; i++)
                {
                    ThreadInfo threadInfo = DataQueue.Dequeue();
                    threadInfo.callback(threadInfo.parameter);
                }
            }
        }
    
        struct ThreadInfo
        {
            public readonly Action<object> callback;
            public readonly object parameter;

            public ThreadInfo(Action<object> callback, object parameter)
            {
                this.callback = callback;
                this.parameter = parameter;
            }
        }
    }
}
