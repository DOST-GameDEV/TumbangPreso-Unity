using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    public sealed partial class SplashScreen
    {
        private static SplashScreen _menuCurtain;
        private ConvertedMainMenu _preparedMenu;
        private bool _menuActivationFailed;
        private bool _quitAfterMenuFailure;
        private UnityEngine.UI.Button _menuFailureQuit;
        internal static bool MenuActivationPending => _menuCurtain != null;

        private void PrepareMenuActivation()
        {
            // The scene's 90% load does not run menu Awake/Start or canvas layout.
            // Retain the existing picture and owner until that work actually finishes.
            _menuCurtain = this;
            transform.SetParent(null, true);
            DontDestroyOnLoad(gameObject);
            if (_canvas != null)
            {
                DontDestroyOnLoad(_canvas);
                _canvas.GetComponent<Canvas>().sortingOrder = 1500;
                if (_canvas.GetComponent<GraphicRaycaster>() == null) _canvas.AddComponent<GraphicRaycaster>();
            }
            if (_fade != null) _fade.raycastTarget = true;
            if (_artButton != null) _artButton.interactable = false;
            ScreenTakeover.Register(this, () => _menuCurtain == this);
            SceneFlow.BootedThroughSplash = true;
        }

        private IEnumerator ActivatePreparedMenu()
        {
            PrepareMenuActivation();
            SetLoadingStage("opening main menu", .96f);
            if (_menu == null && Application.CanStreamedLevelBeLoaded(SceneFlow.MainMenu))
                _menu = SceneManager.LoadSceneAsync(SceneFlow.MainMenu);
            if (_menu == null) { FailMenuActivation(); yield break; }
            _menu.allowSceneActivation = true;
            while (!_menu.isDone)
            {
                _elapsed += Time.unscaledDeltaTime;
                UpdateLoadingAnimation();
                yield return null;
            }

            // Start builds the native home and the boot account form. Its explicit
            // readiness is separate from AsyncOperation.isDone; a failed Wire is not ready.
            yield return null;
            _preparedMenu = Object.FindAnyObjectByType<ConvertedMainMenu>();
            if (_preparedMenu == null || !_preparedMenu.IsPrepared
                || _preparedMenu.gameObject.scene != SceneManager.GetActiveScene())
            { FailMenuActivation(); yield break; }
            SetLoadingStage("opening main menu", .98f);
            Canvas.ForceUpdateCanvases();
            // Let newly created view components Start and draw once under the curtain.
            yield return null;
        }

        private void FailMenuActivation()
        {
            _menuActivationFailed = true;
            if (_loadingLabel != null) _loadingLabel.text = "MAIN MENU COULD NOT OPEN";
            if (_canvas != null && _menuFailureQuit == null)
            {
                var controls = OwnerUiLayout.DesignArea(_canvas.transform, "LoadingFailureControls");
                _menuFailureQuit = LoadingLink(controls, "LoadingQuit", "EXIT GAME",
                    () => _quitAfterMenuFailure = true, 1440, 760, 360, OwnerUiTheme.Current.Pale, onDark: true);
                InputLayer.ScreenFocus.Install(controls.gameObject).Rebuild();
                var events = UnityEngine.EventSystems.EventSystem.current;
                if (events != null) events.SetSelectedGameObject(_menuFailureQuit.gameObject);
            }
            Debug.LogError("[Splash] main menu activation or initialization failed; readiness was not completed.");
        }

        private void OnDestroy()
        {
            ScreenTakeover.Unregister(this);
            if (_menuCurtain == this) _menuCurtain = null;
            ReleaseStudioIntro();
            if (_canvas != null && _canvas != gameObject) Destroy(_canvas);
        }
    }
}
