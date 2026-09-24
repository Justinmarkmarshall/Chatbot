"""Preparation only: pin/download artifacts and generate independent HF tokenizer fixtures.
Runtime inference never imports Python. Requires tokenizers==0.22.2.
First use --resolve to record upstream commit IDs; normal runs use the saved manifest.
"""
import argparse
import hashlib
import importlib.metadata
import json
from pathlib import Path
import urllib.request
from tokenizers import Tokenizer

ROOT = Path(__file__).resolve().parent
MANIFEST = ROOT / "models.json"
SPECS = [
    dict(id="minilm", repository="sentence-transformers/all-MiniLM-L6-v2",
         graph="onnx/model.onnx", maxTokens=256, precision="FP32", queryPrefix="", passagePrefix=""),
    dict(id="e5", repository="intfloat/e5-small-v2",
         graph="onnx/model_O4.onnx", maxTokens=512, precision="FP16 upstream O4", queryPrefix="query: ", passagePrefix="passage: "),
]

def read_url(url):
    with urllib.request.urlopen(url, timeout=180) as response:
        return response.read()

def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--resolve", action="store_true")
    args = parser.parse_args()
    if args.resolve:
        if MANIFEST.exists():
            raise RuntimeError("Manifest already exists: do not silently change pinned revisions.")
        specs = SPECS
        for spec in specs:
            spec["revision"] = json.loads(read_url("https://huggingface.co/api/models/" + spec["repository"]))["sha"]
    else:
        specs = json.loads(MANIFEST.read_text(encoding="utf-8"))
    cases = json.loads((ROOT / "cases.json").read_text(encoding="utf-8"))
    for spec in specs:
        directory = ROOT / "models" / spec["id"]
        directory.mkdir(parents=True, exist_ok=True)
        hashes = {}
        for filename in [spec["graph"], "vocab.txt", "tokenizer.json", "tokenizer_config.json", "config.json", "README.md"]:
            target = directory / Path(filename).name
            expected = spec.get("hashes", {}).get(target.name)
            if not target.exists():
                print("Downloading", spec["id"], filename, flush=True)
                payload = read_url(f'https://huggingface.co/{spec["repository"]}/resolve/{spec["revision"]}/{filename}')
                target.write_bytes(payload)
            digest = hashlib.sha256(target.read_bytes()).hexdigest()
            if expected and digest != expected:
                raise RuntimeError(f"Hash mismatch: {target}")
            hashes[target.name] = digest
        spec["hashes"] = hashes
        tokenizer = Tokenizer.from_file(str(directory / "tokenizer.json"))
        tokenizer.no_truncation()
        tokenizer.no_padding()
        texts = cases["tokenizerCases"] + [spec[prefix] + group[field]
            for group in cases["groups"]
            for field, prefix in [("query", "queryPrefix"), ("related", "passagePrefix"), ("unrelated", "passagePrefix")]]
        # Exact model boundary, one beyond it, and a long WordPiece token.
        texts += ["word " * (spec["maxTokens"] - 2), "word " * (spec["maxTokens"] - 1), "x" * 120]
        fixtures = []
        for text in texts:
            encoded = tokenizer.encode(text)
            fixtures.append(dict(text=text, ids=encoded.ids, attentionMask=encoded.attention_mask,
                                 typeIds=encoded.type_ids, exceedsLimit=len(encoded.ids) > spec["maxTokens"]))
        tokenizer.enable_padding(length=32)
        padded = [dict(text=text, ids=enc.ids, attentionMask=enc.attention_mask, typeIds=enc.type_ids)
                  for text, enc in zip(["short text", "a somewhat longer sentence for padding"],
                                       tokenizer.encode_batch(["short text", "a somewhat longer sentence for padding"]))]
        fixture = dict(generator="Hugging Face tokenizers", version=importlib.metadata.version("tokenizers"),
                       revision=spec["revision"], cases=fixtures, padded=padded)
        (ROOT / "fixtures").mkdir(exist_ok=True)
        (ROOT / "fixtures" / (spec["id"] + ".json")).write_text(json.dumps(fixture, indent=2, ensure_ascii=False), encoding="utf-8")
        print("Prepared", spec["id"], flush=True)
    MANIFEST.write_text(json.dumps(specs, indent=2) + "\n", encoding="utf-8")

if __name__ == "__main__":
    main()
