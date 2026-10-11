import { HttpErrorResponse } from '@angular/common/http';

/** Extracts a user-facing message from an API error response (ApiErrorDto { message } on the backend). */
export function getApiErrorMessage(error: unknown, fallback = 'Si è verificato un errore. Riprova.'): string {
  if (error instanceof HttpErrorResponse) {
    const body = error.error as { message?: string; errors?: Record<string, unknown> } | string | null;
    if (typeof body === 'string' && body.trim().length > 0) {
      return body;
    }
    if (body && typeof body === 'object') {
      if (typeof body.message === 'string') {
        return body.message;
      }
      // ASP.NET model-validation failures come back as { errors: { Field: ["message", ...] } }.
      const firstValidationMessage = Object.values(body.errors ?? {})
        .flatMap((messages) => (Array.isArray(messages) ? messages : []))
        .find((message): message is string => typeof message === 'string' && message.trim().length > 0);
      if (firstValidationMessage) {
        return firstValidationMessage;
      }
    }
    if (error.status === 0) {
      return 'Impossibile contattare il server. Controlla la connessione e riprova.';
    }
    if (error.status === 429) {
      return 'Troppi tentativi. Riprova tra qualche minuto.';
    }
  }
  return fallback;
}
