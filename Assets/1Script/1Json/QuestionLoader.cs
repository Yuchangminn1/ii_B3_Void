using System.Collections.Generic;
using UnityEngine;

public interface IQuestionTarget
{
    void Initialize(List<QuestionInfo> items);
    List<QuestionInfo> Data();
}

// 이 로더는 StreamingAssets/Json/QuestionConfig.json 의
// "SeasonContainer": [ { Question, Selection[] }, ... ] 구조를 읽습니다.
// JsonLoaderBase는 최상위가 Dictionary<string, T> 형태여야 하므로
// DataType을 List<QuestionInfo>로 사용하고 키는 "SeasonContainer"가 됩니다.
public class QuestionLoader : JsonLoaderBase<List<QuestionInfo>, IQuestionTarget>
{
    // 예: new QuestionLoader("Json/QuestionConfig.json")
    public QuestionLoader(string jsonPath) : base(jsonPath) { }

    public override void ApplyLoadedData(string name, IQuestionTarget obj)
    {
        if (obj == null) return;
        if (!TryGetData(name, out var items) || items == null)
        {
            Debug.LogWarning($"QuestionLoader: No data found for key '{name}'");
            return;
        }
        obj.Initialize(items);
    }

    public override void JsonDataUpdate()
    {
        foreach (var kv in _target)
        {
            var target = kv.Value;
            if (target == null) continue;
            var items = target.Data();
            if (items != null)
            {
                data[kv.Key] = items;
            }
        }
        Save();
    }

    // 편의 헬퍼: 컨테이너 이름(기본 "SeasonContainer")으로 질문 리스트를 반환
    public List<QuestionInfo> GetQuestions(string containerName = "SeasonContainer")
    {
        if (TryGetData(containerName, out var items) && items != null)
        {
            return items;
        }
        return new List<QuestionInfo>();
    }
}
