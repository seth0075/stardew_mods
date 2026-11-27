"""
Fetch insect data from iNaturalist API and compile into a spreadsheet.

This script provides both CLI and GUI interfaces for searching the iNaturalist API
for insects and creating spreadsheets with selected reference images.
"""

import requests
import csv
import sys
from pathlib import Path
from typing import Optional, List, Dict
import logging
from PIL import Image, ImageTk
from io import BytesIO
import tkinter as tk
from tkinter import ttk, messagebox
from urllib.parse import urljoin
import threading
import time

logging.basicConfig(level=logging.INFO, format='%(levelname)s: %(message)s')
logger = logging.getLogger(__name__)

INATURALIST_API_BASE = "https://api.inaturalist.org/v1"
TAXON_ENDPOINT = f"{INATURALIST_API_BASE}/taxa"
OBSERVATIONS_ENDPOINT = f"{INATURALIST_API_BASE}/observations"
API_DELAY = 0.5  # Delay between API requests (seconds) to avoid rate limiting
MAX_RETRIES = 3  # Max retries for 429 rate limit errors

# Taxa to REJECT (things that are definitely NOT insects)
REJECTED_TAXA = {
    "mammalia",      # Mammals
    "aves",          # Birds
    "actinopterygii",  # Ray-finned fish
    "amphibia",      # Amphibians
    "reptilia",      # Reptiles
    "plantae",       # Plants
    "fungi",         # Fungi
    "animalia",      # Too broad, but if only this we'll accept
    "chordata",      # Too broad chordata
}


def is_valid_insect(taxon: dict) -> bool:
    """
    Verify that a taxon result is NOT a mammal, bird, plant, etc.
    Uses rejection list rather than acceptance list for better coverage.
    
    Args:
        taxon: The taxon object from iNaturalist API
        
    Returns:
        True if NOT in rejected categories, False if definitely rejected
    """
    # Check the main taxon name
    taxon_name = taxon.get("name", "").lower()
    
    # Check ancestry chain for rejected categories
    ancestors = taxon.get("ancestors", [])
    
    # Check if this is definitely NOT an insect (is a mammal, bird, etc.)
    for ancestor in ancestors:
        ancestor_name = ancestor.get("name", "").lower()
        for rejected in REJECTED_TAXA:
            if rejected in ancestor_name:
                logger.debug(f"Rejecting '{taxon_name}' - found rejected ancestor '{ancestor_name}'")
                return False
    
    # Check the taxon name itself for rejected categories
    for rejected in REJECTED_TAXA:
        if rejected in taxon_name:
            logger.debug(f"Rejecting '{taxon_name}' - is a {rejected}")
            return False
    
    # If not explicitly rejected, probably safe to include
    return True


def name_similarity(str1: str, str2: str) -> float:
    """
    Simple similarity check between two strings (0.0 to 1.0).
    Returns how much of str1 appears in str2.
    
    Args:
        str1: Search term
        str2: Result name
        
    Returns:
        Similarity score 0.0 to 1.0
    """
    str1 = str1.lower()
    str2 = str2.lower()
    
    if str1 == str2:
        return 1.0
    
    # Check if one contains the other
    if str1 in str2 or str2 in str1:
        return 0.8
    
    # Check word overlap
    words1 = set(str1.split())
    words2 = set(str2.split())
    
    if words1 and words2:
        overlap = len(words1 & words2) / len(words1 | words2)
        return overlap
    
    return 0.0


