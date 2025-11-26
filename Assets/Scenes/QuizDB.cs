using UnityEngine;
using System.Collections.Generic;
using System.Data;
using Mono.Data.Sqlite;

public class QuizDB
{
    // 매개변수로 시나리오 ID를 받도록 수정
    public List<KeyValuePair<int, string>> LoadQuestions(int targetScenarioId)
    {
        List<KeyValuePair<int, string>> qList = new List<KeyValuePair<int, string>>();

        // DB 경로 설정 (Android/PC 분기 처리가 필요할 수 있음)
        string connStr = "URI=file:" + Application.streamingAssetsPath + "/test.db";

        using (IDbConnection conn = new SqliteConnection(connStr))
        {
            conn.Open();
            using (IDbCommand cmd = conn.CreateCommand())
            {
                // [핵심 변경] WHERE 절을 추가하여 특정 scenario_id만 가져옵니다.
                // question_id와 content를 가져와서 KeyValuePair로 만듭니다.
                cmd.CommandText = "SELECT question_id, content FROM Question WHERE scenario_id = " + targetScenarioId + " ORDER BY RANDOM() LIMIT 3";
                using (IDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        // 0번째: question_id, 1번째: content
                        int id = reader.GetInt32(0);
                        string content = reader.GetString(1);

                        qList.Add(new KeyValuePair<int, string>(id, content));
                    }
                }
            }
            conn.Close();
        }

        return qList;
    }
}