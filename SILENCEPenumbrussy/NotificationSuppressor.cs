using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Dalamud.Interface.ImGuiNotification;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;

namespace SilencePenumbrussy;

internal sealed class NotificationSuppressor
{
    private readonly Configuration configuration;
    private readonly IPluginLog log;
    private readonly NotificationReflection reflection = new();
    private bool warned;

    public NotificationSuppressor(Configuration configuration, IPluginLog log)
    {
        this.configuration = configuration;
        this.log = log;
    }

    public void Update(IFramework _)
    {
        if (!configuration.Enabled)
        {
            return;
        }

        if (!configuration.SuppressModComplete &&
            (!configuration.AutoDismissModComplete || configuration.AutoDismissSeconds <= 0))
        {
            return;
        }

        if (!reflection.TryInitialize(log, ref warned))
        {
            return;
        }

        var now = DateTime.Now;
        foreach (var notification in reflection.EnumerateNotifications())
        {
            if (notification.DismissReason != null)
            {
                continue;
            }

            if (!IsTarget(notification))
            {
                continue;
            }

            if (configuration.SuppressModComplete)
            {
                notification.DismissNow();
                continue;
            }

            if (configuration.AutoDismissModComplete &&
                configuration.AutoDismissSeconds > 0 &&
                now - notification.CreatedAt >= TimeSpan.FromSeconds(configuration.AutoDismissSeconds))
            {
                notification.DismissNow();
            }
        }
    }

    private bool IsTarget(IActiveNotification notification)
    {
        var initiatorName = reflection.TryGetInitiatorName(notification);
        if (configuration.MatchPenumbraSource &&
            initiatorName != null &&
            !string.Equals(initiatorName, "Penumbra", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var title = notification.Title ?? string.Empty;
        var content = notification.Content ?? string.Empty;

        var titleMatch = title.StartsWith("Successfully imported", StringComparison.OrdinalIgnoreCase);
        var contentMatch = content.StartsWith("Successfully extracted", StringComparison.OrdinalIgnoreCase);

        if (configuration.StrictTextMatch)
        {
            return titleMatch && contentMatch;
        }

        return titleMatch || contentMatch;
    }

    private sealed class NotificationReflection
    {
        private object? notificationManager;
        private FieldInfo? notificationsField;
        private FieldInfo? pendingNotificationsField;
        private FieldInfo? initiatorPluginField;
        private PropertyInfo? initiatorNameProperty;
        private bool initialized;
        private bool failed;

        public bool TryInitialize(IPluginLog log, ref bool warned)
        {
            if (initialized)
            {
                return true;
            }

            if (failed)
            {
                return false;
            }

            try
            {
                var dalamudAssembly = typeof(IDalamudPlugin).Assembly;
                var managerType = dalamudAssembly.GetType(
                    "Dalamud.Interface.ImGuiNotification.Internal.NotificationManager",
                    throwOnError: false);
                var serviceType = dalamudAssembly.GetType("Dalamud.Service`1", throwOnError: false);
                if (managerType == null || serviceType == null)
                {
                    return Fail(log, ref warned, "Notification manager types were not found.");
                }

                var serviceGeneric = serviceType.MakeGenericType(managerType);
                var getMethod = serviceGeneric.GetMethod("Get", BindingFlags.Public | BindingFlags.Static);
                if (getMethod == null)
                {
                    return Fail(log, ref warned, "Notification manager service accessor was not found.");
                }

                notificationManager = getMethod.Invoke(null, null);
                if (notificationManager == null)
                {
                    return Fail(log, ref warned, "Notification manager service was null.");
                }

                notificationsField = managerType.GetField("notifications", BindingFlags.Instance | BindingFlags.NonPublic);
                pendingNotificationsField = managerType.GetField("pendingNotifications", BindingFlags.Instance | BindingFlags.NonPublic);

                var activeType = dalamudAssembly.GetType(
                    "Dalamud.Interface.ImGuiNotification.Internal.ActiveNotification",
                    throwOnError: false);
                if (activeType != null)
                {
                    initiatorPluginField = activeType.GetField("initiatorPlugin", BindingFlags.Instance | BindingFlags.NonPublic);
                    if (initiatorPluginField != null)
                    {
                        initiatorNameProperty = initiatorPluginField.FieldType.GetProperty(
                            "Name",
                            BindingFlags.Instance | BindingFlags.Public);
                    }
                }

                initialized = notificationsField != null && pendingNotificationsField != null;
                if (!initialized)
                {
                    return Fail(log, ref warned, "Notification manager fields were not found.");
                }

                return true;
            }
            catch (Exception ex)
            {
                if (!warned)
                {
                    warned = true;
                    log.Warning(ex, "SILENCEPenumbrussy: failed to initialize notification hooks.");
                }

                failed = true;
                return false;
            }
        }

        public IEnumerable<IActiveNotification> EnumerateNotifications()
        {
            foreach (var notification in EnumerateFromField(notificationsField))
            {
                yield return notification;
            }

            foreach (var notification in EnumerateFromField(pendingNotificationsField))
            {
                yield return notification;
            }
        }

        public string? TryGetInitiatorName(IActiveNotification notification)
        {
            if (initiatorPluginField == null || initiatorNameProperty == null)
            {
                return null;
            }

            var plugin = initiatorPluginField.GetValue(notification);
            if (plugin == null)
            {
                return null;
            }

            return initiatorNameProperty.GetValue(plugin) as string;
        }

        private IEnumerable<IActiveNotification> EnumerateFromField(FieldInfo? field)
        {
            if (notificationManager == null || field == null)
            {
                yield break;
            }

            if (field.GetValue(notificationManager) is not IEnumerable notifications)
            {
                yield break;
            }

            foreach (var notification in notifications)
            {
                if (notification is IActiveNotification active)
                {
                    yield return active;
                }
            }
        }

        private bool Fail(IPluginLog log, ref bool warned, string message)
        {
            if (!warned)
            {
                warned = true;
                log.Warning("SILENCEPenumbrussy: {Message}", message);
            }

            failed = true;
            return false;
        }
    }
}
