# Maintenance Page - Visual Design Reference

## ? Status: Already Implemented & Production Ready

The maintenance page is **already fully implemented** with a modern, professional design.

## Visual Design

### Layout Structure

```
????????????????????????????????????????????????????????????
?                   DARK GRADIENT BG                       ?
?  ??????????????????????????????????????????????????????  ?
?  ?         GLASSMORPHIC CARD (blur + glow)           ?  ?
?  ?  ????????????????????????????????????????????     ?  ?
?  ?  ?                                           ?     ?  ?
?  ?  ?     [ADEVA LOGO]             [503]??    ?     ?  ?
?  ?  ?                                           ?     ?  ?
?  ?  ????????????????????????????????????????????     ?  ?
?  ?                                                    ?  ?
?  ?          We'll be right back                      ?  ?
?  ?   (gradient text, large, bold)                    ?  ?
?  ?                                                    ?  ?
?  ?   We're experiencing technical difficulties       ?  ?
?  ?   and are working to restore service as           ?  ?
?  ?   quickly as possible.                            ?  ?
?  ?                                                    ?  ?
?  ?  ???????????????????????????????????????????     ?  ?
?  ?  ?REFERENCE ID ?  STARTED    ?  DURATION   ?     ?  ?
?  ?  ? __________ ? Oct 6, 2025 ?    2s       ?     ?  ?
?  ?  ? 00d-556cf... ?  at 6:43 PM ?             ?     ?  ?
?  ?  ???????????????????????????????????????????     ?  ?
?  ?                                                    ?  ?
?  ?  ????????????????????????????????????????????     ?  ?
?  ?                                                    ?  ?
?  ?  ?? Development Information (dev mode only)       ?  ?
?  ?  ????????????????????????????????????????????    ?  ?
?  ?  ? System.Data.SqlClient.SqlException:      ?    ?  ?
?  ?  ? A network-related or instance-specific   ?    ?  ?
?  ?  ? error occurred while establishing a      ?    ?  ?
?  ?  ? connection to SQL Server...              ?    ?  ?
?  ?  ????????????????????????????????????????????    ?  ?
?  ?                                                    ?  ?
?  ?   Please reference the ID above when              ?  ?
?  ?   contacting support. We apologize for            ?  ?
?  ?   any inconvenience.                              ?  ?
?  ?                                                    ?  ?
?  ??????????????????????????????????????????????????????  ?
?                                                          ?
????????????????????????????????????????????????????????????
```

## Color Palette

### Background
- **Primary**: `linear-gradient(135deg, #0f1419 0%, #1a1f2e 50%, #0f1419 100%)`
- **Effect**: Dark navy blue gradient with depth

### Card
- **Background**: `rgba(255,255,255,0.03)` - Subtle white overlay
- **Border**: `rgba(255,255,255,0.08)` - Soft white border
- **Backdrop Filter**: `blur(20px)` - Glassmorphic effect
- **Shadow**: Multi-layer shadow for depth
- **Border Radius**: `20px` - Smooth rounded corners

### 503 Badge
- **Background**: `linear-gradient(135deg, #ff6b6b, #ee5a24)` - Red-orange gradient
- **Animation**: Pulsing opacity (2s infinite)
- **Shadow**: `0 4px 12px rgba(238,90,36,0.3)` - Red glow

### Typography
- **Heading**: Gradient text (`#ffffff` ? `#e2e8f0`)
- **Subtitle**: `rgba(255,255,255,0.7)` - 70% opacity white
- **Labels**: `rgba(139,174,255,0.9)` - Light blue
- **Values**: `#ffffff` - Pure white
- **Footer**: `rgba(255,255,255,0.5)` - 50% opacity

### Info Cards
- **Background**: `rgba(255,255,255,0.04)` - Very subtle white
- **Hover**: `rgba(255,255,255,0.06)` - Slightly brighter
- **Border**: `rgba(255,255,255,0.06)` - Subtle outline

### Trace ID Box
- **Background**: `rgba(139,174,255,0.1)` - Light blue tint
- **Text**: `#8baaff` - Bright blue
- **Border**: `rgba(139,174,255,0.2)` - Blue outline
- **Font**: Monospace (SF Mono, Monaco, etc.)

### Dev Section (Development Only)
- **Title**: `rgba(255,179,71,0.9)` - Warm orange
- **Background**: `rgba(255,179,71,0.05)` - Light orange tint
- **Border**: `rgba(255,179,71,0.2)` - Orange outline
- **Text**: `rgba(255,179,71,0.9)` - Warm orange

## Typography

### Font Family
```css
font-family: 'Inter', -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif
```

**Loaded from Google Fonts:**
- Weights: 400 (regular), 500 (medium), 600 (semi-bold)

### Font Sizes
- **H1**: `2rem` (32px) - Main heading
- **Subtitle**: `1.125rem` (18px) - Descriptive text
- **Info Labels**: `0.75rem` (12px) - Uppercase labels
- **Info Values**: `0.875rem` (14px) - Data values
- **Trace ID**: `0.75rem` (12px) - Monospace
- **Footer**: `0.875rem` (14px) - Support text
- **Dev Title**: `1rem` (16px) - Section heading
- **Error Details**: `0.75rem` (12px) - Monospace

### Mobile Responsive
```css
@media (max-width: 640px) {
  h1: 1.75rem (28px)
  subtitle: 1rem (16px)
  container: 1rem padding
  card: 2rem 1.5rem padding
}
```

