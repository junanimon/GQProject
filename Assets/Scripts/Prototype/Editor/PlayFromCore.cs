using UnityEditor;
using UnityEditor.SceneManagement;

namespace GuildProto.EditorTools
{
    // Day · Night 씬만 열어 놓고 Play를 눌러도 Core 씬부터 시작하게 한다.
    // 끄려면 메뉴 GQ > Play는 Core에서 시작
    [InitializeOnLoad]
    static class PlayFromCore
    {
        const string CorePath = "Assets/Scenes/Core.unity";
        const string MenuPath = "GQ/Play는 Core에서 시작";
        const string PrefKey = "GQ.PlayFromCore";

        static PlayFromCore() => EditorApplication.delayCall += Apply;

        static bool Enabled
        {
            get => EditorPrefs.GetBool(PrefKey, true);
            set => EditorPrefs.SetBool(PrefKey, value);
        }

        [MenuItem(MenuPath)]
        static void Toggle()
        {
            Enabled = !Enabled;
            Apply();
        }

        [MenuItem(MenuPath, true)]
        static bool ToggleValidate()
        {
            Menu.SetChecked(MenuPath, Enabled);
            return true;
        }

        static void Apply() =>
            EditorSceneManager.playModeStartScene = Enabled ? AssetDatabase.LoadAssetAtPath<SceneAsset>(CorePath) : null;
    }
}
