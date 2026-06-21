/**
 * Jellyfin Audio Description Badge Plugin
 *
 * Injects an WCAG 2.2-compliant badge onto library cards for any media item
 * that contains an Audio Description audio track.
 *
 * Accessibility notes
 * âââââââââââââââââââ
 * â¢ The badge element uses role="img" + aria-label so screen readers announce
 *   "Audio Description available" when navigating cards.
 * â¢ Color is never the sole differentiator â the badge always carries visible
 *   text ("AD") and a title tooltip.
 * â¢ Contrast ratio of badge text on background meets WCAG 2.2 SC 1.4.3 (â¥4.5:1).
 * â¢ Focus is not stolen; the badge is non-interactive (presentational only on
 *   the card link that already has focus management).
 * â¢ Live region announces when badges have been applied to newly rendered cards
 *   so screen-reader users know the state has updated (SC 4.1.3).
 */

(function () {
  "use strict";

  // ââ Constants ââââââââââââââââââââââââââââââââââââââââââââââââââââââââââââââ

  const PLUGIN_ID = "audio-description-badge";
  const API_BASE = "/AudioDescription";
  const BATCH_SIZE = 50; // max IDs per API call
  const DEBOUNCE_MS = 150; // wait after DOM settles before querying
  const PROCESSED_ATTR = "data-ad-checked"; // marks cards already evaluated
  const HAS_AD_ATTR = "data-ad-track"; // value = track title

  // ââ State ââââââââââââââââââââââââââââââââââââââââââââââââââââââââââââââââââ

  /** Map of itemId â track title (or false = confirmed no AD track) */
  const cache = new Map();

  /** Config fetched from plugin API */
  let pluginConfig = {
    badgePosition: "topleft",
    showBadgeLabel: true,
  };

  /** ARIA live region for announcing badge updates to screen readers */
  let liveRegion;

  // ââ Bootstrap âââââââââââââââââââââââââââââââââââââââââââââââââââââââââââââ

  async function init() {
    await loadConfig();
    injectStyles();
    createLiveRegion();
    observeDOM();
    // Process any cards already on-screen at load time
    scheduleCardScan();
  }

  // Jellyfin's /Config and /Check endpoints require auth. Prefer the web
  // client's ApiClient (it attaches the access token automatically); fall back
  // to a token-appended fetch in case ApiClient isn't present on the page yet.
  // A plain fetch with credentials:"same-origin" sends no token and gets 401.
  function apiGetJson(path) {
    const rel = path.replace(/^\//, ""); // ApiClient.getUrl expects no leading slash
    if (typeof ApiClient !== "undefined" && ApiClient.getUrl && ApiClient.ajax) {
      return ApiClient.ajax({ type: "GET", url: ApiClient.getUrl(rel), dataType: "json" });
    }
    const token =
      typeof ApiClient !== "undefined" && ApiClient.accessToken ? ApiClient.accessToken() : "";
    const sep = path.includes("?") ? "&" : "?";
    const url = path + (token ? `${sep}api_key=${encodeURIComponent(token)}` : "");
    return fetch(url, { credentials: "same-origin" }).then((r) =>
      r.ok ? r.json() : Promise.reject(new Error("HTTP " + r.status))
    );
  }

  async function loadConfig() {
    try {
      pluginConfig = await apiGetJson(`${API_BASE}/Config`);
    } catch (e) {
      console.warn("[AD Badge] Could not load plugin config, using defaults.", e);
    }
  }

  // ââ DOM Observation ââââââââââââââââââââââââââââââââââââââââââââââââââââââââ

  let scanTimer = null;

  function observeDOM() {
    const observer = new MutationObserver(() => {
      scheduleCardScan();
    });

    observer.observe(document.body, {
      childList: true,
      subtree: true,
    });
  }

  function scheduleCardScan() {
    clearTimeout(scanTimer);
    scanTimer = setTimeout(scanCards, DEBOUNCE_MS);
  }

  // ââ Card Scanning & Batch API Calls âââââââââââââââââââââââââââââââââââââââ

  async function scanCards() {
    // Jellyfin renders cards with a data-id attribute on the .card element.
    // We also check the inner cardBox > cardScalable > cardPadder anchor.
    const allCards = document.querySelectorAll(
      `.card[data-id]:not([${PROCESSED_ATTR}])`
    );

    if (allCards.length === 0) return;

    // Gather unresolved IDs
    const pending = [];
    for (const card of allCards) {
      const id = card.dataset.id;
      if (!id) continue;
      card.setAttribute(PROCESSED_ATTR, "1"); // mark so we don't re-query
      if (!cache.has(id)) {
        pending.push(id);
      }
    }

    // Fetch from API in batches
    if (pending.length > 0) {
      for (let i = 0; i < pending.length; i += BATCH_SIZE) {
        const batch = pending.slice(i, i + BATCH_SIZE);
        await fetchBatch(batch);
      }
    }

    // Apply badges to all cards now that cache is populated
    let newBadgeCount = 0;
    for (const card of allCards) {
      const id = card.dataset.id;
      if (!id) continue;
      const trackTitle = cache.get(id);
      if (trackTitle && !card.querySelector(`.${PLUGIN_ID}`)) {
        applyBadge(card, trackTitle);
        newBadgeCount++;
      }
    }

    if (newBadgeCount > 0) {
      announceToScreenReader(
        `${newBadgeCount} item${newBadgeCount > 1 ? "s" : ""} with Audio Description found.`
      );
    }
  }

  async function fetchBatch(ids) {
    try {
      const params = new URLSearchParams({ ids: ids.join(",") });
      /** @type {Record<string, string>} */
      const data = await apiGetJson(`${API_BASE}/Check?${params}`);

      // Populate cache: items returned have AD; items not in response do not
      for (const id of ids) {
        cache.set(id, data[id] ?? false);
      }
    } catch (e) {
      console.warn("[AD Badge] Batch fetch failed:", e);
    }
  }

  // ââ Badge Rendering ââââââââââââââââââââââââââââââââââââââââââââââââââââââââ

  function applyBadge(card, trackTitle) {
    // Find the image container â the overlay target
    const imageContainer =
      card.querySelector(".cardImageContainer") ||
      card.querySelector(".cardScalable") ||
      card;

    // Ensure the container can hold an absolute-positioned child
    const pos = window.getComputedStyle(imageContainer).position;
    if (pos === "static") {
      imageContainer.style.position = "relative";
    }

    const badge = document.createElement("span");
    // Config delivers the position as the C# enum name (e.g. "TopLeft"); the CSS
    // corner classes are lowercase (.ad-badge--topleft), and CSS class names are
    // case-sensitive — so normalise to lowercase or the position never matches.
    const position = String(pluginConfig.badgePosition || "topleft").toLowerCase();
    badge.className = `${PLUGIN_ID} ad-badge--${position}`;

    // ââ Accessibility ââââââââââââââââââââââââââââââââââââââââââââââââââââââ
    // role="img" turns the span into a perceivable image object for AT.
    // aria-label provides the full accessible name.
    // title provides a hover tooltip for pointer users.
    badge.setAttribute("role", "img");
    badge.setAttribute(
      "aria-label",
      `Audio Description available: ${trackTitle}`
    );
    badge.setAttribute("title", `Audio Description: ${trackTitle}`);

    // Visual: headphone icon (inline SVG, no external dependency)
    const icon = document.createElementNS("http://www.w3.org/2000/svg", "svg");
    icon.setAttribute("viewBox", "0 0 24 24");
    icon.setAttribute("width", "14");
    icon.setAttribute("height", "14");
    icon.setAttribute("aria-hidden", "true"); // icon is decorative; label is on parent
    icon.setAttribute("focusable", "false");
    icon.innerHTML = `
      <path fill="currentColor"
        d="M12 3a9 9 0 0 0-9 9v5a3 3 0 0 0 3 3h1a1 1 0 0 0 1-1v-4a1 1 0 0 0-1-1H5v-2
           a7 7 0 0 1 14 0v2h-2a1 1 0 0 0-1 1v4a1 1 0 0 0 1 1h1a3 3 0 0 0 3-3v-5
           a9 9 0 0 0-9-9z"/>`;
    badge.appendChild(icon);

    // Visible text label (can be toggled via config)
    if (pluginConfig.showBadgeLabel) {
      const label = document.createElement("span");
      label.className = "ad-badge__label";
      label.textContent = "AD";
      // aria-hidden: the label is a visual duplicate of the aria-label above
      label.setAttribute("aria-hidden", "true");
      badge.appendChild(label);
    }

    imageContainer.appendChild(badge);
    card.setAttribute(HAS_AD_ATTR, trackTitle);
  }

  // ââ CSS Injection ââââââââââââââââââââââââââââââââââââââââââââââââââââââââââ

  function injectStyles() {
    if (document.getElementById(`${PLUGIN_ID}-styles`)) return;

    const style = document.createElement("style");
    style.id = `${PLUGIN_ID}-styles`;
    // The full stylesheet is in audioDescription.css; this is an inline
    // fallback in case the external CSS file hasn't loaded yet.
    style.textContent = getCSSText();
    document.head.appendChild(style);
  }

  function getCSSText() {
    return `
/* ââ Audio Description Badge â WCAG 2.2 compliant ââ */

.${PLUGIN_ID} {
  /* Position: overlaid on the card image */
  position: absolute;
  z-index: 10;
  display: inline-flex;
  align-items: center;
  gap: 3px;
  padding: 3px 6px;
  border-radius: 4px;

  /* High-contrast colour scheme.
     Background: #00538C  (Jellyfin blue, but darkened for contrast)
     Foreground: #FFFFFF
     Contrast ratio: 7.5:1 â exceeds WCAG 2.2 SC 1.4.3 (4.5:1) and
     SC 1.4.6 Enhanced (7:1)                                       */
  background-color: #00538C;
  color: #FFFFFF;

  /* SC 1.4.11: double-ring â white inner (vs dark) + black outer (vs light) */
  box-shadow: 0 0 0 1.5px #ffffff, 0 0 0 3px #000000;

  font-family: inherit;
  font-size: 11px;
  font-weight: 700;
  line-height: 1;
  letter-spacing: 0.03em;

  /* Prevents badge from being a focus trap */
  pointer-events: none;

  /* Smooth appearance */
  opacity: 0;
  animation: ad-badge-appear 0.2s ease forwards;
}

@keyframes ad-badge-appear {
  to { opacity: 1; }
}

/* Respect reduced-motion preference (WCAG 2.2 SC 2.3.3) */
@media (prefers-reduced-motion: reduce) {
  .${PLUGIN_ID} {
    animation: none;
    opacity: 1;
  }
}

/* ââ Corner positions ââ */
.ad-badge--topleft    { top: 6px;    left: 6px;    }
.ad-badge--topright   { top: 6px;    right: 6px;   }
.ad-badge--bottomleft { bottom: 6px; left: 6px;    }
.ad-badge--bottomright{ bottom: 6px; right: 6px;   }

/* Default if no position modifier applied */
.${PLUGIN_ID}:not([class*="ad-badge--"]) {
  top: 6px;
  left: 6px;
}

.ad-badge__label {
  font-size: 11px;
  font-weight: 700;
}

/* High-contrast mode support (WCAG 2.2 SC 1.4.11) */
@media (forced-colors: active) {
  .${PLUGIN_ID} {
    background-color: Highlight;
    color: HighlightText;
    border: 1px solid ButtonText;
    forced-color-adjust: none;
  }
}
`;
  }

  // ââ ARIA Live Region âââââââââââââââââââââââââââââââââââââââââââââââââââââââ

  function createLiveRegion() {
    if (document.getElementById(`${PLUGIN_ID}-live`)) return;

    liveRegion = document.createElement("div");
    liveRegion.id = `${PLUGIN_ID}-live`;
    // role="status" = polite live region; does not interrupt current reading
    liveRegion.setAttribute("role", "status");
    liveRegion.setAttribute("aria-live", "polite");
    liveRegion.setAttribute("aria-atomic", "true");

    // Visually hidden but accessible to screen readers (WCAG 2.2 SC 1.3.1)
    Object.assign(liveRegion.style, {
      position: "absolute",
      width: "1px",
      height: "1px",
      padding: "0",
      margin: "-1px",
      overflow: "hidden",
      clip: "rect(0,0,0,0)",
      whiteSpace: "nowrap",
      border: "0",
    });

    document.body.appendChild(liveRegion);
  }

  function announceToScreenReader(message) {
    if (!liveRegion) return;
    // Clear first so repeated identical messages still fire
    liveRegion.textContent = "";
    // Small timeout ensures AT picks up the change
    setTimeout(() => {
      liveRegion.textContent = message;
    }, 50);
  }

  // ââ Entry point ââââââââââââââââââââââââââââââââââââââââââââââââââââââââââââ

  // Jellyfin's SPA fires DOMContentLoaded once but navigates without full
  // reloads. The MutationObserver handles subsequent navigation; we still
  // want to run init() once the DOM is ready.
  if (document.readyState === "loading") {
    document.addEventListener("DOMContentLoaded", init);
  } else {
    init();
  }
})();
