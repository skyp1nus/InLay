namespace InLay.Core;

/// <summary>
/// Stable product identity: the display name plus the fixed identifiers used for the
/// single-instance mutex, the cross-instance activation event, and the autostart registry value.
/// These identifiers must never change once shipped — they are the contract between running instances.
/// </summary>
public static class ProductInfo
{
    /// <summary>The product display name.</summary>
    public const string Name = "InLay";

    /// <summary>Registry value name written under the HKCU Run key for autostart.</summary>
    public const string AutostartValueName = Name;

    /// <summary>Stable identifier for the single-instance mutex.</summary>
    public static readonly Guid MutexId = new("7C9E6B2A-4F1D-4A3B-9E8C-2D5A1B6F3C4E");

    /// <summary>Stable identifier for the cross-instance "activate the primary" event.</summary>
    public static readonly Guid ActivationEventId = new("1A2B3C4D-5E6F-4071-8293-A4B5C6D7E8F9");

    /// <summary>Session-local name of the single-instance mutex (one running instance per user session).</summary>
    public static string MutexName => $@"Local\{Name}-{MutexId:N}";

    /// <summary>Session-local name of the event a second launch signals to surface the primary's Settings window.</summary>
    public static string ActivationEventName => $@"Local\{Name}-activate-{ActivationEventId:N}";
}
