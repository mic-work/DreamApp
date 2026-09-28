import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface UploadResult {
  imageUrl: string;
}

@Injectable({ providedIn: 'root' })
export class ImageService {
  constructor(private readonly http: HttpClient) {}

  /** Uploads a gift photo and returns its URL, to collect into a gift's `photos` gallery. */
  upload(file: File): Observable<UploadResult> {
    const formData = new FormData();
    formData.append('file', file);
    return this.http.post<UploadResult>(`${environment.apiUrl}/images`, formData);
  }

  /** Resolves a (possibly relative) ImageUrl coming from the API against the backend origin. */
  resolveUrl(imageUrl: string | null | undefined): string | null {
    if (!imageUrl) {
      return null;
    }
    if (imageUrl.startsWith('http://') || imageUrl.startsWith('https://')) {
      return imageUrl;
    }
    const apiOrigin = new URL(environment.apiUrl).origin;
    return `${apiOrigin}${imageUrl}`;
  }
}
