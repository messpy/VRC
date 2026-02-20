#!/usr/bin/env python3
# -*- coding: utf-8 -*-

"""
VRChat screenshot sender for Discord Webhook (cross-platform, stdlib-only)

Features:
- Try to auto-detect VRChat screenshot directories on Windows/macOS/Linux.
- Or specify --screenshots-dir (recommended for reliability).
- Collect images and send them in chunks before hitting Discord payload size limit.
- Standard library only: urllib + multipart/form-data.

Usage examples:
  python vrcSendDiscord.py --webhook-url "https://discord.com/api/webhooks/...."
  python vrcSendDiscord.py --webhook-url "..." --screenshots-dir "/path/to/VRChat"
  python vrcSendDiscord.py --webhook-url "..." --since-days 7
"""

from __future__ import annotations

import argparse
import datetime as _dt
import json
import mimetypes
import os
from pathlib import Path
import sys
import time
import urllib.request
import urllib.error
import uuid
from typing import Iterable, List, Optional, Tuple


IMAGE_EXTS = {".png", ".jpg", ".jpeg", ".gif", ".webp"}


def _now() -> _dt.datetime:
    return _dt.datetime.now()


def _human_bytes(n: int) -> str:
    units = ["B", "KB", "MB", "GB"]
    f = float(n)
    for u in units:
        if f < 1024.0 or u == units[-1]:
            return f"{f:.2f}{u}" if u != "B" else f"{int(f)}{u}"
        f /= 1024.0
    return f"{n}B"


def guess_vrchat_screenshot_dirs() -> List[Path]:
    """
    Best-effort guesses. VRChat can be configured; this won't be perfect.
    We return candidates; caller checks existence.
    """
    candidates: List[Path] = []

    home = Path.home()

    # Windows typical locations
    # e.g. C:\Users\<user>\Pictures\VRChat
    candidates.append(home / "Pictures" / "VRChat")
    candidates.append(home / "Pictures" / "VRChat" / "VRChat")  # sometimes nested

    # macOS typical locations
    candidates.append(home / "Pictures" / "VRChat")
    candidates.append(home / "Pictures" / "VRChat" / "VRChat")

    # Linux typical locations (Steam/Proton paths vary; include common ones)
    candidates.append(home / "Pictures" / "VRChat")
    candidates.append(home / "Pictures" / "VRChat" / "VRChat")

    # Some people store screenshots under "Videos" or "Documents" (add mild guesses)
    candidates.append(home / "Documents" / "VRChat")
    candidates.append(home / "Videos" / "VRChat")

    # Environment override (if user sets it)
    env_dir = os.environ.get("VRCHAT_SCREENSHOTS_DIR")
    if env_dir:
        candidates.insert(0, Path(env_dir))

    # Deduplicate while preserving order
    seen = set()
    uniq: List[Path] = []
    for p in candidates:
        ps = str(p)
        if ps not in seen:
            seen.add(ps)
            uniq.append(p)
    return uniq


def find_existing_dir(candidates: Iterable[Path]) -> Optional[Path]:
    for p in candidates:
        try:
            if p.exists() and p.is_dir():
                return p
        except OSError:
            continue
    return None


def iter_image_files(root: Path, recursive: bool = True) -> Iterable[Path]:
    if recursive:
        for p in root.rglob("*"):
            if p.is_file() and p.suffix.lower() in IMAGE_EXTS:
                yield p
    else:
        for p in root.iterdir():
            if p.is_file() and p.suffix.lower() in IMAGE_EXTS:
                yield p


def filter_since(files: Iterable[Path], since: Optional[_dt.datetime]) -> List[Path]:
    if since is None:
        return sorted(files, key=lambda p: p.stat().st_mtime)
    out: List[Path] = []
    since_ts = since.timestamp()
    for p in files:
        try:
            if p.stat().st_mtime >= since_ts:
                out.append(p)
        except OSError:
            continue
    return sorted(out, key=lambda p: p.stat().st_mtime)


