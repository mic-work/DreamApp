import { Component, OnInit, inject } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';
import { GiftService } from '../../core/services/gift.service';
import { ImageService } from '../../core/services/image.service';
import { WishlistService } from '../../core/services/wishlist.service';
import { Gift } from '../../models/gift.model';
import { UserSummary } from '../../models/user.model';
import { getApiErrorMessage } from '../../shared/api-error.util';
import { ConfirmDialogComponent } from '../../shared/components/confirm-dialog/confirm-dialog.component';
import { IconComponent } from '../../shared/components/icon/icon.component';
import { PageHeaderComponent } from '../../shared/components/page-header/page-header.component';
import { PriorityBadgeComponent } from '../../shared/components/priority-badge/priority-badge.component';

/**
 * Full detail of a single gift — name, photo gallery, description, notes,
 * product link. Reached by tapping a row in "my wishlist" or a friend's
 * wishlist (GiftCardComponent), which show only a compact name+priority row.
 * Edit/delete are only shown when the gift is the caller's own
 * (gift.isOwner, computed server-side — never trusted from elsewhere).
 * A "mark as received" control is always shown too, but the two roles work
 * differently (see GiftService.setReception / Models/GiftReception on the
 * backend): the OWNER picks who gave it from the people who can see their
 * wishlist, and that's definitive immediately (gift.receivedByMe /
 * receivedFromUsername). A FRIEND can only ever claim to have given it
 * themselves — one click, nothing to pick — and that claim stays private
 * and "pending" (gift.myClaimPending) until the owner corroborates it by
 * naming the same friend; only then does gift.receivedByMe flip to true
 * for that friend too, automatically.
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
  private readonly wishlistService = inject(WishlistService);
  private readonly authService = inject(AuthService);

  gift: Gift | null = null;
  loading = true;
  errorMessage: string | null = null;
  confirmingDeletion = false;

  viewers: UserSummary[] = [];
  editingReception = false;
  selectedGiverId: string | null = null;
  savingReception = false;

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id')!;
    this.giftService.getById(id).subscribe({
      next: (gift) => {
        this.gift = gift;
        this.loading = false;
        if (gift.isOwner) {
          // Only the owner needs the candidate list for "chi l'ha
          // regalato?" — a friend can only ever claim to have given it
          // themselves, so there's nothing to pick from on their side.
          this.loadViewers(gift.wishlistId);
        }
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

  get photoUrls(): string[] {
    if (!this.gift) {
      return [];
    }
    return this.gift.photos
      .map((photo) => this.imageService.resolveUrl(photo))
      .filter((url): url is string => !!url);
  }

  edit(): void {
    if (this.gift) {
      this.router.navigate(['/wishlist/me/gifts', this.gift.id, 'edit']);
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

  private loadViewers(wishlistId: string): void {
    this.wishlistService.getViewersOf(wishlistId).subscribe({
      next: (viewers) => {
        this.viewers = viewers;
        // Prefill with whoever is already confirmed, otherwise default to
        // the first friend who has privately claimed this gift (a hint,
        // never a confirmation on its own — see gift.claimedByUserIds) so
        // the owner usually just has to double-check and confirm.
        this.selectedGiverId = this.gift?.receivedFromUserId ?? this.gift?.claimedByUserIds[0] ?? null;
      },
      error: () => {}
    });
  }

  startMarkingReceived(): void {
    this.editingReception = true;
  }

  cancelMarkingReceived(): void {
    this.editingReception = false;
  }

  /** Owner flow: confirm the picked giver. Definitive — see class doc. */
  confirmReceived(): void {
    if (!this.gift || !this.selectedGiverId) {
      return;
    }

    this.savingReception = true;
    this.giftService.setReception(this.gift.id, { received: true, givenByUserId: this.selectedGiverId }).subscribe({
      next: (gift) => {
        this.gift = gift;
        this.editingReception = false;
        this.savingReception = false;
      },
      error: (error) => {
        this.errorMessage = getApiErrorMessage(error, 'Non è stato possibile salvare.');
        this.savingReception = false;
      }
    });
  }

  /**
   * Friend flow: one click, no picker — a friend can only ever claim to
   * have given the gift themselves. Stays private/pending until the owner
   * corroborates it (see class doc).
   */
  markGivenByMe(): void {
    if (!this.gift) {
      return;
    }

    this.savingReception = true;
    this.giftService.setReception(this.gift.id, { received: true }).subscribe({
      next: (gift) => {
        this.gift = gift;
        this.savingReception = false;
      },
      error: (error) => {
        this.errorMessage = getApiErrorMessage(error, 'Non è stato possibile salvare.');
        this.savingReception = false;
      }
    });
  }

  undoReceived(): void {
    if (!this.gift) {
      return;
    }

    this.savingReception = true;
    this.giftService.setReception(this.gift.id, { received: false }).subscribe({
      next: (gift) => {
        this.gift = gift;
        this.savingReception = false;
      },
      error: (error) => {
        this.errorMessage = getApiErrorMessage(error, 'Non è stato possibile aggiornare.');
        this.savingReception = false;
      }
    });
  }
}
