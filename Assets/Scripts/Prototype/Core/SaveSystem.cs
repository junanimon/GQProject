using System.IO;
using UnityEngine;

namespace GuildProto
{
    // 세이브 파일 한 칸 (persistentDataPath/save.json) + 엔딩 도감 (PlayerPrefs)
    public static class SaveSystem
    {
        static string Path => System.IO.Path.Combine(Application.persistentDataPath, "save.json");
        const string EndingKey = "GQ.Endings";

        public static bool HasSave => File.Exists(Path);

        public static void Save(SaveData data)
        {
            data.savedAt = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm");
            File.WriteAllText(Path, JsonUtility.ToJson(data));
        }

        public static SaveData Load()
        {
            if (!HasSave) return null;
            try { return JsonUtility.FromJson<SaveData>(File.ReadAllText(Path)); }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[SaveSystem] 세이브를 읽지 못했습니다: {e.Message}");
                return null;
            }
        }

        public static void Delete()
        {
            if (HasSave) File.Delete(Path);
        }

        // ───── 엔딩 도감 ─────

        public static bool HasEnding(string id) => ("|" + PlayerPrefs.GetString(EndingKey, "") + "|").Contains("|" + id + "|");

        public static void UnlockEnding(string id)
        {
            if (string.IsNullOrEmpty(id) || HasEnding(id)) return;
            string cur = PlayerPrefs.GetString(EndingKey, "");
            PlayerPrefs.SetString(EndingKey, cur.Length == 0 ? id : cur + "|" + id);
            PlayerPrefs.Save();
        }
    }
}
