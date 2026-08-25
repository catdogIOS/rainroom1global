using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GoogleMobileAds.Api;
using UnityEngine.UI;
using System;
using GoogleMobileAds.Api.Mediation.UnityAds;


public class AdmobADS : MonoBehaviour
{

    //영상
    private RewardedAd rewardedAd, rewardedAdout;
    private string _rewardedAdUnitId;


    //외출 광고
    private string _GoOutADSid;

    Color color;
    public GameObject Toast_obj; ////blackimg
    public Text adPop_txt;
    public Button cutTime_btn;

    System.DateTime now;
    System.DateTime lastDateTimenow;

    public GameObject GM;

    //  중요: 두 광고의 보상 획득 여부를 메인 스레드로 전달하기 위한 플래그 분리
    private bool isFirstAdRewardPending = false;
    private bool isSecondAdRewardPending = false;

    private bool isReloadPending = false;
    private bool isReloadInterstitialPending = false;

    private int loadFailCount = 0;
    private int loadFailCountInterstitial = 0;

    // 기존 플래그들 아래에 추가
    private bool isFirstAdLoadSuccessPending = false;
    private bool isSecondAdLoadSuccessPending = false;

    // 애드몹 초기화 상태를 저장할 변수 추가
    private bool isAdmobInitialized = false;
    private bool isInitializing = false;
    private Coroutine networkRoutine = null;
    private Coroutine initTimeoutRoutine = null;
    private bool isInitCompletePending = false;

    private bool isRewardedAdLoading = false;
    private bool isInterstitialAdLoading = false;

    public GameObject adsBtn;
    private Button adsBtnComponent;

    private void Awake()
    {
        adsBtnComponent = adsBtn.GetComponent<Button>();
    }

    // Use this for initialization 앱 ID
    void Start()
    {
        color = new Color(1f, 1f, 1f);

        _rewardedAdUnitId = "ca-app-pub-9179569099191885/7953689433";
        _GoOutADSid = "ca-app-pub-9179569099191885/3126866600";

       // InitializeAds();

        if (PlayerPrefs.GetInt("outtimecut", 0) == 4 && PlayerPrefs.GetInt("scene", 0) == 0)
        {
            cutTime_btn.interactable = false;
        }
    }

    // 3초마다 인터넷이 켜졌는지 확인하는 감시자 역할
    private IEnumerator CheckNetworkRoutine()
    {
        // 애드몹이 초기화되지 않은 동안에만 무한 반복
        while (!isAdmobInitialized)
        {
            yield return new WaitForSeconds(3f); // 3초 쉬고 

            if (isInitializing) continue;

            // 인터넷이 켜졌는지 다시 확인
            if (Application.internetReachability != NetworkReachability.NotReachable)
            {
                //Debug.Log("인터넷 연결 감지! 애드몹 초기화를 시작합니다.");
                InitializeAds(); // 연결되었으니 다시 초기화 시도
            }
        }
        networkRoutine = null;
    }

    public void OnButtonClick()
    {
        MobileAds.OpenAdInspector((AdInspectorError error) =>
        {
            // Error will be set if there was an issue and the inspector was not displayed.
        });
    }
    private void Update()
    {
        if (isFirstAdRewardPending)
        {
            isFirstAdRewardPending = false;
            ExecuteFirstAdReward(); // 메인 스레드에서 안전하게 실행!
        }

        if (isSecondAdRewardPending)
        {
            isSecondAdRewardPending = false;
            ExecuteSecondAdReward(); // 메인 스레드에서 안전하게 실행!
        }

        if (isReloadPending)
        {
            isReloadPending = false;
            if (adsBtnComponent != null) adsBtnComponent.interactable = false;

            if (!IsInvoking("LoadRewardedAd")) // ← 이미 예약됐는지 체크
            {
                float delay = Mathf.Min(1f * Mathf.Pow(2, loadFailCount), 30f); // 최대 30초
                loadFailCount++;
                Invoke("LoadRewardedAd", delay);
            }
        }

        if (isReloadInterstitialPending)
        {
            isReloadInterstitialPending = false;
            if (cutTime_btn != null) cutTime_btn.interactable = false;

            if (!IsInvoking("LoadRewardedAd2")) // ← 이미 예약됐는지 체크
            {
                float delay = Mathf.Min(1f * Mathf.Pow(2, loadFailCountInterstitial), 30f); // 최대 30초
                loadFailCountInterstitial++;
                Invoke("LoadRewardedAd2", delay);
            }
        }

        // 기존 if문들 아래에 추가
        if (isFirstAdLoadSuccessPending)
        {
            isFirstAdLoadSuccessPending = false;
            adsBtnComponent.interactable = true;
        }

        if (isSecondAdLoadSuccessPending)
        {
            isSecondAdLoadSuccessPending = false;
            if (cutTime_btn != null) // 먼저 버튼이 존재하는지 확인
            {
                if (PlayerPrefs.GetInt("outtimecut", 0) != 4)
                    cutTime_btn.interactable = true;
            }
        }

        if (isInitCompletePending)
        {
            isInitCompletePending = false;

            isAdmobInitialized = true;
            isInitializing = false;

            if (initTimeoutRoutine != null)
            {
                StopCoroutine(initTimeoutRoutine);
                initTimeoutRoutine = null;
            }

            // 광고 로드 시작
            LoadRewardedAd();
            LoadRewardedAd2();
        }

    }


