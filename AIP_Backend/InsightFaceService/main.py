"""
InsightFace REST API: face detection and embedding for offender recognition.
Endpoints: POST /detect, POST /embed
"""
import os
import logging
from typing import Optional

import cv2
import numpy as np
from fastapi import FastAPI, HTTPException, Request
from fastapi.responses import JSONResponse
from pydantic import BaseModel

logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)

app = FastAPI(title="InsightFace Service", version="1.0.0")

# Global FaceAnalysis app (loaded once at startup)
face_app = None


class FaceRectangle(BaseModel):
    left: int
    top: int
    width: int
    height: int


class DetectResponse(BaseModel):
    faceDetected: bool
    faceRectangle: Optional[FaceRectangle] = None


class EmbedResponse(BaseModel):
    faceDetected: bool
    embedding: Optional[list[float]] = None
    faceRectangle: Optional[FaceRectangle] = None


def _bytes_to_image(body: bytes):
    """Decode image bytes (JPEG/PNG) to BGR numpy array for OpenCV/InsightFace."""
    arr = np.frombuffer(body, dtype=np.uint8)
    img = cv2.imdecode(arr, cv2.IMREAD_COLOR)
    if img is None:
        raise ValueError("Could not decode image")
    return img


def _bbox_to_rect(bbox):
    """InsightFace bbox is [x1, y1, x2, y2]. Convert to left, top, width, height."""
    x1, y1, x2, y2 = map(int, bbox)
    return FaceRectangle(left=x1, top=y1, width=x2 - x1, height=y2 - y1)


@app.on_event("startup")
def load_model():
    global face_app
    try:
        from insightface.app import FaceAnalysis
        # Use buffalo_l for detection + recognition; ctx_id=-1 for CPU, 0 for GPU
        ctx_id = int(os.environ.get("INSIGHTFACE_CTX_ID", "-1"))
        face_app = FaceAnalysis(name="buffalo_l", root=os.path.dirname(os.path.abspath(__file__)))
        face_app.prepare(ctx_id=ctx_id, det_size=(640, 640))
        logger.info("InsightFace model loaded successfully")
    except Exception as e:
        logger.error("Failed to load InsightFace model: %s", e)
        raise


@app.get("/health")
def health():
    return {"status": "ok", "model_loaded": face_app is not None}


def _run_detect(body: bytes) -> DetectResponse:
    if not body:
        return DetectResponse(faceDetected=False, faceRectangle=None)
    img = _bytes_to_image(body)
    try:
        faces = face_app.get(img)
    except Exception as e:
        logger.warning("Face detection failed: %s", e)
        return DetectResponse(faceDetected=False, faceRectangle=None)
    if not faces:
        return DetectResponse(faceDetected=False, faceRectangle=None)
    bbox = faces[0].bbox
    return DetectResponse(faceDetected=True, faceRectangle=_bbox_to_rect(bbox))


@app.post("/detect", response_model=DetectResponse)
async def detect(request: Request):
    """Detect face in image. Body: raw image bytes (application/octet-stream)."""
    if face_app is None:
        raise HTTPException(status_code=503, detail="Model not loaded")
    body = await request.body()
    return _run_detect(body)


def _run_embed(body: bytes) -> EmbedResponse:
    if not body:
        return EmbedResponse(faceDetected=False, embedding=None, faceRectangle=None)
    try:
        img = _bytes_to_image(body)
    except ValueError as e:
        raise HTTPException(status_code=400, detail=str(e))
    try:
        faces = face_app.get(img)
    except Exception as e:
        logger.warning("Face analysis failed: %s", e)
        return EmbedResponse(faceDetected=False, embedding=None, faceRectangle=None)
    if not faces:
        return EmbedResponse(faceDetected=False, embedding=None, faceRectangle=None)
    face = faces[0]
    emb = face.normed_embedding
    if emb is None:
        return EmbedResponse(faceDetected=False, embedding=None, faceRectangle=None)
    embedding_list = emb.tolist()
    rect = _bbox_to_rect(face.bbox) if face.bbox is not None else None
    return EmbedResponse(faceDetected=True, embedding=embedding_list, faceRectangle=rect)


@app.post("/embed", response_model=EmbedResponse)
async def embed(request: Request):
    """Extract face embedding. Body: raw image bytes (application/octet-stream)."""
    if face_app is None:
        raise HTTPException(status_code=503, detail="Model not loaded")
    body = await request.body()
    return _run_embed(body)


if __name__ == "__main__":
    import uvicorn
    uvicorn.run(app, host="0.0.0.0", port=8000)