def load_insects_from_file(file_path: str = None) -> List[str]:
    """
    Load insect list from a text file (one insect name per line).
    
    Args:
        file_path: Path to the insects list file. If None, looks for 
                   insects_list.txt in the script directory.
                   
    Returns:
        List of insect names, stripped of whitespace
        
    Raises:
        FileNotFoundError: If the insects file cannot be found
    """
    if file_path is None:
        file_path = Path(__file__).parent / "insects_list.txt"
    
    file_path = Path(file_path)
    
    if not file_path.exists():
        raise FileNotFoundError(
            f"Insects list file not found: {file_path}\n"
            f"Please create a 'insects_list.txt' file with one insect name per line."
        )
    
    try:
        with open(file_path, "r", encoding="utf-8") as f:
            # Read lines, strip whitespace, and filter out empty lines
            insects = [line.strip() for line in f if line.strip()]
        
        logger.info(f"Loaded {len(insects)} insects from {file_path.name}")
        return insects
        
    except IOError as e:
        logger.error(f"Error reading insects file: {e}")
        raise


def search_insect(common_name: str) -> Optional[dict]:
    """
    Search iNaturalist for an insect by common name.
    Validates results to avoid mammals/birds/plants.
    Tries exact match first, then fuzzy matching if needed.
    Includes retry logic for rate limiting (HTTP 429).
    
    Args:
        common_name: The common name of the insect to search for
        
    Returns:
        Dictionary with insect data or None if not found
    """
    time.sleep(API_DELAY)  # Respect API rate limits
    
    for attempt in range(MAX_RETRIES):
        try:
            # Try exact match first
            params = {
                "q": common_name,
                "is_active": "true",
                "per_page": "10",  # Get more results for better matching
            }
            response = requests.get(TAXON_ENDPOINT, params=params, timeout=10)
            
            # Handle rate limiting
            if response.status_code == 429:
                wait_time = 2 ** attempt
                logger.warning(f"Rate limited (429). Waiting {wait_time}s before retry...")
                time.sleep(wait_time)
                continue
            
            response.raise_for_status()
            
            data = response.json()
            search_term = common_name.lower()
            best_match = None
            best_similarity = 0.0
            
            if data.get("results") and len(data["results"]) > 0:
                # Try to find best match (must be valid insect AND good name match)
                for result in data["results"]:
                    taxon = result
                    
                    # Reject if it's definitely NOT an insect (mammal, bird, plant, etc.)
                    if not is_valid_insect(taxon):
                        continue
                    
                    result_common = taxon.get("preferred_common_name", "").lower()
                    result_name = taxon.get("name", "").lower()
                    
                    # Check for exact match on common name
                    if search_term == result_common:
                        logger.info(f"'{common_name}' -> exact match")
                        return extract_taxon_data(taxon)
                    
                    # Track best partial match
                    similarity = name_similarity(search_term, result_common or result_name)
                    if similarity > best_similarity:
                        best_match = taxon
                        best_similarity = similarity
                
                # Use best match if any valid match found
                # Lowered threshold: accept any match > 0.3 similarity
                if best_match and best_similarity > 0.3:
                    matched = extract_taxon_data(best_match)
                    logger.info(f"'{common_name}' -> matched to '{matched['common_name']}' (similarity: {best_similarity:.1%})")
                    return matched
                
                # If we got results but no good matches, try partial search
                logger.info(f"No good match for '{common_name}' in top results - trying partial search")
            
            # Try partial search with just first word if multi-word name
            if " " in common_name:
                first_word = common_name.split()[0]
                logger.info(f"Trying partial search with '{first_word}'")
                params["q"] = first_word
                response = requests.get(TAXON_ENDPOINT, params=params, timeout=10)
                
                if response.status_code == 429:
                    wait_time = 2 ** attempt
                    logger.warning(f"Rate limited (429). Waiting {wait_time}s before retry...")
                    time.sleep(wait_time)
                    continue
                
                response.raise_for_status()
                data = response.json()
                best_match = None
                best_similarity = 0.0
                
                if data.get("results") and len(data["results"]) > 0:
                    for result in data["results"]:
                        taxon = result
                        
                        # Skip rejected taxa
                        if not is_valid_insect(taxon):
                            continue
                        
                        result_common = taxon.get("preferred_common_name", "").lower()
                        result_name = taxon.get("name", "").lower()
                        
                        # Check similarity
                        similarity = name_similarity(search_term, result_common or result_name)
                        if similarity > best_similarity:
                            best_match = taxon
                            best_similarity = similarity
                    
                    # Accept partial match with lower threshold (>0.2)
                    if best_match and best_similarity > 0.2:
                        matched = extract_taxon_data(best_match)
                        logger.info(f"'{common_name}' -> partial match to '{matched['common_name']}'")
                        return matched
            
            logger.warning(f"Could not find match for '{common_name}'")
            return None
            
        except requests.RequestException as e:
            if "429" in str(e):
                wait_time = 2 ** attempt
                logger.warning(f"Rate limited. Waiting {wait_time}s before retry...")
                time.sleep(wait_time)
                continue
            logger.error(f"Error searching for '{common_name}': {e}")
            return None
    
    logger.error(f"Failed to find '{common_name}' after {MAX_RETRIES} retries")
    return None


