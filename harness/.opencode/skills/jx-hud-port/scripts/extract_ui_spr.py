#!/usr/bin/env python3
"""Extract UI SPR files to PNG using JX3 SPR format (matching SprDecoder.cs)."""
import struct, os, zlib
from pathlib import Path

SPR_ROOT = Path("/var/www/vltk-mobile/jxwin-kinnox/SourceNew/swrod3/Utility/Run/spr/Ui3")
OUT_ROOT = Path("/var/www/vltk-mobile/Assets/UI/HUD/Art")
OUT_ROOT.mkdir(parents=True, exist_ok=True)

SPR_SIGNATURE = 0x00525053  # "SPR\0"

def decode_spr(data: bytes):
    """Decode SPR matching C# SprDecoder.Decode()."""
    if len(data) < 32:
        return None, "Too small"
    
    off = 0
    signature = struct.unpack_from('<I', data, off)[0]; off += 4
    width = struct.unpack_from('<H', data, off)[0]; off += 2
    height = struct.unpack_from('<H', data, off)[0]; off += 2
    center_x = struct.unpack_from('<H', data, off)[0]; off += 2
    center_y = struct.unpack_from('<H', data, off)[0]; off += 2
    frames = struct.unpack_from('<H', data, off)[0]; off += 2
    colors = struct.unpack_from('<H', data, off)[0]; off += 2
    directions = struct.unpack_from('<H', data, off)[0]; off += 2
    interval = struct.unpack_from('<H', data, off)[0]; off += 2
    reserved = data[off:off+12]; off += 12  # 6 * u16
    
    if (signature & 0x00FFFFFF) != SPR_SIGNATURE:
        return None, f"Bad sig: {signature:#010x}"
    
    # Palette
    palette_size = colors * 3
    palette = data[off:off + palette_size]
    off += palette_size
    
    # Frame offset table
    offsets = []
    for i in range(frames):
        foff = struct.unpack_from('<I', data, off)[0]; off += 4
        flen = struct.unpack_from('<I', data, off)[0]; off += 4
        offsets.append((foff, flen))
    
    frame_data_base = off  # after offset table
    
    result_frames = []
    for i in range(frames):
        foff, flen = offsets[i]
        start = frame_data_base + foff
        if start + flen > len(data):
            result_frames.append(None)
            continue
        blob = data[start:start + flen]
        frame = decode_frame(blob, palette)
        result_frames.append(frame)
    
    return {
        'width': width, 'height': height,
        'frames': frames, 'colors': colors,
        'directions': directions, 'frame_data': result_frames
    }, None

def decode_frame(blob, palette):
    """Decode one SPR frame (RLE compressed, bottom-up rows)."""
    if len(blob) < 8:
        return None
    
    w = blob[0] | (blob[1] << 8)
    h = blob[2] | (blob[3] << 8)
    ox = blob[4] | (blob[5] << 8)
    oy = blob[6] | (blob[7] << 8)
    
    if w == 0 or h == 0:
        return None
    
    rgba = bytearray(w * h * 4)  # all zeros = transparent
    
    src = 8
    for row in range(h - 1, -1, -1):  # bottom-up
        col = 0
        while col < w and src + 1 < len(blob):
            run_len = blob[src]; src += 1
            alpha = blob[src]; src += 1
            
            if alpha == 0:
                col += run_len
                continue
            
            for r in range(run_len):
                if col >= w or src >= len(blob):
                    break
                color_idx = blob[src]; src += 1
                pi = (row * w + col) * 4
                pal_off = color_idx * 3
                if pal_off + 2 < len(palette):
                    rgba[pi + 0] = palette[pal_off + 0]
                    rgba[pi + 1] = palette[pal_off + 1]
                    rgba[pi + 2] = palette[pal_off + 2]
                rgba[pi + 3] = alpha
                col += 1
    
    return (w, h, bytes(rgba))

def make_png(width, height, rgba):
    """Generate PNG from RGBA bytes."""
    def chunk(tag, cdata):
        c = tag + cdata
        return struct.pack('>I', len(cdata)) + c + struct.pack('>I', zlib.crc32(c) & 0xFFFFFFFF)
    
    raw = b''
    for y in range(height):
        raw += b'\x00'
        raw += rgba[y * width * 4:(y + 1) * width * 4]
    
    sig = b'\x89PNG\r\n\x1a\n'
    ihdr = chunk(b'IHDR', struct.pack('>IIBBBBB', width, height, 8, 6, 0, 0, 0))
    idat = chunk(b'IDAT', zlib.compress(raw, 9))
    iend = chunk(b'IEND', b'')
    return sig + ihdr + idat + iend

def extract_spr(spr_path: Path):
    data = spr_path.read_bytes()
    result, err = decode_spr(data)
    if err:
        return 0
    
    base = spr_path.stem
    count = 0
    for i, frame in enumerate(result['frame_data']):
        if frame is None:
            continue
        w, h, rgba = frame
        out_name = f"{base}.png" if len(result['frame_data']) == 1 else f"{base}_{i:02d}.png"
        png = make_png(w, h, rgba)
        (OUT_ROOT / out_name).write_bytes(png)
        count += 1
    
    return count

def main():
    # Walk all SPR under Ui3
    spr_files = sorted(SPR_ROOT.rglob('*.spr'))
    total = 0
    for spr in spr_files:
        rel = spr.relative_to(SPR_ROOT)
        count = extract_spr(spr)
        if count > 0:
            print(f"  OK ({count}): {rel}")
            total += count
        else:
            print(f"  SKIP: {rel}")
    
    print(f"\nExtracted {total} frames to {OUT_ROOT}")

if __name__ == '__main__':
    main()
