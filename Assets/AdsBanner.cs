using GoogleMobileAds.Api;
using UnityEngine;

public class AdsBanner : MonoBehaviour
{
    // Create a 320x50 banner at top of the screen.


   private void Start()
   {
       BannerView bannerView = new BannerView("ca-app-pub-3940256099942544/6300978111", AdSize.Banner, AdPosition.Bottom);

       // Send a request to load an ad into the banner view.
       bannerView.LoadAd(new AdRequest());
   }
   
}
