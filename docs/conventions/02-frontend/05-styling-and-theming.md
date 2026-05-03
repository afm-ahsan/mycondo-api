# Styling and Theming

The frontend uses **Tailwind CSS v4** + the **Metronic theme** that ships with the template. Custom CSS is rare; design tokens live in `@theme` blocks.

---

## 1. Tailwind v4 Setup

Tailwind v4 uses **CSS-first configuration**. Most tokens are defined in CSS, not `tailwind.config.ts`.

```css
/* src/styles/globals.css */
@import "tailwindcss";
@import "./metronic/theme.css";

@theme {
  --color-primary: oklch(63% 0.15 252);
  --color-primary-foreground: white;
  --color-success: oklch(70% 0.16 142);
  --color-warning: oklch(80% 0.15 80);
  --color-danger: oklch(60% 0.21 27);

  --radius: 0.5rem;
  --font-sans: 'Inter', system-ui, sans-serif;
  --font-mono: 'JetBrains Mono', monospace;
}

@layer base {
  body {
    @apply bg-background text-foreground antialiased;
    font-feature-settings: "cv02", "cv03", "cv04", "cv11";
  }
  :focus-visible {
    @apply outline-2 outline-primary outline-offset-2;
  }
}
```

### Rules

- **Tokens live in `@theme`** in CSS, not in `tailwind.config.ts`. The config file is only for plugins / content paths.
- **Use `oklch()` for colors** — perceptual uniformity, easier to derive shades.
- **Body font set on `body`**, not on each component.
- **Focus styles globally enforced** via `:focus-visible`.

---

## 2. Metronic Theme Layers

Metronic ships its own CSS layers (`@layer base`, `@layer components`, `@layer utilities`). Keep them; don't fight them.

```css
/* src/styles/metronic/theme.css */
@layer base { /* Metronic resets and typography */ }
@layer components { /* Metronic component classes */ }
@layer utilities { /* Metronic utility extensions */ }
```

### Rules

- **Don't edit Metronic CSS directly.** Override via your own classes in `globals.css`.
- **Tailwind utilities win** because they're loaded last. Use them to override theme defaults inline.

---

## 3. The `cn()` Helper

`src/lib/cn.ts`:

```ts
import { clsx, type ClassValue } from 'clsx';
import { twMerge } from 'tailwind-merge';

export function cn(...inputs: ClassValue[]) {
  return twMerge(clsx(inputs));
}
```

Use it for **all** conditional or composed class strings:

```tsx
<button
  className={cn(
    'rounded-lg px-4 py-2 font-medium transition-colors',
    variant === 'primary' && 'bg-primary text-primary-foreground hover:bg-primary/90',
    variant === 'ghost' && 'hover:bg-accent',
    disabled && 'opacity-50 cursor-not-allowed',
    className,    // forward consumer overrides
  )}
>
  {children}
</button>
```

### Rules

- **Always `cn()`**, never raw string concatenation or template literals for class strings.
- **Forward `className`** as the last argument so consumers can override.
- **`tailwind-merge` deduplicates conflicting utilities** (`bg-red-500 bg-blue-500` → `bg-blue-500`).

---

## 4. Component Variants with `cva`

For components with multiple visual states, use `class-variance-authority`:

```ts
// src/components/ui/badge.tsx
import { cva, type VariantProps } from 'class-variance-authority';
import { cn } from '@/lib/cn';

const badgeVariants = cva(
  'inline-flex items-center gap-1 rounded-md px-2 py-0.5 text-xs font-medium',
  {
    variants: {
      variant: {
        default:  'bg-secondary text-secondary-foreground',
        primary:  'bg-primary text-primary-foreground',
        success:  'bg-success/10 text-success',
        warning:  'bg-warning/10 text-warning',
        danger:   'bg-danger/10 text-danger',
        outline:  'border border-input bg-background',
      },
      size: {
        sm: 'text-[10px] px-1.5',
        md: 'text-xs px-2',
        lg: 'text-sm px-3 py-1',
      },
    },
    defaultVariants: { variant: 'default', size: 'md' },
  }
);

type BadgeProps = React.HTMLAttributes<HTMLSpanElement> & VariantProps<typeof badgeVariants>;

export function Badge({ className, variant, size, ...props }: BadgeProps) {
  return <span className={cn(badgeVariants({ variant, size }), className)} {...props} />;
}
```

### Rules

- **One variant config per component.**
- **`defaultVariants` always set.**
- **Variants for visual variations only.** Use props for behavior (disabled, loading).

---

## 5. Layout — Tailwind, Not CSS Grid Files

Compose layouts with Tailwind utilities:

```tsx
<div className="grid grid-cols-1 gap-4 lg:grid-cols-12">
  <aside className="lg:col-span-3"> ... </aside>
  <main  className="lg:col-span-9"> ... </main>
</div>
```

### Rules

- **Container queries** (`@container`) for components that should respond to their parent's width:
  ```tsx
  <div className="@container">
    <div className="@md:flex @md:gap-4"> ... </div>
  </div>
  ```
- **Spacing via Tailwind scales** (`gap-4`, `space-y-3`). Avoid arbitrary values like `gap-[17px]`.
- **No CSS grid in stylesheets.** It's all utilities.

---

## 6. Dark Mode

Metronic supports light + dark. Tailwind v4 uses the `@variant` syntax:

```css
@layer base {
  :root {
    --color-background: white;
    --color-foreground: oklch(15% 0 0);
  }
  .dark {
    --color-background: oklch(12% 0 0);
    --color-foreground: oklch(95% 0 0);
  }
}
```

