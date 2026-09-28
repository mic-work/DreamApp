import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { Gift, GiftCreateRequest, GiftReceptionRequest, GiftUpdateRequest } from '../../models/gift.model';

@Injectable({ providedIn: 'root' })
export class GiftService {
  constructor(private readonly http: HttpClient) {}

  getById(id: string): Observable<Gift> {
    return this.http.get<Gift>(`${environment.apiUrl}/gifts/${id}`);
  }

  create(request: GiftCreateRequest): Observable<Gift> {
    return this.http.post<Gift>(`${environment.apiUrl}/gifts`, request);
  }

  update(id: string, request: GiftUpdateRequest): Observable<Gift> {
    return this.http.put<Gift>(`${environment.apiUrl}/gifts/${id}`, request);
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${environment.apiUrl}/gifts/${id}`);
  }

  /** Privately records (for the caller only) whether this gift has already been received, and from whom. */
  setReception(id: string, request: GiftReceptionRequest): Observable<Gift> {
    return this.http.put<Gift>(`${environment.apiUrl}/gifts/${id}/reception`, request);
  }
}
