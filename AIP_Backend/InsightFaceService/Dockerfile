# InsightFace REST service (Python)
FROM python:3.11-slim

WORKDIR /app

# Install system deps for OpenCV/ONNX if needed
RUN apt-get update && apt-get install -y --no-install-recommends \
    libgl1-mesa-glx \
    libglib2.0-0 \
    && rm -rf /var/lib/apt/lists/*

COPY requirements.txt .
RUN pip install --no-cache-dir -r requirements.txt

COPY main.py .

# Models are downloaded at runtime on first request (InsightFace downloads buffalo_l)
ENV INSIGHTFACE_CTX_ID=-1
EXPOSE 8000

CMD ["uvicorn", "main:app", "--host", "0.0.0.0", "--port", "8000"]
