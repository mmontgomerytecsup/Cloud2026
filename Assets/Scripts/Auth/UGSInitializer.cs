using UnityEngine;
using System;
using System.Threading.Tasks;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Core.Environments;

public class UGSInitializer : MonoBehaviour
{
    [Header("Unity Gaming Services Settings")]
    [SerializeField] private string environmentName = "production";
    [SerializeField] private bool initializeOnStart = true;
    
    // Events for initialization results
    public event Action OnInitializationSuccess;
    public event Action<string> OnInitializationFailed;
    public event Action OnAuthenticationSuccess;
    public event Action<string> OnAuthenticationFailed;
    
    // State tracking
    public bool IsInitialized { get; private set; }
    public bool IsAuthenticated { get; private set; }
    public string PlayerId { get; private set; }
    public string AccessToken { get; private set; }
    
    // Reference to LoginManager (optional, for integration)
    private LoginManager _loginManager;

    void Awake()
    {
        // Ensure this object persists across scenes
        DontDestroyOnLoad(gameObject);
        
        // Find LoginManager if it exists in the scene
        _loginManager = FindObjectOfType<LoginManager>();
        
        Debug.Log("UGSInitializer created");
    }

    async void Start()
    {
        if (initializeOnStart)
        {
            await InitializeUGS();
        }
    }

    /// <summary>
    /// Initialize Unity Gaming Services with optional custom environment
    /// </summary>
    public async Task<bool> InitializeUGS()
    {
        if (IsInitialized)
        {
            Debug.LogWarning("Unity Gaming Services already initialized");
            return true;
        }

        try
        {
            Debug.Log("Initializing Unity Gaming Services...");
            
            // Configure initialization options
            var options = new InitializationOptions();
            
            // Set custom environment if specified
            if (!string.IsNullOrEmpty(environmentName))
            {
                options.SetEnvironmentName(environmentName);
                Debug.Log($"Using environment: {environmentName}");
            }
            
            // Initialize Unity Services Core
            await UnityServices.InitializeAsync(options);
            
            IsInitialized = true;
            Debug.Log("Unity Gaming Services initialized successfully");
            
            OnInitializationSuccess?.Invoke();
            
            // Auto-authenticate if desired
            await AuthenticateAsync();
            
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to initialize Unity Gaming Services: {ex.Message}");
            IsInitialized = false;
            OnInitializationFailed?.Invoke(ex.Message);
            return false;
        }
    }

    /// <summary>
    /// Authenticate the player using Unity Authentication Service
    /// </summary>
    public async Task<bool> AuthenticateAsync()
    {
        if (!IsInitialized)
        {
            Debug.LogError("Cannot authenticate: Unity Gaming Services not initialized");
            OnAuthenticationFailed?.Invoke("Services not initialized");
            return false;
        }

        try
        {
            Debug.Log("Authenticating player...");
            
            // Check if already signed in
            if (AuthenticationService.Instance.IsSignedIn)
            {
                Debug.Log("Player already authenticated");
                UpdatePlayerInfo();
                OnAuthenticationSuccess?.Invoke();
                return true;
            }
            
            // Sign in anonymously using Unity Authentication
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
            
            if (AuthenticationService.Instance.IsSignedIn)
            {
                Debug.Log("Player authenticated successfully");
                UpdatePlayerInfo();
                OnAuthenticationSuccess?.Invoke();
                
                // Notify LoginManager if available
                NotifyLoginManagerOfSuccess();
                
                return true;
            }
            else
            {
                throw new Exception("Authentication failed - not signed in");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Authentication failed: {ex.Message}");
            OnAuthenticationFailed?.Invoke(ex.Message);
            return false;
        }
    }

    /// <summary>
    /// Sign out the current player
    /// </summary>
    public async Task<bool> SignOutAsync()
    {
        if (!IsAuthenticated)
        {
            Debug.LogWarning("No player is currently authenticated");
            return false;
        }

        try
        {
            Debug.Log("Signing out player...");
            
            await AuthenticationService.Instance.SignOutAsync();
            
            IsAuthenticated = false;
            PlayerId = null;
            AccessToken = null;
            
            Debug.Log("Player signed out successfully");
            
            // Notify LoginManager if available
            _loginManager?.Logout();
            
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"Sign out failed: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Update local player information from AuthenticationService
    /// </summary>
    private void UpdatePlayerInfo()
    {
        if (AuthenticationService.Instance.IsSignedIn)
        {
            PlayerId = AuthenticationService.Instance.PlayerId;
            AccessToken = AuthenticationService.Instance.AccessToken;
            IsAuthenticated = true;
            
            Debug.Log($"Player ID: {PlayerId}");
        }
    }

    /// <summary>
    /// Notify LoginManager of successful authentication (if linked)
    /// </summary>
    private void NotifyLoginManagerOfSuccess()
    {
        if (_loginManager != null && !_loginManager.IsLoggedIn)
        {
            // Mark as logged in with UGS player ID
            // Note: This is a simplified integration
            Debug.Log($"UGS authentication complete. Player ready for game services.");
        }
    }

    /// <summary>
    /// Get the current environment name
    /// </summary>
    public string GetCurrentEnvironment()
    {
        return environmentName;
    }

    /// <summary>
    /// Switch to a different environment (requires re-initialization)
    /// </summary>
    public void SetEnvironment(string newEnvironmentName)
    {
        if (IsInitialized)
        {
            Debug.LogWarning("Environment can only be changed before initialization");
            return;
        }
        
        environmentName = newEnvironmentName;
        Debug.Log($"Environment set to: {newEnvironmentName}");
    }

    /// <summary>
    /// Refresh the player's access token
    /// </summary>
    public async Task<bool> RefreshTokenAsync()
    {
        if (!IsAuthenticated)
        {
            Debug.LogWarning("Cannot refresh token: Player not authenticated");
            return false;
        }

        try
        {
            await AuthenticationService.Instance.RefreshSessionAsync();
            UpdatePlayerInfo();
            Debug.Log("Access token refreshed successfully");
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"Token refresh failed: {ex.Message}");
            return false;
        }
    }

    // Unity Lifecycle Methods
    
    void OnApplicationQuit()
    {
        Debug.Log("UGSInitializer shutting down");
    }

    void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            Debug.Log("Application paused");
        }
        else
        {
            Debug.Log("Application resumed");
            // Optionally refresh session on resume
            // await RefreshTokenAsync();
        }
    }
}
