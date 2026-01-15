using GoogleMobileAds.Api;
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

    private void Start()
    {
        // (Optionnel mais recommandé) initialise le SDK si ce n'est pas déjà fait ailleurs.
        // MobileAds.Initialize(_ => { });

        bannerView = new BannerView(AdUnitId, AdSize.Banner, AdPosition.Bottom);

        var request = new AdRequest();
        bannerView.LoadAd(request);
    }

    private void OnDestroy()
    {
        // Important : libère la bannière quand la scène / l'objet est détruit
        if (bannerView != null)
        {
            bannerView.Destroy();
            bannerView = null;
        }
    }
}