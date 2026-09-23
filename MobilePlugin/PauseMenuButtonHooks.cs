using StArray.ModManager.Hooks;
using StArray.ModManager.Manager;

namespace MobilePauseButton.Mobile;

/// <summary>
/// Restores PauseMenu.backButton at the two points where the PC build can
/// leave it hidden: PauseMenu.Awake and opening the settings page.
/// </summary>
internal static partial class PauseMenuButtonHooks
{
    private static GameApi? _game;

    internal static bool Install(GameApi game)
    {
        Uninstall();
        _game = game;

        try
        {
            bool installed = InstallHooks();
            if (!installed)
            {
                Logger.Debug(MobilePauseButtonPlugin.LogTag,
                    "Pause-menu back-button hooks unavailable");
                UninstallHooks();
                _game = null;
            }
            return installed;
        }
        catch (Exception exception)
        {
            try { UninstallHooks(); } catch { }
            _game = null;
            Logger.Warn(MobilePauseButtonPlugin.LogTag,
                $"Pause-menu back-button hook installation failed: {exception.Message}");
            return false;
        }
    }

    internal static void Uninstall()
    {
        try
        {
            UninstallHooks();
        }
        catch (Exception exception)
        {
            Logger.Debug(MobilePauseButtonPlugin.LogTag,
                $"Pause-menu back-button hook removal failed: {exception.Message}");
        }
        finally
        {
            _game = null;
        }
    }

    [UnmanagedHook("Assembly-CSharp.dll", "PauseMenu", "Awake", ParameterCount = 0)]
    private static void PauseMenuAwake(nint instance, nint methodInfo)
    {
        PauseMenuAwakeOriginal(instance, methodInfo);
        EnableBackButton(instance, "Mobile pause-menu back button enabled");
    }

    [UnmanagedHook(
        "Assembly-CSharp.dll",
        "PauseMenu",
        "ShowSettingsMenu",
        ParameterCount = 0)]
    private static void ShowSettingsMenu(nint instance, nint methodInfo)
    {
        ShowSettingsMenuOriginal(instance, methodInfo);
        EnableBackButton(instance, "Mobile settings back button enabled");
    }

    private static void EnableBackButton(nint instance, string successMessage)
    {
        try
        {
            if (_game?.ShowPauseMenuBackButton(instance) == true)
                Logger.Debug(MobilePauseButtonPlugin.LogTag, successMessage);
        }
        catch (Exception exception)
        {
            Logger.Warn(MobilePauseButtonPlugin.LogTag,
                $"Could not enable PauseMenu.backButton: {exception.Message}");
        }
    }
}
