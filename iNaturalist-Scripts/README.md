# iNaturalist Insect Data Fetcher

A Python utility that searches the iNaturalist API for insects and compiles data into spreadsheets with reference images. Features an interactive GUI for selecting images or a quick CLI mode.

**Current insect list: 200 insects** (beetles, butterflies, moths, mantises, and more)

---

## ⚡ Quick Start (2 Minutes)

```bash
# 1. Navigate to the folder
cd iNaturalist-Scripts

# 2. Create virtual environment (recommended, one-time only)
py -m venv venv

# 3. Activate virtual environment (Windows PowerShell)
.\venv\Scripts\Activate.ps1

# 4. Install dependencies (one-time only)
py -m pip install -r requirements-inaturalist.txt

# 5. Run the script
py fetch_insect_data.py

# 6. Choose GUI or CLI mode
# Output files appear in: data/insect_data.csv and data/insect_data.xlsx
```

**Note:** Processing 200 insects takes ~2-3 minutes due to API rate limiting. This is normal and prevents server overload. ⏱️

---

## 📋 What It Does

### Features
- 🔍 **Search iNaturalist** for insects by common name (with smart validation)
- 🖼️ **Browse images** for each insect (up to 15 images per insect)
- 🖱️ **Select reference images** interactively through the GUI
- 📊 **Export to spreadsheet** with selected images (CSV and Excel)
- ⚙️ **CLI mode** for quick automated processing
- ⚡ **Responsive UI** - images load asynchronously in background
- ✅ **Result validation** - ensures results are actually insects, not random animals

### Output Spreadsheet Contains
- Common name
- Scientific name  
- Description (from Wikipedia when available)
- **Selected reference image URL**
- Taxon ID

---

## 🚀 Usage Modes

### GUI Mode (Recommended) ✨

```bash
python fetch_insect_data.py
# Select: "Yes - Use GUI with image selection"
```

**Workflow:**
1. Script fetches data for all insects (~2-3 minutes)
2. GUI opens showing:
   - Insect name, scientific name, description
   - Up to 15 available images (sorted by community votes)
3. For each insect:
   - **Click an image** to select it as the reference
   - **Next >** button to continue
   - **< Previous** button to go back
   - **Skip** to use the default image
4. Click **Finish & Export** when done
5. Files saved to `data/` folder

### CLI Mode (Quick)

```bash
python fetch_insect_data.py
# Select: "No - Quick CLI mode"
```

**Workflow:**
1. Script fetches data for all insects
2. Uses default images from iNaturalist
3. Exports to CSV and Excel automatically
4. No interaction required

---

## 📝 Editing the Insect List

### Format: `insects_list.txt`

One insect name per line. That's it!

```
Silver Jewel Scarab
Golden Jewel Scarab
Rainbow Weevil
Rainbow Leaf Beetle
# Comment with # (ignored)
Glasswing Butterfly
```

### How to Edit

1. **Open** `insects_list.txt` in any text editor (Notepad, VS Code, etc.)
2. **Add/remove/reorder** insect names
3. **Save** the file
4. **Run** `python fetch_insect_data.py` - it loads the new list automatically

### Valid Formats

- One insect per line ✓
- Empty lines (ignored) ✓
- Whitespace trimmed automatically ✓
- Comments with `#` prefix ✓

```
# BEETLES
Silver Jewel Scarab
  Golden Jewel Scarab  

# BUTTERFLIES
Glasswing Butterfly
```

All valid! The script handles formatting automatically.

### Adding Insects

```bash
# Windows (PowerShell) - append to file
Add-Content insects_list.txt "Monarch Butterfly"

# macOS/Linux - append to file
echo "Monarch Butterfly" >> insects_list.txt
```

Or just open the file in Notepad and type!

---

## 📂 File Structure

```
iNaturalist-Scripts/
├── fetch_insect_data.py ................. Main script (run this)
├── insects_list.txt ..................... Insect names (edit this)
├── requirements-inaturalist.txt ......... Python dependencies
├── README.md ............................ This file
└── data/ ............................... Output folder (created on first run)
    ├── insect_data.csv ................. Spreadsheet format
    └── insect_data.xlsx ................ Excel with formatting
```

---

## 🛠️ Installation & Setup

### Requirements
- Python 3.7+
- Internet connection
- Display environment (for GUI mode)

### Dependencies

Install with:
```bash
pip install -r requirements-inaturalist.txt
```

**What it installs:**
- `requests` - API communication
- `openpyxl` - Excel file creation
- `Pillow` - Image display in GUI

### Troubleshooting Installation

**Missing pip?**
```bash
python -m pip install --upgrade pip
```

**Still having issues?**
```bash
# Try upgrading pip first
python -m pip install --upgrade pip

# Then install requirements
pip install -r requirements-inaturalist.txt

# Verify installation
pip list | grep -E "requests|openpyxl|Pillow"
```

---

## 🎯 Examples

### Example 1: Test with 5 Insects

1. Open `insects_list.txt`
2. Keep only first 5 lines, delete the rest
3. Run: `python fetch_insect_data.py`
4. Test GUI or CLI mode

