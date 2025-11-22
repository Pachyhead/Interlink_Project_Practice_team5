using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Data;
using Mono.Data.Sqlite;
using System.Collections.Generic;
using System.Linq;

public class LinearGraph : MonoBehaviour
{
    [Header("연결 설정")]
    [SerializeField] private RectTransform graphContainer;
    [SerializeField] private Sprite circleSprite;
    [SerializeField] private RectTransform labelYTemplate;
    [SerializeField] private Image guidelineTemplate;
    [SerializeField] private RectTransform labelXTemplate; // [추가] X축 라벨 템플릿

    [Header("정답률 데이터(0 ~ 100)")]
    public float[] answerRates;

    [Header("X축 설정")] // [추가]
    public string[] xAxisLabels; // 구역 이름 텍스트 (예: "구역 1", "구역 2", "구역 3")
    public float xAxisLabelOffset = 20f; // X축 라벨이 그래프에서 아래로 떨어지는 거리

    [Header("디자인 옵션")]
    public Color graphColor = Color.green;
    public float dotSize = 20f;
    public float lineThickness = 7f;

    [Header("여백 설정")]
    public float verticalPaddingRatio = 0.03f;
    public float horizontalPaddingRatio = 0.03f;

    [Header("Y축 설정")]
    public int yAxisInterval = 10;
    public float yAxisLabelOffset = 50f;
    public Color guidelineColor = new Color(0, 0, 0, 0.5f);
    public float labelFontSize = 21f;

    private void Start()
    {
        // 템플릿 비활성화 확인
        if (labelYTemplate != null) labelYTemplate.gameObject.SetActive(false);
        if (guidelineTemplate != null) guidelineTemplate.gameObject.SetActive(false);
        if (labelXTemplate != null) labelXTemplate.gameObject.SetActive(false); // [추가]


        // load from db
        float[] recordData = new float[3];
        
        string sceneName = graphContainer.gameObject.name;
        if (sceneName == "Scenario1")
        {
            recordData = LoadData(1);
        }
        else if (sceneName == "Scenario2")
        {
            recordData = LoadData(2);
        }
        else if (sceneName == "Scenario3")
        {
            recordData = LoadData(3);
        }
        else if (sceneName == "Total")
        {
            recordData = LoadData();
        }
        ShowGraph(recordData);

        //if (answerRates != null && answerRates.Length > 0)
        //    ShowGraph(answerRates);
        //else
        //    ShowGraph(new float[] { 10, 50, 30 }); // 3개 요소 테스트용
    }

    public void ShowGraph(float[] valueList)
    {
        // 기존 그래프 요소들 삭제 (생략)
        foreach (Transform child in graphContainer)
        {
            if (child.gameObject == labelYTemplate.gameObject ||
                child.gameObject == guidelineTemplate.gameObject ||
                child.gameObject == labelXTemplate.gameObject) continue; // [수정]
            Destroy(child.gameObject);
        }

        if (graphContainer == null || valueList.Length == 0) return;

        // 1. 전체 높이/너비 및 여백 계산
        float containerHeight = graphContainer.rect.height;
        float containerWidth = graphContainer.rect.width;

        float verticalPadding = containerHeight * verticalPaddingRatio;
        float horizontalPadding = containerWidth * horizontalPaddingRatio;

        float effectiveGraphHeight = containerHeight - (verticalPadding * 2);
        float effectiveGraphWidth = containerWidth - (horizontalPadding * 2);

        float yOffset = verticalPadding;
        float xOffset = horizontalPadding;

        float yMaximum = 100f;
        float xSize = effectiveGraphWidth / (valueList.Length + 1);

        GameObject lastCircle = null;

        // Y축 라벨 및 가이드 라인 생성
        for (int i = 0; i <= yMaximum; i += yAxisInterval)
        {
            float yPosition = (i / yMaximum) * effectiveGraphHeight + yOffset;

            // 라벨 생성
            CreateYAxisLabel(i.ToString(), new Vector2(xOffset - yAxisLabelOffset, yPosition));

            // 가이드 라인 생성 (0점 라인은 제외하거나 다르게 표현 가능)
            if (i >= 0) // 0점 라인은 그리지 않거나 필요에 따라 추가
            {
                CreateGuideline(new Vector2(xOffset, yPosition), effectiveGraphWidth);
            }
        }
        // 그래프 점 및 선 그리기 + X축 라벨 생성
        for (int i = 0; i < valueList.Length; i++)
        {
            float xPosition = xOffset + (i + 1) * xSize;
            float yPosition = (valueList[i] / yMaximum) * effectiveGraphHeight + yOffset;

            GameObject circle = CreateCircle(new Vector2(xPosition, yPosition));

            // [추가] X축 라벨 생성 (데이터 개수와 라벨 개수가 맞아야 함)
            if (xAxisLabels != null && xAxisLabels.Length > i)
            {
                // 라벨 위치는 그래프 아래쪽 여백(yOffset)에서 다시 아래로 xAxisLabelOffset 만큼 이동
                Vector2 labelPosition = new Vector2(xPosition, yOffset - xAxisLabelOffset);
                CreateXAxisLabel(xAxisLabels[i], labelPosition);
            }

            if (lastCircle != null)
            {
                CreateDotConnection(lastCircle.GetComponent<RectTransform>().anchoredPosition,
                                     circle.GetComponent<RectTransform>().anchoredPosition);
            }
            lastCircle = circle;
        }
    }

