namespace InLay.App.Autostart;

/// <summary>Reads and toggles whether InLay launches automatically when the user signs in.</summary>
internal interface IAutostartService
{
    /// <summary>Whether an autostart entry currently exists for this application.</summary>
    bool IsEnabled { get; }

    /// <summary>Registers the current executable to run at sign-in.</summary>
    void Enable();

    /// <summary>Removes the autostart registration if present.</summary>
    void Disable();
}
