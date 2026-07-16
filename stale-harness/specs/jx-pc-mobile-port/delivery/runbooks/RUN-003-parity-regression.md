# RUN-003: Xử lý parity regression

- **Trigger:** logic mismatch, SSIM <0.99, sai SPR/audio/map/avatar.
- **Owner:** domain DRI + parity reviewer.
- **Thực hiện:** đóng promotion, pin source/tool/content/golden revisions, tái lập case, xác định winner/provenance, cập nhật contradiction nếu oracle chưa rõ.
- **Rollback:** trở lại content/app revision đạt gate; không tăng tolerance để che lỗi.
- **Xác minh:** case lỗi và full affected matrix pass, reviewer ký lại.