def chunk_files_by_size(
    files: List[Path],
    max_bytes: int,
    max_files_per_post: int,
    overhead_bytes: int = 200_000,
) -> List[List[Path]]:
    """
    Group files into chunks where total size + overhead stays below max_bytes.
    overhead_bytes: rough safety margin for multipart headers + json payload.
    """
    chunks: List[List[Path]] = []
    current: List[Path] = []
    current_size = overhead_bytes

    for f in files:
        try:
            sz = f.stat().st_size
        except OSError:
            continue

        # If single file is too large, send alone and let it fail or user adjust max_bytes
        # (alternatively: skip; but user asked to split before limit, not compress/resize).
        if not current:
            if sz + overhead_bytes <= max_bytes and max_files_per_post > 0:
                current.append(f)
                current_size = sz + overhead_bytes
            else:
                # start a "single oversized file" chunk
                chunks.append([f])
                current = []
                current_size = overhead_bytes
            continue

        if (len(current) >= max_files_per_post) or (current_size + sz > max_bytes):
            chunks.append(current)
            current = [f]
            current_size = sz + overhead_bytes
        else:
            current.append(f)
            current_size += sz

    if current:
        chunks.append(current)

    return chunks


def build_multipart_formdata(
    payload_json: dict,
    files: List[Tuple[str, bytes, str]],
) -> Tuple[bytes, str]:
    """
    files: list of (fieldname, content_bytes, filename)
    Returns: (body_bytes, content_type_header_value)
    """
    boundary = f"------------------------{uuid.uuid4().hex}"
    crlf = b"\r\n"

    parts: List[bytes] = []

    # payload_json part
    payload_bytes = json.dumps(payload_json, ensure_ascii=False).encode("utf-8")
    parts.append(f"--{boundary}".encode("utf-8"))
    parts.append(b'Content-Disposition: form-data; name="payload_json"')
    parts.append(b"Content-Type: application/json; charset=utf-8")
    parts.append(b"")
    parts.append(payload_bytes)

    # file parts
    for fieldname, content, filename in files:
        ctype, _ = mimetypes.guess_type(filename)
        if not ctype:
            ctype = "application/octet-stream"
        parts.append(f"--{boundary}".encode("utf-8"))
        parts.append(
            f'Content-Disposition: form-data; name="{fieldname}"; filename="{filename}"'.encode(
                "utf-8"
            )
        )
        parts.append(f"Content-Type: {ctype}".encode("utf-8"))
        parts.append(b"")
        parts.append(content)

    parts.append(f"--{boundary}--".encode("utf-8"))
    parts.append(b"")

    body = crlf.join(parts)
    content_type = f"multipart/form-data; boundary={boundary}"
    return body, content_type


def discord_webhook_post(
    webhook_url: str,
    content: str,
    file_paths: List[Path],
    username: Optional[str] = None,
    timeout_sec: int = 60,
) -> None:
    """
    Sends one message with multiple attachments using Discord webhook.
    """
    payload: dict = {"content": content}
    if username:
        payload["username"] = username

    files_data: List[Tuple[str, bytes, str]] = []
    for i, p in enumerate(file_paths):
        # Discord expects files[n]
        field = f"files[{i}]"
        try:
            data = p.read_bytes()
        except OSError as e:
            raise RuntimeError(f"Failed to read file: {p} ({e})") from e
        files_data.append((field, data, p.name))

    body, content_type = build_multipart_formdata(payload, files_data)

    req = urllib.request.Request(
        webhook_url,
        data=body,
        headers={"Content-Type": content_type, "User-Agent": "vrcSendDiscord-stdlib"},
        method="POST",
    )

    try:
        with urllib.request.urlopen(req, timeout=timeout_sec) as resp:
            # Discord often returns 204 No Content for webhooks
            _ = resp.read()
    except urllib.error.HTTPError as e:
        detail = ""
        try:
            detail = e.read().decode("utf-8", errors="replace")
        except Exception:
            pass
        raise RuntimeError(f"Discord webhook HTTPError {e.code}: {e.reason}\n{detail}") from e
    except urllib.error.URLError as e:
        raise RuntimeError(f"Discord webhook URLError: {e.reason}") from e


class vrcSendDiscord:
    def __init__(self, screenshots_dir: Path, webhook_url: str):
        self.screenshots_dir = screenshots_dir
        self.webhook_url = webhook_url

    def collect_images(
        self,
        recursive: bool = True,
        since: Optional[_dt.datetime] = None,
    ) -> List[Path]:
        files = list(iter_image_files(self.screenshots_dir, recursive=recursive))
        return filter_since(files, since=since)

    def send_batched(
        self,
        images: List[Path],
        max_bytes: int,
        max_files_per_post: int,
        message_prefix: str = "",
        username: Optional[str] = None,
        sleep_sec: float = 1.0,
    ) -> None:
        if not images:
            print("No images to send.")
            return

        chunks = chunk_files_by_size(
            images, max_bytes=max_bytes, max_files_per_post=max_files_per_post
        )
        total = len(images)
        print(f"Sending {total} image(s) in {len(chunks)} post(s).")
        for idx, ch in enumerate(chunks, start=1):
            size_sum = 0
            for p in ch:
                try:
                    size_sum += p.stat().st_size
                except OSError:
                    pass

            content = f"{message_prefix}({idx}/{len(chunks)}) {len(ch)} file(s), ~{_human_bytes(size_sum)}"
            discord_webhook_post(
                self.webhook_url,
                content=content,
                file_paths=ch,
                username=username,
            )
            print(f"Posted {idx}/{len(chunks)}: {len(ch)} file(s)")
            if idx != len(chunks) and sleep_sec > 0:
                time.sleep(sleep_sec)


