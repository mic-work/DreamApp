import { FormControl } from '@angular/forms';
import { normalizeProductUrl, optionalWebUrl } from './validators';

describe('optionalWebUrl', () => {
  const check = (value: string) => optionalWebUrl(new FormControl(value));

  it('accepts an empty value (the link is optional)', () => {
    expect(check('')).toBeNull();
    expect(check('   ')).toBeNull();
  });

  it('accepts full and scheme-less web addresses', () => {
    expect(check('https://www.amazon.it/dp/123')).toBeNull();
    expect(check('http://example.com')).toBeNull();
    expect(check('www.amazon.it/prodotto')).toBeNull();
  });

  it('rejects text that is not a web address', () => {
    expect(check('ciao')).toEqual({ webUrl: true });
    expect(check('javascript:alert(1)')).toEqual({ webUrl: true });
    expect(check('ftp://example.com')).toEqual({ webUrl: true });
  });
});

describe('normalizeProductUrl', () => {
  it('adds https:// only when there is no scheme', () => {
    expect(normalizeProductUrl('www.amazon.it/x')).toBe('https://www.amazon.it/x');
    expect(normalizeProductUrl(' http://a.it ')).toBe('http://a.it');
    expect(normalizeProductUrl('')).toBe('');
  });
});
