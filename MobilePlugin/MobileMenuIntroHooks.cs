using StArray.ModManager.Hooks;
using StArray.ModManager.Manager;

namespace MobilePauseButton.Mobile;

/// <summary>Optional button refresh for the MobileMenu scene.</summary>
internal static partial class MobileMenuIntroHooks
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
                Uninstall();
            return installed;
        }
        catch (Exception exception)
        {
            Uninstall();
            Logger.Debug(MobilePauseButtonPlugin.LogTag,
                $"MobileMenu button hook unavailable: {exception.Message}");
            return false;
        }
    }

    internal static void Uninstall()
    {
        try { UninstallHooks(); }
        catch (Exception exception)
        {
            Logger.Debug(MobilePauseButtonPlugin.LogTag,
                $"MobileMenu button hook removal failed: {exception.Message}");
        }
        finally { _game = null; }
    }

    [UnmanagedHook(
        "Assembly-CSharp.dll",
        "MobileMenu",
        "scnMobileMenu",
        "ShowIntroScreen",
        ParameterCount = 0)]
    private static void ShowIntroScreen(nint instance, nint methodInfo)
    {
        ShowIntroScreenOriginal(instance, methodInfo);
        try
        {
            if (_game?.ShowPauseButtonForMenu() == true)
                Logger.Debug(MobilePauseButtonPlugin.LogTag,
                    "Outside-game menu button enabled (MobileMenu)");
        }
        catch (Exception exception)
        {
            Logger.Debug(MobilePauseButtonPlugin.LogTag,
                $"Could not enable MobileMenu button: {exception.Message}");
        }
    }
}
