using System;
using System.Collections.Generic;
using GoogleMobileAds.Api;
using GoogleMobileAds.Ump.Api;
using UnityEngine;

namespace TapOrb.Ads
{
    public class AdsManager : MonoBehaviour
    {
        public static AdsManager Instance { get; private set; }

        public event Action AdsReady;
        public event Action<string> InterstitialShown;

        [Header("Initialization")]
        [SerializeField] private bool initializeOnStart = true;
        [SerializeField] private bool enableAds = true;
        [SerializeField] private bool enableBanner = true;
        [SerializeField] private bool enableInterstitial = true;

        [Header("Ad Unit IDs")]
        [SerializeField] private string androidBannerAdUnitId = "ca-app-pub-1810486296187934/7874409170";
        [SerializeField] private string iosBannerAdUnitId = "ca-app-pub-1810486296187934/2867804438";
        [SerializeField] private string androidInterstitialAdUnitId = "ca-app-pub-1810486296187934/7874409170";
        [SerializeField] private string iosInterstitialAdUnitId = "ca-app-pub-1810486296187934/2735942490";

        [Header("Consent / Debug")]
        [SerializeField] private bool tagForUnderAgeOfConsent = false;
        [SerializeField] private bool forceDebugGeographyEea = false;
        [SerializeField] private bool enableTestMode = false;
        [SerializeField] private bool openAdInspectorInDebug = false;
        [SerializeField] private List<string> testDeviceHashedIds = new List<string>();

        private AdsConsentService consentService;
        private AdsBannerService bannerService;
        private AdsInterstitialService interstitialService;

        public bool IsInitialized { get; private set; }
        public bool CanRequestAds => ConsentInformation.CanRequestAds();

        private bool adsReadySignaled;
        private bool isInitializing;

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
                InitializeAds();
            }
        }

        public void InitializeAds()
        {
            if (!enableAds || isInitializing || IsInitialized)
            {
                return;
            }

            isInitializing = true;
            consentService ??= new AdsConsentService();
            var consentOptions = new AdsConsentService.Options
            {
                TagForUnderAgeOfConsent = tagForUnderAgeOfConsent,
                ForceDebugGeographyEea = forceDebugGeographyEea,
                TestDeviceHashedIds = testDeviceHashedIds
            };

            consentService.RequestConsent(consentOptions, result =>
            {
                LogConsentState("Consent flow terminé", result);
                if (result.Error != null)
                {
                    isInitializing = false;
                    return;
                }

                if (!result.CanRequestAds)
                {
                    Debug.LogWarning("[AdsManager] Consentement insuffisant pour charger des pubs.");
                    isInitializing = false;
                    return;
                }

                ConfigureRequestSettings();
                InitializeMobileAds();
            });
        }

        public void ShowPrivacyOptionsForm(Action<FormError> onComplete)
        {
            consentService ??= new AdsConsentService();
            consentService.ShowPrivacyOptionsForm(onComplete);
        }

        public void ResetConsent()
        {
            consentService ??= new AdsConsentService();
            consentService.ResetConsent();
            InitializeAds();
        }

        public void LoadBanner()
        {
            if (!enableBanner || !CanRequestAds)
            {
                return;
            }

            bannerService ??= new AdsBannerService(this, GetBannerAdUnitId());
            bannerService.Load();
        }

        public void HideBanner()
        {
            bannerService?.Hide();
        }

        public void ShowBanner()
        {
            bannerService?.Show();
        }

        public bool ShowInterstitial(string placement = null)
        {
            if (!enableInterstitial || !CanRequestAds)
            {
                return false;
            }

            interstitialService ??= new AdsInterstitialService(GetInterstitialAdUnitId());
            return interstitialService.Show(() =>
            {
                Debug.Log($"[AdsManager] Interstitiel affiché ({placement ?? "unknown"}).");
                InterstitialShown?.Invoke(placement);
            });
        }

        public void LoadInterstitial()
        {
            if (!enableInterstitial || !CanRequestAds)
            {
                return;
            }

            interstitialService ??= new AdsInterstitialService(GetInterstitialAdUnitId());
            interstitialService.Load();
        }

        private void InitializeMobileAds()
        {
#if !UNITY_ANDROID && !UNITY_IOS
            isInitializing = false;
            IsInitialized = true;
            SignalAdsReady();
            return;
#else
            Debug.Log("[AdsManager] Initialisation Mobile Ads...");
            MobileAds.Initialize(status =>
            {
                if (status == null)
                {
                    Debug.LogError("[AdsManager] Google Mobile Ads initialization failed.");
                    isInitializing = false;
                    return;
                }

                IsInitialized = true;
                isInitializing = false;
                Debug.Log("[AdsManager] Google Mobile Ads initialization complete.");
                SignalAdsReady();

                if (enableBanner)
                {
                    LoadBanner();
                }

                if (enableInterstitial)
                {
                    LoadInterstitial();
                }

#if UNITY_ANDROID || UNITY_IOS
                if (Debug.isDebugBuild && openAdInspectorInDebug)
                {
                    MobileAds.OpenAdInspector(error =>
                    {
                        if (error != null)
                        {
                            Debug.LogWarning("Ad Inspector n'a pas pu s'ouvrir (mode dev) : " + error);
                            return;
                        }

                        Debug.Log("Ad Inspector ouvert (mode dev)." );
                    });
                }
#endif
            });
#endif
        }

        private void ConfigureRequestSettings()
        {
#if UNITY_ANDROID || UNITY_IOS
            var requestConfiguration = new RequestConfiguration();
            if (enableTestMode && testDeviceHashedIds != null && testDeviceHashedIds.Count > 0)
            {
                requestConfiguration.TestDeviceIds = testDeviceHashedIds;
                Debug.Log("[AdsManager] Test devices activés.");
            }

            MobileAds.SetRequestConfiguration(requestConfiguration);
#endif
        }

        private void SignalAdsReady()
        {
            if (adsReadySignaled || !IsInitialized || !CanRequestAds)
            {
                return;
            }

            adsReadySignaled = true;
            AdsReady?.Invoke();
        }

        private string GetBannerAdUnitId()
        {
#if UNITY_ANDROID
            return androidBannerAdUnitId;
#elif UNITY_IOS
            return iosBannerAdUnitId;
#else
            return string.Empty;
#endif
        }

        private string GetInterstitialAdUnitId()
        {
#if UNITY_ANDROID
            return androidInterstitialAdUnitId;
#elif UNITY_IOS
            return iosInterstitialAdUnitId;
#else
            return string.Empty;
#endif
        }

        private void LogConsentState(string context, AdsConsentService.Result result)
        {
            Debug.Log(
                $"[AdsConsent] {context} | CanRequestAds={result.CanRequestAds} | " +
                $"Status={result.Status} | FormShown={result.FormWasShown?.ToString() ?? "inconnu"}");
        }

        private void OnDestroy()
        {
            bannerService?.Destroy();
            interstitialService?.Destroy();
        }
    }
}
