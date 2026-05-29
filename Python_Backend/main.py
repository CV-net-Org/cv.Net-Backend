import os
import shutil
import uvicorn
from fastapi import FastAPI, UploadFile, File, Form, HTTPException

# 1. Imports from your sub-folders (using folder.file syntax)
from Cv_handle.DataExtract import extract_structured_cv
from Cv_handle.service import map_cv_to_schema as map_pdf_to_schema
from Cv_handle.DataHandler import DataHandler as PDFDataHandler

from fill_with_Linkedinn.linkedin_service import get_linkedin_data, map_linkedin_to_schema
from fill_with_Linkedinn.linkedin_data_handler import LinkedInDataHandler


app = FastAPI()

# Configuration
PDF_DIR = "pdfs"
if not os.path.exists(PDF_DIR):
    os.makedirs(PDF_DIR)

# --- ENDPOINT 1: CV EXTRACTION (PDF) ---
@app.post("/extract-cv")
async def process_cv(user_id: str = Form(...), file: UploadFile = File(...)):
    file_path = os.path.join(PDF_DIR, file.filename)
    with open(file_path, "wb") as buffer:
        shutil.copyfileobj(file.file, buffer)

    try:
        # Extract text and map to Master Schema v2.0
        raw_data = extract_structured_cv(file_path)
        cv_text = ""
        for sec, lines in raw_data.get("_raw_sections", {}).items():
            cv_text += f"\n{sec.upper()}\n" + "\n".join(lines)

        structured_json = map_pdf_to_schema(cv_text)
        
        # Save to PostgreSQL (Wipe and Replace mode)
        pdf_handler = PDFDataHandler()
        db_success = pdf_handler.save_to_postgres(user_id, structured_json)

        os.remove(file_path) # Cleanup
        return {"status": "success", "message": "CV Processed", "data": structured_json}
    except Exception as e:
        if os.path.exists(file_path): os.remove(file_path)
        raise HTTPException(status_code=500, detail=str(e))

# --- ENDPOINT 2: LINKEDIN SYNC (PILOTERR) ---
# --- ENDPOINT 2: LINKEDIN SYNC (PILOTERR) ---
@app.post("/sync-linkedin")
async def sync_linkedin(user_id: str = Form(...), profile_url: str = Form(...)):
    print(f"\n--- STARTING LINKEDIN SYNC ---")
    print(f"User ID: {user_id}")
    print(f"Target URL: {profile_url}")
    
    try:
        # 1. Scrape
        print("1. Fetching LinkedIn Data from Piloterr...")
        raw_data = get_linkedin_data(profile_url)
        if not raw_data:
            print("❌ FAILURE: Piloterr returned empty or invalid data.")
            raise HTTPException(status_code=500, detail="Failed to scrape data from LinkedIn.")
        print(f"   -> Successfully fetched data. Payload size: {len(str(raw_data))} bytes")

        # 2. Map
        print("2. Sending data to AI for Schema Mapping...")
        structured_data = map_linkedin_to_schema(raw_data)
        if not structured_data:
            print("❌ FAILURE: AI Mapping returned None. Check AI logs.")
            raise HTTPException(status_code=500, detail="AI data mapping failed.")
        print(f"   -> AI Mapping successful. Found {len(structured_data.get('experience', []))} experience records.")

        # 3. Merge
        print("3. Executing Smart Merge to PostgreSQL...")
        li_handler = LinkedInDataHandler()
        success = li_handler.merge_data(user_id, structured_data)
        
        if not success:
            print("❌ FAILURE: Database merge aborted (likely missing active profile).")
            raise HTTPException(status_code=500, detail="Database merge failed. User profile might not exist.")

        print("✅ LINKEDIN SYNC COMPLETELY SUCCESSFUL!")
        return {"status": "success", "message": "LinkedIn Merged", "data": structured_data}
        
    except Exception as e:
        print(f"🔥 CRITICAL EXCEPTION CAUGHT IN MAIN: {str(e)}")
        raise HTTPException(status_code=500, detail=str(e))

# --- START THE ENGINE ---
if __name__ == "__main__":
    print("📡 CV.net Unified Engine running on http://localhost:8000")
    uvicorn.run(app, host="0.0.0.0", port=8000)