    // ... (CreateCircle, CreateDotConnection, CreateYAxisLabel 함수는 생략 / 변경 없음)

    // [새 함수] X축 라벨 생성
    private void CreateXAxisLabel(string labelText, Vector2 anchoredPosition)
    {
        if (labelXTemplate == null) { Debug.LogError("X축 라벨 템플릿이 설정되지 않았습니다."); return; }

        GameObject labelObj = Instantiate(labelXTemplate.gameObject, graphContainer);
        labelObj.SetActive(true);
        RectTransform rect = labelObj.GetComponent<RectTransform>();
        TextMeshProUGUI tmpText = labelObj.GetComponent<TextMeshProUGUI>();

        if (tmpText == null) { Debug.LogError("X축 라벨 템플릿에 TextMeshProUGUI 컴포넌트가 없습니다."); return; }

        tmpText.text = labelText;
        tmpText.fontSize = labelFontSize;
        tmpText.color = Color.black;
        tmpText.alignment = TextAlignmentOptions.Center; // [중요] 중앙 정렬

        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = new Vector2(150, labelFontSize * 1.5f);
        rect.anchorMin = new Vector2(0, 0);
        rect.anchorMax = new Vector2(0, 0);
    }

    private GameObject CreateCircle(Vector2 anchoredPosition)
    {
        GameObject circleObj = new GameObject("circle", typeof(Image));
        circleObj.transform.SetParent(graphContainer, false);
        circleObj.GetComponent<Image>().sprite = circleSprite;
        circleObj.GetComponent<Image>().color = graphColor;

        RectTransform rect = circleObj.GetComponent<RectTransform>();
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = new Vector2(dotSize, dotSize);
        rect.anchorMin = new Vector2(0, 0);
        rect.anchorMax = new Vector2(0, 0);

        return circleObj;
    }

    private void CreateDotConnection(Vector2 dotPositionA, Vector2 dotPositionB)
    {
        GameObject lineObj = new GameObject("dotConnection", typeof(Image));
        lineObj.transform.SetParent(graphContainer, false);
        lineObj.GetComponent<Image>().color = new Color(graphColor.r, graphColor.g, graphColor.b, 0.5f);

        RectTransform rect = lineObj.GetComponent<RectTransform>();
        Vector2 dir = (dotPositionB - dotPositionA).normalized;
        float distance = Vector2.Distance(dotPositionA, dotPositionB);

        rect.anchorMin = new Vector2(0, 0);
        rect.anchorMax = new Vector2(0, 0);
        rect.sizeDelta = new Vector2(distance, lineThickness);
        rect.anchoredPosition = dotPositionA + dir * distance * 0.5f;

        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        rect.localEulerAngles = new Vector3(0, 0, angle);
    }

