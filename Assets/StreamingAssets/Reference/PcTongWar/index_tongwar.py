import os
import json
import re

base_dir = '/var/www/vltksource_new/vl_update_27'

pattern = re.compile(r'(tongwar|bangchien|factionwar)', re.IGNORECASE)

found_files = []

if not os.path.exists(base_dir):
    print(f"Error: {base_dir} does not exist.")
else:
    for root, dirs, files in os.walk(base_dir):
        # We only care about files in a 'settings' or 'script' folder somewhere in the path
        # Normalize paths just in case
        parts = root.split(os.sep)
        # Check if 'settings' or 'script' is in the path parts (case insensitive could be safer but let's assume lowercase or capitalized)
        # Actually, let's just do it broadly: check if '/settings/' or '/script/' is in the normalized path, case insensitive.
        # Sometimes it might be 'Settings' or 'Script'
        path_lower = root.lower()
        if '/settings' in path_lower or '/script' in path_lower:
            for file in files:
                if pattern.search(file):
                    found_files.append(os.path.join(root, file))

out_dir = 'Assets/StreamingAssets/Reference/PcTongWar'
os.makedirs(out_dir, exist_ok=True)
out_file = os.path.join(out_dir, 'TongWarIndex.json')

with open(out_file, 'w', encoding='utf-8') as f:
    json.dump({"files": found_files}, f, indent=4)

print(f"Indexed {len(found_files)} files.")
