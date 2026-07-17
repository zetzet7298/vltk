# Exec Plan

## Goal

Pin canonical static oracle độc lập cho toàn bộ root skill Cái Bang và biến nó thành gate bắt buộc của parity suite.

## Steps

1. [completed] Xác định 26 root IDs, source slices và relationship supplements.
2. [completed] Tạo deterministic stdlib generator + JSON/SHA artifact.
3. [completed] Thêm EditMode hash/coverage/catalog verifier.
4. [completed] Gắn generator `--check` và fixture vào Cái Bang test runner.
5. [completed] Chạy compile/test, sửa mismatch bằng canonical evidence, không nới assertion.
6. [completed] Independent Herdr proof audit chống circular oracle; xử lý 3 medium findings.
7. [completed] Record Harness validation/trace and pass story verify.

## Stop Conditions

- Repo slice không còn khớp hash/provenance.
- PC static row và Lua relationship mâu thuẫn.
- Test chỉ pass bằng cách lấy expected từ Unity implementation.
