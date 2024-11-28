using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

public static class EventExtensions
{
    public static async UniTask InvokeAndWaitAsync(this Delegate eventDelegate, params object[] args)
    {
        if (eventDelegate == null) 
            return;

        var invocationList = eventDelegate.GetInvocationList();
        var tasks = new List<UniTask>();

        foreach (var handler in invocationList)
        {
            if (handler is Delegate uniTaskHandler)
            {
                var task = (UniTask)uniTaskHandler.DynamicInvoke(args);
                tasks.Add(task);
            }
        }

        await UniTask.WhenAll(tasks);
    }
}
