namespace InLay.Core;

/// <summary>
/// A resolved keyboard layout: its language id, BCP-47 locale name, and the short user-facing label
/// the indicators display (e.g. <c>UK</c>, <c>EN</c>). Immutable value; per-language colors and custom
/// labels arrive in M4 (docs §4.4). Mirrors the <see cref="AppPathSet"/> record style.
/// </summary>
/// <param name="LangId">Language identifier (LANGID) — the low word of the HKL.</param>
/// <param name="LocaleName">BCP-47 locale name, e.g. <c>uk-UA</c> (empty when it could not be resolved).</param>
/// <param name="Label">Short display label, e.g. <c>UK</c>.</param>
public sealed record LayoutInfo(ushort LangId, string LocaleName, string Label);
