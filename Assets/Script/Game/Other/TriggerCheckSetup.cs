using UnityEngine;
using System.Collections.Generic;

public class TriggerCheckSetup : MonoBehaviour
{
    // Tên của layer cho triggerObj
    [SerializeField]
    private string triggerLayerName = "TriggerLayer";
    
    // Danh sách các GameObject đang va chạm (hiển thị trong Inspector)
    [SerializeField]
    private List<GameObject> collidingObjectsList = new(); // Sử dụng cho mục đích hiển thị

    // HashSet để quản lý danh sách đối tượng đang va chạm thực tế
    private readonly HashSet<GameObject> collidingObjects = new(); // Khởi tạo ngay khi khai báo

    // Cache component references
    private Rigidbody2D parentRigidbody;

    private void Awake()
    {
        parentRigidbody = GetComponent<Rigidbody2D>();
    }

    void Start()
    {
        GameObject triggerObj = CreateOrGetTriggerObject(); // Sử dụng hàm kiểm tra và tái sử dụng triggerObj nếu đã tồn tại
        Collider2D mainCollider = GetMainCollider();

        if (mainCollider != null)
        {
            Collider2D triggerCollider = AddAndCopyCollider(triggerObj, mainCollider);
            ConfigureTriggerCollider(triggerObj, triggerCollider);
            SetLayerForTriggerObject(triggerObj);
        }
    }

    // Kiểm tra hoặc tạo GameObject con và đặt tên là "triggerObj"
    private GameObject CreateOrGetTriggerObject()
    {
        // Kiểm tra nếu "triggerObj" đã tồn tại
        Transform existingTrigger = transform.Find("triggerObj");
        if (existingTrigger != null)
        {
            Debug.Log("Reusing existing triggerObj.");
            return existingTrigger.gameObject;
        }

        // Nếu chưa tồn tại, tạo mới
        GameObject triggerObj = new GameObject("triggerObj");
        triggerObj.transform.parent = transform;
        triggerObj.transform.localPosition = Vector3.zero;
        return triggerObj;
    }

    // Lấy Collider2D từ GameObject chính
    private Collider2D GetMainCollider()
    {
        Collider2D mainCollider = GetComponent<Collider2D>();
        if (mainCollider == null)
        {
            Debug.LogError("No Collider2D found on the main GameObject.");
        }
        return mainCollider;
    }

    // Thêm Collider2D vào triggerObj và sao chép thuộc tính từ Collider chính
    private Collider2D AddAndCopyCollider(GameObject triggerObj, Collider2D mainCollider)
    {
        Collider2D triggerCollider = null;

        if (mainCollider is BoxCollider2D)
        {
            triggerCollider = triggerObj.AddComponent<BoxCollider2D>();
            CopyBoxColliderProperties((BoxCollider2D)mainCollider, (BoxCollider2D)triggerCollider);
        }
        else if (mainCollider is CircleCollider2D)
        {
            triggerCollider = triggerObj.AddComponent<CircleCollider2D>();
            CopyCircleColliderProperties((CircleCollider2D)mainCollider, (CircleCollider2D)triggerCollider);
        }
        else if (mainCollider is CapsuleCollider2D)
        {
            triggerCollider = triggerObj.AddComponent<CapsuleCollider2D>();
            CopyCapsuleColliderProperties((CapsuleCollider2D)mainCollider, (CapsuleCollider2D)triggerCollider);
        }
        else if (mainCollider is PolygonCollider2D)
        {
            triggerCollider = triggerObj.AddComponent<PolygonCollider2D>();
            CopyPolygonColliderProperties((PolygonCollider2D)mainCollider, (PolygonCollider2D)triggerCollider);
        }
        else
        {
            Debug.LogWarning("Collider type not supported for copying properties.");
        }

        return triggerCollider;
    }

    // Cấu hình Collider của triggerObj
    private void ConfigureTriggerCollider(GameObject triggerObj, Collider2D triggerCollider)
    {
        if (triggerCollider != null)
        {
            triggerCollider.isTrigger = true;

            Rigidbody2D rb = triggerObj.GetComponent<Rigidbody2D>();
            if (rb == null)
            {
                rb = triggerObj.AddComponent<Rigidbody2D>();
            }

            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.simulated = true;
            rb.useFullKinematicContacts = true;

            // Thêm sự kiện va chạm cho triggerCollider
            triggerObj.AddComponent<TriggerCollisionHandler>().Initialize(this);
        }
    }

    // Sao chép thuộc tính của BoxCollider2D
    private void CopyBoxColliderProperties(BoxCollider2D source, BoxCollider2D target)
    {
        target.size = source.size;
        target.offset = source.offset;
    }

    // Sao chép thuộc tính của CircleCollider2D
    private void CopyCircleColliderProperties(CircleCollider2D source, CircleCollider2D target)
    {
        target.radius = source.radius;
        target.offset = source.offset;
    }

    // Sao chép thuộc tính của CapsuleCollider2D
    private void CopyCapsuleColliderProperties(CapsuleCollider2D source, CapsuleCollider2D target)
    {
        target.size = source.size;
        target.offset = source.offset;
        target.direction = source.direction;
    }

    // Sao chép thuộc tính của PolygonCollider2D
    private void CopyPolygonColliderProperties(PolygonCollider2D source, PolygonCollider2D target)
    {
        target.points = source.points;
    }

    // Đặt layer cho triggerObj dựa trên tên layer
    private void SetLayerForTriggerObject(GameObject triggerObj)
    {
        int layer = LayerMask.NameToLayer(triggerLayerName);
        if (layer == -1)
        {
            Debug.LogError($"Layer '{triggerLayerName}' does not exist. Please create it in the Layer settings.");
        }
        else
        {
            triggerObj.layer = layer;
        }
    }

    // Thêm đối tượng vào danh sách va chạm
    public void AddCollidingObject(GameObject obj)
    {
        if (obj == null || obj.transform.IsChildOf(transform)) return; // Kiểm tra nếu đối tượng là con của triggerObj

        if (collidingObjects.Add(obj))
        {
            collidingObjectsList.Add(obj); // Cập nhật danh sách hiển thị trong Inspector
        }
    }


    // Xóa đối tượng khỏi danh sách va chạm
    public void RemoveCollidingObject(GameObject obj)
    {
        if (obj == null) return; // Kiểm tra null

        if (collidingObjects.Remove(obj))
        {
            collidingObjectsList.Remove(obj); // Cập nhật danh sách hiển thị trong Inspector
        }
    }

    // Lấy danh sách các GameObject đang va chạm
    public List<GameObject> GetCollidingObjects()
    {
        return collidingObjectsList; // Trả về danh sách hiển thị
    }
}

// Lớp xử lý sự kiện va chạm của triggerObj
public class TriggerCollisionHandler : MonoBehaviour
{
    private TriggerCheckSetup parentSetup;

    public void Initialize(TriggerCheckSetup setup)
    {
        parentSetup = setup;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        parentSetup.AddCollidingObject(collision.gameObject);
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        parentSetup.RemoveCollidingObject(collision.gameObject);
    }
}
