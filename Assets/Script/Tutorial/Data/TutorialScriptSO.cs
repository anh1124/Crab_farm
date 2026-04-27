using UnityEngine;
using System.Collections.Generic;

namespace Script.Tutorial
{
    /// <summary>
    /// Kịch bản đầy đủ cho tutorial - chứa list các dialog lines
    /// </summary>
    [CreateAssetMenu(fileName = "TutorialScript", menuName = "Tutorial/Script")]
    public class TutorialScriptSO : ScriptableObject
    {
        [Header("Info")]
        public string tutorialName;
        
        [Header("Dialog Lines")]
        [Tooltip("Chạy tuần tự từ trên xuống")]
        public List<TutorialDialogLineSO> dialogLines = new List<TutorialDialogLineSO>();
    }
}
