using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GoogleMobileAds.Ump.Api;
using GoogleMobileAds.Api;
using UnityEngine.UI;

public class GDPRmessage : MonoBehaviour
{
    [SerializeField, Tooltip("Button to show the privacy options form.")]
    public Button _privacyButton;

    void Start()
    {
        ConsentRequestParameters request = new ConsentRequestParameters
        {
            TagForUnderAgeOfConsent = false,
        };

        // Check the current consent information status.
        ConsentInformation.Update(request, OnConsentInfoUpdated);
    }

    void OnConsentInfoUpdated(FormError consentError)
    {
        if (consentError != null)
        {
            // Handle the error.
            UnityEngine.Debug.LogError(consentError);
            return;
        }

        // If the error is null, the consent information state was updated.
        // You are now ready to check if a form is available.
        ConsentForm.LoadAndShowConsentFormIfRequired((FormError formError) =>
        {
            if (formError != null)
            {
                // Consent gathering failed.
                UnityEngine.Debug.LogError(formError); // (참고: consentError -> formError로 수정함)
                return;
            }

            bool canRequestAds = ConsentInformation.CanRequestAds();

            //Debug.Log($"[GDPR Test] Can Request Ads 결과: {canRequestAds}");


            GoogleMobileAds.Mediation.UnityAds.Api.UnityAds.SetConsentMetaData("gdpr.consent", canRequestAds);
            GoogleMobileAds.Mediation.UnityAds.Api.UnityAds.SetConsentMetaData("privacy.consent", canRequestAds);
            // ---------------------------------------------------------
            if (canRequestAds)
            {
                var ads = GetComponent<AdmobADS>();
                if (ads != null) ads.InitializeAds();
                else
                {
                    // 못 찾았을 때만 경고 로그 띄우기
                   // Debug.LogWarning("AdmobADS 객체를 씬에서 찾을 수 없습니다.");
                }
            }

            if (_privacyButton != null)
            {
                bool isRequired = ConsentInformation.PrivacyOptionsRequirementStatus == PrivacyOptionsRequirementStatus.Required;
                _privacyButton.gameObject.SetActive(isRequired);
            }
        });
    }

    public void ShowPrivacyOptionsForm()
    {
        Debug.Log("Showing privacy options form.");

        ConsentForm.ShowPrivacyOptionsForm((FormError showError) =>
        {
            if (showError != null)
            {
               // Debug.LogError("Error showing privacy options form with error: " + showError.Message);
            }
            else
            {
                bool canRequestAds = ConsentInformation.CanRequestAds();

                GoogleMobileAds.Mediation.UnityAds.Api.UnityAds.SetConsentMetaData("gdpr.consent", canRequestAds);
                GoogleMobileAds.Mediation.UnityAds.Api.UnityAds.SetConsentMetaData("privacy.consent", canRequestAds);
                // ---------------------------------------------------------
            }
            if (_privacyButton != null)
            {
                bool isRequired = ConsentInformation.PrivacyOptionsRequirementStatus == PrivacyOptionsRequirementStatus.Required;
                _privacyButton.gameObject.SetActive(isRequired);
            }
        });
    }
}