def fetch_images_for_taxon(taxon_id: int, limit: int = 10) -> List[Dict]:
    """
    Fetch observation images for a specific taxon from iNaturalist.
    Includes rate limit handling.
    
    Args:
        taxon_id: The iNaturalist taxon ID
        limit: Maximum number of images to fetch
        
    Returns:
        List of image data dictionaries with URLs and details
    """
    time.sleep(API_DELAY)  # Respect API rate limits
    
    for attempt in range(MAX_RETRIES):
        try:
            params = {
                "taxon_id": taxon_id,
                "per_page": limit,
                "order_by": "votes",
                "order": "desc",
            }
            response = requests.get(OBSERVATIONS_ENDPOINT, params=params, timeout=10)
            
            # Handle rate limiting
            if response.status_code == 429:
                wait_time = 2 ** attempt
                logger.warning(f"Rate limited (429). Waiting {wait_time}s before retry...")
                time.sleep(wait_time)
                continue
            
            response.raise_for_status()
            
            data = response.json()
            images = []
            
            for obs in data.get("results", []):
                if obs.get("photos"):
                    for photo in obs["photos"]:
                        images.append({
                            "url": photo.get("url", "").replace("square", "medium"),
                            "attribution": photo.get("attribution", "Unknown"),
                            "observation_id": obs.get("id", ""),
                            "observer": obs.get("user", {}).get("name", "Unknown"),
                        })
            
            return images
            
        except requests.RequestException as e:
            if "429" in str(e):
                wait_time = 2 ** attempt
                logger.warning(f"Rate limited. Waiting {wait_time}s before retry...")
                time.sleep(wait_time)
                continue
            logger.error(f"Error fetching images for taxon {taxon_id}: {e}")
            return []
    
    return []


def extract_taxon_data(taxon: dict) -> dict:
    """
    Extract relevant information from an iNaturalist taxon object.
    
    Args:
        taxon: The taxon object from iNaturalist API
        
    Returns:
        Dictionary with extracted data
    """
    # Get the preferred common name
    common_name = taxon.get("preferred_common_name", taxon.get("name", ""))
    
    # Get scientific name
    scientific_name = taxon.get("name", "")
    
    # Get description
    description = ""
    if taxon.get("wikipedia_summary"):
        description = taxon["wikipedia_summary"]
    elif taxon.get("matched_term"):
        description = f"Matched term: {taxon['matched_term']}"
    
    # Get image URL
    image_url = ""
    if taxon.get("default_photo"):
        image_url = taxon["default_photo"].get("medium_url", "")
    
    return {
        "common_name": common_name,
        "scientific_name": scientific_name,
        "description": description[:500],  # Limit description length
        "image_url": image_url,
        "taxon_id": taxon.get("id", ""),
    }


