using Avalonia.Threading;
using System;
using System.Threading.Tasks;

namespace Soundboard.Core.Utilities;

public static class UiThreadHelper
{
    public static void Run(Action action)
    {
        if (Dispatcher.UIThread.CheckAccess())
        {
            action();
        }
        else
        {
            Dispatcher.UIThread.Post(action);
        }
    }

    public static Task RunAsync(Func<Task> asyncAction)
    {
        if (Dispatcher.UIThread.CheckAccess())
        {
            return asyncAction();
        }
        else
        {
            return Dispatcher.UIThread.InvokeAsync(async () => await asyncAction());
        }
    }
}
