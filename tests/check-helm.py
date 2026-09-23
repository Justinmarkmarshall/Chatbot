"""Validate offline Helm renders. Dependency: PyYAML (validation only)."""
import pathlib
import sys
import yaml

app = [d for d in yaml.safe_load_all(pathlib.Path(sys.argv[1]).read_text(encoding="utf-8-sig")) if d]
db = [d for d in yaml.safe_load_all(pathlib.Path(sys.argv[2]).read_text(encoding="utf-8-sig")) if d]
checks = 0
def check(ok, label):
    global checks
    assert ok, label
    checks += 1
    print("PASS:", label)

def one(items, kind):
    matches = [d for d in items if d["kind"] == kind]
    assert len(matches) == 1, (kind, len(matches))
    return matches[0]

workloads = [d for d in app if d["kind"] == "Deployment"]
check(len(workloads) == 2, "Separate web and worker deployments; no bundled Ollama")
web = next(d for d in workloads if "args" not in d["spec"]["template"]["spec"]["containers"][0])
worker = next(d for d in workloads if d != web)
wc = web["spec"]["template"]["spec"]["containers"][0]
jc = worker["spec"]["template"]["spec"]["containers"][0]
check(jc["args"] == ["--document-worker"] and worker["spec"]["replicas"] == 1, "Existing worker mode with one replica")
check(wc["image"] == jc["image"], "One shared image for both modes")
stateful = one(db, "StatefulSet")
check(stateful["spec"]["replicas"] == 1, "Single PostgreSQL replica")
pg = stateful["spec"]["template"]["spec"]["containers"][0]
check(pg["image"] == "pgvector/pgvector:0.8.6-pg17-bookworm", "Validated PostgreSQL/pgvector image")
check(pg["volumeMounts"] == [{"name":"data", "mountPath":"/var/lib/postgresql/data"}], "Correct PostgreSQL 17 data mount")
claim = stateful["spec"]["volumeClaimTemplates"][0]
check(claim["metadata"]["name"] == "data" and claim["spec"]["resources"]["requests"]["storage"] == "10Gi", "Named persistent 10Gi volume claim template")
check("storageClassName" not in claim["spec"], "Default cluster storage class is not hardcoded")
check(stateful["spec"]["persistentVolumeClaimRetentionPolicy"] == {"whenDeleted":"Retain","whenScaled":"Retain"}, "Database PVC retained after deletion/scaling")
dbservices = [d for d in db if d["kind"] == "Service"]
check(all(d["spec"]["type"] == "ClusterIP" and not d["spec"].get("externalIPs") and all("nodePort" not in p for p in d["spec"]["ports"]) for d in dbservices), "All database services are internal ClusterIP only")
check(not any(d["kind"] in ["Ingress","HTTPRoute"] for d in db), "No external database route")
check(any(d["metadata"]["name"] == stateful["spec"]["serviceName"] and d["spec"]["clusterIP"] == "None" for d in dbservices), "StatefulSet governing headless service exists")
for svc in dbservices:
    check(all(stateful["spec"]["template"]["metadata"]["labels"].get(k)==v for k,v in svc["spec"]["selector"].items()), "Database service selects database pod")
svc = one(app, "Service")
check(svc["spec"]["type"]=="ClusterIP", "Web service is internal")
labels=web["spec"]["template"]["metadata"]["labels"]
workerlabels=worker["spec"]["template"]["metadata"]["labels"]
check(all(labels.get(k)==v for k,v in svc["spec"]["selector"].items()) and not all(workerlabels.get(k)==v for k,v in svc["spec"]["selector"].items()), "Web service cannot route to worker")
route=one(app,"HTTPRoute")
check(all(b["name"]==svc["metadata"]["name"] and b["port"]==svc["spec"]["ports"][0]["port"] for r in route["spec"]["rules"] for b in r["backendRefs"]), "HTTPRoute references only web service and correct port")
we={e["name"]:e for e in wc["env"]}; je={e["name"]:e for e in jc["env"]}
check({k:v for k,v in we.items() if k.startswith("Database__")}=={k:v for k,v in je.items() if k.startswith("Database__")}, "Web and worker database settings are identical")
check(we["Database__Host"]["value"] in [s["metadata"]["name"] for s in dbservices], "Application database DNS matches rendered database service")
pgsecret=next(e for e in pg["env"] if e["name"]=="POSTGRES_PASSWORD")["valueFrom"]
check(we["Database__Password"]["valueFrom"]==pgsecret, "Both releases reference the same password Secret/key")
check(all("valueFrom" in we[k] for k in ["Authentication__Google__ClientId","Authentication__Google__ClientSecret"]), "OAuth credentials come from Secret references")
check(we["Documents__ModelRoot"]["value"]==je["Documents__ModelRoot"]["value"]=="/model-assets", "Both workloads use baked local model root")
check(all(not d["spec"]["template"]["spec"].get("initContainers") for d in workloads), "No runtime model download/init container")
check(not any(d["kind"] in ["StatefulSet","Secret"] for d in app) and not any(d["kind"]=="Secret" for d in db), "Database lifecycle is separate; charts contain no credential values")
pvc=one(app,"PersistentVolumeClaim")
check(pvc["metadata"]["annotations"]["helm.sh/resource-policy"]=="keep", "Application login key PVC is retained")
check(web["spec"]["template"]["spec"]["volumes"][0]["persistentVolumeClaim"]["claimName"]==pvc["metadata"]["name"], "Web login-key volume reference matches PVC")
check(wc["readinessProbe"]["httpGet"]["path"]=="/health/ready" and wc["livenessProbe"]["httpGet"]["path"]=="/health/live", "Web health probes use implemented endpoints")
check("pg_isready" in pg["readinessProbe"]["exec"]["command"][-1], "PostgreSQL readiness uses pg_isready")
check(not jc.get("ports") and not jc.get("readinessProbe"), "Worker has no unnecessary HTTP server or exposed port")
check(all("requests" in c["resources"] and "limits" in c["resources"] for c in [wc,jc,pg]), "Every workload has resource requests and limits")
print(f"All {checks} Helm render checks passed.")
