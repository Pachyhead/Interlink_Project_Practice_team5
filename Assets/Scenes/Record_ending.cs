using UnityEngine;
using UnityEngine.UI;
using System;
using System.Data;
using Mono.Data.Sqlite;
using System.Collections.Generic;
using TMPro;

public class ImageSwitcher : MonoBehaviour
{
    private const float NO_DATA = -1f;
    void Start()
    {
        float[] rates = LoadData();
        UpdateZone1(rates[0]); // 40f
        UpdateZone2(rates[1]); // 60f
        UpdateZone3(rates[2]); // 85f
    } //임시 테스트용
    // ▼▼▼ 1. 구역별 UI Image 컴포넌트 변수 (3개) ▼▼▼
    [Header("Zone Targets")]
    public Image zone1Image;
    public Image zone2Image;
    public Image zone3Image;

    // ▼▼▼ 2. 모든 구역이 공유할 Sprite 에셋 변수 (3개) ▼▼▼
    [Header("Shared Sprites (All Zones Use These)")]
    public Sprite goodSprite;    // 80% 이상
    public Sprite normalSprite;  // 50% 이상
    public Sprite badSprite;     // 50% 미만

    [Header("Zone Targets (Texts)")]
    public TextMeshProUGUI zone1Text;
    public TextMeshProUGUI zone2Text;
    public TextMeshProUGUI zone3Text;
    // ------------------------------------------------------------------
    // 핵심 로직: 정답률에 따라 이미지를 설정하는 재활용 함수
    // ------------------------------------------------------------------
    // 이제 SetImageBasedOnRate 함수는 공유 Sprite만 사용합니다.
    private void SetImageBasedOnRate(Image target, TextMeshProUGUI targetText, float rate)
    {
        if (target == null) return;
        if (rate == NO_DATA)
        {
            if (targetText != null)
            {
                targetText.gameObject.SetActive(true);
                targetText.transform.SetAsLastSibling();
                targetText.text = "기록\n없음"; // 줄바꿈 포함
                targetText.color = Color.black; // 회색 처리
            }
            return;
        }

        if (rate >= 80f)
        {
            target.sprite = goodSprite; // 공유 good Sprite 사용
        }
        else if (rate >= 50f)
        {
            target.sprite = normalSprite; // 공유 normal Sprite 사용
        }
        else
        {
            target.sprite = badSprite; // 공유 bad Sprite 사용
        }
    }

    // ------------------------------------------------------------------
    // ▼▼▼ 3. 외부에서 호출할 구역별 함수 (코드는 더 간결해짐) ▼▼▼
    // ------------------------------------------------------------------

    public void UpdateZone1(float rate)
    {
        SetImageBasedOnRate(zone1Image, zone1Text, rate);
    }

    public void UpdateZone2(float rate)
    {
        SetImageBasedOnRate(zone2Image, zone2Text, rate);
    }

    public void UpdateZone3(float rate)
    {
        SetImageBasedOnRate(zone3Image, zone3Text, rate);
    }

    // ------------------------------------------------------------------
    // ▼▼▼ 4. DB에서 정답률 가져오기 ▼▼▼
    // ------------------------------------------------------------------ 

    public float[] LoadData()
    {
        float[] rates = new float[3];

        // 1. 일단 배열을 모두 '데이터 없음(-1)'으로 초기화
        for (int i = 0; i < rates.Length; i++)
        {
            rates[i] = -1f; // NO_DATA
        }
        System.Collections.Generic.List<float> tempList = new System.Collections.Generic.List<float>();

        string connectionString = "URI=file:" + Application.streamingAssetsPath + "/test.db";
        IDbConnection dbConnection = new SqliteConnection(connectionString);
        dbConnection.Open();

        string tablename = "Record";

        IDbCommand dbCommand = dbConnection.CreateCommand();
        dbCommand.CommandText = "SELECT rate_total FROM " + tablename + " ORDER BY session_id DESC LIMIT 3"; // 최근 3개(최대)의 세션만 가져오기
        IDataReader dataReader = dbCommand.ExecuteReader();

        while (dataReader.Read())
        {
            if(!dataReader.IsDBNull(0))
            {
                float rateSession = dataReader.GetFloat(0);
                tempList.Add(rateSession);
            }
        }

        dataReader.Close();
        dbConnection.Close();

        tempList.Reverse();

        for (int i = 0; i < tempList.Count; i++)
        {
            rates[i] = tempList[i];
        }

        return rates;
    }
}
