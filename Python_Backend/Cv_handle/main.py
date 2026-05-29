from fastapi import FastAPI, HTTPException
from pydantic import BaseModel
import requests
import os
import uuid
import json
from DataExtract import extract_structured_cv
from service import map_cv_to_schema
from DataHandler import DataHandler

app = FastAPI()
handler = DataHandler()

PDF_DIR = "pdfs"
if not os.path.exists(PDF_DIR):
    os.makedirs(PDF_DIR)

# ✅ Define the exact JSON payload expected from React
class PDFProcessRequest(BaseModel):
    userId: str
    cvUrl: str

@app.post("/process-pdf")
async def process_cv(payload: PDFProcessRequest):
    # 1. Generate a temporary local filename
    temp_filename = f"{uuid.uuid4()}.pdf"
    file_path = os.path.join(PDF_DIR, temp_filename)
    
    try:
        # 2. Download the PDF from Cloudinary securely
        response = requests.get(payload.cvUrl, stream=True)
        response.raise_for_status()
        with open(file_path, 'wb') as f:
            for chunk in response.iter_content(chunk_size=8192):
                f.write(chunk)

        # 3. Extract text using DataExtractor.py
        raw_data = extract_structured_cv(file_path)
        
        # 4. Format text for the AI Brain
        cv_text = ""
        for sec, lines in raw_data.get("_raw_sections", {}).items():
            cv_text += f"\n{sec.upper()}\n" + "\n".join(lines)

        # 5. Send to AI Brain (service.py)
        structured_json = map_cv_to_schema(cv_text)
        if not structured_json:
            raise HTTPException(status_code=500, detail="AI Brain failed to process CV")

        # Inject the Cloudinary URL so DataHandler saves it to the database
        structured_json['cvUrl'] = payload.cvUrl

        # 6. Save everything to PostgreSQL (DataHandler.py)
        db_success = handler.save_to_postgres(payload.userId, structured_json)

        return {
            "status": "success" if db_success else "database_error",
            "message": "CV processed and synchronized to database successfully",
            "extracted_data": structured_json
        }

    except Exception as e:
        raise HTTPException(status_code=500, detail=str(e))
    finally:
        # 7. GUARANTEED CLEANUP: Delete the PDF from the Python server
        if os.path.exists(file_path):
            os.remove(file_path)
        
        json_temp_name = f"{os.path.splitext(temp_filename)[0]}_extracted.json"
        if os.path.exists(json_temp_name):
            os.remove(json_temp_name)

if __name__ == "__main__":
    import uvicorn
    uvicorn.run(app, host="0.0.0.0", port=8000)