In components, use Tailwind's `dark:` modifier:

```tsx
<div className="bg-white dark:bg-zinc-900">...</div>
```

Mode switching lives in a Redux UI slice; the wrapper element gets `class="dark"` toggled.

### Rules

- **Prefer CSS variables over `dark:` modifiers** for design tokens. The variables flip automatically.
- **Use `dark:`** only for one-off cases not covered by tokens.
- **Persist user preference** in `localStorage`. Honor `prefers-color-scheme` on first visit.

---

## 7. Icons

The Metronic template ships with **KeenIcons** (a wrapper around an icon font and SVGs). For modern usage, prefer the **Lucide** library:

```tsx
import { Plus, Pencil, Trash2 } from 'lucide-react';

<Button>
  <Plus className="size-4" />
  Add Customer
</Button>
```

### Rules

- **Lucide for new components.** KeenIcons preserved where Metronic uses them (don't break the theme).
- **`size-4`** (Tailwind 16px) for inline icons; **`size-5`** for buttons; **`size-6`** for prominent UI.
- **Icons are decorative** unless they have semantic meaning. Add `aria-hidden="true"` for decorative; `aria-label` for actionable.

---

## 8. Typography

```css
@layer base {
  h1 { @apply text-3xl font-semibold tracking-tight; }
  h2 { @apply text-2xl font-semibold tracking-tight; }
  h3 { @apply text-xl font-medium; }
  h4 { @apply text-lg font-medium; }
  p  { @apply leading-relaxed; }
}
```

### Rules

- **One H1 per page.** Use `<h2>`, `<h3>` for sections.
- **No hand-rolled font sizes** unless designed. Tailwind's scale (`text-xs` → `text-5xl`) covers nearly everything.
- **Line-height via Tailwind** (`leading-relaxed`) — readable bodies are 1.5–1.7.

---

## 9. Spacing Scale

| Tailwind utility | Pixels (default 4px scale) | Use case                              |
|------------------|----------------------------|---------------------------------------|
| `space-y-1`      | 4px                        | Tightly grouped (label + input)       |
| `space-y-2`      | 8px                        | Form fields                           |
| `space-y-3`      | 12px                       | Card sections                         |
| `space-y-4`      | 16px                       | Default                               |
| `space-y-6`      | 24px                       | Major sections                        |
| `space-y-8`      | 32px                       | Page-level breaks                     |
| `space-y-12`     | 48px                       | Hero / landing                        |

### Rules

- **Stick to the scale.** `gap-3` not `gap-[10px]`.
- **Vertical rhythm**: `space-y-*` on parent, not `mt-*` on every child.

---

## 10. Responsive Design

Mobile-first. Stack on small screens; lay out on `md:` (768px) and `lg:` (1024px):

```tsx
<div className="flex flex-col gap-4 md:flex-row md:items-center">
  <Input placeholder="Search..." className="md:max-w-md" />
  <Button>Filter</Button>
</div>
```

### Rules

- **Test at 320px, 768px, 1280px, 1920px** at minimum.
- **Sidebars collapse to drawers below `lg:`.**
- **No horizontal scroll** on any viewport.

---

## 11. Animations

Use Tailwind's transition utilities. For custom keyframes, define in `@theme`:

```css
@theme {
  --animate-spin: spin 1s linear infinite;
  --animate-fade-in: fadeIn 200ms ease-out;
}

@keyframes fadeIn {
  from { opacity: 0; }
  to   { opacity: 1; }
}
```

```tsx
<div className="animate-fade-in">...</div>
```

### Rules

- **Transitions for state changes only** (hover, focus, open/close).
- **Respect `prefers-reduced-motion`**:
  ```css
  @media (prefers-reduced-motion: reduce) {
    *, *::before, *::after { animation-duration: 0.01ms !important; }
  }
  ```
- **No heavy parallax / scroll-driven animations** in business apps.

---

## 12. Inline Styles

Use **only** for dynamic values that can't be expressed as classes:

```tsx
<div style={{ '--progress': `${percent}%` } as React.CSSProperties}>
  <div className="bg-primary h-2 rounded" style={{ width: 'var(--progress)' }} />
</div>
```

### Rules

- **Computed positions, dynamic widths/heights** — inline `style`.
- **Anything static** — Tailwind class.

---

## 13. CSS Modules (Last Resort)

For components where Tailwind genuinely doesn't work (complex pseudo-elements, third-party widget overrides):

```tsx
// Component.module.css
.thirdPartyOverride :global(.foreign-class) {
  border-radius: 8px !important;
}
```

### Rules

- **Use sparingly.** If you reach for CSS Modules in 5+ places, ask why.
- **`:global()`** for overriding third-party CSS only.

---

## 14. Common Mistakes

| Mistake                                                | Fix                                                              |
|--------------------------------------------------------|------------------------------------------------------------------|
| Conditional class via template literal                 | `cn()`                                                           |
| `tailwind.config.ts` for theme tokens                  | Use `@theme` in CSS                                              |
| Arbitrary spacing values (`mt-[17px]`)                 | Stick to the scale                                               |
| `dark:bg-zinc-900` everywhere                          | Use design tokens that flip via CSS variables                    |
| One H1 per section                                     | One H1 per page                                                  |
| Inline `style` for static values                       | Tailwind utility                                                 |
| Custom CSS file per component                          | Tailwind utilities + cva                                         |
| Long `className=""` strings without wrapping           | Format with `cn()` or a constant                                 |
| Importing Tailwind in every CSS Module                 | Tailwind is global; modules add scoped overrides only            |
