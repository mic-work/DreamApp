import { Component, OnInit } from '@angular/core';
import { RouterLink } from '@angular/router';
import { WishlistService } from '../../core/services/wishlist.service';
import { Gift } from '../../models/gift.model';
import { GiftCardComponent } from '../../shared/components/gift-card/gift-card.component';
import { IconComponent } from '../../shared/components/icon/icon.component';

/**
 * The caller's own wishlist. Gifts are split into two tabs: still to
 * receive, and already received BY THE CALLER (gift.receivedByMe — here,
 * since every gift is the caller's own, it's the owner's definitive "I
 * already got this, from X" record — immediate, no one else's
 * corroboration needed; see GiftReception on the backend).
 */
@Component({
  selector: 'app-my-wishlist',
  standalone: true,
  imports: [RouterLink, GiftCardComponent, IconComponent],
  templateUrl: './my-wishlist.component.html',
  styleUrl: './my-wishlist.component.scss'
})
export class MyWishlistComponent implements OnInit {
  gifts: Gift[] = [];
  loading = true;
  errorMessage: string | null = null;
  activeTab: 'pending' | 'received' = 'pending';

  constructor(private readonly wishlistService: WishlistService) {}

  ngOnInit(): void {
    this.loading = true;
    this.wishlistService.getMyWishlist().subscribe({
      next: (wishlist) => {
        this.gifts = wishlist.gifts;
        this.loading = false;
      },
      error: () => {
        this.errorMessage = 'Non è stato possibile caricare la tua wishlist.';
        this.loading = false;
      }
    });
  }

  /** Gifts not yet marked as received by the caller. */
  get pendingGifts(): Gift[] {
    return this.gifts.filter((gift) => !gift.receivedByMe);
  }

  /** Gifts the caller has already received, most recently confirmed first (gift.receivedAt). */
  get receivedGifts(): Gift[] {
    return this.gifts
      .filter((gift) => gift.receivedByMe)
      .sort((a, b) => new Date(b.receivedAt ?? 0).getTime() - new Date(a.receivedAt ?? 0).getTime());
  }

  setTab(tab: 'pending' | 'received'): void {
    this.activeTab = tab;
  }
}
