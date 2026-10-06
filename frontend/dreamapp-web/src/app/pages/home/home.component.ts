import { Component, HostListener, OnInit } from '@angular/core';
import { RouterLink } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';
import { WishlistService } from '../../core/services/wishlist.service';
import { WishlistSummary } from '../../models/wishlist.model';
import { AddFriendDialogComponent } from '../../shared/components/add-friend-dialog/add-friend-dialog.component';
import { ConfirmDialogComponent } from '../../shared/components/confirm-dialog/confirm-dialog.component';
import { IconComponent } from '../../shared/components/icon/icon.component';

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [RouterLink, IconComponent, ConfirmDialogComponent, AddFriendDialogComponent],
  templateUrl: './home.component.html',
  styleUrl: './home.component.scss'
})
export class HomeComponent implements OnInit {
  friendsWishlists: WishlistSummary[] = [];
  loading = true;

  /** Number of gifts on the caller's own wishlist ("Desideri" stat). */
  desideriCount = 0;
  /** Number of the caller's own gifts privately confirmed as received ("Ricevuti" stat). */
  ricevutiCount = 0;

  /** Id of the friend wishlist whose "⋮" menu is currently open (one at a time). */
  addFriendOpen = false;
  /** Usernames already added, so the add-friend popup can mark them instead of offering them again. */
  friendUsernames: string[] = [];
  openMenuId: string | null = null;
  friendPendingRemoval: WishlistSummary | null = null;

  constructor(
    readonly authService: AuthService,
    private readonly wishlistService: WishlistService
  ) {}

  ngOnInit(): void {
    this.loadFriends();

    // Feeds the "Desideri" / "Ricevuti" stat pills — kept separate from the
    // friends fetch above so a failure here never blocks the friends list.
    this.wishlistService.getMyWishlist().subscribe({
      next: (wishlist) => {
        this.desideriCount = wishlist.gifts.length;
        this.ricevutiCount = wishlist.gifts.filter((gift) => gift.receivedByMe).length;
      },
      error: () => undefined
    });
  }

  private loadFriends(): void {
    this.wishlistService.getFriendsWishlists().subscribe({
      next: (wishlists) => {
        this.friendsWishlists = wishlists;
        this.friendUsernames = wishlists.map((w) => w.ownerUsername);
        this.loading = false;
      },
      error: () => (this.loading = false)
    });
  }

  openAddFriend(): void {
    this.addFriendOpen = true;
  }

  closeAddFriend(): void {
    this.addFriendOpen = false;
  }

  /** A friend's list was just added from the popup: close it and refresh the list behind it. */
  onFriendAdded(): void {
    this.addFriendOpen = false;
    this.loadFriends();
  }

  /** Closes the open "⋮" menu when clicking anywhere outside it. */
  @HostListener('document:click')
  closeMenu(): void {
    this.openMenuId = null;
  }

  toggleMenu(wishlistId: string, event: Event): void {
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
        this.friendUsernames = this.friendsWishlists.map((w) => w.ownerUsername);
        this.friendPendingRemoval = null;
      },
      error: () => {
        this.friendPendingRemoval = null;
      }
    });
  }
}
