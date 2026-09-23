using StArray.ModManager.Hooks;
using StArray.ModManager.Manager;

namespace MobilePauseButton.Mobile;

internal static partial class GameHooks
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
                Logger.Warn(MobilePauseButtonPlugin.LogTag,
                    "scrController.Awake was not hooked; the button will remain unchanged");
            }
            return installed;
        }
        catch (Exception exception)
        {
            Logger.Error(MobilePauseButtonPlugin.LogTag,
                $"Pause button hook installation failed: {exception}");
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
            Logger.Warn(MobilePauseButtonPlugin.LogTag,
                $"Pause button hook removal failed: {exception.Message}");
        }
        finally
        {
            _game = null;
        }
    }

    /// <summary>
    /// The PC build disables the mobile branch because its platform identity is Windows.
    /// Temporarily exposing Android makes the original method install its own UnityEvent
    /// listener, so the button calls the game's real TogglePauseGame implementation.
    /// </summary>
    [UnmanagedHook("Assembly-CSharp.dll", "scrController", "Awake", ParameterCount = 0)]
    private static void ControllerAwake(nint instance, nint methodInfo)
    {
        GameApi? game = _game;
        int previousPlatform = 0;
        bool platformForced = game?.TryEnterMobilePlatform(out previousPlatform) == true;

        try
        {
            ControllerAwakeOriginal(instance, methodInfo);
        }
        finally
        {
            if (platformForced)
                game!.RestorePlatform(previousPlatform);
        }

        try
        {
            // scnMobileMenu.firstTimeLoadingScene can still be true when a controller
            // is created. The original mobile branch adds the listener in that case but
            // intentionally hides the object; gameplay gets this visibility fallback.
            if (game?.ShowPauseButtonForGameWorld(instance) == true)
                Logger.Debug(MobilePauseButtonPlugin.LogTag, "Gameplay pause button enabled");
        }
        catch (Exception exception)
        {
            // Never turn a UI convenience failure into a gameplay initialization failure.
            Logger.Warn(MobilePauseButtonPlugin.LogTag,
                $"Could not enable gameplay pause button: {exception.Message}");
        }
    }
}