def fetch_insect_list(insect_names: list) -> list:
    """
    Fetch data for a list of insects.
    
    Args:
        insect_names: List of insect common names to search for
        
    Returns:
        List of dictionaries containing insect data
    """
    results = []
    total = len(insect_names)
    
    for idx, name in enumerate(insect_names, 1):
        logger.info(f"Fetching data for '{name}' ({idx}/{total})")
        data = search_insect(name)
        
        if data:
            results.append(data)
        else:
            # Add a placeholder for failed searches
            results.append({
                "common_name": name,
                "scientific_name": "Not found",
                "description": "Data not found in iNaturalist",
                "image_url": "",
                "taxon_id": "",
            })
    
    return results


def save_to_csv(data: list, output_file: str = "insect_data.csv") -> None:
    """
    Save insect data to a CSV file.
    
    Args:
        data: List of dictionaries containing insect data
        output_file: Path to the output CSV file
    """
    if not data:
        logger.warning("No data to save")
        return
    
    fieldnames = ["common_name", "scientific_name", "description", "image_url", "taxon_id"]
    
    try:
        with open(output_file, "w", newline="", encoding="utf-8") as csvfile:
            writer = csv.DictWriter(csvfile, fieldnames=fieldnames)
            writer.writeheader()
            writer.writerows(data)
        
        logger.info(f"Data saved to '{output_file}'")
    except IOError as e:
        logger.error(f"Error saving to CSV: {e}")
        raise


def save_to_excel(data: list, output_file: str = "insect_data.xlsx") -> None:
    """
    Save insect data to an Excel file.
    
    Requires openpyxl to be installed: pip install openpyxl
    
    Args:
        data: List of dictionaries containing insect data
        output_file: Path to the output Excel file
    """
    try:
        import openpyxl
        from openpyxl.styles import Font, PatternFill, Alignment
    except ImportError:
        logger.error("openpyxl is required for Excel output. Install it with: pip install openpyxl")
        return
    
    if not data:
        logger.warning("No data to save")
        return
    
    try:
        workbook = openpyxl.Workbook()
        worksheet = workbook.active
        worksheet.title = "Insects"
        
        # Set headers
        headers = ["Common Name", "Scientific Name", "Description", "Image URL", "Taxon ID"]
        worksheet.append(headers)
        
        # Style header row
        header_fill = PatternFill(start_color="366092", end_color="366092", fill_type="solid")
        header_font = Font(color="FFFFFF", bold=True)
        
        for cell in worksheet[1]:
            cell.fill = header_fill
            cell.font = header_font
            cell.alignment = Alignment(horizontal="center", vertical="center")
        
        # Add data rows
        for item in data:
            worksheet.append([
                item.get("common_name", ""),
                item.get("scientific_name", ""),
                item.get("description", ""),
                item.get("image_url", ""),
                item.get("taxon_id", ""),
            ])
        
        # Adjust column widths
        worksheet.column_dimensions["A"].width = 25
        worksheet.column_dimensions["B"].width = 25
        worksheet.column_dimensions["C"].width = 50
        worksheet.column_dimensions["D"].width = 50
        worksheet.column_dimensions["E"].width = 12
        
        # Wrap text in description column
        for row in worksheet.iter_rows(min_row=2, max_row=len(data) + 1, min_col=3, max_col=3):
            for cell in row:
                cell.alignment = Alignment(wrap_text=True, vertical="top")
        
        workbook.save(output_file)
        logger.info(f"Data saved to '{output_file}'")
        
    except IOError as e:
        logger.error(f"Error saving to Excel: {e}")
        raise