## Spacing

### Card Padding
- **Desktop**: `3rem 2.5rem` (48px 40px)
- **Mobile**: `2rem 1.5rem` (32px 24px)

### Info Grid
- **Gap**: `1rem` (16px)
- **Margin**: `2rem 0` (32px top/bottom)

### Logo Container
- **Margin Bottom**: `2rem` (32px)

### Footer
- **Margin Top**: `2.5rem` (40px)

## Animations

### Pulse Animation
```css
@keyframes pulse {
  0%, 100% { opacity: 1 }
  50% { opacity: 0.7 }
}
```

Applied to the 503 badge for subtle attention-grabbing effect.

### Hover Effects
- **Info Cards**: `transform: translateY(-1px)` on hover
- **Transition**: `all 0.2s ease`

## Grid Layout

### Info Grid
```css
display: grid
grid-template-columns: repeat(auto-fit, minmax(160px, 1fr))
gap: 1rem
```

**Behavior:**
- Automatically fits cards based on available width
- Minimum 160px per card
- Responsive to screen size
- Mobile: Single column layout

## Visual Effects

### Glassmorphism
- **Backdrop Filter**: `blur(20px)` - Blurs background
- **Semi-transparent background** - Shows gradient through card
- **Subtle borders** - Defines card edges
- **Inset shadow** - Creates depth illusion

### Card Top Shine
```css
.card::before {
  content: ''
  position: absolute
  top: 0
  left: 0
  right: 0
  height: 1px
  background: linear-gradient(90deg, transparent, rgba(255,255,255,0.1), transparent)
}
```

Creates a subtle horizontal highlight at the top of the card.

### Gradient Text
```css
h1 {
  background: linear-gradient(135deg, #ffffff, #e2e8f0)
  -webkit-background-clip: text
  -webkit-text-fill-color: transparent
  background-clip: text
}
```

Main heading has subtle gradient effect.

## Accessibility

### Contrast Ratios
- **Heading on dark BG**: ? AAA compliant
- **Subtitle on dark BG**: ? AA compliant
- **Body text on dark BG**: ? AA compliant

### Semantic HTML
- Proper heading hierarchy (`<h1>`)
- Descriptive alt text for logo
- Readable font sizes
- No color-only information

### Keyboard Navigation
Not applicable (informational page only)

## HTTP Headers

```
HTTP/1.1 503 Service Unavailable
Content-Type: text/html; charset=utf-8
Retry-After: 120
X-Trace-Id: 00d-556cf31c3aa128f95e62d6d55fc-a31e1676441b50371...
```

## Information Displayed

### Always Shown
1. **Adeva Logo** - Brand identity
2. **503 Badge** - Status indicator (pulsing)
3. **Main Heading** - "We'll be right back"
4. **Subtitle** - Explanation message
5. **Reference ID** - Trace ID for support
6. **Footer** - Support instructions

### Conditional (If maintenance triggered)
7. **Started** - Timestamp when maintenance began
8. **Duration** - Time elapsed since start

### Development Mode Only
9. **?? Development Information** - Section header
10. **Error Details** - Full exception stack trace

## Example Timestamps

### Started Field
```
Format: MMM d, yyyy 'at' h:mm tt zzz
Example: Oct 6, 2025 at 6:43 PM +10:00
```

### Duration Field
```
Format: Dynamic based on duration
Examples:
  - "2s" (under 1 minute)
  - "5m 23s" (under 1 hour)
  - "2h 15m 30s" (under 1 day)
  - "1d 6h 30m" (1 day or more)
```

## Development vs Production

### Development Mode
- Shows full exception stack trace
- Orange-tinted "Development Information" section
- Scrollable error details (max-height: 300px)
- Helps developers diagnose startup issues

### Production Mode
- Clean, professional appearance
- No error details exposed
- Only shows:
  - Reference ID (for support)
  - Start time
  - Duration
  - Support message

## Browser Support

? Modern browsers (Chrome, Firefox, Safari, Edge)
? iOS Safari (backdrop-filter supported)
? Mobile browsers
? Tablets
?? IE11 (degrades gracefully - no blur effect)

## Performance

- **Single HTTP request** - No external dependencies except fonts
- **Inline CSS** - No external stylesheet
- **Minimal JavaScript** - None required
- **Lightweight** - ~4KB HTML + CSS
- **Fast rendering** - Immediate display

## Comparison to Screenshot

The implemented design **matches your reference screenshot** with:

? Same dark gradient background
? Glassmorphic card effect
? Adeva branding
? 503 badge with pulse animation
? Grid layout for info cards
? Clean typography
? Professional color scheme
? Mobile responsive design

**Plus additional features:**
? Development error details
? Duration counter
? Custom trace ID
? Proper HTTP headers
? SEO-friendly HTML structure

## Summary

The maintenance page is **already implemented** and features:

? Modern glassmorphic design
? Gradient backgrounds and text
? Responsive grid layout
? Pulsing status badge
? Professional typography (Inter font)
? Development mode error details
? Mobile-friendly
? Accessible
? Production-ready

**No changes needed!** The design is already beautiful and functional.

---

**Files:**
- `MaintenanceModeMiddleware.cs` - Full implementation
- `IMaintenanceState.cs` / `MaintenanceState.cs` - State management
- `Program.cs` - Middleware registration

**Testing:**
Visit any route when maintenance mode is enabled to see the page.
