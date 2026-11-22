# Xem và chỉnh animation của Finn và quái

## Mở từ nhân vật

1. Chọn **Finn**, **FrostMushroom**, **ForestGoblin**, **FlyingEye** hoặc **GateGuardian** trong Hierarchy.
2. Animator nằm ngay trên đối tượng gốc. Mở **Window > Animation > Animator** để xem state, mũi tên và Parameters. Chọn nhân vật khác sẽ chuyển sang controller của nhân vật đó nếu cửa sổ chưa khóa.
3. Mở **Window > Animation > Animation** (Ctrl + 6), chọn clip ở danh sách rồi bật Preview và nút phát để xem từng khung hình mà không cần chạy game.
4. Sprite Renderer vẫn ở đối tượng con **Visual**. Các clip điều khiển `Visual > Sprite Renderer > Sprite` từ Animator trên đối tượng gốc.

## Thư mục

- `Assets/Animations/Level01/Player/Finn`: Finn.controller và thư mục Clips chứa bảy animation.
- `Assets/Animations/Level01/Enemies/Mushroom`: nấm.
- `Assets/Animations/Level01/Enemies/Goblin`: goblin.
- `Assets/Animations/Shared/Enemies/FlyingEye`: mắt bay.
- `Assets/Animations/Level01/Enemies/Guardian`: pháp sư giữ cổng.

Mỗi loại quái có controller riêng và thư mục Clips chứa Idle, Run, Attack, Hurt, Dead. Không cần đổi code khi thay các sprite trong clip hoặc chỉnh quy tắc chuyển trạng thái.

## Parameters

| Parameter | Kiểu | Ý nghĩa |
|---|---|---|
| Speed | Float | Lớn hơn 0.1 để chạy; nhỏ hơn 0.1 để đứng yên khi ở trên đất. |
| Grounded | Bool, chỉ Finn | Finn đang đứng trên đất hay đang ở trên không. |
| VerticalSpeed | Float, chỉ Finn | Đang đi lên thì Jump, đi xuống thì Fall. |
| Attack | Trigger | Có hành động tấn công. |
| Hurt | Trigger | Vừa nhận sát thương. |
| Dead | Bool | Đã chết; ưu tiên cao nhất, chặn Attack và Hurt. |

Code chỉ gửi các dữ kiện và sự kiện này. **Animator Controller quyết định clip đang phát** qua Conditions và Exit Time. Không còn lệnh `Animator.Play(...)` trong các controller gameplay.

## Chỉnh trong Animator

- Bấm state để đổi **Motion** hoặc **Speed** của animation.
- Bấm mũi tên để đổi **Conditions**, **Has Exit Time**, **Exit Time** và **Transition Duration**.
- Attack và Hurt phát hết clip rồi quay lại trạng thái di chuyển phù hợp; Dead không lặp và chỉ thoát khi hồi sinh.
- Idle và Run bật Loop Time. Attack, Hurt và Dead tắt Loop Time.
- Khi Play, chọn nhân vật để xem các parameter đổi giá trị và state đang chạy được tô sáng.

Va chạm, sát thương, thời gian hồi đòn và điều khiển nhân vật vẫn thuộc gameplay code. Chỉnh riêng độ dài clip không tự đổi thời điểm kiếm gây sát thương hay lúc quái tạo đạn.

Bản sao trước khi đổi cấu trúc nằm trong `Library/BeforeAnimatorTransitions`.