### Example 2: Focus on Beetles Only

Edit `insects_list.txt` to include only lines 1-50 (all beetles).

### Example 3: Custom List

```
# My Favorite Insects
Monarch Butterfly
Luna Moth
Jewel Stag Beetle
Praying Mantis
Rainbow Grasshopper
```

---

## 🖼️ GUI Interface Overview

### Main Window
```
┌────────────────────────────────────────────┐
│ Select Reference Images  |  Insect 1 / 200  │
├────────────────────────────────────────────┤
│ 📋 Insect Information                      │
│ Monarch Butterfly                          │
│ Scientific: Danaus plexippus              │
│ Description: The monarch butterfly is...   │
├────────────────────────────────────────────┤
│ 🖼️ Available Images                        │
│ [Image] [Image] [Image] [Image] [Image]   │
│  By: Observer name                         │
│  Obs ID: 123456789                         │
├────────────────────────────────────────────┤
│ [< Prev] [Next >] [Skip] [Finish & Export] │
└────────────────────────────────────────────┘
```

### Image Selection
- **Click an image** to select it
- **Scroll horizontally** to see more images
- **Observer name & ID** shown for attribution

### Navigation
- **Next >** - Move to next insect (or click image)
- **< Previous** - Go back to previous insect
- **Skip** - Use default image, move to next
- **Finish & Export** - Save and exit

---

## 📊 Current Insect List (200 Total)

| Category | Count | Examples |
|----------|-------|----------|
| Beetles | 50 | Jewel Scarabs, Stag Beetles, Rhino Beetles |
| Butterflies | 35 | Swallowtails, Owls, Leachings, Pansy |
| Moths | 40 | Luna, Hawk, Moon, Tiger, Silk Moths |
| Mantises | 15 | Flower, Ghost, Dragon, Spiny |
| Grasshoppers/Crickets | 15 | Weta, Katydids, Lubber Grasshoppers |
| Stick Insects | 11 | Various walking sticks and nymphs |
| Bees/Wasps | 11 | Orchid Bees, Carpenter, Tarantula Hawk |
| Other | 28 | Damselflies, Cicadas, Treehoppers, etc. |

---

## ⚙️ Customization

### Change Insect Names

