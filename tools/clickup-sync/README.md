# ClickUp Sync (tray tool)

Mirrors ClickUp tickets into a folder of markdown files and pushes your file edits back, using your
own personal API key (so the connector's 100 calls/day does not apply; the personal-token limit is
100 calls/minute, which the tool enforces itself).

## Setup (Windows, Python 3.10+ with the `py` launcher)
1. Put your key in a user environment variable, default name `API_KEY_CLICKUP`
   (`setx API_KEY_CLICKUP pk_...`, then restart the tool). The key is never stored by the tool.
2. Run `install.cmd` (installs `pystray` + `Pillow` for the tray icon, adds a Startup entry).
   Or just `py -m clickup_sync`. `--headless` runs without UI; `--pull` / `--push` do one pass.
3. In the window: environment variable name, cache folder (default `~/ClickUpCache`), space ids
   (default `90149675055`, the Dimensions Mod space; empty = all spaces), rate limit, quiet seconds,
   **Reset cache**. Closing the window hides it to the tray.

## Cache layout
`<cache>/<List name>/<ticket id> - <slug>.md`, plus `_index.md` per list and a `README.md` for agents.
See `clickup_sync/cache.py` for the format. Each file has front matter (editable: name, status,
assignees, priority, start_date, due_date, time_estimate_hours), `## Description`,
`## Post comment` (posted then cleared) and `## Agent notes` (local only, kept across pulls and
across Reset cache unless you answer "No" to keep notes).

## Sync rules
* **Push only after quiet.** Any file create/modify/delete in the cache folder restarts a timer
  (default 180 s). When it runs out, files differing from the last-synced remote snapshot are pushed:
  only the changed fields, so remote edits to other fields are not clobbered.
* **No start dates.** `start_date` edits are ignored unless "Push start dates" is on (Astra, 2026-11-02).
  Closing a ticket = set `status: "complete"`; assignees must be set in the file (usernames).
* Pull runs on start and every 15 minutes. A ticket with unpushed local edits is never overwritten.
* Deleting a file never deletes in ClickUp; it comes back on the next pull.
* Rate limiting: sliding window of N calls/minute, plus `429`/`Retry-After`/`X-RateLimit-Reset`
  handling with retries. A full pull of ~200 tickets is about 20 calls.
* **New tickets:** a file named `NEW - <anything>.md` in a list folder becomes a ClickUp task on the
  next push (`py -m clickup_sync --new "<List>" "<Name>"` writes a template). Parents are created
  first and the file is renamed to `<id> - <slug>.md`. Whether a subtask may sit in a different list
  than its parent is up to ClickUp; keep parent and child in the same list to be safe.
* **Hierarchy and links:** `parent`, `depends_on`, `blocks` and `links` (related) are editable on new
  and existing tickets. Values are ticket ids or exact ticket names (including other NEW files).
* Not synced: comments (other than posting), attachments, custom fields, dependencies (shown read-only).

## Tests
`py -m unittest discover -s tests` (mock ClickUp server, no key needed).
