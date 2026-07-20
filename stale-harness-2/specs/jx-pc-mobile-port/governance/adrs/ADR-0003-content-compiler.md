# ADR-0003: Compile content offline và pin version

- **Trạng thái:** Chấp nhận
- **Quyết định:** vltktool/compiler tạo bundle immutable có version/hash/provenance cho Python backend và Unity; production không đọc PC files hay hot-reload.
- **Hệ quả:** Config gameplay authoritative ở Python backend; Unity chỉ nhận projection cần cho presentation/prediction.