    public void InitializeAds()
    {
        // 이미 초기화가 끝났거나, 현재 초기화가 진행 중이면 아무것도 안 하고 돌아감
        if (isAdmobInitialized || isInitializing) return;

        if (Application.internetReachability != NetworkReachability.NotReachable) //인터넷연결된경우?
        {
            isInitializing = true; // 잠금장치 ON (초기화 시작)
            initTimeoutRoutine = StartCoroutine(InitTimeoutRoutine());

            MobileAds.Initialize((InitializationStatus initStatus) =>
            {
                if (isAdmobInitialized) return; // 이미 다른 시도로 초기화 완료된 경우 무시
                isInitCompletePending = true;
            });


        }
        else
        {
            adsBtnComponent.interactable = false; // 인터넷 없으면 비활성화
            if (cutTime_btn != null)
                cutTime_btn.interactable = false;
            if (networkRoutine == null)
            {
                //Debug.Log("인터넷 없음. 3초마다 재연결을 확인합니다.");
                networkRoutine = StartCoroutine(CheckNetworkRoutine());
            }
        }
    }


    public void LoadRewardedAd()
    {
        adsBtnComponent.interactable = false;
        if (isRewardedAdLoading) return;

        isRewardedAdLoading = true; // 로딩 시작
        if (rewardedAd != null)
        {
            rewardedAd.Destroy();
            rewardedAd = null;
        }

        //Debug.Log("Loading the rewarded ad.");

        // create our request used to load the ad.
        var adRequest = new AdRequest();

        // send the request to load the ad.
        RewardedAd.Load(_rewardedAdUnitId, adRequest,
            (RewardedAd ad, LoadAdError error) =>
            {
                isRewardedAdLoading = false; // 로드 완료(또는 실패) 시 플래그 해제
                // if error is not null, the load request failed.
                if (error != null || ad == null)
                {
                    //Debug.Log("광고 로드 실패 재시도");
                    isReloadPending = true; // 여기서도 플래그를 세워주면 무한 동력 완성!
                    adsBtnComponent.interactable = true; // true냐 false냐 선택알아서
                    return;
                }

                //Debug.Log("Rewarded ad loaded with response : " + ad.GetResponseInfo());

                loadFailCount = 0;
                rewardedAd = ad;
                RegisterEventHandlers(ad); //이벤트 등록
                isFirstAdLoadSuccessPending = true;
            });

    }


    private void RegisterEventHandlers(RewardedAd ad)
    {
        ad.OnAdFullScreenContentClosed += () =>
        {
            isReloadPending = true; // 플래그만 세움, 여기서 직접 호출 X
            //Debug.Log("광고 종료");
        };
        ad.OnAdFullScreenContentFailed += (AdError error) =>
        {
            isReloadPending = true; // 플래그만 세움, 여기서 직접 호출 X
        };
    }




    public void showAdmobVideo()
    {
        //Debug.Log("상태보기 : " + rewardedAd);

        if (PlayerPrefs.GetInt("talk", 5) >= 5)
        {
            Toast_obj.SetActive(true);
            adPop_txt.text = "Number of Talk's already the max," + "\n" + "so you can't.";
        }
        else
        {
            if (rewardedAd != null && rewardedAd.CanShowAd())
            {
                adsBtnComponent.interactable = false; // ← 광고 표시 직전 비활성화
                //blackimg.SetActive(true);
                rewardedAd.Show((Reward reward) =>
                {
                    isFirstAdRewardPending = true; // Update()로 신호만 보냄
                });

            }
            else
            {
                Toast_obj.SetActive(true);
                adPop_txt.text = "Can't see it yet." + "\n" + "Try later.";
                LoadRewardedAd();
            }
        }
    }

    // 메인 스레드에서 안전하게 실행될 첫 번째 보상 로직
    private void ExecuteFirstAdReward()
    {
        lastDateTimenow = System.DateTime.UtcNow;
        int sceneIndex = PlayerPrefs.GetInt("scene", 0);

        if (sceneIndex == 2) PlayerPrefs.SetString("adtimespark", lastDateTimenow.ToString("o"));
        else if (sceneIndex == 3) PlayerPrefs.SetString("adtimescity", lastDateTimenow.ToString("o"));
        else PlayerPrefs.SetString("adtimes", lastDateTimenow.ToString("o"));

        Toast_obj.SetActive(true);
        adPop_txt.text = "Number of talk has been reset to 5.";

        GM.GetComponent<ShowAds>().AdReward();
        PlayerPrefs.SetInt("talk", 5);
        PlayerPrefs.Save();
    }

