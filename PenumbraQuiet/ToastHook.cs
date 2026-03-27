using System;
using System.Reflection;
using Dalamud.Plugin.Services;

namespace PenumbraQuiet;

internal sealed class ToastHook : IDisposable
{
    private readonly NotificationSuppressor suppressor;
    private readonly IPluginLog log;
    private object? toastService;
    private EventInfo? errorToastEvent;
    private Delegate? errorToastHandler;
    private bool warned;

    public ToastHook(NotificationSuppressor suppressor, IPluginLog log)
    {
        this.suppressor = suppressor;
        this.log = log;
    }

    public void Initialize(object toastGui)
    {
        if (toastService != null)
        {
            return;
        }

        toastService = toastGui;
        errorToastEvent = FindErrorToastEvent(toastService.GetType());
        if (errorToastEvent == null)
        {
            WarnOnce("Toast error event was not found.");
            return;
        }

        var handlerType = errorToastEvent.EventHandlerType;
        if (handlerType == null)
        {
            WarnOnce("Toast error event handler type was not found.");
            return;
        }

        var handlerMethod = typeof(NotificationSuppressor).GetMethod(
            nameof(NotificationSuppressor.HandleErrorToast),
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (handlerMethod == null)
        {
            WarnOnce("Toast error handler method was not found.");
            return;
        }

        errorToastHandler = Delegate.CreateDelegate(handlerType, suppressor, handlerMethod);
        errorToastEvent.AddEventHandler(toastService, errorToastHandler);
    }

    public void Dispose()
    {
        if (toastService != null && errorToastEvent != null && errorToastHandler != null)
        {
            errorToastEvent.RemoveEventHandler(toastService, errorToastHandler);
        }
    }

    private static EventInfo? FindErrorToastEvent(Type type)
    {
        return type.GetEvent("OnErrorToast", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            ?? type.GetEvent("ErrorToast", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
    }

    private void WarnOnce(string message)
    {
        if (warned)
        {
            return;
        }

        warned = true;
        log.Warning("PenumbraQuiet: {Message}", message);
    }
}
