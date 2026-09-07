import { Component, OnInit } from '@angular/core';
import { RouterLink } from '@angular/router';
import { WishlistService } from '../../core/services/wishlist.service';
import { Gift } from '../../models/gift.model';
import { GiftCardComponent } from '../../shared/components/gift-card/gift-card.component';
import { IconComponent } from '../../shared/components/icon/icon.component';

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
}
