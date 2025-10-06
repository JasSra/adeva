# MVC + Tailwind UI Kit (drop-in)

## What you get

- Tailwind component classes in `Styles/input.css` under `@layer components` using `tw-*` prefixes (buttons, badges, cards, alerts)
- Shared partials:
  - `Views/Shared/Partials/_ThemeScript.cshtml`: automatic dark-mode with OS sync, exposes `window.toggleDarkMode()`
  - `Views/Shared/Partials/_Footer.cshtml`: simple responsive footer
  - `Views/Shared/Partials/_UiKitDemo.cshtml`: optional demo snippet

## How to use

1) Build CSS

   - npm run build (or npm run dev to watch)

2) Use classes

   - Buttons: `tw-btn`, `tw-btn-outline`, `tw-btn-ghost`, `tw-btn-danger`, `tw-btn-danger-outline`
   - Badges: `tw-badge`, `tw-badge-dev`
   - Cards: `tw-card`, `tw-card-header`, `tw-card-body`
   - Alerts: `tw-alert-<info|success|warning|danger>`

3) Include partials

   - Theme:

     ```cshtml
     @await Html.PartialAsync("~/Views/Shared/Partials/_ThemeScript.cshtml")
     ```

     (Already wired in `_Layout`)

   - Footer:

     ```cshtml
     @await Html.PartialAsync("~/Views/Shared/Partials/_Footer.cshtml")
     ```

     (Already wired in `_Layout`)

   - Demo (optional):

     ```cshtml
     @await Html.PartialAsync("~/Views/Shared/Partials/_UiKitDemo.cshtml")
     ```

## Notes

- Components are additive alongside existing `brand.css` to avoid breaking changes. You can migrate `brand.css` buttons to `tw-*` by replacing class names incrementally.
- Dark mode is class-based (`tailwind.config.cjs` `darkMode: 'class'`).
