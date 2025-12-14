Mục	Màu	Ghi chú
Nền tổng thể	#0D1114	Xanh xám đậm
Panel phụ / Card	#15171A	Nền panel
Đường viền	#1F2429	Viền nhẹ giữa card
Text chính	#E6EEF3	Trắng xanh lam
Text phụ	#9AA6B2	Xám nhạt
Nhiệt độ cao	#F0625D	Đỏ cam
Cảnh báo radar	#FFD166	Vàng sáng
Node bình thường	#3FCF8E	Xanh lục
Node offline	#7B7E85	Xám
Accent button	#2979FF	Xanh dương sáng

1 — Quy tắc typography & density (dành cho 4K — 3840×2160)

Mục tiêu: nhiều nội dung nhưng vẫn đọc được từ ~2–3m (phòng giám sát).

Font: Segoe UI Variable (or Segoe UI Semilight).

Scales (px at 96dpi; tuned for 4K wall):

Page title / header main: 22–24 px

KPI numbers (top bar): 20–24 px

Section titles: 16–18 px

Body / card labels: 12–13 px

Values / small numbers: 14 px (semi-bold)

Tiny metadata / timestamps: 10–11 px

Card padding reduced: 8–12 px. Card corner radius smaller (6–10 px).

Line-height tighter: 1.1–1.2.

Dashboard (Layer 0) luôn tồn tại và tiếp tục cập nhật dữ liệu realtime.

Layer 1 (popup, flyout) dùng cho thao tác nhanh, không ảnh hưởng layout chính.

Layer 2 (module chi tiết) mở dưới dạng cửa sổ nổi độc lập: có 3 chế độ hiển thị khả dụng cho từng module:

Detached Window (Window riêng, có thể kéo sang màn hình khác) — phù hợp cho kỹ thuật viên làm sâu.

Partial Overlay (che một phần dashboard) — phù hợp cho thao tác nhanh, vẫn nhìn thấy nhiều dashboard.

Compact Overlay / Always-on-top (PiP-like) — nhỏ, luôn trên, dùng cho video hoặc điều khiển cấp nhanh.

Người dùng chọn chế độ mở khi mở module (hoặc dùng default do role/permission).

Khi Layer 2 đóng, dashboard trở lại nguyên trạng, mọi trạng thái thao tác được ghi log (audit).