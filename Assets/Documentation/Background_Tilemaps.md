# Chỉnh background bằng Tile Palette

Trong scene Level01_FrozenForest, mở Map_Level01 > Grid:

- BackgroundFarTilemap: rừng xa, parallax Factor 0.22.
- BackgroundNearTilemap: cây gần, parallax Factor 0.07.
- GroundTilemap: mặt đất có collider.
- DecorationTilemap và ForegroundTilemap: các lớp trang trí khác.

Hai Tilemap background dùng layer và Sorting Layer Background, không có collider.

Mở Tools > Ooo Adventure > Map > Open Background Far Palette hoặc Open Background Near Palette. Menu sẽ chọn đúng Tilemap và palette tương ứng. Chọn ô gốc của cây trong Tile Palette rồi dùng Brush để tô trong Scene; dùng Eraser để xóa. Với cây lớn, ô chọn nằm ở chân cây. Tắt Edit Palette khi vẽ vào Scene.

Palette nằm ở Assets/Palettes/Level01/FrozenForest_BackgroundFar.prefab và FrozenForest_BackgroundNear.prefab. Các Tile nằm trong Assets/Tiles/Level01/Background. Mỗi Tile tham chiếu sprite có sẵn, lưu kích thước và màu mặc định cho cây mới tô.

Các cây đã có giữ nguyên vị trí, màu và kích thước riêng qua transform của từng ô Tilemap. Chỉnh hình dáng sprite gốc trong Art/Source nếu cần; không có code tạo nền khi chạy game.
