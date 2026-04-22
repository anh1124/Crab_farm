using UnityEngine;

namespace Script.Tutorial
{
    [CreateAssetMenu(fileName = "New Hint Data", menuName = "Hint/Data")]
    public class HintDataSO : ScriptableObject
    {
        [Header("Nội dung hiển thị")]
        public string title = "Town Center";

        [TextArea(3, 10)]
        public string description = "Thả coin vào để nâng cấp...";
    }

    [System.Obsolete("Renamed to HintDataSO. Kept for backward-compatible Unity references.")]
    public class TutorialDataSO : HintDataSO
    {
    }
}
