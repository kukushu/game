"""Export the actual authored Unity design DTO for the headless host, not a second balance profile."""
import json
import sys
from pathlib import Path
import yaml

source = Path(sys.argv[1]).read_text(encoding="utf-8-sig")
# The sole tagged Unity object is a ScriptableObject wrapper; its definition is plain YAML.
plain = "\n".join(line for line in source.splitlines() if not line.startswith(("%", "---")))
definition = yaml.safe_load(plain)["MonoBehaviour"]["definition"]
assert definition["schemaVersion"] == 4
Path(sys.argv[2]).write_text(json.dumps(definition, ensure_ascii=False, indent=2), encoding="utf-8")
