import { Component, OnInit, inject } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { GiftService } from '../../core/services/gift.service';
import { ImageService } from '../../core/services/image.service';
import { Gift } from '../../models/gift.model';
import { getApiErrorMessage } from '../../shared/api-error.util';
import { ConfirmDialogComponent } from '../../shared/components/confirm-dialog/confirm-dialog.component';
import { IconComponent } from '../../shared/components/icon/icon.component';
import { PageHeaderComponent } from '../../shared/components/page-header/page-header.component';
import { PriorityBadgeComponent } from '../../shared/components/priority-badge/priority-badge.component';

/**
 * Full detail of a single gift — name, photo, description, notes, product
 * link. Reached by tapping a row in "my wishlist" or a friend's wishlist
 * (GiftCardComponent), which now show only a compact name+priority row.
 * Edit/delete are only shown when the gift is the caller's own
 * (gift.isOwner, computed server-side — never trusted from elsewhere).
 */
@Component({
  selector: 'app-gift-detail',
  standalone: true,
  imports: [IconComponent, PageHeaderComponent, PriorityBadgeComponent, ConfirmDialogComponent],
  templateUrl: './gift-detail.component.html',
  styleUrl: './gift-detail.component.scss'
})
export class GiftDetailComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly giftService = inject(GiftService);
  private readonly imageService = inject(ImageService);

  gift: Gift | null = null;
  loading = true;
  errorMessage: string | null = null;
  confirmingDeletion = false;

  ngOnInit(): void {
    const id = Number(this.route.snapshot.paramMap.get('id'));
    this.giftService.getById(id).subscribe({
      next: (gift) => {
        this.gift = gift;
        this.loading = false;
      },
      error: (error) => {
        this.errorMessage =
          error?.status === 403
            ? 'Non hai accesso a questo desiderio.'
            : getApiErrorMessage(error, 'Non è stato possibile caricare questo desiderio.');
        this.loading = false;
      }
    });
  }

  get imageUrl(): string | null {
    return this.gift ? this.imageService.resolveUrl(this.gift.imageUrl) : null;
  }

  edit(): void {
    if (this.gift) {
      this.router.navigate(['/gifts', this.gift.id, 'edit']);
    }
  }

  askDelete(): void {
    this.confirmingDeletion = true;
  }

  cancelDelete(): void {
    this.confirmingDeletion = false;
  }

  confirmDelete(): void {
    if (!this.gift) {
      return;
    }

    this.giftService.delete(this.gift.id).subscribe({
      next: () => this.router.navigateByUrl('/wishlist/me'),
      error: (error) => {
        this.confirmingDeletion = false;
        this.errorMessage = getApiErrorMessage(error, 'Non è stato possibile eliminare il regalo.');
      }
    });
  }
}
