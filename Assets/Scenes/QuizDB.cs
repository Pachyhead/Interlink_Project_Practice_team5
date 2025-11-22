using System.Collections.Generic;
using System.Data;
using Mono.Data.Sqlite;
using UnityEngine;

public class QuizDB
{
    private string GetDBPath()
    {
        //return "URI=file:" + Application.streamingAssetsPath +"/Quiz.db";
        return "URI=file:" + Application.streamingAssetsPath + "/test.db";
    }

    // 문제 리스트 불러오기
    public List<KeyValuePair<int, string>> LoadQuestions()
    {
        List<KeyValuePair<int, string>> questions = new List<KeyValuePair<int, string>>();

        using (IDbConnection conn = new SqliteConnection(GetDBPath()))
        {
            conn.Open();
            using (IDbCommand cmd = conn.CreateCommand())
            {
                //cmd.CommandText = "SELECT question FROM Quiz";
                cmd.CommandText = "SELECT question_id, content FROM Question";
                using (IDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        if (!reader.IsDBNull(0) && !reader.IsDBNull(1))
                        {
                            int id = reader.GetInt32(0);
                            string q = reader.GetString(1);
                            KeyValuePair<int, string> pair = new KeyValuePair<int, string>(id, q);
                            questions.Add(pair);
                        }
                    }
                }
            }
        }

        return questions;
    }
}
