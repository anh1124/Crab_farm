using UnityEngine;

namespace Script.Tutorial
{
    /// <summary>
    /// 1 dòng dialog trong tutorial với type và condition
    /// </summary>
    [CreateAssetMenu(fileName = "NewDialogLine", menuName = "Tutorial/Dialog Line")]
    public class TutorialDialogLineSO : ScriptableObject
    {
        [Header("Content")]
        [Tooltip("Tên người nói")]
        public string speakerName;
        
        [Tooltip("Nội dung"), TextArea(3, 6)]
        public string content;
        
        [Tooltip("Avatar (optional)")]
        public Sprite avatar;
        
        [Header("Type")]
        [Tooltip("Loại dialog")]
        public TutorialDialogType dialogType = TutorialDialogType.Skipable;
        
        [Header("Condition (chỉ cho ExitConditional)")]
        [Tooltip("Tên condition để lắng nghe"),]
        public string conditionName; // vd: "TownCenterEntered", "TownCenterUpgraded"
        
        [Header("On Start Action")]
        [Tooltip("Action tự động chạy khi dialog này bắt đầu hiển thị")]
        public TutorialActionType onStartAction = TutorialActionType.None;
        
        [Tooltip("Tham số cho action (ví dụ: số coin cho SpawnCoinCloud)")]
        public int onStartParam;
        
        [Tooltip("Vị trí cho action (ví dụ: spawn position)")]
        public Vector3 onStartPosition;
    }
}
