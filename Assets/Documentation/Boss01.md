# Boss 01 – Kẻ Giữ Băng

Mở `Assets/Scenes/Boss01_FrostGuardian.unity`, chọn Game rồi nhấn Play.

- A/D hoặc mũi tên: di chuyển. Space: nhảy. Enter: đánh kiếm. Esc: Pause.
- Boss có 630 máu, kiếm Finn gây 35 sát thương. Từ nửa máu trở xuống boss chuyển sang giai đoạn 2.
- Băng cầu: boss sáng xanh trước khi bắn; nhảy hoặc đổi hướng để né.
- Cột băng: rời vùng đỏ trước khi băng mọc lên. Các khoảng trống giữa cột vẫn an toàn.
- Lao tới: boss đỏ trước khi lao; nhảy qua rồi quay lại đánh khi boss nghỉ.
- Hai bình thuốc ở hai bên hồi máu. Thử lại sẽ khởi động lại toàn bộ trận đấu.

## Chỉnh trong Unity

- Chọn FrostGuardian: xem Animator và chỉnh máu, giới hạn di chuyển, prefab đạn/cột băng trong FrostBossController.
- Animator và các clip nằm ở `Assets/Animations/Boss01/FrostGuardian`. Animator trên đối tượng gốc, ảnh ở con Visual, giống Level 1.
- Chọn Map_Boss01/Grid/GroundTilemap để sửa nền, DecorationTilemap để sửa trang trí. BackgroundTilemap nằm sau nhân vật. Chỉ GroundTilemap có collider.
- Tile Palette: `Assets/Palettes/Boss01/FrostArena.prefab`. Có thể dùng thêm hai palette Level01 đang có.
- Boss, FrostBolt, IcePillar nằm ở `Assets/Prefabs/Chapter01/Bosses`. Prefab Finn dùng chung và bình thuốc của Chương 1 được dùng lại trong trận boss.
- Mã quản lý trận đấu nằm ở `Assets/Scripts/Chapter01/Levels/BossArenaGame.cs`. Hành vi và chiêu của boss nằm ở `Assets/Scripts/Chapter01/Bosses`. Finn và EnemyController dùng chung IDamageable để kiếm đánh được cả quái thường lẫn boss.
- Menu Pause dùng Canvas và ảnh PNG Home/Play có sẵn. Home vẫn là nút dự phòng vì chưa có scene Home.

Đây là một scene thử độc lập; chưa nối luồng hoàn thành Level 1 sang trận boss. Level 1 vẫn là scene đầu trong danh sách build.

## Bộ hình boss

Frost Guardian FREE v1.0 của chierit, CC BY 4.0: https://chierit.itch.io/boss-frost-guardian . PNG và Aseprite gốc nằm ở `Assets/Art/Chapter01/Source/FrostGuardian`. Bản FREE có 5 animation; ba cơ chế đánh hiện dùng lại animation Attack 1. Chân được đặt tại pixel (96, 18) tính từ góc dưới trái, 28 Pixels Per Unit. Ảnh gốc quay trái nên Flip X được bật khi boss quay phải.

## Đạn băng của boss

FrostBolt dùng Ice Projectile FREE của PinoGames: hình F0–F2 khi xuất hiện/bay, F3–F4 khi chạm Finn hoặc tường. Animator và các clip Spawn/Fly/Impact nằm ở `Assets/Animations/Boss01/FrostBolt`. Đạn quay theo hướng bay, trừ 14 máu khi chạm Finn, ngừng va chạm trong lúc vỡ và tự hủy. Bản IceBolt cũ của Level 1 vẫn giữ hình và cách hủy cũ.
