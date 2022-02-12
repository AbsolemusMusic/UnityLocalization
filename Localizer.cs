using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class Localizer : MonoBehaviour
{
    private static Localizer shared;
    public static Localizer Shared
    {
        get
        {
            if (!shared)
            {
                isInitFromScript = true;
                shared = Camera.main.gameObject.AddComponent<Localizer>();
                shared.Init(shared.languages);
            }
            return shared;
        }
    }

    public static bool isInitFromScript;

    [SerializeField]
    private List<SystemLanguage> languages = new List<SystemLanguage>();

    private Dictionary<SystemLanguage, LocalizationData> localization = new Dictionary<SystemLanguage, LocalizationData>();

    private int currentCharID = 0;
    private string currentLocalizedString = "";

    private const string FILEPATH_FORMAT = "Localization/{0}";
    private const string LOCALIZED_FORMAT = "\"\" = \"\";";


    private void OnEnable()
    {
        if (shared && !isInitFromScript)
        {
            Destroy(this);
            return;
        }

        if (isInitFromScript)
            return;

        shared = this;
        shared.Init(languages);
    }

    public void Init(List<SystemLanguage> languages)
    {
        if (languages.Count == 0)
        {
            Debug.LogError("Не хватает выбранных языков");
            return;
        }

        this.languages = languages;
        localization.Clear();
        ResetTemp();

        foreach (SystemLanguage language in languages)
            LoadTextFor(language);

        Debug.Log("result count: " + localization.Count);
    }

    private void LoadTextFor(SystemLanguage language)
    {
        string filepath = GetLocalizedFilePath(language);
        TextAsset textFile = Resources.Load<TextAsset>(filepath);
        if (!textFile)
        {
            Debug.LogError("Ошибка загрузки файла:  " + filepath + " для языка:  " + language.ToString());
            return;
        }

        string text = textFile.text;

        LocalizationData localizationData = new LocalizationData();
        ParseText(text, localizationData);

        localization.Add(language, localizationData);
    }

    private void ParseText(string text, LocalizationData _locData)
    {
        char[] chars = text.ToCharArray();
        ParseChars(chars, _locData);
    }

    private void ParseChars(char[] chars, LocalizationData _locData)
    {
        foreach (char textChar in chars)
        {
            if (textChar.Equals(LOCALIZED_FORMAT[currentCharID]))
            {
                currentCharID++;
            }

            if (currentCharID != 0)
            {
                currentLocalizedString += textChar.ToString();
            }

            if (currentCharID >= LOCALIZED_FORMAT.Length)
            {
                SplitLocalizedString(currentLocalizedString, _locData);
                ResetTemp();
            }
        }
    }

    private void SplitLocalizedString(string keyWithLocalize, LocalizationData _locData)
    {
        string[] format = new string[1]{ " = " };
        string[] split = keyWithLocalize.Split(format, System.StringSplitOptions.None);
        if (split.Length != 2)
            return;

        string key = split[0];
        string value = split[1].Remove(split[1].Length - 1);

        AddKeyAndValue(key, value, _locData);
    }

    private void AddKeyAndValue(string key, string value, LocalizationData _locData)
    {
        key = key.Remove(key.Length - 1).Remove(0, 1);
        value = value.Remove(value.Length - 1).Remove(0, 1);

        LocalizationItem localizationItem = new LocalizationItem(key, value);
        _locData.items.Add(localizationItem);
    }

    private void ResetTemp()
    {
        currentCharID = 0;
        currentLocalizedString = string.Empty;
    }

    private string GetLocalizedFilePath(SystemLanguage language)
    {
        string fileName = GetFileName(language);
        string filePath = string.Format(FILEPATH_FORMAT, fileName);
        return filePath;
    }

    private string GetFileName(SystemLanguage language)
    {
        string langName = language.ToString();
        string fileName = langName.Remove(2);
        return fileName;
    }

    public string GetLocalization(string key)
    {
        SystemLanguage current = Application.systemLanguage;
        string result = GetLocalization(key, current);
        return result;
    }

    private string GetLocalization(string key, SystemLanguage language)
    {
        LocalizationData _locData;
        if (localization.TryGetValue(language, out _locData))
        {
            string result = _locData.GetLocalization(key);
            if (!string.IsNullOrEmpty(result))
                return result;
        }

        if (localization.TryGetValue(SystemLanguage.English, out _locData))
        {
            string result = _locData.GetLocalization(key);
            if (!string.IsNullOrEmpty(result))
                return result;
        }
        return "";
    }
}

public class LocalizationData
{
    public List<LocalizationItem> items = new List<LocalizationItem>();

    public string GetLocalization(string key)
    {
        foreach (LocalizationItem item in items)
        {
            if (item.key.Equals(key))
                return item.value;
        }
        return key;
    }
}

public class LocalizationItem
{
    public string key;
    public string value;

    public LocalizationItem(string key, string value)
    {
        this.key = key;
        this.value = value;
    }
}

public static class ExtensionMethods
{
    public static string Localize(this string key)
    {
        string result = Localizer.Shared.GetLocalization(key);
        if (!string.IsNullOrEmpty(result))
            return result;
        return key;
    }
}