    // [새 함수] Y축 라벨 생성
    private void CreateYAxisLabel(string labelText, Vector2 anchoredPosition)
    {
        if (labelYTemplate == null) { Debug.LogError("Y축 라벨 템플릿이 설정되지 않았습니다."); return; }

        GameObject labelObj = Instantiate(labelYTemplate.gameObject, graphContainer);
        labelObj.SetActive(true);
        RectTransform rect = labelObj.GetComponent<RectTransform>();
        TextMeshProUGUI tmpText = labelObj.GetComponent<TextMeshProUGUI>();

        if (tmpText == null) { Debug.LogError("Y축 라벨 템플릿에 TextMeshProUGUI 컴포넌트가 없습니다."); return; }

        tmpText.text = labelText;
        tmpText.fontSize = labelFontSize;
        tmpText.color = Color.black; // 라벨 색상
        tmpText.alignment = TextAlignmentOptions.Right; // 오른쪽 정렬 (그래프에서 왼쪽으로 나오게)

        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = new Vector2(100, labelFontSize * 1.5f); // 라벨 크기
        rect.anchorMin = new Vector2(0, 0);
        rect.anchorMax = new Vector2(0, 0);
    }

    // [새 함수] 가이드 라인 생성
    private void CreateGuideline(Vector2 startPosition, float width)
    {
        if (guidelineTemplate == null) { Debug.LogError("가이드 라인 템플릿이 설정되지 않았습니다."); return; }

        GameObject lineObj = Instantiate(guidelineTemplate.gameObject, graphContainer);
        lineObj.SetActive(true);
        lineObj.GetComponent<Image>().color = guidelineColor; // 투명도 있는 색상

        RectTransform rect = lineObj.GetComponent<RectTransform>();
        rect.anchoredPosition = new Vector2(startPosition.x + width / 2, startPosition.y); // 라인 중앙 정렬
        rect.sizeDelta = new Vector2(width, 2f); // 라인 길이와 두께
        rect.anchorMin = new Vector2(0, 0);
        rect.anchorMax = new Vector2(0, 0);
    }
    // ... (기존 CreateCircle, CreateDotConnection, CreateYAxisLabel, CreateGuideline 함수 포함)

    // load from db(scenario ver)
    private float[] LoadData(int sceneNum)
    {
        System.Collections.Generic.List<float> rateList = new System.Collections.Generic.List<float>();  

        string dbname = "/test.db";
        string connectionString = "URI=file:" + Application.streamingAssetsPath + dbname;
        IDbConnection dbConnection = new SqliteConnection(connectionString);
        dbConnection.Open();

        string tablename = "Record";

        IDbCommand dbCommand = dbConnection.CreateCommand();

        dbCommand.CommandText = "SELECT rate_stage_" + (2*sceneNum-1) + " FROM " + tablename + " ORDER BY session_id DESC LIMIT 3"; // get scenario rate(1 or 2 or 3) for latest 3 session
        Debug.Log(dbCommand.CommandText);

        IDataReader dataReader = dbCommand.ExecuteReader();

        while (dataReader.Read())
        {
            if (!dataReader.IsDBNull(0))
            {
                float rateStage = dataReader.GetFloat(0);
                rateList.Add(rateStage);
            }
            else
            {
                Debug.Log("stage " + (2 * sceneNum - 1) + "'s data is empty");
            }
        }
        dataReader.Close();
        dbConnection.Close();
        if(rateList.Any())
        {
            rateList.Reverse();
        }

        return rateList.ToArray();
    }
    
    // load from db(total ver)
    private float[] LoadData()
    {
        System.Collections.Generic.List<float> rateList = new System.Collections.Generic.List<float>();

        string dbname = "/test.db";
        string connectionString = "URI=file:" + Application.streamingAssetsPath + dbname;
        IDbConnection dbConnection = new SqliteConnection(connectionString);
        dbConnection.Open();

        string tablename = "Record";

        IDbCommand dbCommand = dbConnection.CreateCommand();

        dbCommand.CommandText = "SELECT rate_total FROM " + tablename + " ORDER BY session_id DESC LIMIT 3"; // get scenario rate(1 or 2 or 3) for latest 3 session
        Debug.Log(dbCommand.CommandText);

        IDataReader dataReader = dbCommand.ExecuteReader();

        while (dataReader.Read())
        {
            if (!dataReader.IsDBNull(0))
            {
                float rateStage = dataReader.GetFloat(0);
                rateList.Add(rateStage);
            }
            else
            {
                Debug.Log("total data is empty");
            }
        }
        dataReader.Close();
        dbConnection.Close();

        if (rateList.Any())
        {
            rateList.Reverse();
        }

        return rateList.ToArray();
    }
}
