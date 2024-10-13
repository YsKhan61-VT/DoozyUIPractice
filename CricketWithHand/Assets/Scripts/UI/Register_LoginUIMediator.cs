using CricketWithHand.Authentication;
using PlayFab;
using PlayFab.ClientModels;
using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using YSK.Utilities;


namespace CricketWithHand.UI
{
    public class Register_LoginUIMediator : MonoBehaviour
    {
        #region Events

        [SerializeField]
        private UnityEvent<string> _onLoggedInWithDisplayname;

        [SerializeField]
        private UnityEvent _onLoggedInWithoutDisplayName;

        [SerializeField]
        private UnityEvent _onLinkAccountSuccess;

        [SerializeField]
        private UnityEvent _onLogOutSuccess;

        [SerializeField]
        private UnityEvent _onAutoLoginFailed;

        #endregion


        [SerializeField]
        private RegisterUI _registerUI;

        [SerializeField]
        private LoginUI _loginUI;

        [SerializeField]
        private DisplayNameUI _displayNameUI;

        [SerializeField, Tooltip("For debug purposes")]
        private bool _clearPlayerPrefs;

        [SerializeField]
        private GetPlayerCombinedInfoRequestParams _infoRequestParams;

        [SerializeField]
        private string _googleWebClientId;

        [SerializeField]
        private int _authenticationTimeOutInSeconds = 10;

        public bool IsLoggedIn => _authServiceFacade.IsLoggedIn;
        public bool IsLinkedInWithGoogle => _authServiceFacade.IsLinkedWithGoogle;

        private PlayFabAuthServiceFacade _authServiceFacade;
        private LoadingUI _loadingUI;
        private CancellationTokenSource _cts;

        private void Start()
        {
            _loadingUI = LoadingUI.instance;
            _authServiceFacade = PlayFabAuthServiceFacade.Instance;

            if (_clearPlayerPrefs)
            {
                ForgetLastAccountDetails();
            }

            _loginUI.ToggleRememberMeUI(_authServiceFacade.AuthData.RememberMe);

            TryLoginWithRememberedAccount();
        }

        private void OnDestroy()
        {
            CancelAndDisposeCTS();
        }

        private void OnApplicationQuit()
        {
            AutoLogOutOnApplicationQuit();
        }

        public void RegisterWithEmailAndPassword(string email, string password, string confirmPassword, bool rememberMe)
        {
            _loadingUI.Show();
            _authServiceFacade.RegisterWithEmailAndPassword(
                email, 
                password, 
                _infoRequestParams,
                rememberMe,
                (result) =>
                {
                    OnPlayFabLoginSuccess(result);
                },
                (error) =>
                {
                    OnPlayFabError(error);
                }
            );
        }

        public void LoginWithEmailAndPassword(string email, string password, bool rememberMe)
        {
            _loadingUI.Show();
            _authServiceFacade.AuthenticateEmailPassword(
                email, 
                password, 
                _infoRequestParams,
                rememberMe,

                (result) =>
                {
                    OnPlayFabLoginSuccess(result);
                },

                (error) =>
                {
                    OnPlayFabError(error);
                }
            );
        }

        public void LoginAsAGuest()
        {
            _loadingUI.Show();
            _authServiceFacade.SilentlyAuthenticate(
                _infoRequestParams,
                (result) =>
                {
                    OnPlayFabLoginSuccess(result);
                },
                (error) =>
                {
                    OnPlayFabError(error);
                } 
            );
        }

        public void LoginWithGoogleAccount(bool rememberMe)
        {
            LogUI.instance.AddStatusText("Logging in with google account...");

            _loadingUI.Show();
            StartTimeOutCalculation(_authenticationTimeOutInSeconds);

            _authServiceFacade.AuthenticateWithGoogle(
                _googleWebClientId, 
                _infoRequestParams,
                rememberMe,
                (result) =>
                {
                    LogUI.instance.AddStatusText("PlayFab login with google success!");
                    OnPlayFabLoginSuccess(result);
                    CancelAndDisposeCTS();
                },
                (error) =>
                {
                    OnPlayFabError(error);
                    CancelAndDisposeCTS();
                }
            );
        }

        public void LinkAccountWithGoogle()
        {
            LogUI.instance.AddStatusText("Linking with google ...");

            _loadingUI.Show();
            StartTimeOutCalculation(_authenticationTimeOutInSeconds);

            _authServiceFacade.LinkWithGoogle(
                _googleWebClientId, 
                _infoRequestParams,
                (result) =>
                {
                    LogUI.instance.AddStatusText("PlayFab linked with google success!");
                    OnLinkAccountWithGoogleSuccess();
                    CancelAndDisposeCTS();
                },
                (error) =>
                {
                    OnPlayFabError(error);
                    CancelAndDisposeCTS();
                }
            );
        }

        public void SetDisplayName(string displayName)
        {
            _loadingUI.Show();
            _authServiceFacade.SetDisplayName(
                displayName,
                (userName) => OnUserDisplayNameSet(userName),
                (error) => OnPlayFabError(error)
            );
        }

