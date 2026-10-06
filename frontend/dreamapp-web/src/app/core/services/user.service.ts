import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { User, UserPublic } from '../../models/user.model';

@Injectable({ providedIn: 'root' })
export class UserService {
  constructor(private readonly http: HttpClient) {}

  /** Looks up a user by username, used by the "add friend" search. */
  findByUsername(username: string): Observable<UserPublic> {
    return this.http.get<UserPublic>(`${environment.apiUrl}/users/${encodeURIComponent(username)}`);
  }

  /**
   * Users whose username contains the given text (case-insensitive), for the
   * "add friend" popup. The caller is never part of the results.
   */
  searchUsers(query: string): Observable<UserPublic[]> {
    return this.http.get<UserPublic[]>(`${environment.apiUrl}/users/search`, { params: new HttpParams().set('q', query) });
  }

  /** Updates the personal data (username, email) of the currently authenticated user. */
  updateMe(payload: { username: string; email: string }): Observable<User> {
    return this.http.put<User>(`${environment.apiUrl}/users/me`, payload);
  }
}
