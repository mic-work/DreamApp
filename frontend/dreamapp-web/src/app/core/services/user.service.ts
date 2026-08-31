import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { UserPublic } from '../../models/user.model';

@Injectable({ providedIn: 'root' })
export class UserService {
  constructor(private readonly http: HttpClient) {}

  /** Looks up a user by username, used by the "add friend" search. */
  findByUsername(username: string): Observable<UserPublic> {
    return this.http.get<UserPublic>(`${environment.apiUrl}/users/${encodeURIComponent(username)}`);
  }
}