    public void LoadRewardedAd2()
    {
        if (cutTime_btn != null)
        {
            cutTime_btn.interactable = false;
        }
        if (isInterstitialAdLoading) return;

        isInterstitialAdLoading = true; // 로딩 시작
        if (rewardedAdout != null)
        {
            rewardedAdout.Destroy();
            rewardedAdout = null;
        }

        //Debug.Log("Loading the rewarded ad.");

        // create our request used to load the ad.
        var adRequest = new AdRequest();

        // send the request to load the ad.
        RewardedAd.Load(_GoOutADSid, adRequest,
            (RewardedAd ad, LoadAdError error) =>
            {
                isInterstitialAdLoading = false; // 로드 완료(또는 실패) 시 플래그 해제
                // if error is not null, the load request failed.
                if (error != null || ad == null)
                {
                    Debug.Log("광고 로드 실패, 재시도");
                    isReloadInterstitialPending = true;
                    if (cutTime_btn != null) // 먼저 버튼이 존재하는지 확인
                    {
                        if (PlayerPrefs.GetInt("outtimecut", 0) != 4)
                            cutTime_btn.interactable = true;
                    }
                    return;
                }

                loadFailCountInterstitial = 0;
                rewardedAdout = ad;
                RegisterEventHandlers2(ad); //이벤트 등록
                isSecondAdLoadSuccessPending = true;
            });

    }


    //보상형 전면 광고 보여주기
    public void ShowRewardedInterstitialAd()
    {
        //Debug.Log("상태보기 : " + rewardedInterstitialAd);

        if (rewardedAdout != null && rewardedAdout.CanShowAd())
        {
            if (cutTime_btn != null)
                cutTime_btn.interactable = false;
            rewardedAdout.Show((Reward reward) =>
            {
                isSecondAdRewardPending = true; // Update()로 신호만 보냄
            });
        }
        else
        {
            Toast_obj.SetActive(true);
            adPop_txt.text = "Can't see it yet." + "\n" + "Try later.";
            LoadRewardedAd2();
        }

    }
    // 메인 스레드에서 안전하게 실행될 두 번째 보상 로직 (UI 조작 포함)
    private void ExecuteSecondAdReward()
    {
        PlayerPrefs.SetInt("outtimecut", 4);
        PlayerPrefs.Save();

        if (cutTime_btn != null)
            cutTime_btn.interactable = false;
        Toast_obj.SetActive(true);
        adPop_txt.text = "Time needed to go out" + "\n" + "was reduced.";
    }

    private void RegisterEventHandlers2(RewardedAd ad)
    {
        ad.OnAdFullScreenContentClosed += () =>
        {
            isReloadInterstitialPending = true;
         //   Debug.Log("광고 종료");
        };
        ad.OnAdFullScreenContentFailed += (AdError error) =>
        {
            isReloadInterstitialPending = true;
        };
    }

    private void OnDestroy()
    {
        CancelInvoke();
        if (rewardedAd != null)
        {
            rewardedAd.Destroy();
            rewardedAd = null;
        }
        if (rewardedAdout != null)
        {
            rewardedAdout.Destroy();
            rewardedAdout = null;
        }
    }
    private void OnDisable()
    {
        if (networkRoutine != null)
        {
            StopCoroutine(networkRoutine); // 혹시 모를 찌꺼기 실행을 확실히 정지
            networkRoutine = null;         // 변수를 깨끗하게 청소!
        }
        if (initTimeoutRoutine != null)
        {
            StopCoroutine(initTimeoutRoutine);
            initTimeoutRoutine = null;
        }
    }

    // 초기화가 특정 시간 내에 안 끝나면 강제로 잠금을 풀어주는 코루틴
    private IEnumerator InitTimeoutRoutine()
    {
        yield return new WaitForSeconds(15f);
        if (isAdmobInitialized)
        {
            yield break;
        }
        if (isInitializing)
        {
            // Debug.Log("애드몹 초기화 타임아웃! 잠금을 해제하여 재시도를 허용합니다.");
            isInitializing = false;
        }

        if (networkRoutine == null)
        {
            networkRoutine = StartCoroutine(CheckNetworkRoutine());
        }
    }
    void OnApplicationPause(bool pause)
    {
        if (!pause && isAdmobInitialized) // 초기화 완료 후에만 체크
        {
            if (rewardedAd == null || !rewardedAd.CanShowAd())
            {
                LoadRewardedAd();
            }
            if (rewardedAdout == null || !rewardedAdout.CanShowAd())
            {
                LoadRewardedAd2();
            }
        }
    }
}



