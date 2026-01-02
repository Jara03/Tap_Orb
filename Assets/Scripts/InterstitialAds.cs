using UnityEngine;
using GoogleMobileAds.Api;
using System;

public class InterstitialAds : MonoBehaviour
{
    private InterstitialAd interstitialAd; 

#if UNITY_ANDROID
    private string adUnitId = "ca-app-pub-1810486296187934/7874409170"; // ID test officiel AdMob
#elif UNITY_IPHONE
    private string adUnitId = "ca-app-pub-3940256099942544/1033173712";
#else
    private string adUnitId = "unused";
#endif

    private void Start()
    {
        LoadInterstitialAd();
    }

    public void LoadInterstitialAd()
    {
        // Détruit une ancienne pub si nécessaire
        if (interstitialAd != null)
        {
            interstitialAd.Destroy();
        }

        var adRequest = new AdRequest();

        InterstitialAd.Load(adUnitId, adRequest, (InterstitialAd ad, LoadAdError error) =>
        {
            if (error != null || ad == null)
            {
                Debug.LogError("Erreur lors du chargement de l’interstitiel : " + error);
                return;
            }

            Debug.Log("Interstitial chargé !");
            interstitialAd = ad;

            // Abonnement aux événements
            interstitialAd.OnAdFullScreenContentClosed += () =>
            {
                Debug.Log("Interstitial fermé. Rechargement...");
                LoadInterstitialAd();
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
            LoadInterstitialAd();
        }
    }
}