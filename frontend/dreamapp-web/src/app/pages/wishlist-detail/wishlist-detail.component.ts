import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { switchMap } from 'rxjs';
import { WishlistService } from '../../core/services/wishlist.service';
import { WishlistDetail } from '../../models/wishlist.model';
import { getApiErrorMessage } from '../../shared/api-error.util';
import { ConfirmDialogComponent } from '../../shared/components/confirm-dialog/confirm-dialog.component';
import { GiftCardComponent } from '../../shared/components/gift-card/gift-card.component';

@Component({
  selector: 'app-wishlist-detail',
  standalone: true,
  imports: [RouterLink, GiftCardComponent, ConfirmDialogComponent],
  templateUrl: './wishlist-detail.component.html',
  styleUrl: './wishlist-detail.component.scss'
})
export class WishlistDetailComponent implements OnInit {
  wishlist: WishlistDetail | null = null;
  loading = true;
  errorMessage: string | null = null;
  confirmingRemoval = false;

  constructor(
    private readonly route: ActivatedRoute,
    private readonly router: Router,
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

  removeFriend(): void {
    if (!this.wishlist) {
      return;
    }
    this.wishlistService.revokeAccessToSelf(this.wishlist.id).subscribe({
      next: () => this.router.navigateByUrl('/home'),
      error: (error) => (this.errorMessage = getApiErrorMessage(error))
    });
  }
}
