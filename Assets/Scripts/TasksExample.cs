#if UNITY_EDITOR
using Cysharp.Threading.Tasks;
using System;
using System.Linq;
using UnityEngine;

public class TasksExample : MonoBehaviour
{
    public event Func<UniTask> AsyncEvent;

    async UniTask RaiseEventAndWaitAll()
    {
        if (AsyncEvent != null)
        {
            var invocationList = AsyncEvent.GetInvocationList();
            var tasks = invocationList.Select(del => ((Func<UniTask>)del)()).ToArray();
            await UniTask.WhenAll(tasks);
            Debug.Log("All completed.");
        }
    }

    async UniTask Listener1()
    {
        await UniTask.Delay(1000); // Имитация асинхронной задачи
        Debug.Log("Listener 1 completed.");
    }

    async UniTask Listener2()
    {
        await UniTask.Delay(2000); // Имитация другой асинхронной задачи
        Debug.Log("Listener 2 completed.");
    }

    async UniTask Listener3()
    {
        await UniTask.Delay(4000); // Имитация другой асинхронной задачи
        Debug.Log("Listener 3 completed.");
    }

    void Start()
    {
        AsyncEvent += Listener1;
        AsyncEvent += Listener2;
        AsyncEvent += Listener3;

        // Вызов события и ожидание завершения всех подписанных задач
        RaiseEventAndWaitAll().Forget();
    }

}
#endif