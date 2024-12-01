using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

public static class EventExtensions
{
    public static async UniTask InvokeAndWaitAsync(this Delegate eventDelegate, params object[] args)
    {
        if (eventDelegate == null) 
            return;

        Delegate[] invocationList = eventDelegate.GetInvocationList();
        List<UniTask> tasks = new();

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

    public static async UniTask InvokeAndWaitAsync(this Delegate eventDelegate, CancellationToken cancellationToken, params object[] args)
    {
        if (eventDelegate == null)
            return;

        Delegate[] invocationList = eventDelegate.GetInvocationList();
        List<UniTask> tasks = new();

        foreach (var handler in invocationList)
        {
            var parameters = handler.Method.GetParameters();

            if (parameters.Length > 0 && parameters[^1].ParameterType == typeof(CancellationToken))
            {
                object[] updatedArgs = new object[args.Length + 1];
                Array.Copy(args, updatedArgs, args.Length);
                updatedArgs[^1] = cancellationToken;

                var task = (UniTask)handler.DynamicInvoke(updatedArgs);
                tasks.Add(task);
            }
            else
            {
                var task = (UniTask)handler.DynamicInvoke(args);
                tasks.Add(task);
            }
        }

        await UniTask.WhenAll(tasks);
    }
}
