# Game strings

Edit `Assets/Resources/Localization/Strings.csv` in a spreadsheet editor or text editor. Keep the `key` column unchanged. `context` explains where a line appears; `en` is the source text; add or update translations in locale columns such as `zh-CN`. The game looks up text by key and uses the English column when the selected locale has no translation.

When using Excel, save this file as **CSV UTF-8**. Do not save an `.xlsx` workbook over the `.csv` path; Unity reads this file as plain-text CSV.

For a new line of UI text:

1. Add one row with a unique, stable key and its English source text.
2. Add the Simplified Chinese translation in `zh-CN`.
3. In code, request the string by key with `LocalizationTable.Get("your.key")`.

The CSV is the source for strings that are connected to `LocalizationTable`. It does not automatically translate every TextMeshPro label in a scene. Any runtime label that still changes language incorrectly is not wired to a localization key yet; add a key and make its controller read that key instead of relying on the scene's authored text.

Before sharing a build, run `python scripts/localization_audit.py` from the Unity project root. It checks duplicate and empty rows, English/Chinese placeholder parity, literal keys used by scripts, localization keys on shop and upgrade assets, and all six Chinese comic pages. Play through the title screen, settings, tutorial, level-up, shop and wheel, round settlement, credits, and both endings in each language. The original English comic pages stay in `Assets/_Project/Art/novel start`; the Simplified Chinese image variants live in `Assets/Resources/Localization/Comic` and are selected at runtime. Other text embedded in artwork still needs separate localized art.

If a cell contains a comma, quote the whole cell and double any quote characters inside it. Save as UTF-8 CSV.

The settings screen includes a saved language selector for `zh-CN` and `en`. Text keys are stable IDs; do not translate or rename them. The project uses **Noto Sans SC Regular**, a Simplified Chinese typeface distributed under the SIL Open Font License. Chinese text is assigned this CJK font directly; English text keeps its authored font. The bundled `OFL-NotoSansSC.txt` is the font license.

After adding or changing Chinese text, use **Debt Runner > Localization > Generate Chinese TMP Font Fallback** in Unity to rebuild the static atlas used by Scene view and as a TMP fallback. Runtime UI can add characters to its dynamic CJK atlas from `NotoSansSC-Regular.ttf`.

Unity's Scene view shows serialized authoring text and layout guides. It does not run the game's saved language setting. Use the Game view in Play mode to inspect the language selected in Settings.
