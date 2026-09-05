# Frontend: one static page, two twins

**Status: BUILT (2026-08-25, PR #3, merge `cdb580a`; acceptance walked on both twins, suite 52/52).** Talk-side decision 13 made this a fenced stretch goal ("built only at the very end"); the build fence was lifted on 2026-08-25 with all three slices archived, the suite at 52/52, and the package freeze in force. The DECK fence still stands: the talk and the slides are written as if this page does not exist, and it becomes a beat only if dry-run 1 comes in under 45 minutes with room. Nothing here may become a dependency of the deck, the suite, or CI.

**Why it exists:** the two twins share one HTTP contract, and the shared Alba suite proves it. This page proves it a second way, for a human, in a browser: one file, plain `fetch`, a switch between the two backends, and the same journey behaving identically on both. It is ugly on purpose. Its whole argument is that it does not care which architecture is on the other end.

## Deliverables

1. `src/frontend/index.html`. One file. Inline CSS and JS. No framework, no package manager, no build step, no bundler, no CDN, no web fonts, no external request of any kind except to the two twins. Opens straight from disk (`file://`), so a single inline `<script>` (no ES modules, no relative imports). Vanilla ES2020, no TypeScript.
2. A Development-only CORS policy in BOTH twins (`src/before/LayerCake.WebApi/Program.cs`, `src/after/LayerCake.Slices/Program.cs`), symmetric, as small as it can be: allow any origin, any header, any method, and expose the `Location` header so the page can show it. The page's origin is `null` when opened from disk; `AllowAnyOrigin` covers that. Keep it inside the existing `IsDevelopment()` branch on both sides, with the same two-line comment on both pointing at `src/frontend/`. Register it in the services block, apply it before endpoint mapping. Expected cost: about three lines per twin, plus the comment.
3. README: a short "The frontend" section under the run instructions (start Postgres, run both twins, open the file), stating what it is and that it is not part of the proof.
4. Bookkeeping (see the end of this doc).

## The page

### Header: the switch

A two-way switch, big enough to read from the back of a room: **Before** (`http://localhost:42010`) and **After** (`http://localhost:42020`). The active twin's name and base URL are printed large. Colors: Before in coral (`#E06C5E`), After in green (`#8FBE6A`), on a warm-dark background (`#17130E`, text `#ECE6DB`) so a screenshot sits naturally in the deck if it ever earns one. Next to the switch, a reachability indicator driven by `GET /ping` on the active twin (green dot on 200, red dot with "not reachable at {url}" otherwise).

Switching the twin clears every result panel and re-runs the two reads (catalog, baker's board). No state carries across the switch except the text in the form inputs, so the demo can flip mid-journey and re-submit the same input.

### Section 1: the catalog (`GET /cakes`)

A table: name, description, price (two decimals). A Refresh button. Loads on page open and after every switch and every successful publish.

### Section 2: publish a cake (`POST /cakes`)

Inputs: name, description, price. Submit sends `{ name, description, price }` as JSON. On `201`, show "Published {name}" with the id and the `Location` header value, and refresh the catalog. On `400` or `409`, show the problem's `detail` (or `title` if there is no `detail`) in the section, in the coral color.

### Section 3: check a coupon (`GET /coupons/{code}`)

One input (code, sent as typed; the twins uppercase it) and a Check button. Render the envelope as a badge: `valid` in green with "{percentOff}% off", `expired` / `notYetActive` / `invalid` in coral. Seed codes are listed under the input as hints: `BDAY10`, `SUMMER25`, `HOLIDAY30`, and "anything else".

### Section 4: place an order (`POST /orders`)

One row per cake in the catalog (built from the Section 1 response, so the ids are always the active twin's own): name, unit price, a quantity input that starts blank with a `0` placeholder. An optional coupon-code input. Submit sends `{ lines: [ { cakeId, quantity } for every row with something typed ], couponCode }` with `couponCode` omitted (not null, not empty) when blank. A blank quantity means the row is not part of the order; anything typed, an explicit 0 included, is sent exactly as entered (decision recorded in `docs/build-log.md`, 2026-08-25). Send the lines exactly as entered; the twins own the guards, and the page must not pre-validate (a zero-line order and a zero-quantity line are demo cases for the 400s).

On `201`, render the receipt: each line (name, quantity, unit price, line total), subtotal, discount, total, coupon code if present, and the order id as a link that runs `GET /orders/{id}` into the wire pane. On `400` or `422`, show the problem's `detail` in coral. Then start the baker poll (Section 5).

### Section 5: the baker's board (`GET /baker/tasks`)

A list: order id (short form, first 8 characters), summary, createdAt. Refresh button. Loads on page open and after every switch. After a successful order, poll `GET /baker/tasks?orderId={id}` every 250 ms for up to 5 seconds until a task for that order appears, then re-load the full board and highlight the new row. Same constants as the suite's scenario, so the page and the tests make the same promise. If the poll times out, say so in coral rather than silently stopping.

### The wire pane

A fixed panel (right column on wide screens, bottom on narrow ones) showing the last request and response: method, full URL, request body (pretty-printed JSON, or nothing), response status and reason, `Content-Type`, `Location` when present, response body pretty-printed, and elapsed milliseconds. Monospace, at least 16 px. Every fetch the page makes goes through one `call(method, path, body)` helper that updates this pane; there is no second code path. The pane is the point of the page: the audience sees identical wire traffic against two architectures.

### Presentation rules

- Base font at least 18 px, wire pane at least 16 px. The journey (header through Section 5) fits a 1920x1080 window without scrolling; the wire pane may scroll internally.
- Every request sets `Accept: application/json, application/problem+json`; every request with a body sets `Content-Type: application/json`.
- Network failure (twin not running) renders "not reachable at {url}" in the section and the wire pane; never a blank panel and never an uncaught promise.
- No em dashes anywhere in the page (talk rule: it could land on a slide). Hyphens are fine.
- No `localStorage`, no cookies, no query-string state. Reloading the page is the reset button.
- Target under 350 lines total. If it wants to grow past that, it is doing something the spec did not ask for.

## Out of scope

Auth, styling beyond legibility, a framework of any kind, a "run on both twins at once" mode (seed cake ids differ per twin, so it would need per-twin id resolution and it doubles the JS), editing or deleting anything, serving the page from either twin, CritterWatch anything, tests for the page (it is a demo surface; the suite is the proof), and any change to the twins beyond the CORS lines. If the CORS lines tempt a refactor of either `Program.cs`, do not.

## Acceptance (manual, both twins running)

`docker compose up -d`, `dotnet run` in both twin projects (ports 42010 and 42020), open `src/frontend/index.html` from disk. Walk this twice, once per twin, flipping the switch between the two passes without reloading:

1. Catalog shows the three seed cakes.
2. Publish "Opera" 36.00: 201, Location shown, catalog now has four. Publish "Opera" again: 409 with the detail. Publish with a blank name: 400 with the detail.
3. Check `bday10`: valid, 10% off, code shown uppercased. `SUMMER25`: expired. `HOLIDAY30`: notYetActive. `NOPE`: invalid.
4. Order 2x Chocolate Stout with `BDAY10`: 201, subtotal 68.00, discount 6.80, total 61.20. Order with `SUMMER25`: 422 with the status in the detail. Order with every quantity 0: 400. Order with one quantity 0 and one quantity 1: 400.
5. After the successful order, the baker's board shows the new task within 5 seconds on both twins.
6. The wire pane shows the same status, content type, and body shape for each step on both twins (the `Location` header is absolute on Before and relative on After; that is the one recorded divergence and it is fine).

Then: `dotnet test` is still 52/52 (the CORS lines change nothing the suite sees), and CI is green.

## Bookkeeping

- `docs/build-log.md`: one dated block, "Frontend built (session spec `docs/frontend.md`)", with any decision made along the way (for example where `UseCors` landed relative to Swagger in each pipeline).
- `docs/file-inventory.md`: append a section "Frontend (recorded YYYY-MM-DD, outside both counts)" listing `src/frontend/index.html` and, under "twins edited", the two `Program.cs` files with the line delta each. Then re-run the whole-twin line counts (all `.cs` under `src/before` excluding `obj/` and `Persistence/Migrations/`; all `.cs` under `src/after` excluding `obj/`; raw count including blanks) and record the new pair next to the old one (1,696 and 687 as of 2026-08-25). The scorecard slide reads that number.
- `CLAUDE.md`: under "Where detail lives" add `docs/frontend.md`; in the "Slice slate" section change "The static single-page frontend is a fenced stretch goal" to say it exists in `src/frontend/` and is not part of the proof; under "Commands", one line on how to open it. Nothing else in the charter changes.
- Not an OpenSpec change. This is not a slice and it adds no capability behavior; it follows the upgrade pass's shape instead (session spec in `docs/`, one branch, one PR). Unlike the upgrade prompt, this doc stays in the repo after the PR as the page's design record.

## Kickoff (paste into the Rider agent)

> Read CLAUDE.md, then docs/frontend.md, and build the frontend exactly as that spec describes on a branch named `frontend-single-page`. Deliverables: `src/frontend/index.html`, the symmetric Development-only CORS lines in both twins' Program.cs, the README section, and the bookkeeping section at the end of the spec (build log, file inventory with re-run line counts, the three CLAUDE.md edits). Do not touch anything else in either twin. Before opening the PR, run the manual acceptance walk in the spec against both twins and confirm `dotnet test` is 52/52. Commit messages plain, no tool attribution. Open PR #3 with the acceptance results in the body.
