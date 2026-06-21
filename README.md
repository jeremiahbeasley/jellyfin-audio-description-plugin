# Jellyfin Audio Description Badges Plugin

Surfaces a visual, high-contrast **"AD" badge** on every library card for media
that contains an Audio Description audio track. The badge is fully WCAG 2.2
compliant — screen readers announce it, it is visible in Windows High Contrast
Mode, and it never relies on colour alone.

---

## Features

- **Auto-detection** — scans each audio track's `Title`, `DisplayTitle`, and
  `Language` fields for configurable keywords (`audio description`,
  `descriptive audio`, `AD`, `VI`, …).
- **Word-boundary matching** — keywords match whole words only, so short tokens
  like `AD` and `VI` flag a track labelled "AD" but never false-match inside
  ordinary words (e.g. "Br**ad**" in a commentary credit, or "mo**vi**e").
- **Self-contained injection** — the badge CSS/JS is injected into the web
  client by the plugin's own middleware. No dependency on the File
  Transformation plugin, and nothing is written to Jellyfin's `index.html` on
  disk, so it survives server upgrades.
- **WCAG 2.2 compliant** — 7:1+ contrast, `role="img"` + `aria-label`,
  forced-colours / HCM support, polite ARIA live region, reduced-motion support.
- **Batch API** — card lookups are grouped into batches to avoid per-card HTTP
  round-trips, and authenticated through the web client's `ApiClient`.
- **Configurable** — keyword list, badge corner, and label visibility are all
  adjustable from the Jellyfin plugin settings page.

---

## Requirements

| Dependency | Version |
|---|---|
| Jellyfin Server | ≥ 10.11.0 |
| .NET SDK (build only) | 9.0 |

---

## Build

```bash
cd jellyfin-audio-description-plugin
dotnet build -c Release
```

The compiled DLL will be at:

```
bin/Release/net9.0/Jellyfin.Plugin.AudioDescription.dll
```

---

## Install

1. Stop your Jellyfin server.

2. Copy the compiled DLL into a sub-folder of your Jellyfin plugins directory.
   The default paths are:

   | OS | Path |
   |---|---|
   | Linux | `/var/lib/jellyfin/plugins/AudioDescriptionBadges/` |
   | macOS | `~/.local/share/jellyfin/plugins/AudioDescriptionBadges/` |
   | Windows | `%APPDATA%\Jellyfin\plugins\AudioDescriptionBadges\` |
   | Docker | `/config/plugins/AudioDescriptionBadges/` |

   Create the sub-folder if it does not exist. The folder name is arbitrary;
   Jellyfin discovers plugins by scanning all sub-folders and reading each
   `meta.json`.

3. Start Jellyfin.

4. In the web UI, go to **Dashboard → Plugins → Audio Description Badges** to
   verify the plugin loaded and adjust settings.

> Prefer automatic updates? Add the JB11 plugin repository to
> **Dashboard → Plugins → Repositories** and install from the catalogue.

---

## How it works

```
Jellyfin Server (startup)
  └─ IStartupFilter installs IndexInjectionMiddleware
       └─ rewrites the index.html response in-memory, inserting:
            <link  href="/AudioDescription/audioDescription.css">
            <script src="/AudioDescription/audioDescription.js">

Browser (web client)
  Page load / card scroll
    └─ MutationObserver fires (debounced)
         └─ batch GET /AudioDescription/Check?ids=…  (auth via ApiClient)
                                   └─ server reads each item's audio MediaStreams
                                        └─ word-boundary keyword match on
                                           Title / DisplayTitle / Language
                                        └─ returns { itemId: "track title" }
    └─ accessible badge injected on each matching card
         • role="img" aria-label="Audio Description available: <track title>"
         • #00538C background / #FFFFFF text  (≥7:1 contrast)
         • double-ring box-shadow so the boundary stays ≥3:1 over any poster
         • Windows HCM: Highlight + HighlightText system colours
         • polite live region announces the count to screen readers
```

The plugin runs server-side only; the API endpoints (`/AudioDescription/Check`
and `/AudioDescription/Config`) require authentication, and the injected script
calls them through `ApiClient`, which supplies the access token. The badge
assets are served from `[AllowAnonymous]` routes so a plain `<script>`/`<link>`
tag can load them.

---

## Configuration

| Setting | Default | Description |
|---|---|---|
| **Keywords** | `audio description, descriptive audio, described video, visually impaired, AD, VI` | Comma-separated; matched case-insensitively and on **word boundaries** against each audio track's title and language. |
| **Badge position** | Top-left | Corner of the card thumbnail. |
| **Show text label** | Yes | Displays "AD" text next to the headphone icon. Recommended so the badge is meaningful without relying on the icon alone. |

---

## Accessibility notes (WCAG 2.2)

| Criterion | Requirement | Implementation |
|---|---|---|
| 1.1.1 Non-text Content | Text alternative for icon | `role="img"` + `aria-label="Audio Description available: <track>"` |
| 1.4.1 Use of Colour | Not colour alone | Badge shows text ("AD") + headphone icon; colour is additive |
| 1.4.3 Contrast (Minimum) | ≥ 4.5:1 text | `#FFFFFF` on `#00538C` |
| 1.4.11 Non-text Contrast | ≥ 3:1 UI boundary | Double-ring (white inner / black outer) box-shadow |
| 2.3.3 Animation | Respect `prefers-reduced-motion` | `@media (prefers-reduced-motion: reduce)` disables fade-in |
| 4.1.3 Status Messages | Polite live region | `role="status"` `aria-live="polite"` announces badge count |

---

## Development

The web assets (`audioDescription.js`, `audioDescription.css`, `configPage.html`)
are embedded as resources in the DLL via `<EmbeddedResource>` in the `.csproj`
and served by `AudioDescriptionController`. Injection is handled by
`Injection/IndexInjectionMiddleware` + `Injection/InjectionStartupFilter`, with
the tag insertion in `Helpers/TransformationPatches`.

---

## License

MIT — see [LICENSE](LICENSE)
