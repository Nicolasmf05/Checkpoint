"""Publish complete stable Checkpoint releases, then hide older releases as drafts."""
import argparse
import hashlib
import json
import os
from pathlib import Path
import re
import subprocess
import urllib.parse
import urllib.request
import urllib.error
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parent.parent
BASE = "https://api.github.com/repos/Nicolasmf05/Checkpoint"

def git(*args):
    return subprocess.run(["git", "-c", "safe.directory=" + ROOT.as_posix(), *args],
                          cwd=ROOT, text=True, capture_output=True, check=True).stdout.strip()

def file_hash(file):
    with file.open("rb") as stream:
        return hashlib.file_digest(stream, "sha256").hexdigest()

def packages(version):
    names = [f"Checkpoint-{version}-win-x64.zip", f"Checkpoint-{version}-win-x64.msi",
             f"Checkpoint-source-{version}.zip"]
    expected = {}
    for name in names:
        file = ROOT / "dist" / name
        digest = file_hash(file)
        checksum = ROOT / "dist" / (name + ".sha256")
        if file.stat().st_size <= 0 or checksum.read_text(encoding="ascii").strip() != digest + "  " + name:
            raise RuntimeError("Missing, empty or invalid package/checksum: " + name)
        for candidate in [file, checksum]:
            expected[candidate.name] = (candidate.stat().st_size,
                "sha256:" + file_hash(candidate))
    return expected

class GitHub:
    def __init__(self):
        token = os.environ.get("GH_TOKEN") or os.environ.get("GITHUB_TOKEN")
        if not token:
            result = subprocess.run(["git", "-c", "credential.interactive=never", "credential", "fill"],
                input="protocol=https\nhost=github.com\n\n", text=True, capture_output=True, check=True)
            token = dict(line.split("=", 1) for line in result.stdout.splitlines() if "=" in line).get("password")
        if not token:
            raise RuntimeError("GitHub credentials unavailable")
        self.token = token

    def request(self, method, url, data=None, file=None):
        parsed = urllib.parse.urlparse(url)
        if parsed.scheme != "https" or parsed.hostname not in ("api.github.com", "uploads.github.com"):
            raise RuntimeError("Unexpected API host")
        headers = {"Authorization": "Bearer " + self.token, "Accept": "application/vnd.github+json",
                   "User-Agent": "Checkpoint-release", "X-GitHub-Api-Version": "2022-11-28"}
        if file is not None:
            headers.update({"Content-Type": "application/octet-stream", "Content-Length": str(file.stat().st_size)})
            with file.open("rb") as stream:
                req = urllib.request.Request(url, data=stream, method=method, headers=headers)
                with urllib.request.urlopen(req, timeout=180) as reply:
                    return json.load(reply)
        headers["Content-Type"] = "application/json"
        req = urllib.request.Request(url, data=None if data is None else json.dumps(data).encode(),
                                     method=method, headers=headers)
        with urllib.request.urlopen(req, timeout=30) as reply:
            return json.load(reply)

    def releases(self):
        result = []
        for page in range(1, 101):
            batch = self.request("GET", BASE + f"/releases?per_page=100&page={page}")
            result.extend(batch)
            if len(batch) < 100:
                return result
        raise RuntimeError("Too many releases; refusing an incomplete inventory")

def verify_assets(assets, expected):
    if {a["name"] for a in assets} != set(expected) or len(assets) != len(expected):
        raise RuntimeError("Release does not contain exactly the six required assets")
    for asset in assets:
        if asset["state"] != "uploaded" or (asset["size"], asset.get("digest")) != expected[asset["name"]]:
            raise RuntimeError("Remote package verification failed: " + asset["name"])

def publish(notes, name=None):
    version = ET.parse(ROOT / "Directory.Build.props").findtext(".//Version")
    if not re.fullmatch(r"[0-9]+\.[0-9]+\.[0-9]+", version or ""):
        raise RuntimeError("A numeric stable version is required")
    if git("status", "--porcelain"):
        raise RuntimeError("Commit all source changes before publishing")
    head = git("rev-parse", "HEAD")
    expected = packages(version)
    body = Path(notes).read_text(encoding="utf-8")
    if not body.strip():
        raise RuntimeError("Release notes are required")
    api = GitHub()
    runs = api.request("GET", BASE + "/actions/runs?head_sha=" + head + "&per_page=100")["workflow_runs"]
    checks = [r for r in runs if r["head_sha"] == head and r["name"] in ("Build and verify", "Checkpoint Web")]
    if not any(r["name"] == "Build and verify" for r in checks) or any(r["conclusion"] != "success" for r in checks):
        raise RuntimeError("Required CI checks are missing or have not succeeded")
    tag = "v" + version
    release = next((r for r in api.releases() if r["tag_name"] == tag), None)
    if release and not release["draft"]:
        raise RuntimeError("Already public; never replace public assets. Use a new version")
    try:
        obj = api.request("GET", BASE + "/git/ref/tags/" + tag)["object"]
    except urllib.error.HTTPError as error:
        if error.code != 404 or release is not None:
            raise
        obj = api.request("POST", BASE + "/git/refs", {"ref": "refs/tags/" + tag, "sha": head})["object"]
    for _ in range(5):
        if obj["type"] == "commit":
            break
        obj = api.request("GET", BASE + "/git/tags/" + obj["sha"])["object"]
    if obj["type"] != "commit" or obj["sha"] != head:
        raise RuntimeError("Existing draft tag does not match the checked commit")
    if release is None:
        release = api.request("POST", BASE + "/releases", {"tag_name": tag, "target_commitish": head,
            "name": name or "Checkpoint " + version, "body": body, "draft": True, "prerelease": False})
    assets = api.request("GET", release["assets_url"] + "?per_page=100")
    for asset_name in expected:
        current = next((a for a in assets if a["name"] == asset_name), None)
        if current is None:
            current = api.request("POST", release["upload_url"].split("{")[0] + "?name=" +
                urllib.parse.quote(asset_name), file=ROOT / "dist" / asset_name)
            assets.append(current)
        if current["state"] != "uploaded" or (current["size"], current.get("digest")) != expected[asset_name]:
            raise RuntimeError("Asset mismatch: " + asset_name)
    verify_assets(api.request("GET", release["assets_url"] + "?per_page=100"), expected)
    result = api.request("PATCH", BASE + "/releases/" + str(release["id"]),
                        {"draft": False, "prerelease": False, "make_latest": "true", "body": body})
    public = api.request("GET", BASE + "/releases/tags/" + tag)
    verify_assets(public["assets"], expected)
    if public["draft"] or public["prerelease"] or api.request("GET", BASE + "/releases/latest")["id"] != result["id"]:
        raise RuntimeError("Stable latest publication could not be verified; older releases preserved")
    for old in api.releases():
        if old["id"] != result["id"] and not old["draft"]:
            archived = api.request("PATCH", BASE + "/releases/" + str(old["id"]), {"draft": True})
            if not archived["draft"]:
                raise RuntimeError("Could not hide previous release: " + old["tag_name"])
    visible = [r for r in api.releases() if not r["draft"]]
    if len(visible) != 1 or visible[0]["id"] != result["id"]:
        raise RuntimeError("Public release visibility check failed")
    print("Published complete stable release: " + public["html_url"])

if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--notes", required=True, help="UTF-8 release notes file")
    parser.add_argument("--name", help="Release title")
    args = parser.parse_args()
    publish(args.notes, args.name)
