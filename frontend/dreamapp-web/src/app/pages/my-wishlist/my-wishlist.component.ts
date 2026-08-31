import { Component, OnInit } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { GiftService } from '../../core/services/gift.service';
import { WishlistService } from '../../core/services/wishlist.service';
import { Gift } from '../../models/gift.model';
import { ConfirmDialogComponent } from '../../shared/components/confirm-dialog/confirm-dialog.component';
import { GiftCardComponent } from '../../shared/components/gift-card/gift-card.component';

@Component({
  selector: 'app-my-wishlist',
  standalone: true,
  imports: [RouterLink, GiftCardComponent, ConfirmDialogComponent],
  templateUrl: './my-wishlist.component.html',
  styleUrl: './my-wishlist.component.scss'
})
export class MyWishlistComponent implements OnInit {
  gifts: Gift[] = [];
  loading = true;
  errorMessage: string | null = null;

  giftPendingDeletion: Gift | null = null;

  constructor(
    private readonly wishlistService: WishlistService,
    private readonly giftService: GiftService,
    private readonly router: Router
  ) {}

  ngOnInit(): void {
    this.load();
  }

  private load(): void {
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

  editGift(gift: Gift): void {
    this.router.navigate(['/gifts', gift.id, 'edit']);
  }

  askDelete(gift: Gift): void {
    this.giftPendingDeletion = gift;
  }

  cancelDelete(): void {
    this.giftPendingDeletion = null;
  }

  confirmDelete(): void {
    if (!this.giftPendingDeletion) {
      return;
    }
    const id = this.giftPendingDeletion.id;
    this.giftService.delete(id).subscribe({
      next: () => {
        this.gifts = this.gifts.filter((g) => g.id !== id);
        this.giftPendingDeletion = null;
      },
      error: () => {
        this.errorMessage = 'Non è stato possibile eliminare il regalo.';
        this.giftPendingDeletion = null;
      }
    });
  }
}
