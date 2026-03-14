# InsightFace REST Service

Face detection and embedding API for offender recognition. Used by the .NET backend when `InsightFace:Enabled` is true.

## Endpoints

- `GET /health` — Health check
- `POST /detect` — Body: raw image bytes (`application/octet-stream`). Returns `{ faceDetected, faceRectangle }`.
- `POST /embed` — Body: raw image bytes. Returns `{ faceDetected, embedding, faceRectangle }`.

## Run locally

```bash
python -m venv .venv
.venv\Scripts\activate   # Windows
pip install -r requirements.txt
uvicorn main:app --host 0.0.0.0 --port 8000
```

Or: `python main.py`

## Configuration

- `INSIGHTFACE_CTX_ID`: GPU device id (default `-1` for CPU). Use `0` for first GPU.

## License

InsightFace code is MIT. For recognition models (e.g. buffalo_l), see [InsightFace licensing](https://github.com/deepinsight/insightface#license).
