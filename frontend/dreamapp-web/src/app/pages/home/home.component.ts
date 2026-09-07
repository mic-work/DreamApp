import { Component, HostListener, OnInit } from '@angular/core';
import { RouterLink } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';
import { WishlistService } from '../../core/services/wishlist.service';
import { WishlistSummary } from '../../models/wishlist.model';
import { ConfirmDialogComponent } from '../../shared/components/confirm-dialog/confirm-dialog.component';
import { IconComponent } from '../../shared/components/icon/icon.component';

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [RouterLink, IconComponent, ConfirmDialogComponent],
  templateUrl: './home.component.html',
  styleUrl: './home.component.scss'
})
export class HomeComponent implements OnInit {
  myGiftsCount = 0;
  friendsWishlists: WishlistSummary[] = [];
  loading = true;

  /** Id of the friend wishlist whose "⋮" menu is currently open (one at a time). */
  openMenuId: number | null = null;
  friendPendingRemoval: WishlistSummary | null = null;

  constructor(
    readonly authService: AuthService,
    private readonly wishlistService: WishlistService
  ) {}

  ngOnInit(): void {
    this.wishlistService.getMyWishlist().subscribe({
      next: (wishlist) => (this.myGiftsCount = wishlist.gifts.length),
      error: () => (this.myGiftsCount = 0)
    });

    this.wishlistService.getFriendsWishlists().subscribe({
      next: (wishlists) => {
        this.friendsWishlists = wishlists;
        this.loading = false;
      },
      error: () => (this.loading = false)
    });
  }

  /** Closes the open "⋮" menu when clicking anywhere outside it. */
  @HostListener('document:click')
  closeMenu(): void {
    this.openMenuId = null;
  }

  toggleMenu(wishlistId: number, event: Event): void {
    event.stopPropagation();
    this.openMenuId = this.openMenuId === wishlistId ? null : wishlistId;
  }

  askRemoveFriend(wishlist: WishlistSummary, event: Event): void {
    event.stopPropagation();
    this.openMenuId = null;
    this.friendPendingRemoval = wishlist;
  }

  cancelRemoveFriend(): void {
    this.friendPendingRemoval = null;
  }

  confirmRemoveFriend(): void {
    if (!this.friendPendingRemoval) {
      return;
    }

    const id = this.friendPendingRemoval.id;
    this.wishlistService.revokeAccessToSelf(id).subscribe({
      next: () => {
        this.friendsWishlists = this.friendsWishlists.filter((w) => w.id !== id);
        this.friendPendingRemoval = null;
      },
      error: () => {
        this.friendPendingRemoval = null;
      }
    });
  }
}
