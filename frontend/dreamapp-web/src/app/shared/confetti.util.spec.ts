import { fireConfetti } from './confetti.util';

describe('fireConfetti', () => {
  afterEach(() => document.querySelectorAll('.confetti-layer').forEach((layer) => layer.remove()));

  it('adds a click-through layer with the requested number of pieces', () => {
    fireConfetti({ x: 10, y: 10 }, 12);

    const layer = document.querySelector('.confetti-layer') as HTMLElement | null;
    expect(layer).not.toBeNull();
    expect(layer!.style.pointerEvents).toBe('none');
    expect(layer!.querySelectorAll('.confetti-piece').length).toBe(12);
  });

  it('does nothing when the user prefers reduced motion', () => {
    spyOn(window, 'matchMedia').and.returnValue({ matches: true } as MediaQueryList);

    fireConfetti();

    expect(document.querySelector('.confetti-layer')).toBeNull();
  });
});