class ImageSelectionGUI:
    """GUI for selecting reference images for insects."""
    
    def __init__(self, insect_data: List[Dict]):
        """
        Initialize the GUI.
        
        Args:
            insect_data: List of insect data dictionaries
        """
        self.insect_data = insect_data
        self.current_index = 0
        self.insect_images = {}
        self.selected_images = {}
        self.loading_images = set()
        
        self.root = tk.Tk()
        self.root.title("iNaturalist Image Selector")
        self.root.geometry("900x700")
        self.root.protocol("WM_DELETE_WINDOW", self.on_closing)
        
        self.setup_ui()
        self.load_images_for_current()
    
    def setup_ui(self):
        """Set up the user interface."""
        # Title
        title_frame = ttk.Frame(self.root)
        title_frame.pack(pady=10, padx=10, fill=tk.X)
        
        ttk.Label(title_frame, text="Select Reference Images for Each Insect", 
                 font=("Arial", 14, "bold")).pack(side=tk.LEFT)
        
        # Progress
        self.progress_label = ttk.Label(title_frame, text="")
        self.progress_label.pack(side=tk.RIGHT)
        
        # Insect info
        self.info_frame = ttk.LabelFrame(self.root, text="Insect Information", padding=10)
        self.info_frame.pack(pady=10, padx=10, fill=tk.X)
        
        self.common_name_label = ttk.Label(self.info_frame, text="", font=("Arial", 12, "bold"))
        self.common_name_label.pack(anchor=tk.W)
        
        self.scientific_name_label = ttk.Label(self.info_frame, text="", font=("Arial", 10, "italic"))
        self.scientific_name_label.pack(anchor=tk.W)
        
        self.description_text = tk.Text(self.info_frame, height=3, width=80, wrap=tk.WORD)
        self.description_text.pack(fill=tk.BOTH, expand=True, pady=5)
        
        # Images frame
        self.images_frame = ttk.LabelFrame(self.root, text="Available Images", padding=10)
        self.images_frame.pack(pady=10, padx=10, fill=tk.BOTH, expand=True)
        
        # Canvas for image scrolling
        self.canvas = tk.Canvas(self.images_frame, height=250, bg="gray90")
        self.canvas.pack(side=tk.LEFT, fill=tk.BOTH, expand=True)
        
        scrollbar = ttk.Scrollbar(self.images_frame, orient=tk.HORIZONTAL, command=self.canvas.xview)
        scrollbar.pack(side=tk.BOTTOM, fill=tk.X)
        self.canvas.configure(xscrollcommand=scrollbar.set)
        
        self.images_container = ttk.Frame(self.canvas)
        self.canvas.create_window((0, 0), window=self.images_container, anchor=tk.NW)
        
        # Controls
        control_frame = ttk.Frame(self.root)
        control_frame.pack(pady=10, padx=10, fill=tk.X)
        
        ttk.Button(control_frame, text="< Previous", command=self.previous_insect).pack(side=tk.LEFT, padx=5)
        ttk.Button(control_frame, text="Next >", command=self.next_insect).pack(side=tk.LEFT, padx=5)
        ttk.Button(control_frame, text="Skip (Use Default)", command=self.skip_insect).pack(side=tk.LEFT, padx=5)
        
        ttk.Button(control_frame, text="Finish & Export", command=self.finish, 
                  style="Accent.TButton").pack(side=tk.RIGHT, padx=5)
        
        self.update_display()
    
    def load_images_for_current(self):
        """Load images for the current insect in a background thread."""
        if self.current_index >= len(self.insect_data):
            return
        
        insect = self.insect_data[self.current_index]
        common_name = insect["common_name"]
        taxon_id = insect.get("taxon_id")
        
        if not taxon_id or common_name in self.loading_images:
            return
        
        self.loading_images.add(common_name)
        
        def load():
            if taxon_id:
                images = fetch_images_for_taxon(int(taxon_id), limit=15)
                self.insect_images[common_name] = images
            self.loading_images.discard(common_name)
            self.root.after(0, self.update_images_display)
        
        thread = threading.Thread(target=load, daemon=True)
        thread.start()
    
    def update_display(self):
        """Update the display for the current insect."""
        if self.current_index >= len(self.insect_data):
            return
        
        insect = self.insect_data[self.current_index]
        
        self.common_name_label.config(text=insect.get("common_name", "Unknown"))
        self.scientific_name_label.config(text=f"Scientific: {insect.get('scientific_name', 'Unknown')}")
        
        self.description_text.config(state=tk.NORMAL)
        self.description_text.delete(1.0, tk.END)
        self.description_text.insert(1.0, insect.get("description", "No description available"))
        self.description_text.config(state=tk.DISABLED)
        
        progress = f"Insect {self.current_index + 1} / {len(self.insect_data)}"
        self.progress_label.config(text=progress)
        
        self.update_images_display()
    
    def update_images_display(self):
        """Update the images display."""
        # Clear previous images
        for widget in self.images_container.winfo_children():
            widget.destroy()
        
        insect = self.insect_data[self.current_index]
        common_name = insect["common_name"]
        
        images = self.insect_images.get(common_name, [])
        
        if not images:
            if common_name in self.loading_images:
                label = ttk.Label(self.images_container, text="Loading images...", font=("Arial", 10))
            else:
                label = ttk.Label(self.images_container, text="No images found", font=("Arial", 10))
            label.pack(padx=10, pady=10)
            return
        
        for idx, image_data in enumerate(images):
            self.create_image_button(image_data, idx)
        
        self.images_container.update_idletasks()
        self.canvas.configure(scrollregion=self.canvas.bbox("all"))
    
    def create_image_button(self, image_data: Dict, idx: int):
        """Create a clickable image button with async loading."""
        frame = ttk.Frame(self.images_container, relief=tk.RAISED, borderwidth=2)
        frame.pack(side=tk.LEFT, padx=5, pady=5)
        
        # Create placeholder immediately
        btn_frame = tk.Frame(frame, width=120, height=120, bg="lightgray")
        btn_frame.pack()
        
        loading_label = ttk.Label(btn_frame, text="Loading...", font=("Arial", 8))
        loading_label.place(relx=0.5, rely=0.5, anchor=tk.CENTER)
        
        info = f"By: {image_data['observer']}\nObs ID: {image_data['observation_id']}"
        info_label = ttk.Label(frame, text=info, font=("Arial", 7), wraplength=120)
        info_label.pack(fill=tk.X, padx=2, pady=2)
        
        # Load image in background
        def load_image():
            try:
                response = requests.get(image_data["url"], timeout=5)
                img = Image.open(BytesIO(response.content))
                img.thumbnail((120, 120))  # Smaller preview
                
                photo = ImageTk.PhotoImage(img)
                
                # Update button in main thread
                self.root.after(0, lambda: self._update_image_button(btn_frame, photo, image_data))
            except Exception as e:
                self.root.after(0, lambda: self._show_image_error(btn_frame))
                logger.error(f"Error loading image {idx}: {e}")
        
        thread = threading.Thread(target=load_image, daemon=True)
        thread.start()
    
    def _update_image_button(self, btn_frame, photo, image_data):
        """Update button with loaded image (called from main thread)."""
        # Clear the frame
        for widget in btn_frame.winfo_children():
            widget.destroy()
        
        btn = tk.Button(btn_frame, image=photo, width=120, height=120,
                       command=lambda: self.select_image(image_data))
        btn.image = photo  # Keep a reference
        btn.pack()
    
    def _show_image_error(self, btn_frame):
        """Show error message in image button (called from main thread)."""
        for widget in btn_frame.winfo_children():
            widget.destroy()
        
        error_label = ttk.Label(btn_frame, text="❌", font=("Arial", 20))
        error_label.pack(expand=True)
    
    def select_image(self, image_data: Dict):
        """Select an image for the current insect."""
        insect = self.insect_data[self.current_index]
        self.selected_images[insect["common_name"]] = image_data["url"]
        messagebox.showinfo("Selected", f"Image selected for {insect['common_name']}")
        self.next_insect()
    
    def previous_insect(self):
        """Go to the previous insect."""
        if self.current_index > 0:
            self.current_index -= 1
            self.update_display()
            self.load_images_for_current()
    
    def next_insect(self):
        """Go to the next insect."""
        if self.current_index < len(self.insect_data) - 1:
            self.current_index += 1
            self.update_display()
            self.load_images_for_current()
    
    def skip_insect(self):
        """Skip the current insect and move to the next."""
        self.next_insect()
    
    def finish(self):
        """Finish image selection and export."""
        # Update insect data with selected images
        for insect in self.insect_data:
            common_name = insect["common_name"]
            if common_name in self.selected_images:
                insect["selected_image_url"] = self.selected_images[common_name]
            else:
                insect["selected_image_url"] = insect.get("image_url", "")
        
        # Save results
        output_dir = Path(__file__).parent / "data"
        output_dir.mkdir(exist_ok=True)
        
        csv_file = output_dir / "insect_data.csv"
        excel_file = output_dir / "insect_data.xlsx"
        
        # Create export data with selected images
        export_data = []
        for insect in self.insect_data:
            export_data.append({
                "common_name": insect["common_name"],
                "scientific_name": insect["scientific_name"],
                "description": insect["description"],
                "image_url": insect.get("selected_image_url", insect.get("image_url", "")),
                "taxon_id": insect.get("taxon_id", ""),
            })
        
        save_to_csv(export_data, str(csv_file))
        save_to_excel(export_data, str(excel_file))
        
        messagebox.showinfo("Complete", f"Data exported successfully!\n\nFiles saved to:\n{output_dir}")
        self.root.quit()
    
    def on_closing(self):
        """Handle window closing."""
        if messagebox.askokcancel("Quit", "Are you sure? Any selections will be lost."):
            self.root.quit()
    
    def run(self):
        """Run the GUI."""
        self.root.mainloop()


