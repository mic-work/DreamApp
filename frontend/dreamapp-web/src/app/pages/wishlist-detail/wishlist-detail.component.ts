import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { switchMap } from 'rxjs';
import { WishlistService } from '../../core/services/wishlist.service';
import { Gift } from '../../models/gift.model';
import { WishlistDetail } from '../../models/wishlist.model';
import { getApiErrorMessage } from '../../shared/api-error.util';
import { GiftCardComponent } from '../../shared/components/gift-card/gift-card.component';
import { PageHeaderComponent } from '../../shared/components/page-header/page-header.component';

/**
 * A friend's wishlist, read-only. When it's not the caller's own list
 * (always true here — MyWishlistComponent handles /wishlist/me), gifts are
 * split into two tabs: still to give, and already given BY THE CALLER
 * (gift.receivedByMe — see GiftReception on the backend). The caller's own
 * claim alone never moves a gift here: it needs the OWNER to have
 * confirmed the caller as the giver. A gift the caller has claimed but
 * that isn't confirmed yet stays under "da regalare", flagged
 * gift.myClaimPending (rendered as a small badge by GiftCardComponent).
 */
@Component({
  selector: 'app-wishlist-detail',
  standalone: true,
  imports: [GiftCardComponent, PageHeaderComponent],
  templateUrl: './wishlist-detail.component.html',
  styleUrl: './wishlist-detail.component.scss'
})
export class WishlistDetailComponent implements OnInit {
  wishlist: WishlistDetail | null = null;
  loading = true;
  errorMessage: string | null = null;
  activeTab: 'pending' | 'given' = 'pending';

  constructor(
    private readonly route: ActivatedRoute,
    private readonly wishlistService: WishlistService
  ) {}

  ngOnInit(): void {
    this.route.paramMap
      .pipe(switchMap((params) => this.wishlistService.getByUsername(params.get('username')!)))
      .subscribe({
        next: (wishlist) => {
          this.wishlist = wishlist;
          this.loading = false;
        },
        error: (error) => {
          this.errorMessage =
            error?.status === 403
              ? 'Non hai accesso a questa wishlist.'
              : getApiErrorMessage(error, 'Non è stato possibile caricare questa wishlist.');
          this.loading = false;
        }
      });
  }

  /** Gifts the wishlist owner has not yet received (from the caller's point of view). Priority order is preserved from the API. */
  get pendingGifts(): Gift[] {
    return this.wishlist?.gifts.filter((gift) => !gift.receivedByMe) ?? [];
  }

  /** Gifts the CALLER has personally already given, most recently confirmed first (gift.receivedAt) — see class doc. */
  get givenGifts(): Gift[] {
    return (
      this.wishlist?.gifts
        .filter((gift) => gift.receivedByMe)
        .sort((a, b) => new Date(b.receivedAt ?? 0).getTime() - new Date(a.receivedAt ?? 0).getTime()) ?? []
    );
  }

  setTab(tab: 'pending' | 'given'): void {
    this.activeTab = tab;
  }
}
