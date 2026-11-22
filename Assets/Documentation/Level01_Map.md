# Chỉnh map Level 01 bằng Tile Palette

Mở scene `Assets/Scenes/Level01_FrozenForest.unity`. Map đã được lưu sẵn trong scene, không cần chạy trình dựng map để vẽ hoặc chơi.

## Thư mục

- `Assets/Art/Chapter01/Source` chứa ảnh nguồn riêng của Chương 1. Hình của Finn và bộ UI dùng chung nằm ở `Assets/Art/Shared`.
- `Assets/Tiles/Level01/Ground`: các Tile mặt đất và bệ đứng, có hình dạng va chạm Grid.
- `Assets/Tiles/Level01/Decor`: Tile trang trí, không có va chạm.
- `Assets/Palettes/Level01`: hai bảng FrozenForest_Ground và FrozenForest_Decor.
- `Assets/Prefabs/Chapter01/Map/Background`: prefab cây nền để kéo từ Project vào Scene.
- `Assets/Prefabs/Shared/Player` chứa Finn. `Assets/Prefabs/Chapter01` chứa quái, boss và vật phẩm.
- `Assets/Scripts/Shared` và `Assets/Scripts/Chapter01` chứa code. Animation của màn 1 nằm ở `Assets/Animations/Level01`.

## Vẽ, xóa và di chuyển gạch

1. Dừng Play. Mở **Window > 2D > Tile Palette**.
2. Chọn Palette **FrozenForest_Ground** để sửa đất. Chọn **GroundTilemap** trong ô Active Target của Palette.
3. Chọn gạch trong Palette, dùng cọ Paint để vẽ trong Scene. Dùng Eraser để xóa. Có thể chọn một vùng gạch trong Palette để vẽ cả cụm, hoặc dùng Move trong Palette/Scene để chuyển vùng gạch.
4. Khi vẽ trang trí, đổi Palette sang **FrozenForest_Decor**, đổi Active Target sang **DecorationTilemap**. Chọn **ForegroundTilemap** nếu muốn vật trang trí nằm trước nhân vật.
5. Để bổ sung gạch mới: mở khóa chỉnh Palette bằng biểu tượng bút chì, kéo Sprite đã cắt từ Project vào bảng, chọn đúng thư mục Ground hoặc Decor để lưu Tile. Khóa lại Palette trước khi vẽ vào Scene.
6. Kéo prefab cây từ thư mục Map/Background vào `Map_Level01/Background`, rồi dùng Move/Scale để đặt vị trí.
7. Nhấn **Ctrl + S** để lưu scene.

## Các lớp

| Đối tượng | Sorting Layer | Layer vật lý | Collider |
|---|---|---|---|
| Cây nền xa/gần | Background | Background | Không |
| GroundTilemap | Ground | Ground | Tilemap Collider 2D + Composite Collider 2D, Rigidbody 2D Static |
| DecorationTilemap | Decor | Decor | Không |
| Finn, quái, vật phẩm | Entities | Player / Enemy / Pickup / Projectile | Giữ cấu hình gameplay |
| ForegroundTilemap, tuyết | Foreground | Foreground / cấu hình hiệu ứng | Không thêm collider vào trang trí |
| Canvas | UI, Screen Space Overlay | UI | Không |

**Sorting Layer** quyết định hình nào nằm trước/sau. **Layer vật lý** phục vụ kiểm tra va chạm và GroundMask. Không đổi Layer Ground thành Decor khi chỉnh đất.

Bản sao map trước khi dựng lại nằm ở `Library/BeforeEditableMapRebuild/Assets/Scenes/Level01_FrozenForest.unity`. Các Tile cũ và TagManager cũng có bản sao trong cùng thư mục. Sao lưu này nằm ngoài Assets để không xuất hiện lẫn trong danh sách scene chơi.
