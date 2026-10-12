"""Check locale coverage for the Unity CSV and localized ScriptableObjects.

Run from the Unity project root: python scripts/localization_audit.py
"""

import csv
import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
TABLE = ROOT / "Assets/Resources/Localization/Strings.csv"
CHINESE_FONT = ROOT / "Assets/Resources/Localization/ZixiaohunLiLiangCuHei SDF.asset"
CHINESE_FALLBACK_FONT = ROOT / "Assets/Resources/Localization/NotoSansSC SDF.asset"
SCRIPTS = ROOT / "Assets/_Project/Scripts"
DATA = ROOT / "Assets/_Project/Data"
KEY_CALL = re.compile(r'LocalizationTable\.(?:Get|SetText)\(\s*"([^"]+)"')
LOCAL_HELPER_CALL = re.compile(r'\b(?:L|F)\(\s*"([^"]+)"')
PANEL_CALL = re.compile(r'SetPanel(?:Button)?Text\([^,]+,\s*"[^"]+",\s*"([^"]+)"')
ASSET_KEY = re.compile(r"^  localizationKey: (\S+)", re.MULTILINE)
PLACEHOLDER = re.compile(r"\{\d+(?::[^}]+)?\}")


def main() -> int:
    errors = []
    with TABLE.open(encoding="utf-8-sig", newline="") as source:
        reader = csv.DictReader(source)
        if reader.fieldnames != ["key", "context", "en", "zh-CN"]:
            errors.append(f"Unexpected CSV columns: {reader.fieldnames}")
        rows = list(reader)

    entries = {}
    for index, row in enumerate(rows, start=2):
        key = row.get("key", "").strip()
        if not key:
            errors.append(f"Row {index}: empty key")
            continue
        if key in entries:
            errors.append(f"Row {index}: duplicate key {key}")
        entries[key] = row
        for locale in ("en", "zh-CN"):
            if not (row.get(locale) or "").strip():
                errors.append(f"Row {index}: {key} has no {locale} text")
        en_args = sorted(PLACEHOLDER.findall(row.get("en") or ""))
        zh_args = sorted(PLACEHOLDER.findall(row.get("zh-CN") or ""))
        if en_args != zh_args:
            errors.append(f"Row {index}: {key} placeholder mismatch: {en_args} != {zh_args}")

    def font_glyphs(path):
        if not path.is_file():
            errors.append(f"Missing static Chinese TMP font: {path.relative_to(ROOT)}")
            return set()
        font_source = path.read_text(encoding="utf-8")
        character_table = font_source.split("  m_CharacterTable:", 1)
        if len(character_table) != 2:
            errors.append(f"Static Chinese TMP font has no character table: {path.relative_to(ROOT)}")
            return set()
        return {
            chr(int(code))
            for code in re.findall(r"m_Unicode: (\d+)", character_table[1].split("  m_AtlasTextures:", 1)[0])
        }

    primary_glyphs = font_glyphs(CHINESE_FONT)
    fallback_glyphs = font_glyphs(CHINESE_FALLBACK_FONT)
    for key, row in entries.items():
        missing = sorted({
            char for char in row.get("zh-CN", "")
            if "\u3400" <= char <= "\u9fff" and char not in primary_glyphs
        })
        if missing:
            errors.append(f"{key}: selected Chinese TMP font lacks {''.join(missing)}")
        uncovered = sorted({
            char for char in row.get("zh-CN", "")
            if "\u3400" <= char <= "\u9fff" and char not in primary_glyphs | fallback_glyphs
        })
        if uncovered:
            errors.append(f"{key}: all Chinese TMP fonts lack {''.join(uncovered)}")

    for path in SCRIPTS.rglob("*.cs"):
        source = path.read_text(encoding="utf-8-sig", errors="replace")
        keys = KEY_CALL.findall(source) + PANEL_CALL.findall(source)
        if path.name in {"ShopSystem.cs", "SpinningWheelController.cs"}:
            keys += LOCAL_HELPER_CALL.findall(source)
        for key in keys:
            if key not in entries:
                errors.append(f"{path.relative_to(ROOT)}: missing key {key}")

    for path in DATA.rglob("*.asset"):
        source = path.read_text(encoding="utf-8-sig", errors="replace")
        if "Shop Item Definition" in str(path) and not ASSET_KEY.search(source):
            errors.append(f"{path.relative_to(ROOT)}: missing shop localizationKey")
        for base in ASSET_KEY.findall(source):
            for suffix in (".title", ".description"):
                key = base + suffix
                if key not in entries:
                    errors.append(f"{path.relative_to(ROOT)}: missing key {key}")

    comic_dir = ROOT / "Assets/Resources/Localization/Comic"
    for page in range(1, 7):
        if not (comic_dir / f"p{page}.png").is_file():
            errors.append(f"Missing Chinese comic page p{page}.png")

    for error in errors:
        print(error)
    print(f"Checked {len(rows)} strings; {len(errors)} errors")
    return 1 if errors else 0


if __name__ == "__main__":
    sys.exit(main())