Edit `insects_list.txt` with correct scientific or common names from [iNaturalist.org](https://www.inaturalist.org).

**Tips:**
- Use common names (more reliable than scientific names)
- If a name doesn't work, search iNaturalist directly to find the exact name
- More specific names usually work better than generic ones

### Create Multiple Lists

```
# Keep original
insects_list.txt

# Create variants
insects_colorful.txt
insects_endangered.txt  
insects_test.txt (just 5 for testing)
```

Then create a simple wrapper to choose which list to load.

### Filter by Category

Edit `insects_list.txt` to include only insects from one category:

```
# Just Beetles
Silver Jewel Scarab
Golden Jewel Scarab
# ... (lines 1-50)
# Delete lines 51-200
```

---

## 🐛 Troubleshooting

### "No results found" for an insect

**What's happening:**
- Script now validates that results are actually insects/arachnids (not squirrels, birds, etc.)
- Checks name similarity to prevent wrong results
- If the best match isn't similar enough, returns "Not found" instead of wrong animal

**Solution:**
- Script automatically retries with fuzzy/partial matching
- Tries exact match first, then tries first word of multi-word names
- Example: "Golden Jewel Scarab" → tries "Golden" if full name fails
- Not all insects exist in iNaturalist database
- Search [iNaturalist.org](https://www.inaturalist.org) directly to verify
- Try a more general name (e.g., "beetle" instead of "rare rhinoceros beetle")
- Skip that insect and use the default image

### "No images found" for an insect

**Solution:**
- Some insects have fewer observations on iNaturalist
- Click "Skip" to use the default image
- Browse iNaturalist directly to see what's available

### ImportError for missing packages

**Solution:**
```bash
# Reinstall all dependencies
pip install -r requirements-inaturalist.txt --force-reinstall

# Or install individually
pip install requests openpyxl Pillow
```

### Insects file not found

**Solution:**
```bash
# File must be named exactly: insects_list.txt
# Location: same folder as fetch_insect_data.py

# If missing, recreate it:
echo "Monarch Butterfly" > insects_list.txt
# Then add more insects
```

### Script won't start

**Solution:**
- Check Python version: `python --version` (need 3.7+)
- Verify dependencies: `pip list`
- Try running in a different terminal
- For headless servers: use CLI mode (select "No")

### GUI doesn't appear

**Solution:**
- Make sure you have a display environment
- On headless servers: Use CLI mode instead
- Check if Tkinter is installed: `python -m tkinter`
- If Tkinter missing:
  - Windows: Reinstall Python with Tkinter option
  - macOS: `brew install python-tk@3.9` (or your Python version)
  - Linux: `sudo apt-get install python3-tk`

### Network/API errors (HTTP 429 - Rate Limiting)

**What it is:**
- Error 429 = "Too Many Requests" - iNaturalist API is throttling
- Happens when processing 200 insects rapidly
- This is **normal and expected**!

**Solution:**
- Script now **automatically handles rate limiting**
- Adds delays between requests (0.5 seconds)
- Retries with exponential backoff if 429 occurs
- Processing 200 insects takes ~2-3 minutes (not seconds)
- If you see "Rate limited (429)... Waiting Xs before retry": **This is normal!**
- Just let it run - script will continue automatically

**Don't do:**
- Don't manually stop and restart the script
- Don't send requests faster than 2 per second
- Don't process > 500 insects in a row (use multiple runs)

### Image loading slowly

**Solution:**
- Normal behavior! Images load in background from iNaturalist servers
- UI stays responsive - read insect info while images load
- Try moving to next insect while waiting
- Come back to that insect later if needed

---

## 💡 Tips & Tricks

### Speed up processing

CLI mode is faster for just getting data:
```bash
python fetch_insect_data.py
# Select: No (Quick CLI mode)
```

### Batch editing with text tools

**Windows (PowerShell):**
```powershell
(Get-Content insects_list.txt) -replace 'Old Name', 'New Name' | 
  Set-Content insects_list.txt
```

**macOS/Linux:**
```bash
sed -i 's/Old Name/New Name/g' insects_list.txt
```

### Count insects in list

```bash
# Windows
(Get-Content insects_list.txt | Measure-Object -Line).Lines

# macOS/Linux
wc -l insects_list.txt
```

### Validate insect names

Before running, verify names on [iNaturalist.org](https://www.inaturalist.org) - search for each insect to confirm it has data.

### Export management

Output files are saved to `data/` subfolder:
- `insect_data.csv` - Import to Excel, Google Sheets, etc.
- `insect_data.xlsx` - Already formatted Excel file
- Professional formatting in Excel file (bold headers, wrapped text)

---

## 🔧 Technical Details

### How the Script Works

1. **Load insects** from `insects_list.txt`
2. **Search iNaturalist API** for each insect
3. **Extract data:**
   - Common name
   - Scientific name
   - Description (from Wikipedia)
   - Taxon ID
4. **If GUI mode:**
   - Fetch images for each insect
   - Display interactive selection interface
   - Save selected images
5. **Export to CSV and Excel**

### API Endpoints Used

- **Taxon Search**: `https://api.inaturalist.org/v1/taxa`
- **Observations**: `https://api.inaturalist.org/v1/observations`

No API key required - uses public endpoints.

### Image Sorting

Images are sorted by:
1. Community votes (most popular first)
2. Quality rating
3. Display as 150x150px thumbnails

### Error Handling

- Missing insects: Marked as "Not found" in output
- Failed API calls: Logged to console, process continues
- File errors: Clear error message shown
- Network issues: Graceful retry with error logging

---

## 📚 Using as a Python Module

```python
from fetch_insect_data import (
    search_insect, 
    fetch_insect_list,
    fetch_images_for_taxon,
    save_to_csv,
    save_to_excel
)

# Search single insect
butterfly = search_insect("Monarch Butterfly")
print(butterfly)

# Fetch multiple insects
insects = ["Ladybug", "Dragonfly"]
data = fetch_insect_list(insects)

# Get images for insect
images = fetch_images_for_taxon(10907, limit=15)

# Save results
save_to_csv(data, "my_insects.csv")
save_to_excel(data, "my_insects.xlsx")
```

---

## 📖 Function Reference

### `load_insects_from_file(file_path=None)`
Loads insect list from text file (default: `insects_list.txt`)

### `search_insect(common_name)`
Searches iNaturalist for single insect, returns dict with data

### `fetch_images_for_taxon(taxon_id, limit=10)`
Fetches images for a taxon ID, returns list of image dicts

### `extract_taxon_data(taxon)`
Extracts data from iNaturalist taxon object

### `fetch_insect_list(insect_names)`
Batch search for multiple insects

### `save_to_csv(data, output_file)`
Saves data to CSV file

### `save_to_excel(data, output_file)`
Saves data to formatted Excel file

### `main_gui(insects_to_fetch=None)`
Launches interactive GUI mode

### `main()`
Entry point - shows mode selection dialog

---

## 🔗 Resources

- [iNaturalist API Documentation](https://www.inaturalist.org/pages/api+reference)
- [iNaturalist.org](https://www.inaturalist.org) - Browse insects directly
- [Python Requests Library](https://requests.readthedocs.io/)
- [OpenPyXL Documentation](https://openpyxl.readthedocs.io/)

---

## 📄 License

This script is provided as-is for use with the Stardew Valley Carnivorous Plants mod project.

---

## ❓ Questions?

1. **Can't find an insect?** → Search [iNaturalist.org](https://www.inaturalist.org) first
2. **Script not running?** → Check Python version and dependencies
3. **GUI not working?** → Try CLI mode or check Tkinter installation
4. **Want to modify the list?** → Just edit `insects_list.txt`

**Everything in one file - happy insect hunting!** 🦋🐝🐛
