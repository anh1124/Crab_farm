# Denizen Object Documentation

## Overview
**Denizen** là đối tượng nhân vật cơ bản nhất, bao gồm các vai trò: **Vagrant**, **Villager**, **Archer**, **Worker**, **Farmer**.
Mọi Denizen đều dùng chung một Prefab cơ bản (`denizen`) và thay đổi hành vi thông qua `DenizenRoleController`.

## Unified Flow (Quy trình thống nhất)

### 1. Spawning (Sinh ra)
Việc sinh Denizen được thực hiện thông qua `Spawner.cs` (Helper trung tâm).
- **Skill**: Gọi `Spawner.Instance.SpawnMob(position, denizenPrefab)`.
- **Natural Spawn**: Các Manager (như `VagrantManager`) gọi `Spawner` để tạo instance.

### 2. Initialization & Organization (Khởi tạo & Sắp xếp)
Khi một Denizen được sinh ra (hoặc load từ save):
- **Self-Parenting**: Denizen tự tìm `EntityManager` và chui vào `DenizenContainer`.
- **Self-Registration**: Denizen tự tìm `DenizenManager` để đăng ký sự hiện diện.

### 3. Management & Delegation (Quản lý & Phân quyền)
- **DenizenManager**: Giữ danh sách tổng (`allDenizens`). Khi một Denizen đăng ký với một `NPCRole` cụ thể, `DenizenManager` sẽ bàn giao (delegate) đối tượng đó cho Manager chuyên trách:
    - `Vagrant` -> `VagrantManager`
    - `Archer` -> `GuardManager`
    - `Villager` -> (Hiện tại do `DenizenManager` quản lý trực tiếp)

### 4. Persistence (Lưu trữ)
- **EntitySaveManager**: CHỈ chịu trách nhiệm lưu và tải dữ liệu từ các Container.
- Nó **không** trực tiếp thực hiện việc sắp xếp đối tượng vào container (việc này do `EntityManager` và chính Entity đảm nhiệm).

---

## GameObject Structure (Hierarchy)
```text
Denizen (GameObject) [Tag: Denizen/Vagrant/...]
├── Sprite (Skin/Body) - Hiển thị hình ảnh & Animator
├── Detection (Child) - Chứa các Helper quét vùng
└── [Components] - Logic scripts
```

## Essential Components
1. **DenizenContext**: Lưu trữ các tham chiếu local (Animator, Movement, Status) và các Manager global.
2. **DenizenRoleController**: Bộ não điều phối Role. Tự động gọi Setup và chuyển đổi Job.
3. **DenizenStatus**: Lưu trữ chỉ số (HP, Coins, Tool, Role).
4. **DenizenMovement**: Xử lý di chuyển Kinematic.

## Role Switching Flow
Khi `ChangeRole(newRole)` được gọi:
1. Gỡ đăng ký khỏi Manager cũ (vd: `GuardManager` nếu là Archer).
2. Tắt Job hiện tại, bật Job mới.
3. Đổi Animator Controller tương ứng.
4. Đăng ký vào Manager mới thông qua `DenizenManager`.
5. Cập nhật Tag và Layer.
6. `EntityManager.Instance.MoveToContainer(gameObject)` để đảm bảo luôn ở đúng chỗ.
