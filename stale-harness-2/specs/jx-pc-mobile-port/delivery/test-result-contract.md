# Contract kết quả kiểm thử

`registry/tests.yaml` là danh mục test phải có, không phải bằng chứng test đã pass.
Kết quả thực tế được đăng ký tại `registry/test-results/index.yaml` và phải hợp lệ
theo `schemas/test-result.schema.json`.

## Quy tắc

- `result_path` là artifact local trong package hoặc manifest sidecar đã tải và
  kiểm hash; không dùng URL/console text không pin.
- `sha256` được tính lại từ bytes artifact, không tin ETag. `revision` phải khớp
  source/content/contract revision của release candidate.
- Test dùng golden phải liệt kê `golden_ids`; từng golden phải sẵn sàng, hash
  đúng và có reviewer độc lập.
- `PASS` cũ hơn revision hiện tại, thiếu environment/command trong artifact,
  thiếu reviewer hoặc hash lệch đều không được mở gate.
- Release yêu cầu mọi test bắt buộc có `PASS`; `BLOCKED`, danh sách rỗng hoặc
  bất kỳ `FAIL` nào đều chặn release.

## Nội dung artifact result

Artifact được trỏ bởi `result_path` tối thiểu ghi command, seed/corpus nếu có,
thời gian bắt đầu/kết thúc, môi trường/tool version, input/content hashes, kết
quả assertion, metric raw và liên kết log/screenshot/video content-addressed.
Thông tin nhạy cảm phải được redaction trước khi hash và lưu.
