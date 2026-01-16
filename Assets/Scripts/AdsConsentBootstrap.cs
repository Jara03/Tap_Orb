using GoogleMobileAds.Api;
using GoogleMobileAds.Ump.Api;
using UnityEngine;
using System;

public class AdsConsentBootstrap : MonoBehaviour
{
    public static AdsConsentBootstrap Instance { get; private set; }
    public static event Action AdsReady;

    public const bool ForceDebugGeographyEea = false;

    [SerializeField] private bool initializeOnStart = true;

    public bool IsInitialized { get; private set; }
    public bool ConsentFlowCompleted { get; private set; }

    private bool isRequestInProgress;
    private bool hasSignaledReady;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        if (initializeOnStart)
        {
            RequestConsentAndInitialize();
        }
    }

    public void RequestConsentAndInitialize()
    {
        if (isRequestInProgress)
        {
            Debug.Log("[AdsConsent] Consent flow déjà en cours.");
            return;
        }

#if !UNITY_ANDROID && !UNITY_IOS
        Debug.Log("[AdsConsent] UMP non supporté sur cette plateforme. Initialisation directe.");
        isRequestInProgress = false;
        ConsentFlowCompleted = true;
        InitializeMobileAdsIfAllowed();
        return;
#endif

        isRequestInProgress = true;
        ConsentFlowCompleted = false;
        hasSignaledReady = false;

        ConsentDebugSettings debugSettings = null;
        if (ForceDebugGeographyEea)
        {
            debugSettings = new ConsentDebugSettings
            {
                DebugGeography = DebugGeography.Eea
            };
        }

        var requestParameters = new ConsentRequestParameters
        {
            TagForUnderAgeOfConsent = false,
            ConsentDebugSettings = debugSettings
        };

        Debug.Log("[AdsConsent] Update consent info...");
        try
        {
            ConsentInformation.Update(requestParameters, (FormError updateError) =>
            {
                if (updateError != null)
                {
                    Debug.LogError($"[AdsConsent] Update consent error: {updateError}.");
                    isRequestInProgress = false;
                    ConsentFlowCompleted = true;
                    LogConsentState("Update échoué", null);
                    return;
                }

                LogConsentState("Update ok", null);
                bool formRequired = ConsentInformation.ConsentStatus == ConsentStatus.Required;
                Debug.Log("[AdsConsent] Load/Show consent form si requis...");

                ConsentForm.LoadAndShowConsentFormIfRequired((FormError formError) =>
                {
                    isRequestInProgress = false;
                    ConsentFlowCompleted = true;

                    if (formError != null)
                    {
                        Debug.LogError($"[AdsConsent] Consent form error: {formError}.");
                        LogConsentState("Formulaire en erreur", formRequired);
                        return;
                    }

                    LogConsentState("Formulaire terminé", formRequired);
                    InitializeMobileAdsIfAllowed();
                });
            });
        }
        catch (NullReferenceException exception)
        {
            Debug.LogError($"[AdsConsent] UMP a échoué (NullReference): {exception}");
            isRequestInProgress = false;
            ConsentFlowCompleted = true;
            LogConsentState("Update exception", null);
        }
    }

    public void ResetConsentButton()
    {
        Debug.Log("[AdsConsent] Reset consent demandé.");
        ConsentInformation.Reset();
        RequestConsentAndInitialize();
    }

    public bool CanRequestAds()
    {
        return ConsentInformation.CanRequestAds();
    }

    private void InitializeMobileAdsIfAllowed()
    {
        if (!ConsentInformation.CanRequestAds())
        {
            LogConsentState("Ads bloquées (consent manquant)", null);
            return;
        }

        if (IsInitialized)
        {
            SignalAdsReady();
            return;
        }

        Debug.Log("[AdsConsent] Initialisation Mobile Ads...");
        MobileAds.Initialize((InitializationStatus initstatus) =>
        {
            if (initstatus == null)
            {
                Debug.LogError("[AdsConsent] Google Mobile Ads initialization failed.");
                return;
            }

            IsInitialized = true;
            Debug.Log("[AdsConsent] Google Mobile Ads initialization complete.");
            SignalAdsReady();

#if UNITY_ANDROID || UNITY_IOS
            if (Debug.isDebugBuild)
            {
                MobileAds.OpenAdInspector((AdInspectorError error) =>
                {
                    if (error != null)
                    {
                        Debug.LogWarning("Ad Inspector n'a pas pu s'ouvrir (mode dev) : " + error);
                        return;
                    }

                    Debug.Log("Ad Inspector ouvert (mode dev).");
                });
            }
#endif
        });
    }

    private void SignalAdsReady()
    {
        if (hasSignaledReady || !IsInitialized || !ConsentInformation.CanRequestAds())
        {
            return;
        }

        hasSignaledReady = true;
        AdsReady?.Invoke();
    }

    private void LogConsentState(string context, bool? formShown)
    {
        string formValue = formShown.HasValue ? formShown.Value.ToString() : "inconnu";
        Debug.Log(
            $"[AdsConsent] {context} | CanRequestAds={ConsentInformation.CanRequestAds()} | " +
            $"Status={ConsentInformation.ConsentStatus} | FormShown={formValue}");
    }
}
