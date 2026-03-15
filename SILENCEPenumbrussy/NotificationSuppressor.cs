using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Interface.ImGuiNotification;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;

namespace SilencePenumbrussy;

internal sealed class NotificationSuppressor
{
    private readonly Configuration configuration;
    private readonly IPluginLog log;
    private readonly NotificationReflection reflection = new();
    private readonly PenumbraMessageReflection messageReflection = new();
    private bool notificationWarned;
    private bool messageWarned;
    private DateTime lastMessageCleanup;

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

        var now = DateTime.Now;
        var handleModComplete = configuration.SuppressModComplete;
        var handleErrorToasts = configuration.SuppressPenumbraErrorToasts;
        var handleMessageCleanup = configuration.RemovePenumbraErrorMessages;

        if (!handleModComplete && !handleErrorToasts && !handleMessageCleanup)
        {
            return;
        }

        if (handleModComplete || handleErrorToasts)
        {
            if (reflection.TryInitialize(log, ref notificationWarned))
            {
                foreach (var notification in reflection.EnumerateNotifications())
                {
                    if (notification.DismissReason != null)
                    {
                        continue;
                    }

                    if (handleModComplete && IsModImportNotification(notification))
                    {
                        if (configuration.SuppressModComplete)
                        {
                            notification.DismissNow();
                        }

                        continue;
                    }

                    if (handleErrorToasts && IsPenumbraErrorNotification(notification))
                    {
                        notification.DismissNow();
                    }
                }
            }
        }

