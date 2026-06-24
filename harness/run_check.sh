#!/bin/bash
cat /var/www/vltk-mobile/harness/implementation/worker-result.md
echo "---"
cat /var/www/vltk-mobile/harness/intake/source-recon.md
echo "---"
git -C /var/www/vltk-mobile diff HEAD
echo "---"
git -C /var/www/vltk-mobile status