        /// <summary>
        /// We need to take precautions before logging out of a unlinked guest account, as
        /// doing such will let us loose all guest account data.
        /// </summary>
        public void OnUserWantsToLogOut()
        {
            if (!_authServiceFacade.IsLoggedIn)
            {
                LogUI.instance.AddStatusText("No user logged in!");
                return;
            }

            if (_authServiceFacade.AuthData.AuthType == Authtypes.Silent)
            {
                ConfirmPopupUI.instance.ShowPopup(
                    new ConfirmPopupUI.Payload()
                    {
                        Title = "WARNING!",
                        Message = "This is a guest account, if you logout from this, \n " +
                        "all datas will be lost, and account can't be recovered. \n" +
                        "Better to link this account before logging out.! \n" +
                        "Do you still want to logout?",
                        LeftButtonLabel = "YES",
                        RightButtonLabel = "NO",
                        OnLeftButtonClickedCallback = () =>
                        {
                            LogOut(ForgetLastAccountDetails);
                        }
                    }
                );
            }
            else
            {
                LogOut(ForgetLastAccountDetails);
            }
        }

        [ContextMenu("OnAutoLoginFailed")]
        void Invoke() => _onAutoLoginFailed?.Invoke();

        private async void TryLoginWithRememberedAccount()
        {
            // Let other scripts and Doozy get initialized
            await Task.Delay(5000);

            if (!_authServiceFacade.AuthData.RememberMe)
            {
                _onAutoLoginFailed?.Invoke();
                return;
            }

            _loadingUI.Show();

            _authServiceFacade.LoginRememberedAccount(
                _infoRequestParams,

                (result) =>
                {
                    OnPlayFabLoginSuccess(result);
                },

                (error) =>
                {
                    OnPlayFabError(error);
                    _onAutoLoginFailed?.Invoke();
                }
            );
        }

        /// <summary>
        /// Guest account if not linked, then if logged out, will loose all datas.
        /// So don't force logout of guest account.
        /// </summary>
        private void AutoLogOutOnApplicationQuit()
        {
            if (_authServiceFacade.AuthData.AuthType == Authtypes.Silent)
                return;

            LogOut();
        }

        private void LogOut(Action onSuccess = null)
        {
            _authServiceFacade.LogOut(
                (result) =>
                {
                    LogUI.instance.AddStatusText(result);
                    onSuccess?.Invoke();
                    _onLogOutSuccess?.Invoke();
                },
                (error) =>
                {
                    LogUI.instance.AddStatusText(error);
                }
            );
        }

        private void OnPlayFabLoginSuccess(LoginResult result)
        {
            LogUI.instance.AddStatusText($"Logged In as: {result.PlayFabId}");

            string displayName = result.InfoResultPayload.AccountInfo.TitleInfo.DisplayName;
            if (displayName != null)
            {
                LogUI.instance.AddStatusText($"Welcome: {displayName}");
                _onLoggedInWithDisplayname?.Invoke(displayName);
            }
            else
            {
                LogUI.instance.AddStatusText("User display name needs to be set!");
                _onLoggedInWithoutDisplayName?.Invoke();
            }

            _loadingUI.Hide();
        }

        private void OnPlayFabError(PlayFabError error)
        {
            string errorReport = error.GenerateErrorReport();
            LogUI.instance.AddStatusText($"Error code: {error.Error} \n Message: {errorReport} \n");
            PopupUI.instance.ShowPopup($"Error code: {error.Error}", $"Message: {errorReport} \n");

            _loadingUI.Hide();
        }

        private void OnUserDisplayNameSet(string displayName)
        {
            _loadingUI.Hide();
            LogUI.instance.AddStatusText($"Display name set to: . {displayName}");
            _onLoggedInWithDisplayname?.Invoke(displayName);
        }

        private void OnLinkAccountWithGoogleSuccess()
        {
            _loadingUI.Hide();
            PopupUI.instance.ShowPopup("Link with Google Success", "You have successfully linked your account with google!");
            _onLinkAccountSuccess?.Invoke();
        }

        private async void StartTimeOutCalculation(int waitForSeconds)
        {
            // Dispose of the previous CancellationTokenSource if it exists
            _cts?.Dispose();
            _cts = new CancellationTokenSource();

            try
            {
                // Wait for the specified time or cancellation
                await Task.Delay(waitForSeconds * 1000, _cts.Token);

                // Check if the token hasn't been canceled before hiding the UI
                if (!_cts.Token.IsCancellationRequested)
                {
                    // Force stop authentication
                    _authServiceFacade.OnAuthenticationTimeOut();
                    _loadingUI.Hide();
                }
            }
            catch (TaskCanceledException)
            {
                // Task was canceled, do nothing as UI is being handled elsewhere
            }
            catch (Exception ex)
            {
                // Log any unexpected errors
                Debug.LogError($"An error occurred: {ex.Message}");
            }
        }

        private void ForgetLastAccountDetails()
        {
            _registerUI.Reset();
            _loginUI.Reset();
            _displayNameUI.Reset();
            _authServiceFacade.ClearCache();
        }

        private void CancelAndDisposeCTS()
        {
            _cts?.Cancel();
            _cts?.Dispose();
        }
    }
}
