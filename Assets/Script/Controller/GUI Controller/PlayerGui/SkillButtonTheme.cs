using UnityEngine;
using Script.Game.PlayerCharacter.Skills;

namespace Script.Controller.GUI_Controller.PlayerGui
{
    [CreateAssetMenu(fileName = "SkillButtonTheme", menuName = "UI/SkillButtonTheme")]
    public class SkillButtonTheme : ScriptableObject
    {
        [Header("Mask Colors (Background overlays)")]
        public Color colorNoMana = new Color(0.2f, 0.4f, 1f, 0.6f);      // Xanh lam
        public Color colorNoCoin = new Color(1f, 0.8f, 0f, 0.6f);        // Vàng
        public Color colorNoHealth = new Color(1f, 0.2f, 0.2f, 0.6f);    // Đỏ
        public Color colorExecuting = new Color(0.2f, 0.2f, 0.2f, 0.5f); // Tối khi đang đánh

        [Header("Text Colors (Cost Text)")]
        public Color textMana = Color.cyan;
        public Color textCoin = Color.yellow;
        public Color textHealth = Color.red;
        public Color textDefault = Color.white;
    }
}
