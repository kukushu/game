"""Small regression guard for the supported runtime, not a historical asset audit."""
from pathlib import Path
import re
import sys

root = Path(sys.argv[1]) if len(sys.argv) > 1 else Path(__file__).resolve().parent.parent
scripts = root / "Assets" / "Scripts"
forbidden = {"HarborCityGame", "CityTraffic", "CityRoads", "TrafficTrip", "TrafficState", "CitySaveData", "CityRoadTopology", "CityDailyAnalysis"}
files = list(scripts.rglob("*.cs"))
guids = set()
for path in files:
    source = path.read_text(encoding="utf-8-sig")
    if forbidden.intersection(re.findall(r"\b(?:class|struct|enum|interface)\s+(\w+)", source)):
        raise SystemExit(f"Obsolete runtime declaration: {path}")
    if not Path(str(path) + ".meta").is_file():
        raise SystemExit(f"Missing script meta: {path}")
    match = re.search(r"^guid: ([a-f0-9]{32})$", Path(str(path) + ".meta").read_text(encoding="utf-8-sig"), re.M)
    if not match or match[1] in guids:
        raise SystemExit(f"Malformed or duplicate script GUID: {path}")
    guids.add(match[1])
entry = (scripts / "Runtime/Presentation/Game/HarborCityRoadCoreGame.cs").read_text(encoding="utf-8-sig")
if "HarborCityGame" in entry or "CITY_TRAFFIC_BENCHMARK" in entry:
    raise SystemExit("Production launch still depends on the old entry or benchmark guard")
if not (scripts / "Runtime/Simulation/Core/CityBuildingSaveData.cs").is_file():
    raise SystemExit("Shared building save DTOs missing")
print(f"PASS one CityCore/RoadCore runtime; {len(files)} scripts have meta; no obsolete engine declarations or launch dependency")
