using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Advertisements;
using UnityEngine.UI;

public class UnityADS : MonoBehaviour
{

    private string gameId = "4270906";//★ Window > Services 설정 테스트 바꿀것 (test용 1486550) //3657850
    public int soundck;
    public GameObject ad_obj, radio_ani, adBtn_obj;

    int sG, mG;
    int sG2, mG2;

    Color color;
    public GameObject Toast_obj;

    //스프라이트 이미지로 변경
    public Sprite radioMove1_spr, radioMove2_spr;



    public Text adPop_txt;
    public Button cutTime_btn;

    System.DateTime now;
    System.DateTime lastDateTimenow;

    public GameObject GM;

    public int init_i;

    // Use this for initialization
    void Start()
    {


        if (PlayerPrefs.GetInt("outtimecut", 4) == 4 && PlayerPrefs.GetInt("scene", 0) == 0)
        {

            cutTime_btn.interactable = false;
        }
        color = new Color(1f, 1f, 1f);


        Advertisement.Initialize(gameId, false); //true 테스트모드 **false로 꼭 변경할 ㄱ**
    }

    // Update is called once per frame

    public void ShowRewardedAd()
    {
        init_i = 0;
        if (Advertisement.IsReady("Rewarded_iOS"))
        {
            ShowOptions options = new ShowOptions { resultCallback = HandleShowResult };
            Advertisement.Show("Rewarded_iOS", options);
        }
        else
        {
            Toast_obj.SetActive(true);
            adPop_txt.text = "아직 볼 수 없다." + "\n" + " 나중에 시도해보자.";
        }
    }

    public void ShowRewardedAd2()
    {
        init_i = 2;
        if (Advertisement.IsReady("Rewarded_iOS"))
        {
            ShowOptions options = new ShowOptions { resultCallback = HandleShowResult };
            Advertisement.Show("Rewarded_iOS", options);
        }
        else
        {
            Toast_obj.SetActive(true);
            adPop_txt.text = "아직 볼 수 없다." + "\n" + " 나중에 시도해보자.";
        }
    }

    public void adYN()
    {
        PlayerPrefs.SetInt("adrunout", 0);
        ad_obj.SetActive(true);
    }
    public void closeAdYN()
    {
        ad_obj.SetActive(false);
    }
    public void adYes()
    {
        ShowRewardedAd();
        ad_obj.SetActive(false);
    }



    public void Admob()
    {
        radio_ani.SetActive(false);
        adBtn_obj.SetActive(false);
        StopCoroutine("adTimeFlow");
        StopCoroutine("adAniTime");
        StartCoroutine("adTimeFlow");
        StartCoroutine("adAniTime");
        PlayerPrefs.SetInt("talk", 5);
        PlayerPrefs.Save();
        if (PlayerPrefs.GetInt("talk", 5) >= 5)
        {
            PlayerPrefs.SetInt("secf", 240);
        }
    }



    IEnumerator adTimeFlow()
    {
        while (mG > -1)
        {
            sG = PlayerPrefs.GetInt("secf", 240);
            //Debug.Log(sG);
            mG = (int)(sG / 60);
            sG = sG - (sG / 60) * 60;
            if (sG < 0)
            {
                sG = 0;
                mG = 0;
            }
            else
            {
                radio_ani.SetActive(false);
                adBtn_obj.SetActive(false);
            }
            sG = PlayerPrefs.GetInt("secf", 240);
            sG = sG - 1;
            if (sG < 0)
            {
                sG = -1;
            }
            PlayerPrefs.SetInt("secf", sG);
            yield return new WaitForSeconds(1f);
            //Debug.Log("sg" + sG);
        }
    }
    IEnumerator adAniTime()
    {
        int w = 0;
        while (w == 0)
        {
            if (sG < 0)
            {
                if (PlayerPrefs.GetInt("outtrip", 0) == 1)
                {
                    radio_ani.SetActive(true);
                    adBtn_obj.SetActive(true);
                }
                else
                {
                    if (PlayerPrefs.GetInt("front", 0) == 1)
                    {
                        radio_ani.SetActive(true);
                        adBtn_obj.SetActive(true);
                    }
                    else
                    {
                        radio_ani.SetActive(false);
                        adBtn_obj.SetActive(false);
                    }
                }
            }
            yield return null;
        }

    }

    //이미지 1초마다 바꿔주기
    IEnumerator radioImageChange()
    {
        int rd = 0;
        int rdc = 0;
        while (rd == 0)
        {
            if (rdc == 0)
            {
                radio_ani.GetComponent<Image>().sprite = radioMove1_spr;
                rdc = 1;
            }
            else
            {
                radio_ani.GetComponent<Image>().sprite = radioMove2_spr;
                rdc = 0;
            }
            yield return new WaitForSeconds(1f);
        }
    }


    IEnumerator adTimeFlow2()
    {
        while (mG2 > -1)
        {
            sG2 = PlayerPrefs.GetInt("secf2", 240);
            //Debug.Log(sG);
            mG2 = (int)(sG2 / 60);
            sG2 = sG2 - (sG2 / 60) * 60;
            if (sG2 < 0)
            {
                sG2 = 0;
                mG2 = 0;
            }
            else
            {
                radio_ani.SetActive(false);
                adBtn_obj.SetActive(false);
            }
            sG2 = PlayerPrefs.GetInt("secf2", 240);
            sG2 = sG2 - 1;
            if (sG2 < 0)
            {
                sG2 = -1;
            }
            PlayerPrefs.SetInt("secf2", sG2);
            //Debug.Log("sg2" + sG2);
            yield return new WaitForSeconds(1f);
        }
    }
    IEnumerator adAniTime2()
    {
        int w = 0;
        while (w == 0)
        {
            if (sG2 < 0)
            {
                if (PlayerPrefs.GetInt("outtrip", 0) == 1)
                {
                }
                else if (PlayerPrefs.GetInt("front", 0) == 1)
                {
                    radio_ani.SetActive(true);
                    adBtn_obj.SetActive(true);
                }
                else
                {
                    radio_ani.SetActive(false);
                    adBtn_obj.SetActive(false);
                }

            }

            yield return null;
        }

    }

    //시청보상
    public void HandleShowResult(ShowResult result)
    {
        if (result == ShowResult.Finished)
        { 

            if (init_i == 0)
            {
                lastDateTimenow = System.DateTime.Now;
                if (PlayerPrefs.GetInt("scene", 0) == 2)
                {
                    PlayerPrefs.SetString("adtimespark", lastDateTimenow.ToString());
                }
                else if (PlayerPrefs.GetInt("scene", 0) == 3)
                {
                    PlayerPrefs.SetString("adtimescity", lastDateTimenow.ToString());
                }
                else if (PlayerPrefs.GetInt("scene", 0) == 0)
                {
                    PlayerPrefs.SetString("adtimes", lastDateTimenow.ToString());
                }
                else
                {
                    PlayerPrefs.SetString("adtimes", lastDateTimenow.ToString());
                }
                GM.GetComponent<ShowAds>().AdReward();
                PlayerPrefs.SetInt("talk", 5);
            }
            else if (init_i == 2)
            {
                PlayerPrefs.SetInt("outtimecut", 4);
                cutTime_btn.interactable = false;
            }
        }
    }

}
