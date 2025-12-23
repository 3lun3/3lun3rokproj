import cv2
import numpy as np
import subprocess
import os

# --- CONFIGURATION (Start with your current C# numbers) ---
OCR_OFFSET_X = -40
OCR_OFFSET_Y = -57
OCR_WIDTH = 80
OCR_HEIGHT = 24

# Path to your ADB
ADB_PATH = r"C:\adb\platform-tools\adb.exe" 
ASSET_PATH = "assets/btn_send.png"

def get_adb_screenshot():
    print("Capturing screen...")
    pipe = subprocess.Popen([ADB_PATH, "exec-out", "screencap", "-p"], stdout=subprocess.PIPE)
    image_bytes = pipe.stdout.read()
    if not image_bytes: return None
    image_array = np.frombuffer(image_bytes, np.uint8)
    return cv2.imdecode(image_array, cv2.IMREAD_COLOR)

def main():
    screen = get_adb_screenshot()
    if screen is None: return

    if not os.path.exists(ASSET_PATH):
        print(f"Error: {ASSET_PATH} not found.")
        return
    
    template = cv2.imread(ASSET_PATH)
    h, w = template.shape[:2]

    # Find Top-Left
    res = cv2.matchTemplate(screen, template, cv2.TM_CCOEFF_NORMED)
    _, max_val, _, max_loc = cv2.minMaxLoc(res)
    
    if max_val < 0.8:
        print(f"Warning: Low confidence ({max_val:.2f})")

    tl_x, tl_y = max_loc

    # --- CALCULATE CENTER (Same as C# Logic) ---
    center_x = int(tl_x + (w / 2))
    center_y = int(tl_y + (h / 2))
    
    print(f"Anchor (Center): ({center_x}, {center_y})")

    # --- APPLY OFFSETS FROM CENTER ---
    # This now matches your C# logic exactly
    ocr_x = center_x + OCR_OFFSET_X
    ocr_y = center_y + OCR_OFFSET_Y
    
    # DRAWING
    # 1. Blue Dot at Center Anchor
    cv2.circle(screen, (center_x, center_y), 5, (255, 0, 0), -1) 
    
    # 2. Blue Box around the Button
    cv2.rectangle(screen, (tl_x, tl_y), (tl_x + w, tl_y + h), (255, 0, 0), 2)

    # 3. Red Box (The OCR Area)
    cv2.rectangle(screen, (ocr_x, ocr_y), (ocr_x + OCR_WIDTH, ocr_y + OCR_HEIGHT), (0, 0, 255), 2)

    print(f"Final OCR Box: x={ocr_x}, y={ocr_y}")

    cv2.imshow("Debug Vision (Center Anchor)", screen)
    cv2.waitKey(0)
    cv2.destroyAllWindows()

if __name__ == "__main__":
    main()