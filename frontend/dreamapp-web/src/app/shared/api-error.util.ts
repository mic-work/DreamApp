import { HttpErrorResponse } from '@angular/common/http';

/** Extracts a user-facing message from an API error response (ApiErrorDto { message } on the backend). */
export function getApiErrorMessage(error: unknown, fallback = 'Si è verificato un errore. Riprova.'): string {
  if (error instanceof HttpErrorResponse) {
    const body = error.error as { message?: string } | string | null;
    if (typeof body === 'string' && body.trim().length > 0) {
      return body;
    }
    if (body && typeof body === 'object' && typeof body.message === 'string') {
      return body.message;
    }
    if (error.status === 0) {
      return 'Impossibile contattare il server. Controlla la connessione e riprova.';
    }
  }
  return fallback;
}
