using StArray.ModManager.Hooks;
using StArray.ModManager.Manager;

namespace MobilePauseButton.Mobile;

/// <summary>Optional refresh for the custom-level browser.</summary>
internal static partial class CustomLevelSelectHooks
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
                $"scnCLS button hook unavailable: {exception.Message}");
            return false;
        }
    }

    internal static void Uninstall()
    {
        try { UninstallHooks(); }
        catch (Exception exception)
        {
            Logger.Debug(MobilePauseButtonPlugin.LogTag,
                $"scnCLS button hook removal failed: {exception.Message}");
        }
        finally { _game = null; }
    }

    [UnmanagedHook("Assembly-CSharp.dll", "scnCLS", "Start", ParameterCount = 0)]
    private static void CustomLevelSelectStart(nint instance, nint methodInfo)
    {
        CustomLevelSelectStartOriginal(instance, methodInfo);
        try
        {
            bool shown = _game?.ShowPauseButtonForOutsideGame() == true;
            Logger.Debug(MobilePauseButtonPlugin.LogTag,
                shown
                    ? "Outside-game menu button enabled (scnCLS.Start)"
                    : "Outside-game menu button SetActive failed (scnCLS.Start)");
        }
        catch (Exception exception)
        {
            Logger.Warn(MobilePauseButtonPlugin.LogTag,
                $"Could not enable scnCLS menu button: {exception.Message}");
        }
    }
}
