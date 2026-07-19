from __future__ import annotations

import json
import tempfile
import unittest
from pathlib import Path

from scripts.skill_port import review_artifacts


REPO = Path(__file__).resolve().parents[2]
CONTENT = REPO / "Assets/StreamingAssets/Generated/SkillPort"


class SkillPortReviewArtifactTests(unittest.TestCase):
    def test_tuple_matrix_is_deterministic_and_fully_blocked(self) -> None:
        first = review_artifacts.build_review_artifacts(CONTENT)
        second = review_artifacts.build_review_artifacts(CONTENT)
        self.assertEqual(first, second)

        matrix = json.loads(first["skill-port-tuple-matrix.json"])
        summary = json.loads(first["skill-port-review-summary.json"])
        self.assertEqual(matrix["counts"]["skills"], 242)
        self.assertEqual(matrix["counts"]["cases_per_skill"], 60)
        self.assertEqual(matrix["counts"]["tuple_cases"], 14520)
        self.assertEqual(matrix["counts"]["blocked_skills"], 242)
        self.assertEqual(summary["blocked_tuple_cases"], 14520)
        self.assertEqual(summary["review_ready_tuple_cases"], 0)
        self.assertFalse(summary["production_signing_ready"])
        self.assertFalse(summary["parity_done"])

    def test_write_then_check_detects_no_drift(self) -> None:
        with tempfile.TemporaryDirectory() as tmp:
            out = Path(tmp)
            review_artifacts.write_or_check(CONTENT, out, check=False)
            review_artifacts.write_or_check(CONTENT, out, check=True)


if __name__ == "__main__":
    unittest.main()
