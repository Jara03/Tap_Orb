using GoogleMobileAds.Api;
using GoogleMobileAds.Ump.Api;
using UnityEngine;

public class AdsBanner : MonoBehaviour
{
#if UNITY_ANDROID
    private const string AdUnitId = "ca-app-pub-1810486296187934/7874409170";
#elif UNITY_IOS
    private const string AdUnitId = "ca-app-pub-1810486296187934/2867804438";
#else
    private const string AdUnitId = "ca-app-pub-1810486296187934/2867804438";
#endif

    private BannerView bannerView;
    private bool isLoading;
    private int retryAttempt;
    private Coroutine retryCoroutine;

    private void Start()
    {
        TryLoadBanner("Start");
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
        TryLoadBanner("ConsentReady");
    }

    private void TryLoadBanner(string reason)
    {
        if (!ConsentInformation.CanRequestAds())
        {
            Debug.Log("[AdsBanner] Consent manquant, bannière non chargée.");
            return;
        }

        if (isLoading)
        {
            Debug.Log("[AdsBanner] Chargement déjà en cours.");
            return;
        }

        if (retryCoroutine != null)
        {
            StopCoroutine(retryCoroutine);
            retryCoroutine = null;
        }

        if (bannerView == null)
        {
            bannerView = new BannerView(AdUnitId, AdSize.Banner, AdPosition.Bottom);

            bannerView.OnAdLoaded += () =>
            {
                Debug.Log("Bannière chargée.");
                isLoading = false;
                retryAttempt = 0;
            };
            bannerView.OnAdFailedToLoad += (LoadAdError error) =>
            {
                Debug.LogError("Erreur lors du chargement de la bannière (impression non enregistrée) : " + error);
                isLoading = false;
                ScheduleRetry();
            };
            bannerView.OnAdImpressionRecorded += () =>
            {
                Debug.Log("Impression de la bannière enregistrée.");
            };
        }

        Debug.Log($"[AdsBanner] Chargement bannière ({reason}).");
        isLoading = true;
        var request = new AdRequest();
        bannerView.LoadAd(request);
    }

    private void ScheduleRetry()
    {
        if (!ConsentInformation.CanRequestAds())
        {
            Debug.Log("[AdsBanner] Consent manquant, retry annulé.");
            return;
        }

        float delay = Mathf.Min(30f * Mathf.Pow(2f, retryAttempt), 300f);
        retryAttempt++;
        Debug.Log($"[AdsBanner] Retry dans {delay}s.");
        retryCoroutine = StartCoroutine(RetryAfterDelay(delay));
    }

    private System.Collections.IEnumerator RetryAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        retryCoroutine = null;
        TryLoadBanner("Retry");
    }

    private void OnDestroy()
    {
        // Important : libère la bannière quand la scène / l'objet est détruit
        if (bannerView != null)
        {
            bannerView.Destroy();
            bannerView = null;
        }

        if (retryCoroutine != null)
        {
            StopCoroutine(retryCoroutine);
            retryCoroutine = null;
        }
    }
}