def main_gui(insects_to_fetch: List[str] = None):
    """
    Run the GUI version of the script.
    
    Args:
        insects_to_fetch: List of insect names to fetch. If None, loads from insects_list.txt
    """
    if insects_to_fetch is None:
        try:
            insects_to_fetch = load_insects_from_file()
        except FileNotFoundError as e:
            logger.error(str(e))
            return
    
    logger.info(f"Starting GUI to fetch data for {len(insects_to_fetch)} insects...")
    
    # Fetch initial insect data
    insect_data = fetch_insect_list(insects_to_fetch)
    
    # Launch GUI
    gui = ImageSelectionGUI(insect_data)
    gui.run()


def main():
    """Main entry point for the script."""
    root = tk.Tk()
    root.withdraw()
    
    # Load insects from file
    try:
        insects_to_fetch = load_insects_from_file()
    except FileNotFoundError as e:
        messagebox.showerror("Error", str(e))
        root.destroy()
        return
    
    # Ask user whether to use CLI or GUI
    result = messagebox.askyesno(
        "iNaturalist Insect Data Fetcher",
        f"Would you like to use the interactive GUI?\n\n"
        f"Found {len(insects_to_fetch)} insects to process\n\n"
        f"Yes = Use GUI with image selection\n"
        f"No = Quick CLI mode (uses default images)"
    )
    
    root.destroy()
    
    if result:
        main_gui(insects_to_fetch)
    else:
        # CLI mode
        logger.info(f"Starting to fetch data for {len(insects_to_fetch)} insects...")
        insect_data = fetch_insect_list(insects_to_fetch)
        
        output_dir = Path(__file__).parent / "data"
        output_dir.mkdir(exist_ok=True)
        
        csv_file = output_dir / "insect_data.csv"
        excel_file = output_dir / "insect_data.xlsx"
        
        save_to_csv(insect_data, str(csv_file))
        save_to_excel(insect_data, str(excel_file))
        
        logger.info("Done!")


if __name__ == "__main__":
    main()
