# Status page

A GitHub Pages page that shows the mod's progress from the ClickUp tasks in the
**Remnant 2 Dimensions** folder (space "Dimensions Mod").

- `site/index.html`: the page. It reads `status.json` next to it; no ClickUp token in the browser.
- `sync_clickup.py`: pulls every list and task (with subtasks and closed tasks) from the ClickUp API and
  writes `status.json`. Only names, statuses, parent tasks, time estimates and "waiting on" dependencies
  are published, because the page is public. Descriptions, comments and assignees are left out.
- `tracker-ids.json`: ClickUp task id to tracker id (E1, T3, G2…) so the page can show the ids.
- `.github/workflows/status-page.yml`: runs hourly (and on demand), builds the site with a fresh
  `status.json`, and force-pushes it as a single commit to the `gh-pages` branch.

## One-time setup

1. In ClickUp, open **Settings → Apps** and generate a personal API token (starts with `pk_`).
2. In GitHub, **Settings → Secrets and variables → Actions → New repository secret**:
   name `CLICKUP_API_TOKEN`, value the token.
3. **Actions → Status page → Run workflow**. This creates the `gh-pages` branch.
4. **Settings → Pages**: Source "Deploy from a branch", branch `gh-pages`, folder `/ (root)`.

The page is then at `https://clboyrun.github.io/steam-remnant2-dimensions-mod/`.

Optional: set the repository variable `CLICKUP_FOLDER_ID` to point at a different folder
(default `901414897445`). Each run makes about 15 ClickUp API calls.

## Run it locally

```sh
CLICKUP_API_TOKEN=pk_... python status/sync_clickup.py --out status/site/status.json
python -m http.server -d status/site
cd status && python -m unittest
```

`status/site/status.json` is git-ignored.