        if (handleMessageCleanup && now - lastMessageCleanup >= TimeSpan.FromSeconds(1))
        {
            lastMessageCleanup = now;
            if (messageReflection.TryInitialize(log, ref messageWarned))
            {
                messageReflection.RemoveMatchingMessages(
                    IsPenumbraErrorMessage,
                    log,
                    ref messageWarned,
                    configuration.DebugPenumbraMessages);
            }
        }
    }

    private bool IsModImportNotification(IActiveNotification notification)
    {
        if (!IsPenumbraSource(notification))
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

    private bool IsPenumbraErrorNotification(IActiveNotification notification)
    {
        if (!IsPenumbraSource(notification))
        {
            return false;
        }

        var title = notification.Title ?? string.Empty;
        var content = notification.Content ?? string.Empty;

        return ContainsPenumbraErrorText(title) || ContainsPenumbraErrorText(content);
    }

    private bool IsPenumbraSource(IActiveNotification notification)
    {
        if (!configuration.MatchPenumbraSource)
        {
            return true;
        }

        var initiatorName = reflection.TryGetInitiatorName(notification);
        return initiatorName == null ||
            string.Equals(initiatorName, "Penumbra", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsPenumbraErrorMessage(object message)
    {
        foreach (var candidate in EnumerateMessageTextCandidates(message))
        {
            if (ContainsPenumbraErrorText(candidate))
            {
                return true;
            }
        }

        foreach (var candidate in EnumerateAdditionalMessageTextCandidates(message))
        {
            if (ContainsPenumbraErrorText(candidate))
            {
                return true;
            }
        }

        var fallback = message.ToString();
        return !string.IsNullOrWhiteSpace(fallback) && ContainsPenumbraErrorText(fallback);
    }

    private static IEnumerable<string> EnumerateMessageTextCandidates(object message)
    {
        var type = message.GetType();
        var titles = new[]
        {
            TryGetStringProperty(type, message, "NotificationTitle"),
            TryGetStringProperty(type, message, "NotificationMessage"),
            TryGetStringProperty(type, message, "LogMessage"),
            TryGetStringProperty(type, message, "StoredMessage"),
            TryGetStringProperty(type, message, "StoredTooltip"),
        };

        foreach (var value in titles)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                yield return value!;
            }
        }
    }

    private static IEnumerable<string> EnumerateAdditionalMessageTextCandidates(object message)
    {
        var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        var type = message.GetType();

        foreach (var property in type.GetProperties(flags))
        {
            if (property.GetIndexParameters().Length != 0)
            {
                continue;
            }

            string? text = null;
            try
            {
                text = TryGetTextFromValue(property.GetValue(message));
            }
            catch
            {
                continue;
            }

            if (!string.IsNullOrWhiteSpace(text))
            {
                yield return text!;
            }
        }

        foreach (var field in type.GetFields(flags))
        {
            string? text = null;
            try
            {
                text = TryGetTextFromValue(field.GetValue(message));
            }
            catch
            {
                continue;
            }

            if (!string.IsNullOrWhiteSpace(text))
            {
                yield return text!;
            }
        }
    }

    private static string? TryGetTextFromValue(object? value)
    {
        if (value == null)
        {
            return null;
        }

        if (value is string text)
        {
            return text;
        }

        if (value is SeString seString)
        {
            return seString.TextValue;
        }

        var typeName = value.GetType().FullName;
        if (!string.IsNullOrEmpty(typeName) &&
            (typeName.Contains("StringU8", StringComparison.Ordinal) ||
                typeName.Contains("InlineStringU8", StringComparison.Ordinal)))
        {
            return value.ToString();
        }

        return null;
    }

    private static string? TryGetStringProperty(Type type, object message, string propertyName)
    {
        var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        var property = type.GetProperty(propertyName, flags) ??
            type.GetProperties(flags).FirstOrDefault(candidate =>
                candidate.Name.EndsWith("." + propertyName, StringComparison.Ordinal));
        if (property == null)
        {
            return null;
        }

        var value = property.GetValue(message);
        return value switch
        {
            null => null,
            string text => text,
            _ => value.ToString(),
        };
    }

    private static bool ContainsPenumbraErrorText(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        return text.Contains("Forbidden File Redirection", StringComparison.OrdinalIgnoreCase)
            || text.Contains("Collection without ID", StringComparison.OrdinalIgnoreCase);
    }

    public void HandleErrorToast(ref SeString message, ref bool isHandled)
    {
        if (isHandled)
        {
            return;
        }

        if (!configuration.Enabled || !configuration.SuppressPenumbraErrorToasts)
        {
            return;
        }

        var text = message.TextValue;
        if (ContainsPenumbraErrorText(text))
        {
            isHandled = true;
        }
    }

    private sealed class PenumbraMessageReflection
    {
        private object? pluginManager;
        private PropertyInfo? installedPluginsProperty;
        private PropertyInfo? localPluginNameProperty;
        private PropertyInfo? localPluginInternalNameProperty;
        private FieldInfo? localPluginInstanceField;
        private FieldInfo? messagesField;
        private MethodInfo? removeMethod;
        private PropertyInfo? entryKeyProperty;
        private PropertyInfo? entryValueProperty;
        private FieldInfo? taggedMessagesField;
        private MethodInfo? taggedRemoveMethod;
        private PropertyInfo? taggedEntryKeyProperty;
        private PropertyInfo? taggedEntryValueProperty;
        private PropertyInfo? taggedValueItem2Property;
        private bool initialized;
        private bool failed;
        private bool debugStoreLogged;
        private bool debugPluginsLogged;
        private bool debugPenumbraMatchedLogged;
        private bool debugPenumbraMissingLogged;
        private bool debugInstanceMissingLogged;
        private bool debugMessagerMissingLogged;
        private bool debugMessageFieldMissingLogged;
        private DateTime lastNotAvailableLog;
        private readonly HashSet<string> debugLoggedEntries = new(StringComparer.Ordinal);

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
                var managerType = dalamudAssembly.GetType("Dalamud.Plugin.Internal.PluginManager", throwOnError: false);
                var serviceType = dalamudAssembly.GetType("Dalamud.Service`1", throwOnError: false);
                if (managerType == null || serviceType == null)
                {
                    return Fail(log, ref warned, "Plugin manager types were not found.");
                }

                var serviceGeneric = serviceType.MakeGenericType(managerType);
                var getMethod = serviceGeneric.GetMethod("Get", BindingFlags.Public | BindingFlags.Static);
                if (getMethod == null)
                {
                    return Fail(log, ref warned, "Plugin manager service accessor was not found.");
                }

                pluginManager = getMethod.Invoke(null, null);
                if (pluginManager == null)
                {
                    return Fail(log, ref warned, "Plugin manager service was null.");
                }

                installedPluginsProperty = managerType.GetProperty("InstalledPlugins", BindingFlags.Instance | BindingFlags.Public);
                var localPluginType = dalamudAssembly.GetType("Dalamud.Plugin.Internal.Types.LocalPlugin", throwOnError: false);
                localPluginNameProperty = localPluginType?.GetProperty("Name", BindingFlags.Instance | BindingFlags.Public);
                localPluginInternalNameProperty = localPluginType?.GetProperty("InternalName", BindingFlags.Instance | BindingFlags.Public);
                localPluginInstanceField = localPluginType?.GetField("instance", BindingFlags.Instance | BindingFlags.NonPublic);
                if (localPluginInstanceField == null && localPluginType != null)
                {
                    localPluginInstanceField = localPluginType
                        .GetFields(BindingFlags.Instance | BindingFlags.NonPublic)
                        .FirstOrDefault(field =>
                            string.Equals(field.FieldType.FullName, typeof(IDalamudPlugin).FullName, StringComparison.Ordinal));
                }

                initialized = installedPluginsProperty != null &&
                    localPluginInstanceField != null &&
                    (localPluginNameProperty != null || localPluginInternalNameProperty != null);
                if (!initialized)
                {
                    return Fail(log, ref warned, "Plugin manager fields were not found.");
                }

                return true;
            }
            catch (Exception ex)
            {
                if (!warned)
                {
                    warned = true;
                    log.Warning(ex, "SILENCEPenumbrussy: failed to initialize Penumbra message hooks.");
                }

                failed = true;
                return false;
            }
        }

        public void RemoveMatchingMessages(
            Func<object, bool> shouldRemove,
            IPluginLog log,
            ref bool warned,
            bool debugEnabled)
        {
            try
            {
                if (!TryGetMessageStores(out var messages, out var taggedMessages, debugEnabled, log))
                {
                    DebugLogNotAvailable(log, debugEnabled);
                    return;
                }

                if (messages is IEnumerable entries)
                {
                    if (debugEnabled)
                    {
                        DebugLogStore(log, "messages", messages);
                    }

                    var keysToRemove = new List<object>();
                    foreach (var entry in entries)
                    {
                        if (!TryGetEntryParts(entry, messages!, out var key, out var message))
                        {
                            continue;
                        }

                        if (message == null || key == null)
                        {
                            continue;
                        }

                        if (debugEnabled)
                        {
                            DebugLogEntry(log, "messages", key, message);
                        }

                        if (!shouldRemove(message))
                        {
                            continue;
                        }

                        TryInvokeOnRemoval(message);
                        keysToRemove.Add(key);
                    }

                    if (keysToRemove.Count != 0 && removeMethod != null)
                    {
                        foreach (var key in keysToRemove)
                        {
                            removeMethod.Invoke(messages, new[] { key });
                            if (debugEnabled)
                            {
                                DebugLog(log, $"Removed message entry {key}.");
                            }
                        }
                    }
                }

                if (taggedMessages is IEnumerable taggedEntries)
                {
                    if (debugEnabled)
                    {
                        DebugLogStore(log, "taggedMessages", taggedMessages);
                    }

                    var tagKeysToRemove = new List<object>();
                    foreach (var entry in taggedEntries)
                    {
                        if (!TryGetTaggedEntryParts(entry, taggedMessages!, out var key, out var message))
                        {
                            continue;
                        }

                        if (message == null || key == null)
                        {
                            continue;
                        }

                        if (debugEnabled)
                        {
                            DebugLogEntry(log, "taggedMessages", key, message);
                        }

                        if (!shouldRemove(message))
                        {
                            continue;
                        }

                        TryInvokeOnRemoval(message);
                        tagKeysToRemove.Add(key);
                    }

                    if (tagKeysToRemove.Count != 0 && taggedRemoveMethod != null)
                    {
                        var removeParameters = taggedRemoveMethod.GetParameters();
                        foreach (var key in tagKeysToRemove)
                        {
                            if (removeParameters.Length == 1)
                            {
                                taggedRemoveMethod.Invoke(taggedMessages, new[] { key });
                            }
                            else if (removeParameters.Length == 2)
                            {
                                var valueType = removeParameters[1].ParameterType.GetElementType();
                                var outValue = valueType == null
                                    ? null
                                    : valueType.IsValueType
                                        ? Activator.CreateInstance(valueType)
                                        : null;
                                var args = new[] { key, outValue };
                                taggedRemoveMethod.Invoke(taggedMessages, args);
                            }

                            if (debugEnabled)
                            {
                                DebugLog(log, $"Removed tagged message entry {key}.");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                if (!warned)
                {
                    warned = true;
                    log.Warning(ex, "SILENCEPenumbrussy: failed to prune Penumbra messages.");
                }
            }
        }

        private bool TryGetMessageStores(out object? messages, out object? taggedMessages, bool debugEnabled, IPluginLog log)
        {
            messages = null;
            taggedMessages = null;

            if (pluginManager == null || installedPluginsProperty == null)
            {
                if (debugEnabled)
                {
                    DebugLog(log, "Plugin manager service not available.");
                }
                return false;
            }

            if (installedPluginsProperty.GetValue(pluginManager) is not IEnumerable plugins)
            {
                if (debugEnabled)
                {
                    DebugLog(log, "Installed plugins list not available.");
                }
                return false;
            }

            var pluginList = plugins.Cast<object>().ToList();
            if (debugEnabled && !debugPluginsLogged)
            {
                DebugLogPlugins(log, pluginList);
                debugPluginsLogged = true;
            }

            foreach (var plugin in pluginList)
            {
                if (!IsPenumbraPlugin(plugin))
                {
                    continue;
                }

                if (debugEnabled && !debugPenumbraMatchedLogged)
                {
                    DebugLog(log, $"Matched Penumbra plugin entry (Name: {GetPluginName(plugin)}, InternalName: {GetPluginInternalName(plugin)}).");
                    debugPenumbraMatchedLogged = true;
                }

                if (localPluginInstanceField == null)
                {
                    if (debugEnabled && !debugInstanceMissingLogged)
                    {
                        DebugLog(log, "Penumbra plugin instance field not found.");
                        debugInstanceMissingLogged = true;
                    }
                    return false;
                }

                var instance = localPluginInstanceField.GetValue(plugin);
                if (instance == null)
                {
                    if (debugEnabled && !debugInstanceMissingLogged)
                    {
                        DebugLog(log, "Penumbra plugin instance not available yet.");
                        debugInstanceMissingLogged = true;
                    }
                    return false;
                }

                var penumbraType = instance.GetType();
                var messagerProperty = penumbraType.GetProperty(
                    "Messager",
                    BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                object? messageService = null;
                if (messagerProperty != null)
                {
                    var getter = messagerProperty.GetGetMethod(true);
                    var target = getter != null && getter.IsStatic ? null : instance;
                    messageService = messagerProperty.GetValue(target);
                }
                else
                {
                    var messagerField = penumbraType.GetField(
                        "Messager",
                        BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                    if (messagerField != null)
                    {
                        messageService = messagerField.GetValue(messagerField.IsStatic ? null : instance);
                    }
                }

                if (messageService == null)
                {
                    if (debugEnabled && !debugMessagerMissingLogged)
                    {
                        DebugLog(log, "Penumbra Messager property/field not found or not initialized yet.");
                        debugMessagerMissingLogged = true;
                    }
                    return false;
                }

                messagesField ??= FindFieldInHierarchy(messageService.GetType(), "_messages");
                taggedMessagesField ??= FindFieldInHierarchy(messageService.GetType(), "_taggedMessages");

                if (messagesField == null && taggedMessagesField == null)
                {
                    if (debugEnabled && !debugMessageFieldMissingLogged)
                    {
                        DebugLog(log, "Penumbra message fields were not found on Messager.");
                        debugMessageFieldMissingLogged = true;
                    }
                    return false;
                }

                if (messagesField != null)
                {
                    messages = messagesField.GetValue(messageService);
                }

                if (taggedMessagesField != null)
                {
                    taggedMessages = taggedMessagesField.GetValue(messageService);
                }

                return messages != null || taggedMessages != null;
            }

            if (debugEnabled && !debugPenumbraMissingLogged)
            {
                DebugLog(log, "Penumbra plugin not found in installed plugin list.");
                debugPenumbraMissingLogged = true;
            }

            return false;
        }

        private bool IsPenumbraPlugin(object plugin)
        {
            var name = localPluginNameProperty?.GetValue(plugin) as string;
            var internalName = localPluginInternalNameProperty?.GetValue(plugin) as string;

            return string.Equals(name, "Penumbra", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(internalName, "Penumbra", StringComparison.OrdinalIgnoreCase);
        }

        private bool TryGetEntryParts(object entry, object messages, out object? key, out object? message)
        {
            key = null;
            message = null;

            var entryType = entry.GetType();
            if (entryKeyProperty == null || entryKeyProperty.DeclaringType != entryType)
            {
                entryKeyProperty = entryType.GetProperty("Key", BindingFlags.Instance | BindingFlags.Public);
                entryValueProperty = entryType.GetProperty("Value", BindingFlags.Instance | BindingFlags.Public);
            }

            if (entryKeyProperty == null || entryValueProperty == null)
            {
                return false;
            }

            key = entryKeyProperty.GetValue(entry);
            message = entryValueProperty.GetValue(entry);
            if (entryKeyProperty.PropertyType != null)
            {
                EnsureRemoveMethod(messages, entryKeyProperty.PropertyType, entryValueProperty?.PropertyType, ref removeMethod);
            }

            return key != null && message != null;
        }

        private bool TryGetTaggedEntryParts(object entry, object taggedMessages, out object? key, out object? message)
        {
            key = null;
            message = null;

            var entryType = entry.GetType();
            if (taggedEntryKeyProperty == null || taggedEntryKeyProperty.DeclaringType != entryType)
            {
                taggedEntryKeyProperty = entryType.GetProperty("Key", BindingFlags.Instance | BindingFlags.Public);
                taggedEntryValueProperty = entryType.GetProperty("Value", BindingFlags.Instance | BindingFlags.Public);
            }

            if (taggedEntryKeyProperty == null || taggedEntryValueProperty == null)
            {
                return false;
            }

            key = taggedEntryKeyProperty.GetValue(entry);
            var value = taggedEntryValueProperty.GetValue(entry);
            if (value == null)
            {
                return false;
            }

            if (taggedValueItem2Property == null || taggedValueItem2Property.DeclaringType != value.GetType())
            {
                taggedValueItem2Property = value.GetType().GetProperty("Item2", BindingFlags.Instance | BindingFlags.Public);
            }

            message = taggedValueItem2Property?.GetValue(value);
            EnsureRemoveMethod(taggedMessages, taggedEntryKeyProperty.PropertyType, value.GetType(), ref taggedRemoveMethod);
            return key != null && message != null;
        }

        private void EnsureRemoveMethod(object messages, Type? keyType, Type? valueType, ref MethodInfo? removeMethodField)
        {
            if (removeMethodField != null)
            {
                return;
            }

            if (keyType != null)
            {
                removeMethodField = messages.GetType().GetMethod(
                    "Remove",
                    BindingFlags.Instance | BindingFlags.Public,
                    null,
                    new[] { keyType },
                    null);

                if (removeMethodField == null && valueType != null)
                {
                    removeMethodField = messages.GetType().GetMethod(
                        "TryRemove",
                        BindingFlags.Instance | BindingFlags.Public,
                        null,
                        new[] { keyType, valueType.MakeByRefType() },
                        null);
                }
            }

            if (removeMethodField == null)
            {
                var methods = messages.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public);
                removeMethodField = methods.FirstOrDefault(method =>
                    string.Equals(method.Name, "Remove", StringComparison.Ordinal) &&
                    method.GetParameters().Length == 1);

                if (removeMethodField == null)
                {
                    removeMethodField = methods.FirstOrDefault(method =>
                        string.Equals(method.Name, "TryRemove", StringComparison.Ordinal) &&
                        method.GetParameters().Length == 2);
                }
            }
        }

        private void TryInvokeOnRemoval(object message)
        {
            var method = message.GetType().GetMethod("OnRemoval", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            method?.Invoke(message, null);
        }

        private static FieldInfo? FindFieldInHierarchy(Type type, string fieldName)
        {
            var current = type;
            while (current != null)
            {
                var field = current.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
                if (field != null)
                {
                    return field;
                }

                current = current.BaseType;
            }

            return null;
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

        private void DebugLogStore(IPluginLog log, string name, object store)
        {
            if (debugStoreLogged)
            {
                return;
            }

            debugStoreLogged = true;
            var count = TryGetCount(store);
            if (count.HasValue)
            {
                DebugLog(log, $"Found Penumbra {name} store with {count.Value} entries.");
            }
            else
            {
                DebugLog(log, $"Found Penumbra {name} store.");
            }
        }

        private void DebugLogEntry(IPluginLog log, string storeName, object key, object message)
        {
            var entryId = $"{storeName}:{key}:{message.GetType().FullName}";
            if (!debugLoggedEntries.Add(entryId))
            {
                return;
            }

            var candidates = CollectMessageCandidates(message);
            var preview = candidates.Count == 0
                ? "<no string candidates>"
                : string.Join(" | ", candidates.Select(TrimCandidate));

            DebugLog(log, $"{storeName} entry {key} ({message.GetType().FullName}): {preview}");
        }

        private static List<string> CollectMessageCandidates(object message)
        {
            var results = new List<string>();

            foreach (var text in EnumerateMessageTextCandidates(message))
            {
                AddCandidate(results, text);
            }

            foreach (var text in EnumerateAdditionalMessageTextCandidates(message))
            {
                AddCandidate(results, text);
            }

            var fallback = message.ToString();
            AddCandidate(results, fallback);

            return results;
        }

        private static void AddCandidate(ICollection<string> results, string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return;
            }

            if (!results.Contains(text))
            {
                results.Add(text);
            }
        }

        private static string TrimCandidate(string text)
        {
            const int maxLength = 120;
            return text.Length <= maxLength ? text : text.Substring(0, maxLength) + "...";
        }

        private static int? TryGetCount(object store)
        {
            var countProperty = store.GetType().GetProperty("Count", BindingFlags.Instance | BindingFlags.Public);
            if (countProperty == null)
            {
                return null;
            }

            var value = countProperty.GetValue(store);
            return value switch
            {
                int count => count,
                long count => count > int.MaxValue ? int.MaxValue : (int)count,
                _ => null,
            };
        }

        private void DebugLog(IPluginLog log, string message)
        {
            log.Information("SILENCEPenumbrussy debug: {Message}", message);
        }

        private void DebugLogNotAvailable(IPluginLog log, bool debugEnabled)
        {
            if (!debugEnabled)
            {
                return;
            }

            var now = DateTime.UtcNow;
            if (now - lastNotAvailableLog < TimeSpan.FromSeconds(10))
            {
                return;
            }

            lastNotAvailableLog = now;
            DebugLog(log, "Message stores not available yet.");
        }

        private void DebugLogPlugins(IPluginLog log, IReadOnlyList<object> plugins)
        {
            var entries = new List<string>();
            var count = 0;

            foreach (var plugin in plugins)
            {
                count++;
                if (entries.Count >= 25)
                {
                    continue;
                }

                entries.Add($"{GetPluginName(plugin)} ({GetPluginInternalName(plugin)})");
            }

            var suffix = count > entries.Count ? ", ..." : string.Empty;
            DebugLog(log, $"Installed plugins ({count}): {string.Join(", ", entries)}{suffix}");
        }

        private string GetPluginName(object plugin)
        {
            return localPluginNameProperty?.GetValue(plugin) as string ?? "<unknown>";
        }

        private string GetPluginInternalName(object plugin)
        {
            return localPluginInternalNameProperty?.GetValue(plugin) as string ?? "<unknown>";
        }
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
