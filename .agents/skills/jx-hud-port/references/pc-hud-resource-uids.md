# PC HUD Resource UIDs (Hash → Vietnamese name)

Lookup catalog for JX PC HUD sprites. Files live under
`/var/www/jx-source/pak_unpacked/<pak>/unknown/<hash>.spr`
because the unpacker lacks the full path dictionary. Resolve Chinese/GBK INI
paths to these hashes with the `jx-pc-resource-resolver` skill; do not guess.

## Top Status Bar (顶部控制条.ini = 8da7027d.ini)
| Vietnamese name | Chinese | Hash |
| --- | --- | --- |
| Khung viền máu mới | 新血条面板.spr | `973816f3.spr` |
| Thanh sinh lực (HP) | 生命条.spr | `74b299b9.spr` |
| Thanh nội lực (MP) | 内力条.spr | `b72be14b.spr` |
| Thanh thể lực (Stamina) | 体力条.spr | `83e13762.spr` |
| Thanh kinh nghiệm (EXP) | 经验条.spr | `f5d017dd.spr` |

## Bottom Shortcut Bar / Toolbar (工具控制条.ini = dc11ac12.ini)
| Vietnamese name | Chinese | Hash |
| --- | --- | --- |
| Phòng chat | 聊天室按钮.spr | `de6475b9.spr` |
| Nhân vật (F1) | 人物属性按钮_0.spr | `cf92ecbe.spr` |
| Hành trang (F2) | 背包按钮.spr | `175edefc.spr` |
| Túi phụ | 子母袋按钮.spr | `c732baf9.spr` |
| Võ công (F3) | 技能按钮.spr | `2317ae46.spr` |
| Nhiệm vụ (F4) | 任务按钮.spr | `a3717b5e.spr` |
| Tổ đội (F6) | 队伍按钮.spr | `b3455277.spr` |
| Bang hội | 帮会按钮.spr | `234770bb.spr` |
| Chạy/Đi bộ (toggle) | 跑步按钮.spr | `41d364a1.spr` |
| Đả tọa/Ngồi thiền (toggle) | 打坐按钮.spr | `82a5aa21.spr` |
| Lên/xuống ngựa (toggle) | 骑马按钮.spr | `fc8a4f16.spr` |
| Giao dịch | 交易按钮.spr | `cc903517.spr` |
| Đóng/mở PK | PK按钮.spr | `42e22aac.spr` |
| Quay phim/Chụp ảnh | 摄像机按钮.spr | `9aca89f7.spr` |

## Minimap (小地图_小.ini = ec10b91e.ini)
| Vietnamese name | Chinese | Hash |
| --- | --- | --- |
| Nút thu phóng | 小地图－切换按钮0.spr | `14f1acc9.spr` |
| Bản đồ sơn động | 小地图－洞窟.spr | `2e66ad6f.spr` |
| Bản đồ thế giới | 小地图－世界大地图按钮.spr | `c33f656f.spr` |
| Nút cắm cờ | 小地图－旗帜按钮.spr | `c9371d0d.spr` |
| Cờ nhỏ trên radar | 地图小旗帜.spr | `206e74a3.spr` |

## Chat Channels (聊天条.ini = c9c8a750.ini)
| Vietnamese name | Chinese (selected / icon) | Hash (selected / icon) |
| --- | --- | --- |
| Nói thầm | 3be3a09f / 69fbc7e6 | 3be3a09f.spr / 69fbc7e6.spr |
| Bạn bè | 7addeacc / 2c66b90e | 7addeacc.spr / 2c66b90e.spr |
| Thế giới | 59b0db0b / 50d91112 | 59b0db0b.spr / 50d91112.spr |
| Tổ đội | 8ff6d47a / a9d1f2f2 | 8ff6d47a.spr / a9d1f2f2.spr |
| Môn phái | 4074febd / 69f46c8c | 4074febd.spr / 69f46c8c.spr |
| Lân cận | 314af2aa / f434779f | 314af2aa.spr / f434779f.spr |
| Thành thị | a8671666 / b6d58e29 | a8671666.spr / b6d58e29.spr |
| Hệ thống/GM | b2a6f8a3 / e277c438 | b2a6f8a3.spr / e277c438.spr |
| Bang hội | 401cf1d6 / 8340787f | 401cf1d6.spr / 8340787f.spr |
| Liên minh | 9d6df5e0 / 64f8476e | 9d6df5e0.spr / 64f8476e.spr |
| Chiến trường Tống | 58166d73 / 8f8c13b9 | 58166d73.spr / 8f8c13b9.spr |
| Chiến trường Kim | bcc87eec / efb03ac7 | bcc87eec.spr / efb03ac7.spr |
| Tự nói (thường) | — / 50304af7 | — / 50304af7.spr |

Chat frame parts: 频道开与关a/b = `3b255f40`/`34fc44d5`; 聊天条底/顶/中改 =
`bdf9af98`/`8fa68495`/`3483ec02`; 聊天条阴影按钮 = `bcca4952`;
通用拖动条 (scroll) = `23fe2a10`.

## Notes
- Same hash may appear in several `*/unknown/` folders (update01, dmjx01,
  updatejx08, spr, ...). Pick any present copy; byte content is identical.
- Some paks ship Chinese + Vietnamese variants in parallel. Verify the on-image
  text is Vietnamese by decoding the SPR before committing (see `jx-pc-resource-resolver`).

## Bar panel geometry (顶部控制条.ini / Unity)
- `bar_panel_bg.png` (新血条面板): 552×17 background.
- Each bar fill track: 104×9.
- Panel-internal X offsets (PC layout): EXP 58 · HP 170 · MP 282 · Stamina 394.
- Fill art (`bar_hp_fill` / `bar_mp_fill` / `bar_stamina_fill` / `bar_exp_fill`) maps
  to 生命/内力/体力/经验条.spr respectively.
