import { AbstractControl, ValidationErrors } from '@angular/forms';

/**
 * Turns what a user typically pastes ("www.amazon.it/prodotto") into a real
 * absolute URL by adding https:// when no scheme is present. Empty stays empty.
 */
export function normalizeProductUrl(value: string | null | undefined): string {
  const trimmed = (value ?? '').trim();
  if (!trimmed) {
    return '';
  }
  return /^[a-z][a-z0-9+.-]*:/i.test(trimmed) ? trimmed : `https://${trimmed}`;
}

/** Optional http(s) link: empty is fine, anything else must be a usable web address. */
export function optionalWebUrl(control: AbstractControl): ValidationErrors | null {
  const raw = (control.value ?? '').toString().trim();
  if (!raw) {
    return null;
  }

  try {
    const url = new URL(normalizeProductUrl(raw));
    const isWeb = url.protocol === 'http:' || url.protocol === 'https:';
    const hasRealHost = url.hostname.includes('.') || url.hostname === 'localhost';
    return isWeb && hasRealHost ? null : { webUrl: true };
  } catch {
    return { webUrl: true };
  }
}
