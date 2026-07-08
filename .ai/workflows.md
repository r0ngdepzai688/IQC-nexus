# Business Workflows

Tài liệu này lưu trữ các quy trình nghiệp vụ (workflows) và logic thiết kế của các module trong hệ thống IQC Nexus. Việc lưu trữ này giúp đảm bảo không bị quên logic khi dự án ngày càng lớn.

## 0. Core Philosophy (Tư duy Thiết kế cốt lõi)
Tất cả các module trong hệ thống phải đảm bảo 2 yêu cầu tiên quyết:
1. **Trực quan & Quản trị**: Theo dõi được trực quan tiến độ của từng task/nhiệm vụ. Các nhiệm vụ phải được gán rõ ràng tag nhân viên phụ trách (User Badge) để quản lý tiến độ và khối lượng công việc của từng cá nhân.
2. **Dữ liệu Chuẩn hóa (Clean Data)**: Dữ liệu phải được làm sạch, có cấu trúc và lưu trữ khoa học để phục vụ việc tra cứu, trích xuất và tham chiếu về sau (ví dụ: tra cứu lỗi của model cũ để phòng ngừa cho model mới).


## 1. Module: New Models (IQC)

**Mục tiêu (Mục đích của công đoạn IQC đối với New Models):**
Phụ trách việc theo dõi tiến độ phê duyệt linh kiện, kiểm chứng hiệu suất, kiểm tra thu thập lỗi. Đảm bảo trước giai đoạn PR thì 100% linh kiện được phê duyệt (100% Approval Sheet được duyệt).

**Các giai đoạn phát triển sản phẩm:** `DV` -> `PV` -> `PR` -> `SR`
*(IQC cần hành động từ giai đoạn PV và PR)*

### Quy trình quản lý New Model của một Project/Basic model tại công đoạn IQC:

#### Bước 1: Nhận kế hoạch tất cả các model
- Bộ phận phát triển RnD gửi email thông báo kế hoạch.
- **Cell Leader New Model**: Cập nhật thông tin, thông báo cho các bên liên quan và chuẩn bị kế hoạch triển khai cho từng Project/Basic Model.

#### Bước 2: Tại giai đoạn PV
- **Cell Leader**: Gửi yêu cầu tổng hợp Risk factor, DFx của model tiền nhiệm (Base Model - BM) cho các PIC phụ trách từng vendor / nhóm linh kiện. Sau đó tổ chức họp review và tổng hợp để bộ phận RnD gửi HQ (Headquarters) xem xét. Cell Leader cũng có trách nhiệm theo dõi việc trả lời và lưu lại dữ liệu làm tham chiếu sau này.
- **PIC phụ trách**: Tải BOM list từ hệ thống về (file excel), lọc toàn bộ các code linh kiện mà nhà máy sẽ mua từ vendor, loại bỏ các code khác (vì các code này sau này sẽ mua về và qua IQC kiểm tra sau đó cấp đi các công đoạn khác). Chia BOM list theo nhóm linh kiện quản lý và gửi cho các PIC khác.
- **PIC phụ trách**: Nhận danh sách code linh kiện cấp A từ HQ, so sánh với BOM list đã lọc ở trên (chỉ quản lý những code mà nhà máy có mua về). Sau đó gửi list linh kiện cấp A đó cho vendor sản xuất, yêu cầu gửi kết quả đánh giá hiệu suất, tính CPK.
- **PIC phụ trách**: Theo dõi quá trình HQ phê duyệt Approval Sheet (Bảng phê duyệt các tiêu chuẩn của code đó, quy định chi tiết quá trình sản xuất, công đoạn CTQ, CTF, tiêu chuẩn quản lý,...).
- **PIC phụ trách**: Yêu cầu các PIC quản lý linh kiện/vendor tạo ISS (File quy định tiêu chuẩn kiểm tra để gửi cho nhóm Incoming sử dụng làm tiêu chuẩn khi kiểm tra vật liệu đầu vào).
- **PIC phụ trách**: Theo dõi quá trình cung cấp mẫu chuẩn, mẫu limit màu của vendor cho nhóm Incoming để sử dụng trong quá trình đánh giá linh kiện.
- **PIC phụ trách**: Xác nhận với các PIC quản lý linh kiện và tổng hợp danh sách Jig/thiết bị kiểm tra cần order.
- **PIC phụ trách**: Yêu cầu vendor gửi kết quả hiệu suất, tổ chức kiểm chứng code bất kỳ (Option).

#### Bước 3: Chuẩn bị báo cáo
- **Cell Leader**: Chuẩn bị dữ liệu báo cáo MPPR, nhập các nội dung cần các bên cập nhật, chỉnh sửa file báo cáo pptx và làm quy trình phê duyệt.

#### Bước 4: Giai đoạn PR
- **Cell Leader**: Theo dõi tiến độ lô hàng PR (PR lot).
- **Nhóm Incoming**: Theo dõi tiến độ hàng về, tiến hành kiểm tra và đánh giá hiệu suất linh kiện cấp A.
