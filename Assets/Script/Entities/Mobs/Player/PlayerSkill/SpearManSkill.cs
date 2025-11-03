using Script.GUI_Controller.PlayerGui;
using UnityEngine;
using System.Collections; // Vẫn cần cho Coroutine
using Art.Resources.Character.PlayerCharactor.Spear_Man;
using Script.Controller.PlayerController; // Mới: Import namespace của SpearManAnimationController

namespace Script.Game.PlayerCharacter
{
    public class SpearManSkill : MonoBehaviour
    {
        #region Fields
        [Header("Skill 1: Chém thẳng")]
        public Collider2D chopDownCollider;
        public int chopDownDamage = 10;

        [Header("Skill 2: Đâm")]
        public Collider2D thrustCollider;
        public int thrustDamage = 15;

        [Header("Skill 3: Ném lao")]
        public Collider2D throwSpearCollider;
        public int throwSpearDamage = 20;

        public PlayerController playerController;
        private SpearManAnimationController _spearManAnimationController; // Tham chiếu đến SpearManAnimationController
        #endregion

        #region Unity Lifecycle Methods
        private void Start()
        {
            // Luôn lấy instance từ Singleton PlayerController
            playerController = PlayerController.Instance;
            if (playerController == null)
            {
                Debug.LogError("[SpearManSkill] Không tìm thấy PlayerController.Instance! Vui lòng đảm bảo PlayerController đã được khởi tạo.");
            }
            // Lấy tham chiếu đến SpearManAnimationController trên cùng GameObject
            _spearManAnimationController = GetComponent<SpearManAnimationController>();
            if (_spearManAnimationController == null)
            {
                Debug.LogError("[SpearManSkill] Không tìm thấy SpearManAnimationController! Vui lòng đảm bảo script đã được gắn.");
            }

            RegisterSkill();
        }

        private void OnDestroy()
        {
            if (PlayerGui.Instance != null)
            {
                PlayerGui.Instance.OnSkill1PressedACT -= TriggerChopDownAnimation;
                PlayerGui.Instance.OnSkill2PressedACT -= TriggerThrustAnimation;
                PlayerGui.Instance.OnSkill3PressedACT -= TriggerThrowSpearAnimation;
            }
        }
        #endregion

        #region Skill Registration
        private void RegisterSkill()
        {
            if (PlayerGui.Instance != null)
            {
                PlayerGui.Instance.OnSkill1PressedACT += TriggerChopDownAnimation; // Đăng ký ChopDown cho Skill 1
                //PlayerGui.Instance.OnSkill2PressedACT += TriggerThrustAnimation; // Đăng ký Thrust cho Skill 2
               // PlayerGui.Instance.OnSkill3PressedACT += TriggerThrowSpearAnimation; // Đăng ký ThrowSpear cho Skill 3
            }
            else
            {
                Debug.LogError("[SpearManSkill] PlayerGui.Instance không tồn tại khi RegisterSkill. Không thể đăng ký kỹ năng.");
            }
        }
        #endregion

        #region Skill 1: Chop Down
        // Hàm này được đăng ký với GUI để chỉ kích hoạt animation
        public void TriggerChopDownAnimation()
        {
            Debug.Log("[SpearManSkill] Kích hoạt animation Skill 1: Chém thẳng");
            if (_spearManAnimationController == null)
            {
                Debug.LogError("[SpearManSkill] SpearManAnimationController không được tìm thấy. Không thể phát animation.");
                return;
            }
            _spearManAnimationController.TriggerChopDownAnimation();
        }

        // Hàm này được gọi từ Animation Event để xử lý sát thương cho ChopDown
        public void ProcessChopDownDamage()
        {
            Debug.Log("[SpearManSkill] ProcessChopDownDamage được gọi.");
            if (chopDownCollider == null)
            {
                Debug.LogError("[SpearManSkill] ChopDownCollider chưa được gán! Không thể tấn công.");
                return;
            }

            Debug.Log($"[SpearManSkill] Bật Collider {chopDownCollider.name} để xử lý sát thương.");
            chopDownCollider.enabled = true;

            var hits = new Collider2D[10];
            int count = chopDownCollider.OverlapCollider(new ContactFilter2D().NoFilter(), hits);
            Debug.Log($"[SpearManSkill] Collider {chopDownCollider.name} đã tìm thấy {count} đối tượng va chạm.");

            Utility.LogCollidedObjects(chopDownCollider.name, hits, count);

            if (playerController == null)
            {
                Debug.LogError("[SpearManSkill] PlayerController bị null khi cố gắng gây damage. Vui lòng kiểm tra lại thiết lập.");
                chopDownCollider.enabled = false; // Ensure collider is turned off even if error
                return;
            }

            Utility.ProcessAttackHits(gameObject, hits, count, chopDownDamage, playerController.damageAlly);

            chopDownCollider.enabled = false; // Tắt collider ngay lập tức sau khi xử lý va chạm
            Debug.Log($"[SpearManSkill] Tắt Collider {chopDownCollider.name} sau khi xử lý sát thương.");
        }
        #endregion

        #region Skill 2: Thrust
        // Hàm này được đăng ký với GUI để chỉ kích hoạt animation
        public void TriggerThrustAnimation()
        {
            Debug.Log("[SpearManSkill] Kích hoạt animation Skill 2: Đâm");
            if (_spearManAnimationController == null)
            {
                Debug.LogError("[SpearManSkill] SpearManAnimationController không được tìm thấy. Không thể phát animation.");
                return;
            }
            _spearManAnimationController.TriggerThrustAnimation();
        }

        // Hàm này được gọi từ Animation Event để xử lý sát thương cho Thrust
        public void ProcessThrustDamage()
        {
        
        }
        #endregion

        #region Skill 3: Throw Spear
        // Hàm này được đăng ký với GUI để chỉ kích hoạt animation
        public void TriggerThrowSpearAnimation()
        {
            Debug.Log("[SpearManSkill] Kích hoạt animation Skill 3: Ném lao");
            if (throwSpearCollider == null)
            {
                Debug.LogError("[SpearManSkill] ThrowSpearCollider chưa được gán! Không thể tấn công.");
                return;
            }
            if (_spearManAnimationController == null)
            {
                Debug.LogError("[SpearManSkill] SpearManAnimationController không được tìm thấy. Không thể phát animation.");
                return;
            }
            _spearManAnimationController.TriggerThrowSpearAnimation();
        }

        // Hàm này được gọi từ Animation Event để xử lý sát thương cho ThrowSpear
        public void ProcessThrowSpearDamage()
        {
            
        }
        #endregion
    }
}