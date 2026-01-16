using UnityEngine;
using GoogleMobileAds.Api;
using GoogleMobileAds.Ump.Api;
using System;

public class InterstitialAds : MonoBehaviour
{
    private InterstitialAd interstitialAd; 
    public event Action OnInterstitialShown;
    private bool isLoading;

#if UNITY_ANDROID
    private string adUnitId = "ca-app-pub-1810486296187934/7874409170";
#elif UNITY_IPHONE
    private string adUnitId = "ca-app-pub-1810486296187934/2735942490";
#else
    private string adUnitId = "unused";
#endif

    private void Start()
    {
        TryLoadInterstitial("Start");
    }

    private void OnEnable()
    {
        AdsConsentBootstrap.AdsReady += HandleAdsReady;
    }

    private void OnDisable()
    {
        AdsConsentBootstrap.AdsReady -= HandleAdsReady;
    }

    private void HandleAdsReady()
    {
        TryLoadInterstitial("ConsentReady");
    }

    public void TryLoadInterstitial(string reason)
    {
        if (!ConsentInformation.CanRequestAds())
        {
            Debug.Log("[InterstitialAds] Consent manquant, interstitiel non chargé.");
            return;
        }

        if (isLoading)
        {
            Debug.Log("[InterstitialAds] Chargement déjà en cours.");
            return;
        }

        // Détruit une ancienne pub si nécessaire
        if (interstitialAd != null)
        {
            interstitialAd.Destroy();
        }

        var adRequest = new AdRequest();
        isLoading = true;
        Debug.Log($"[InterstitialAds] Chargement interstitiel ({reason}).");

        InterstitialAd.Load(adUnitId, adRequest, (InterstitialAd ad, LoadAdError error) =>
        {
            isLoading = false;
            if (error != null || ad == null)
            {
                Debug.LogError("Erreur lors du chargement de l’interstitiel : " + error);
                return;
            }

            Debug.Log("Interstitial chargé !");
            interstitialAd = ad;

            // Abonnement aux événements
            interstitialAd.OnAdFullScreenContentOpened += () =>
            {
                Debug.Log("Interstitial ouvert.");
                OnInterstitialShown?.Invoke();
            };
            interstitialAd.OnAdFullScreenContentFailed += (AdError adError) =>
            {
                Debug.LogError("Échec d'affichage de l’interstitiel (impression non enregistrée) : " + adError);
                TryLoadInterstitial("FailedToShow");
            };
            interstitialAd.OnAdFullScreenContentClosed += () =>
            {
                Debug.Log("Interstitial fermé. Rechargement...");
                TryLoadInterstitial("Closed");
            };
        });
    }

    public void ShowInterstitialAd()
    {
        if (interstitialAd != null && interstitialAd.CanShowAd())
        {
            interstitialAd.Show();
        }
        else
        {
            Debug.Log("Interstitial non prêt, rechargement...");
            TryLoadInterstitial("NotReady");
        }
    }
}
