import json
import pathlib
import sys
import zipfile

root = pathlib.Path(__file__).resolve().parents[1]
release_dir = root / "_releases"
archives = sorted(release_dir.glob("*.zip"))
if len(archives) != 1:
    raise SystemExit(f"Expected exactly one release ZIP in {release_dir}, found {len(archives)}")

archive = archives[0]
with zipfile.ZipFile(archive) as bundle:
    names = bundle.namelist()
    manifest_paths = [name for name in names if name.endswith("/manifest.json")]
    if len(manifest_paths) != 1:
        raise SystemExit(f"Expected one nested manifest.json, found {manifest_paths}")
    manifest = json.loads(bundle.read(manifest_paths[0]))
    required = {
        "Name": "Beta Farm Planner",
        "Author": "Beta Calendars",
        "UniqueID": "BetaCalendars.BetaFarmPlanner",
        "EntryDll": "BetaFarmPlanner.dll",
        "Version": "1.0.0",
    }
    for key, expected in required.items():
        if manifest.get(key) != expected:
            raise SystemExit(f"Manifest {key} should be {expected!r}, found {manifest.get(key)!r}")
    dll_path = manifest_paths[0].removesuffix("manifest.json") + manifest["EntryDll"]
    if dll_path not in names or not bundle.read(dll_path):
        raise SystemExit(f"The entry DLL is missing or empty: {dll_path}")
    for path in names:
        if path.endswith(".pdb") or "/obj/" in path or "/bin/" in path or ".git/" in path:
            raise SystemExit(f"Build or repository junk found in archive: {path}")
        if path.endswith(("Stardew Valley.dll", "StardewModdingAPI.dll", "MonoGame.Framework.dll")):
            raise SystemExit(f"A game or framework binary must not be redistributed: {path}")
    if not any(name.endswith("/i18n/default.json") for name in names):
        raise SystemExit("English localization is missing.")
    if not any(name.endswith("/i18n/tr.json") for name in names):
        raise SystemExit("Turkish localization is missing.")
    print(f"Validated {archive.name}: {len(names)} entries, manifest v{manifest['Version']}, entry DLL present.")
