import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { Gift, GiftCreateRequest, GiftUpdateRequest } from '../../models/gift.model';

@Injectable({ providedIn: 'root' })
export class GiftService {
  constructor(private readonly http: HttpClient) {}

  getById(id: number): Observable<Gift> {
    return this.http.get<Gift>(`${environment.apiUrl}/gifts/${id}`);
  }

  create(request: GiftCreateRequest): Observable<Gift> {
    return this.http.post<Gift>(`${environment.apiUrl}/gifts`, request);
  }

  update(id: number, request: GiftUpdateRequest): Observable<Gift> {
    return this.http.put<Gift>(`${environment.apiUrl}/gifts/${id}`, request);
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${environment.apiUrl}/gifts/${id}`);
  }
}
