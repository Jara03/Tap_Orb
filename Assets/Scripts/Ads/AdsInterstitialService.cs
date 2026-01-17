using System;
using GoogleMobileAds.Api;
using UnityEngine;

namespace TapOrb.Ads
{
    public sealed class AdsInterstitialService
    {
        private readonly string adUnitId;
        private InterstitialAd interstitialAd;
        private bool isLoading;
        private Action onShown;

        public AdsInterstitialService(string adUnitId)
        {
            this.adUnitId = adUnitId;
        }

        public bool IsReady => interstitialAd != null && interstitialAd.CanShowAd();

        public void Load()
        {
#if !UNITY_ANDROID && !UNITY_IOS
            return;
#else
            if (string.IsNullOrWhiteSpace(adUnitId))
            {
                Debug.LogWarning("[AdsInterstitial] Aucun adUnitId configuré.");
                return;
            }

            if (isLoading)
            {
                Debug.Log("[AdsInterstitial] Chargement déjà en cours.");
                return;
            }

            interstitialAd?.Destroy();
            interstitialAd = null;

            var adRequest = new AdRequest();
            isLoading = true;
            Debug.Log("[AdsInterstitial] Chargement interstitiel.");

            InterstitialAd.Load(adUnitId, adRequest, (ad, error) =>
            {
                isLoading = false;
                if (error != null || ad == null)
                {
                    Debug.LogError("[AdsInterstitial] Erreur lors du chargement : " + error);
                    return;
                }

                interstitialAd = ad;
                RegisterEvents();
                Debug.Log("[AdsInterstitial] Interstitiel prêt.");
            });
#endif
        }

        public bool Show(Action onShownCallback)
        {
#if !UNITY_ANDROID && !UNITY_IOS
            return false;
#else
            if (!IsReady)
            {
                Debug.Log("[AdsInterstitial] Interstitiel non prêt.");
                return false;
            }

            onShown = onShownCallback;
            interstitialAd.Show();
            return true;
#endif
        }

        public void Destroy()
        {
#if UNITY_ANDROID || UNITY_IOS
            interstitialAd?.Destroy();
            interstitialAd = null;
#endif
        }

        private void RegisterEvents()
        {
            interstitialAd.OnAdFullScreenContentOpened += () =>
            {
                Debug.Log("[AdsInterstitial] Interstitiel ouvert.");
                onShown?.Invoke();
                onShown = null;
            };
            interstitialAd.OnAdFullScreenContentClosed += () =>
            {
                Debug.Log("[AdsInterstitial] Interstitiel fermé. Rechargement...");
                Load();
            };
            interstitialAd.OnAdFullScreenContentFailed += error =>
            {
                Debug.LogError("[AdsInterstitial] Échec d'affichage : " + error);
                Load();
            };
            interstitialAd.OnAdClicked += () =>
            {
                Debug.Log("[AdsInterstitial] Clic enregistré.");
            };
            interstitialAd.OnAdImpressionRecorded += () =>
            {
                Debug.Log("[AdsInterstitial] Impression enregistrée.");
            };
        }
    }
}
