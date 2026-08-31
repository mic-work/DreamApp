import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
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

  grantAccessToSelf(wishlistId: number): Observable<void> {
    return this.http.post<void>(`${environment.apiUrl}/wishlists/${wishlistId}/access`, {});
  }

  revokeAccessToSelf(wishlistId: number): Observable<void> {
    return this.http.delete<void>(`${environment.apiUrl}/wishlists/${wishlistId}/access`);
  }
}
