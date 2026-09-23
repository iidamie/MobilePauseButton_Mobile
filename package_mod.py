import shutil
import sys
import zipfile
from pathlib import Path


MOD_ID = "MobilePauseButton"
ROOT = Path(__file__).resolve().parent


def find_dll() -> Path | None:
    candidates = [
        ROOT / "MobilePlugin" / "bin" / "Release" / "net10.0" / f"{MOD_ID}.dll",
        ROOT / f"{MOD_ID}.dll",
    ]
    return next((candidate for candidate in candidates if candidate.is_file()), None)


def main() -> int:
    version = (ROOT / "VERSION.txt").read_text(encoding="utf-8").strip()
    dll = Path(sys.argv[1]).resolve() if len(sys.argv) > 1 else find_dll()
    if not version or dll is None or not dll.is_file():
        print(
            "Build MobilePlugin/MobilePauseButton.csproj first, or pass the DLL path.",
            file=sys.stderr,
        )
        return 1

    staging = ROOT / "tmp_package"
    mod_dir = staging / MOD_ID
    output = ROOT / f"{MOD_ID}-{version}.zip"
    if staging.exists():
        shutil.rmtree(staging)
    if output.exists():
        output.unlink()

    mod_dir.mkdir(parents=True)
    shutil.copy2(dll, mod_dir / f"{MOD_ID}.dll")
    with zipfile.ZipFile(output, "w", zipfile.ZIP_DEFLATED) as archive:
        for path in sorted(mod_dir.rglob("*")):
            if path.is_file():
                archive.write(path, path.relative_to(staging))

    shutil.rmtree(staging)
    print(output)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
