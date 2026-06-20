# Jellyfin Audio Description Badges Plugin

Surfaces a visual, high-contrast "AD" badge on every library card for media that
contains an Audio Description audio track. The badge is fully WCAG 2.2 compliant â
screen readers announce it, it is visible in Windows High Contrast Mode, and it
never relies on colour alone.

---

## Features

- **Auto-detection** â scans audio track `Title`, `DisplayTitle`, and `Language`
  fields for configurable keywords (`Audio Description`, `AD`, `VI`, etc.)
- **WCAG 2.2 compliant** â 7.5:1 colour contrast, `role="img"` + `aria-label`,
  forced-colours / HCM support, polite ARIA live region, reduced-motion support
- **Batch API** â groups card lookups into batches to avoid per-card HTTP round-trips
- **Configurable** â keyword list, badge corner, and label visibility are all
  adjustable from the Jellyfin plugin settings page

---

## Requirements

| Dependency | Version |
|---|---|
| Jellyfin Server | â¥ 10.9.0 |
| .NET SDK | â¥ 8.0 |

---

## Build

```bash
# Clone / download the plugin source, then:
cd Jellyfin.Plugin.AudioDescription
dotnet build -c Release
```

The compiled DLL will be at:

```
bin/Release/net8.0/Jellyfin.Plugin.AudioDescription.dll
```

---

## Install

1. Stop your Jellyfin server.

2. Copy the compiled DLL into your Jellyfin plugins directory.
   The default paths are:

   | OS | Path |
   |---|---|
   | Linux | `/var/lib/jellyfin/plugins/AudioDescriptionBadges/` |
   | macOS | `~/.local/share/jellyfin/plugins/AudioDescriptionBadges/` |
   | Windows | `%APPDATA%\Jellyfin\plugins\AudioDescriptionBadges\` |
   | Docker | `/config/plugins/AudioDescriptionBadges/` |

   Create the sub-folder if it does not exist. The folder name is arbitrary but
   must be unique; Jellyfin discovers plugins by scanning all sub-folders.

3. Start Jellyfin.

4. In the Jellyfin web UI, go to **Dashboard â Plugins â Audio Description
   Badges** to verify the plugin loaded and adjust settings.

---

## How it works

```
Browser                              Jellyfin Server
ââââââ                               âââââââââââââââ
Page load / card scroll
  ââ MutationObserver fires
       ââ batch POST /AudioDescription/Check?ids=â¦
                                       ââ reads MediaStreams for each item
                                            ââ returns {itemId: "track title"} map
  ââ badge injected on matching cards
       â¢ role="img" aria-label="Audio Description available: <track title>"
       â¢ #00538C background / white text  (7.5:1 contrast ratio)
       â¢ Windows HCM: Highlight + HighlightText system colours
       â¢ Polite live region announces count to screen readers
```

---

## Configuration

| Setting | Default | Description |
|---|---|---|
| **Keywords** | `audio description, descriptive audio, described video, visually impaired, AD, VI` | Comma-separated strings matched case-insensitively against audio track title/language |
| **Badge position** | Top-left | Corner of the card thumbnail |
| **Show text label** | Yes | Displays "AD" text next to the headphone icon. Recommended for clarity without relying on the icon alone. |

---

## Accessibility notes (WCAG 2.2)

| Criterion | Requirement | Implementation |
|---|---|---|
| 1.1.1 Non-text Content | Text alternative for icon | `role="img"` + `aria-label="Audio Description available: <track>"` on badge |
| 1.4.1 Use of Colour | Not colour alone | Badge shows text ("AD") + headphone icon; colour is additive |
| 1.4.3 Contrast (Minimum) | â¥ 4.5:1 text | `#ffffff` on `#00538C` = **7.5:1** |
| 1.4.11 Non-text Contrast | â¥ 3:1 UI boundary | Badge background vs card background â¥ 3:1 |
| 1.4.12 Text Spacing | No clipping | `em`/`rem` sizing, no fixed heights |
| 2.3.3 Animation | Respect `prefers-reduced-motion` | `@media (prefers-reduced-motion: reduce)` disables fade-in |
| 4.1.3 Status Messages | Polite live region | `role="status"` aria-live="polite" announces badge count on new cards |

---

## Development

```bash
# Watch-build during development
dotnet watch build

# Restore NuGet packages
dotnet restore
```

The web assets (`audioDescription.js`, `audioDescription.css`, `configPage.html`)
are embedded as resources in the DLL via `<EmbeddedResource>` in the `.csproj`.
Jellyfin serves them automatically.

---

## License

MIT â see [LICENSE](LICENSE)
