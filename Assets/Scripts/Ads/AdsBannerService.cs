using System.Collections;
using GoogleMobileAds.Api;
using UnityEngine;

namespace TapOrb.Ads
{
    public sealed class AdsBannerService
    {
        private readonly MonoBehaviour owner;
        private readonly string adUnitId;
        private BannerView bannerView;
        private bool isLoading;
        private int retryAttempt;
        private Coroutine retryCoroutine;

        public AdsBannerService(MonoBehaviour owner, string adUnitId)
        {
            this.owner = owner;
            this.adUnitId = adUnitId;
        }

        public void Load()
        {
#if !UNITY_ANDROID && !UNITY_IOS
            return;
#else
            if (string.IsNullOrWhiteSpace(adUnitId))
            {
                Debug.LogWarning("[AdsBanner] Aucun adUnitId configuré.");
                return;
            }

            if (isLoading)
            {
                Debug.Log("[AdsBanner] Chargement déjà en cours.");
                return;
            }

            if (retryCoroutine != null)
            {
                owner.StopCoroutine(retryCoroutine);
                retryCoroutine = null;
            }

            if (bannerView == null)
            {
               // var adaptiveSize = AdSize.GetCurrentOrientationAnchoredAdaptiveBannerAdSizeWithWidth(AdSize.FullWidth);
                bannerView = new BannerView(adUnitId, AdSize.Banner, AdPosition.Bottom);

                bannerView.OnBannerAdLoaded += () =>
                {
                    Debug.Log("[AdsBanner] Bannière chargée.");
                    isLoading = false;
                    retryAttempt = 0;
                };
                bannerView.OnBannerAdLoadFailed += error =>
                {
                    Debug.LogError("[AdsBanner] Erreur lors du chargement : " + error);
                    isLoading = false;
                    ScheduleRetry();
                };
                bannerView.OnAdImpressionRecorded += () =>
                {
                    Debug.Log("[AdsBanner] Impression enregistrée.");
                };
                bannerView.OnAdClicked += () =>
                {
                    Debug.Log("[AdsBanner] Clic enregistré.");
                };
            }

            Debug.Log("[AdsBanner] Chargement bannière.");
            isLoading = true;
            var request = new AdRequest();
            bannerView.LoadAd(request);
#endif
        }

        public void Show()
        {
#if UNITY_ANDROID || UNITY_IOS
            bannerView?.Show();
#endif
        }

        public void Hide()
        {
#if UNITY_ANDROID || UNITY_IOS
            bannerView?.Hide();
#endif
        }

        public void Destroy()
        {
#if UNITY_ANDROID || UNITY_IOS
            if (bannerView != null)
            {
                bannerView.Destroy();
                bannerView = null;
            }

            if (retryCoroutine != null)
            {
                owner.StopCoroutine(retryCoroutine);
                retryCoroutine = null;
            }
#endif
        }

        private void ScheduleRetry()
        {
            float delay = Mathf.Min(30f * Mathf.Pow(2f, retryAttempt), 300f);
            retryAttempt++;
            Debug.Log($"[AdsBanner] Retry dans {delay}s.");
            retryCoroutine = owner.StartCoroutine(RetryAfterDelay(delay));
        }

        private IEnumerator RetryAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            retryCoroutine = null;
            Load();
        }
    }
}
