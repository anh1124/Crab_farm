using UnityEngine;
using System.Collections.Generic;

namespace Script.Tutorial
{
    /// <summary>
    /// Một entry action trong danh sách On Start Actions của dialog.
    /// Gõ tên hàm vào methodName (vd: "SpawnCoinCloud"), dùng các trường
    /// param/stringParam/boolParam/position để truyền tham số cho hàm đó.
    /// </summary>
    [System.Serializable]
    public class TutorialActionData
    {
        [Header("Method Name")]
        [Tooltip("Tên hàm cần gọi (vd: SpawnCoinCloud, ShowArrow, PulseCoinButton, SetUI:healBar:false)")]
        public string methodName;
        
        [Header("Parameters")]
        [Tooltip("Tham số số nguyên (vd: số coin cho SpawnCoinCloud)")]
        public int param;

        [Tooltip("Tên chuỗi phụ (vd: tên UI element cho SetUIElementActive)")]
        public string stringParam;

        [Tooltip("Giá trị bool (vd: true=bật, false=tắt cho các action SetActive)")]
        public bool boolParam = true;

        [Tooltip("Vị trí trong thế giới (vd: vị trí spawn cho SpawnCoinCloud)")]
        public Vector3 position;
    }

    /// <summary>
    /// 1 dòng dialog trong tutorial với type, condition, và danh sách action.
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
        [Tooltip("Tên condition để lắng nghe")]
        public string conditionName;

        [Header("On Start Actions")]
        [Tooltip("Danh sách action khi dialog bắt đầu. Mỗi entry: gõ tên hàm vào methodName. Chạy theo thứ tự từ trên xuống.")]
        public List<TutorialActionData> onStartActions = new List<TutorialActionData>();
    }
}
