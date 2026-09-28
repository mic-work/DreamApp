import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { UserSummary } from '../../models/user.model';
import { WishlistDetail, WishlistSummary } from '../../models/wishlist.model';

@Injectable({ providedIn: 'root' })
export class WishlistService {
  constructor(private readonly http: HttpClient) {}

  getMyWishlist(): Observable<WishlistDetail> {
    return this.http.get<WishlistDetail>(`${environment.apiUrl}/wishlists/me`);
  }

  getByUsername(username: string): Observable<WishlistDetail> {
    return this.http.get<WishlistDetail>(`${environment.apiUrl}/wishlists/${encodeURIComponent(username)}`);
  }

  /** Wishlists of friends the current user has been granted access to. */
  getFriendsWishlists(): Observable<WishlistSummary[]> {
    return this.http.get<WishlistSummary[]>(`${environment.apiUrl}/wishlists/friends`);
  }

  /** Friends the caller has added (whose wishlists the caller can see) — used to populate "chi può vedere/deve regalarmi questo desiderio?". */
  getMyFriends(): Observable<UserSummary[]> {
    return this.http.get<UserSummary[]>(`${environment.apiUrl}/wishlists/me/friends`);
  }

  /** Friends who can see a given (possibly someone else's) wishlist — used to populate the "chi l'ha regalato" picker. */
  getViewersOf(wishlistId: string): Observable<UserSummary[]> {
    return this.http.get<UserSummary[]>(`${environment.apiUrl}/wishlists/${wishlistId}/viewers`);
  }

  grantAccessToSelf(wishlistId: string): Observable<void> {
    return this.http.post<void>(`${environment.apiUrl}/wishlists/${wishlistId}/access`, {});
  }

  revokeAccessToSelf(wishlistId: string): Observable<void> {
    return this.http.delete<void>(`${environment.apiUrl}/wishlists/${wishlistId}/access`);
  }
}
