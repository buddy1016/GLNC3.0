# Tesseract Language Data for Invoice OCR

The invoice OCR endpoint requires French language data for processing French invoices.

## Setup

1. Download `fra.traineddata` from the official tessdata repository:
   - **Fast (recommended):** https://github.com/tesseract-ocr/tessdata/raw/main/fra.traineddata
   - **Best quality:** https://github.com/tesseract-ocr/tessdata_best/raw/main/fra.traineddata

2. Place the file in this folder: `glnc_webpart/tessdata/fra.traineddata`

## PowerShell (Windows)

```powershell
Invoke-WebRequest -Uri "https://github.com/tesseract-ocr/tessdata/raw/main/fra.traineddata" -OutFile "fra.traineddata" -UseBasicParsing
```

## curl

```bash
curl -L -o fra.traineddata https://github.com/tesseract-ocr/tessdata/raw/main/fra.traineddata
```

After placing `fra.traineddata` in this folder, the `POST /api/app/invoice/ocr` endpoint will use it for French invoice extraction.
