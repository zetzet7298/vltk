# RUN-005: Content bundle corrupt hoặc không tương thích

- **Trigger:** manifest signature/hash/dependency/locale mismatch hoặc resolver không tìm winner.
- **Owner:** content pipeline + release.
- **Thực hiện:** chặn gameplay trước bootstrap, giữ dữ liệu người chơi, activate bundle compatible trước đó; không fallback filesystem.
- **Xác minh:** client/Go cùng content hash, map53/skill/UI smoke pass và không còn unresolved asset.
