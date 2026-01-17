using System;
using System.Collections.Generic;
using GoogleMobileAds.Ump.Api;
using UnityEngine;

namespace TapOrb.Ads
{
    public sealed class AdsConsentService
    {
        public struct Options
        {
            public bool TagForUnderAgeOfConsent;
            public bool ForceDebugGeographyEea;
            public List<string> TestDeviceHashedIds;
        }

        public struct Result
        {
            public bool CanRequestAds;
            public ConsentStatus Status;
            public bool? FormWasShown;
            public FormError Error;
        }

        public bool IsRequestInProgress { get; private set; }
        public bool ConsentFlowCompleted { get; private set; }

        public void RequestConsent(Options options, Action<Result> onComplete)
        {
#if !UNITY_ANDROID && !UNITY_IOS
            ConsentFlowCompleted = true;
            IsRequestInProgress = false;
            onComplete?.Invoke(new Result
            {
                CanRequestAds = true,
                Status = ConsentStatus.NotRequired,
                FormWasShown = null,
                Error = null
            });
            return;
#else
            if (IsRequestInProgress)
            {
                Debug.Log("[AdsConsent] Consent flow déjà en cours.");
                return;
            }

            IsRequestInProgress = true;
            ConsentFlowCompleted = false;

            ConsentDebugSettings debugSettings = null;
            if (options.ForceDebugGeographyEea)
            {
                debugSettings = new ConsentDebugSettings
                {
                    DebugGeography = DebugGeography.EEA,
                    TestDeviceHashedIds = options.TestDeviceHashedIds ?? new List<string>()
                };
            }

            var requestParameters = new ConsentRequestParameters
            {
                TagForUnderAgeOfConsent = options.TagForUnderAgeOfConsent
            };

            if (debugSettings != null)
            {
                requestParameters.ConsentDebugSettings = debugSettings;
            }

            Debug.Log("[AdsConsent] Update consent info...");
            ConsentInformation.Update(requestParameters, updateError =>
            {
                if (updateError != null)
                {
                    Debug.LogError($"[AdsConsent] Update consent error: code={updateError.ErrorCode} msg={updateError.Message}");
                    Finish(updateError, false, onComplete);
                    return;
                }

                Debug.Log("[AdsConsent] Load/Show consent form si requis...");
                var formRequired = ConsentInformation.ConsentStatus == ConsentStatus.Required;
                ConsentForm.LoadAndShowConsentFormIfRequired(formError =>
                {
                    Finish(formError, formRequired, onComplete);
                });
            });
#endif
        }

        public void ShowPrivacyOptionsForm(Action<FormError> onComplete)
        {
#if UNITY_ANDROID || UNITY_IOS
            ConsentForm.ShowPrivacyOptionsForm(onComplete);
#else
            onComplete?.Invoke(null);
#endif
        }

        public void ResetConsent()
        {
#if UNITY_ANDROID || UNITY_IOS
            ConsentInformation.Reset();
#endif
        }

        private void Finish(FormError error, bool? formShown, Action<Result> onComplete)
        {
            IsRequestInProgress = false;
            ConsentFlowCompleted = true;
            onComplete?.Invoke(new Result
            {
                CanRequestAds = ConsentInformation.CanRequestAds(),
                Status = ConsentInformation.ConsentStatus,
                FormWasShown = formShown,
                Error = error
            });
        }
    }
}
