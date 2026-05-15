# InsightFace REST service (Python)
# Bookworm: stable Debian apt + wheels; avoid `python:3.11-slim` tracking trixie/sid (CI apt 404s).
FROM python:3.11-slim-bookworm

ENV DEBIAN_FRONTEND=noninteractive \
	PIP_DISABLE_PIP_VERSION_CHECK=1 \
	PIP_NO_CACHE_DIR=1

WORKDIR /app

COPY requirements.txt .

# OpenMP for onnxruntime; GL/GObject for OpenCV; ca-certificates for model download during bake step.
RUN apt-get -o Acquire::Retries=5 update \
	&& apt-get install -y --no-install-recommends \
		ca-certificates \
		build-essential \
		libgl1 \
		libglib2.0-0 \
		libgomp1 \
	&& pip install -r requirements.txt \
	&& apt-get purge -y build-essential \
	&& apt-get autoremove -y \
	&& rm -rf /var/lib/apt/lists/*

# Bake buffalo_l weights at build time so startup is fast and probes are not racing a large download.
RUN python -c "\
from insightface.app import FaceAnalysis; \
fa = FaceAnalysis(name='buffalo_l', root='/app'); \
fa.prepare(ctx_id=-1, det_size=(640, 640)); \
print('InsightFace buffalo_l cached under /app')"

COPY main.py .

ENV INSIGHTFACE_CTX_ID=-1
EXPOSE 8000

CMD ["uvicorn", "main:app", "--host", "0.0.0.0", "--port", "8000"]
