# Remnant 2 Dimensions: status page

This branch is the project's public status page:
https://clboyrun.github.io/steam-remnant2-dimensions-mod/

- `about.html`: a poster-style About page that lists what the mod adds (planned features from the design doc). Linked from the status page.
- `index.html`: the page. It reads `status.json`, so the ClickUp token never reaches the browser.
- `wheel.html`: the dependency wheel (which tickets wait on which). It reads the same `status.json`, so it
  updates with the status page. Linked next to the About page.
- `status.json`: the latest snapshot of the ClickUp tasks in the **Dimensions Mod** space. Don't edit it by
  hand; the "Status page" GitHub Action rewrites it on a schedule.
- `tools/sync_clickup.py`: makes `status.json` from the ClickUp API. Only task names, statuses, parent tasks, time
  estimates, dependencies and related links are published. Descriptions, comments and assignees are left out.
- `tools/tracker-ids.json`: ClickUp task id to tracker id (E1, T3, G2…), shown on the page.

To change how the page looks, edit `index.html` on this branch. To change what's on it, change the tasks in ClickUp.

## Setup

1. Repository secret `CLICKUP_API_TOKEN` = a ClickUp personal API token (ClickUp Settings → Apps).
2. Settings → Pages: deploy from branch `gh-pages`, folder `/ (root)`.

The hourly refresh is `.github/workflows/status-page.yml` on the default branch. GitHub only runs scheduled
jobs from the default branch, so that one file can't live here. It checks out this branch, runs the tests in
`tools/`, writes `status.json`, and commits it here when something changed. Run it any time from
Actions → Status page → Run workflow.

## Run it locally

```sh
CLICKUP_API_TOKEN=pk_... python tools/sync_clickup.py --out status.json
python -m http.server
cd tools && python -m unittest
```
