# AtkRange và Visible/Invisible theo slide

## Vùng đánh kiếm

Mở prefab `Assets/Prefabs/Level01/Finn.prefab`, hoặc chọn Finn trong scene và mở `Visual > AtkRange`.

- AtkRange dùng tag và layer `PlayerSword`.
- Box Collider 2D: Is Trigger bật, Size (1.6, 1.1), mặc định Enabled tắt.
- Rigidbody 2D riêng: Kinematic, khóa Position X/Y và Rotation Z. Trigger kiếm tách khỏi Rigidbody của thân Finn.
- Script PlayerSwordHitbox: Owner là Finn, Damage = 35. Mỗi đối tượng chỉ nhận sát thương một lần trong một nhát chém dù có nhiều collider.
- Thân Finn vẫn dùng Box Collider không phải Trigger và vật liệu PlayerNoFriction.

Chọn Finn, mở Window > Animation > Animation (Ctrl + 6), chọn Finn_Attack. Track `Visual/AtkRange > Box Collider 2D > Enabled` có các mốc:

| Thời điểm | Enabled |
|---|---|
| 0 giây | Tắt |
| 0.1 giây | Bật |
| 0.2 giây | Tắt |
| 0.3 giây | Tắt |

Các clip Idle, Run, Jump, Fall, Hurt, Dead luôn tắt collider kiếm. Khi Finn nhận sát thương, code cũng tắt vùng đánh để không giữ hitbox của đòn bị ngắt. Thay đổi nhịp gây sát thương bằng cách chỉnh các keyframe Enabled trong Animation, không cần đổi code thời gian chờ.

FinnController chỉ nhận J/Enter, gửi trigger Atk, mở lượt đánh mới và đổi vị trí trái/phải cho AtkRange. Quái và boss nhận OnTriggerEnter2D/OnTriggerStay2D, kiểm tra tag PlayerSword rồi gọi PlayerSwordHitbox. Không còn OverlapCircleAll để đánh kiếm. DamageZone chỉ nhận collider thân Finn, không tính việc vùng kiếm chạm bẫy là Finn bị trúng.

## Quái xuất hiện trong màn hình

Các prefab FrostMushroom, ForestGoblin, FlyingEye, GateGuardian có EnemyVisibility trên `Visual`, cùng đối tượng với Sprite Renderer.

- OnBecameVisible: gọi EnemyController.SetVisible(true), cho phép AI hoạt động.
- OnBecameInvisible: đặt Active = false, dừng di chuyển ngang và hủy đòn tấn công đang chờ. Quái bay cũng dừng vận tốc dọc.
- Không còn kiểm tra khoảng cách ngang dưới 19 đơn vị. Quái không bị xóa khi ra khỏi màn hình và hoạt động lại khi xuất hiện.
- AI đấu trường FrostGuardian giữ vòng chiến đấu riêng; boss vẫn nhận sát thương từ AtkRange mới.

Unity cũng tính camera Scene trong Visible/Invisible. Kiểm tra trong tab Game; nếu mở Scene hoặc một Game view khác đang nhìn thấy quái, quái vẫn có thể được coi là Visible. Đây là hành vi của các sự kiện Unity, không phải kiểm tra khoảng cách.

## Kiểm tra khi mở project

1. Đợi Unity import và biên dịch xong. Một yêu cầu cấu hình đã chuẩn bị sẽ chạy một lần để Unity chuẩn hóa các curve và kiểm tra liên kết prefab. Có thể chạy lại bằng Tools > Ooo Adventure > Apply Slide Sword and Enemy Visibility.
2. Mở Level01_FrozenForest, Play. Đứng gần quái, nhấn J/Enter. Một nhát gây 35 sát thương; đứng yên không đánh thì vùng kiếm không gây sát thương.
3. Đi trái/phải và đánh để kiểm tra AtkRange ở đúng phía. Kiểm tra cả đánh trong lúc nhảy, nhận đòn, chết và chơi lại.
4. Để quái đi vào/rời vùng camera trong tab Game: quái chỉ di chuyển/tấn công khi Visible. Ra khỏi rồi trở lại màn hình, quái tiếp tục hoạt động.
5. Mở Boss01_FrostGuardian, kiểm tra kiếm vẫn gây 35 sát thương cho boss và riêng vùng kiếm chạm đòn lao/cột băng không làm Finn mất máu.

Code Runtime và Editor đã được biên dịch ngoại tuyến bằng Roslyn của đúng bản Unity, dùng tham chiếu package hiện tại. Prefab, GUID, animation binding và keyframe đã được kiểm tra bằng file. Chưa chạy Play Mode cho thay đổi này theo yêu cầu người dùng; các bước trên cần kiểm tra trong Unity.

## Điều khiển, nhận sát thương và sinh đạn bằng C#

- Finn đọc trục Horizontal và đặt vận tốc theo moveSpeed. Animator dùng Run, Atk và Die.
- Attack Range là trigger riêng của Nấm, Goblin và Mắt bay. Vào vùng này thì quái quay mặt, dừng tuần tra và phát Attack. Sau 0,4 giây, nếu Finn còn ở trong vùng và đứng phía quái đang quay mặt, đòn đánh trừ 10 máu. Collider thân của quái giữ va chạm với nền.
- Thanh máu tính Health / maxHealth. Khi chết, Finn dừng chuyển động, chuyển Rigidbody 2D sang Kinematic và hiện bảng thua sau một giây.
- GateGuardian dùng Transform Pos, prefab đạn và shotCooldown. EnemyController gọi Instantiate tại Pos sau khi bắt đầu hoạt ảnh đánh.
