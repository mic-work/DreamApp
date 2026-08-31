import { Component, OnInit } from '@angular/core';
import { RouterLink } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';
import { WishlistService } from '../../core/services/wishlist.service';
import { WishlistSummary } from '../../models/wishlist.model';

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [RouterLink],
  templateUrl: './home.component.html',
  styleUrl: './home.component.scss'
})
export class HomeComponent implements OnInit {
  myGiftsCount = 0;
  friendsWishlists: WishlistSummary[] = [];
  loading = true;

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
}
