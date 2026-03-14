using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.IO;
using UnityEngine.Networking;
using System.Collections;

[System.Serializable]
public class TextConfigItem
{
    public string Text;
    public CusVector3 local_position;
    public CusVector2 localSize;
    public int fontSize;
    public string alignment;
    public string colorHex;
}

public class TextLoader : JsonLoaderBase<TextConfigItem, Text>
{
    public TextLoader(string jsonPath) : base(jsonPath) { }

    public override void ApplyLoadedData(string name, Text obj)
    {
        if (!data.TryGetValue(name, out var item))
            return;

        obj.text = item.Text;
        obj.rectTransform.localPosition = new Vector3(item.local_position._x, item.local_position._y, 0f);
        obj.rectTransform.sizeDelta = new Vector2(item.localSize._x, item.localSize._y);
        obj.fontSize = item.fontSize;



        if (!string.IsNullOrEmpty(item.colorHex) &&
            ColorUtility.TryParseHtmlString(item.colorHex, out var parsedColor))
        {
            parsedColor.a = obj.color.a; // 기존 알파 값 유지
            obj.color = parsedColor;

        }

        if (!string.IsNullOrEmpty(item.alignment))
        {
            switch (item.alignment.ToLower())
            {
                case "left": obj.alignment = TextAnchor.MiddleLeft; break;
                case "center": obj.alignment = TextAnchor.MiddleCenter; break;
                case "right": obj.alignment = TextAnchor.MiddleRight; break;
            }
        }
    }

    public override void JsonDataUpdate()
    {

        foreach (var kv in _target)
        {
            var txt = kv.Value;
            if (txt == null) continue;

            string cleanFontName = string.Empty;

            if (txt.font != null)
            {
                // Font.name에서 프로젝트명 제거, 파일명만 남김
                string fontFile = Path.GetFileNameWithoutExtension(txt.font.name);
                cleanFontName = $"Fonts/{fontFile}.ttf";
            }

            var cfg = new TextConfigItem
            {
                Text = txt.text,
                local_position = new CusVector3(txt.rectTransform.localPosition.x, txt.rectTransform.localPosition.y, 1f),
                localSize = new CusVector2(txt.rectTransform.rect.width, txt.rectTransform.rect.height),
                fontSize = txt.fontSize,
                alignment = txt.alignment.ToString(),
                colorHex = "#" + ColorUtility.ToHtmlStringRGBA(txt.color)
            };

            data[kv.Key] = cfg;
        }

        // 이름순 정렬
        var ordered = new SortedDictionary<string, TextConfigItem>(data);
        data = new Dictionary<string, TextConfigItem>(ordered);

        Save();
    }
}
