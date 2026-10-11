const COLORS = ['#a1c4fd', '#fbc7d4', '#f6d365', '#c2ffd8', '#ea7f98', '#d9c6ff'];

/**
 * A short burst of pastel confetti, used to celebrate a gift being marked as
 * given/received. Pure DOM + the Web Animations API (no library): the pieces
 * live in a fixed, click-through layer and remove themselves when done.
 *
 * Does nothing for users who asked for reduced motion, or outside a browser.
 *
 * @param origin where the burst starts, in viewport pixels (defaults to the
 *               lower middle of the screen, where the action buttons are).
 */
export function fireConfetti(origin?: { x: number; y: number }, pieces = 36): void {
  if (typeof document === 'undefined' || typeof window === 'undefined') {
    return;
  }

  if (window.matchMedia?.('(prefers-reduced-motion: reduce)').matches) {
    return;
  }

  const startX = origin?.x ?? window.innerWidth / 2;
  const startY = origin?.y ?? window.innerHeight * 0.62;

  const layer = document.createElement('div');
  layer.className = 'confetti-layer';
  layer.setAttribute('aria-hidden', 'true');
  layer.style.cssText = 'position:fixed;inset:0;pointer-events:none;overflow:hidden;z-index:2000;';
  document.body.appendChild(layer);

  let remaining = pieces;
  for (let i = 0; i < pieces; i++) {
    const piece = document.createElement('span');
    const size = 6 + Math.random() * 7;
    const round = Math.random() > 0.55;
    piece.className = 'confetti-piece';
    piece.style.cssText =
      `position:absolute;left:${startX}px;top:${startY}px;width:${size}px;height:${round ? size : size * 1.7}px;` +
      `background:${COLORS[i % COLORS.length]};border-radius:${round ? '50%' : '2px'};will-change:transform,opacity;`;
    layer.appendChild(piece);

    // Fan out upwards, then fall: the second keyframe is the apex, the third the landing.
    const angle = (-90 + (Math.random() - 0.5) * 140) * (Math.PI / 180);
    const power = 90 + Math.random() * 170;
    const dx = Math.cos(angle) * power;
    const dy = Math.sin(angle) * power;
    const spin = (Math.random() - 0.5) * 720;
    const duration = 900 + Math.random() * 700;

    const animation = piece.animate(
      [
        { transform: 'translate(0, 0) rotate(0deg)', opacity: 1 },
        { transform: `translate(${dx}px, ${dy}px) rotate(${spin * 0.6}deg)`, opacity: 1, offset: 0.55 },
        { transform: `translate(${dx * 1.2}px, ${dy + 280}px) rotate(${spin}deg)`, opacity: 0 }
      ],
      { duration, easing: 'cubic-bezier(0.22, 0.8, 0.4, 1)', fill: 'forwards' }
    );

    animation.onfinish = () => {
      remaining -= 1;
      if (remaining === 0) {
        layer.remove();
      }
    };
  }
}
