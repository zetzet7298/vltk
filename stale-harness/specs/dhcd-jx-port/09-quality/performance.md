# Performance Budget

| Trường | Giá trị |
|---|---|
| Mục đích | Đặt budget đo được cho portrait mobile và pilot server |
| Trạng thái | `design` |
| Owner / reviewer | Performance owner / client lead |
| Cập nhật | 2026-07-15 |

## Client gate

- P0 chỉ pass khi release build giữ 60 FPS trong battle scenario chuẩn trên mọi thiết bị thuộc device manifest máy tầm trung đã pin.
- Render-thread và GPU frame time `p95 <= 16.67 ms`; không có cửa sổ một giây liên tục dưới 60 FPS trong measurement window.
- Không allocation/frame spike dài trong wave; pool NPC/effect.
- Texture/audio memory budget và load time phải có số đo theo build artifact; chưa tự đặt số khi chưa profile.
- Portrait orientation và safe-area không tạo overdraw/blocked input.

## Device manifest

Artifact `android-midrange-pilot-v1` phải pin trước baseline: manufacturer/model, SoC/GPU, RAM, Android/API, native resolution/refresh rate, thermal/power mode, Unity quality config và release build hash. Performance owner và client lead duyệt manifest; đổi device sau failure cần ADR và vẫn phải giữ kết quả cũ để tránh đổi mẫu nhằm né gate. Khi chưa có giá trị thực, performance result chỉ là exploratory và P0 chưa pass.

## Server target

- 100 CCU pilot với threshold tạm thời phải pin trước final gate: REST p95 <= 500 ms/p99 <= 1 s; HTTP 5xx <= 1% trên tổng request không phải 4xx trong cửa sổ load 10 phút; WSS input-ack p95 <= 150 ms; tick lag p99 <= 2 ticks; verifier p95 <= 2 s cho run scenario chuẩn; unexpected quarantine <= 0.1% trên tổng run hoàn tất (loại trừ các run cố ý gửi input/replay sai trong security test).
- Đây là NFR product/ops, không phải số liệu recovered từ DHCD. Baseline load test phải ghi artifact; chỉ thay threshold bằng ADR trước gate, không để placeholder sau khi bắt đầu pilot.

## Scenario chuẩn và mẫu đo

Mọi baseline phải dùng cùng một manifest bất biến, lưu cùng artifact load test:

| Trường | Yêu cầu |
|---|---|
| `scenario_id` | `perf-normal-solo-v1` |
| `seed` | Một seed 64-bit cố định, ghi bằng giá trị thực; không dùng random mỗi lần chạy |
| `catalog_version` / `config_snapshot_id` | ID immutable của catalog và snapshot config được verifier dùng |
| `input/replay_corpus_sha256` | SHA-256 của corpus input/replay; cùng corpus giữa các build |
| `load_window` | 10 phút, 100 CCU; ghi số request, số run hoàn tất và số verifier call |
| `run_length` | Battle 10 phút theo scenario `perf-normal-solo-v1`; không đổi giữa các lần so sánh |

Nếu một trường chưa có giá trị thực trong artifact thì kết quả chỉ là exploratory, chưa được dùng làm pilot gate. Các giá trị và threshold trên là NFR do sản phẩm đặt, không phải claim đã recovered từ DHCD.

## Method

Profile release build 10 phút battle, capture Unity profiler, Go pprof/metrics, PostgreSQL stats; lưu artifact/hash và so sánh regression.

## Acceptance

- [ ] Device matrix, release build hash và profiler artifacts được lưu.
- [ ] Manifest `perf-normal-solo-v1` có seed, catalog/config ID, corpus SHA-256, load window, run length và số mẫu đo.
- [ ] Battle scenario đạt gate 60 FPS/frame-time trên mọi device của `android-midrange-pilot-v1` và server load đạt 100 CCU target.
- [ ] Regression vượt budget tạo blocker/rollback decision.