def parse_args(argv: List[str]) -> argparse.Namespace:
    p = argparse.ArgumentParser(description="Send VRChat screenshots to Discord Webhook.")
    p.add_argument("--webhook-url", required=False, help="Discord webhook URL.")
    p.add_argument(
        "--screenshots-dir",
        required=False,
        help="Directory containing images. If omitted, tries to auto-detect.",
    )
    p.add_argument(
        "--since-days",
        type=int,
        default=None,
        help="Only send images modified within the last N days.",
    )
    p.add_argument(
        "--recursive",
        action="store_true",
        help="Scan directories recursively (default: false).",
    )
    p.add_argument(
        "--max-bytes",
        type=int,
        default=8 * 1024 * 1024,
        help="Max total bytes per Discord post (default: 8MB). Adjust as needed.",
    )
    p.add_argument(
        "--max-files",
        type=int,
        default=10,
        help="Max number of files per Discord post (default: 10).",
    )
    p.add_argument(
        "--username",
        type=str,
        default=None,
        help="Webhook username override (optional).",
    )
    p.add_argument(
        "--message-prefix",
        type=str,
        default="VRChat screenshots ",
        help="Message prefix for each post.",
    )
    p.add_argument(
        "--dry-run",
        action="store_true",
        help="Do not send; only show what would be sent.",
    )
    p.add_argument(
        "--sleep-sec",
        type=float,
        default=1.0,
        help="Sleep seconds between posts to avoid rate limits (default: 1.0).",
    )

    args = p.parse_args(argv)

    # Also allow env var for webhook
    if not args.webhook_url:
        args.webhook_url = os.environ.get("DISCORD_WEBHOOK_URL")

    return args


def main(argv: List[str]) -> int:
    args = parse_args(argv)

    if not args.webhook_url:
        print("Error: --webhook-url is required (or set DISCORD_WEBHOOK_URL env var).", file=sys.stderr)
        return 2

    if args.screenshots_dir:
        screenshots_dir = Path(args.screenshots_dir).expanduser()
    else:
        screenshots_dir = find_existing_dir(guess_vrchat_screenshot_dirs()) or None
        if screenshots_dir is None:
            print(
                "Error: Could not auto-detect screenshots directory.\n"
                "Please pass --screenshots-dir or set VRCHAT_SCREENSHOTS_DIR env var.",
                file=sys.stderr,
            )
            return 2

    if not screenshots_dir.exists() or not screenshots_dir.is_dir():
        print(f"Error: screenshots dir not found: {screenshots_dir}", file=sys.stderr)
        return 2

    since = None
    if args.since_days is not None:
        since = _now() - _dt.timedelta(days=int(args.since_days))

    sender = vrcSendDiscord(screenshots_dir=screenshots_dir, webhook_url=args.webhook_url)
    images = sender.collect_images(recursive=args.recursive, since=since)

    if not images:
        print("No images matched.")
        return 0

    chunks = chunk_files_by_size(images, max_bytes=args.max_bytes, max_files_per_post=args.max_files)
    if args.dry_run:
        print(f"[DRY RUN] screenshots_dir={screenshots_dir}")
        print(f"[DRY RUN] matched {len(images)} image(s), will send {len(chunks)} post(s)")
        for i, ch in enumerate(chunks, start=1):
            sz = sum((p.stat().st_size for p in ch if p.exists()), 0)
            print(f"[DRY RUN] Post {i}/{len(chunks)}: {len(ch)} file(s), ~{_human_bytes(sz)}")
            for p in ch:
                print(f"  - {p.name}")
        return 0

    sender.send_batched(
        images=images,
        max_bytes=args.max_bytes,
        max_files_per_post=args.max_files,
        message_prefix=args.message_prefix,
        username=args.username,
        sleep_sec=args.sleep_sec,
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main(sys.argv[1:]))
