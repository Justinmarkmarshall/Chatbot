"""Build-time only: fetch the pinned MiniLM artifacts and verify every SHA-256."""
import hashlib
import json
import pathlib
import sys
import urllib.request

manifest = json.loads(pathlib.Path(sys.argv[1]).read_text(encoding="utf-8-sig"))
target = pathlib.Path(sys.argv[2]) / manifest["id"]
target.mkdir(parents=True, exist_ok=True)
for name, expected in manifest["hashes"].items():
    remote = manifest["graph"] if name == "model.onnx" else name
    url = f'https://huggingface.co/{manifest["repository"]}/resolve/{manifest["revision"]}/{remote}'
    digest = hashlib.sha256()
    with urllib.request.urlopen(url, timeout=180) as response, (target / name).open("wb") as output:
        while block := response.read(1024 * 1024):
            output.write(block)
            digest.update(block)
    if digest.hexdigest() != expected:
        raise SystemExit(f"Pinned artifact SHA-256 mismatch: {name}")
    print(f"Verified {name}", flush=True)
