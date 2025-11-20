using GoogleMobileAds.Api;
using UnityEngine;

public class AdsBanner : MonoBehaviour
{
    // Create a 320x50 banner at top of the screen.


   private void Start()
   {
       BannerView bannerView = new BannerView("ca-app-pub-1810486296187934/1500572514", AdSize.Banner, AdPosition.Bottom);

       // Send a request to load an ad into the banner view.
       bannerView.LoadAd(new AdRequest());
   }
   
}
