import { HttpErrorResponse } from '@angular/common/http';
import { getApiErrorMessage } from './api-error.util';

describe('getApiErrorMessage', () => {
  it('uses the message of an ApiErrorDto', () => {
    const error = new HttpErrorResponse({ status: 400, error: { message: 'Username già in uso.' } });
    expect(getApiErrorMessage(error)).toBe('Username già in uso.');
  });

  it('uses the first model-validation message of an ASP.NET problem response', () => {
    const error = new HttpErrorResponse({
      status: 400,
      error: { title: 'One or more validation errors occurred.', errors: { ProductUrl: ['Il link al prodotto non è un URL valido.'] } }
    });
    expect(getApiErrorMessage(error, 'fallback')).toBe('Il link al prodotto non è un URL valido.');
  });

  it('explains a rate-limited request', () => {
    expect(getApiErrorMessage(new HttpErrorResponse({ status: 429 }))).toContain('Troppi tentativi');
  });

  it('reports an unreachable server', () => {
    expect(getApiErrorMessage(new HttpErrorResponse({ status: 0 }))).toContain('Impossibile contattare');
  });

  it('falls back when there is nothing useful in the response', () => {
    expect(getApiErrorMessage(new HttpErrorResponse({ status: 500, error: {} }), 'fallback')).toBe('fallback');
  });
});
