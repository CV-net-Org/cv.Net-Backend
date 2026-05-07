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

# 2. Import the new Enhancer service
from Enhanser.enhancer_service import enhance_text_with_ai

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
@app.post("/sync-linkedin")
async def sync_linkedin(user_id: str = Form(...), profile_url: str = Form(...)):
    try:
        # Scrape and Map
        raw_data = get_linkedin_data(profile_url)
        structured_data = map_linkedin_to_schema(raw_data)
        
        # Smart Merge to PostgreSQL (Additive mode)
        li_handler = LinkedInDataHandler()
        success = li_handler.merge_data(user_id, structured_data)
        
        return {"status": "success", "message": "LinkedIn Merged", "data": structured_data}
    except Exception as e:
        raise HTTPException(status_code=500, detail=str(e))

# --- ENDPOINT 3: AI TEXT ENHANCER (MISTRAL-NEMOTRON) ---
@app.post("/ai/enhance")
async def enhance_text_endpoint(
    text: str = Form(...), 
    mode: str = Form(...), # 'summarize', 'formalize', or 'custom'
    instruction: str = Form(None)
):
    result = enhance_text_with_ai(text, mode, instruction)
    return {"status": "success", "enhanced_text": result}

# --- START THE ENGINE ---
if __name__ == "__main__":
    print("📡 CV.net Unified Engine running on http://localhost:8000")
    uvicorn.run(app, host="0.0.0.0", port=8000)