import { Component, OnInit, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { finalize } from 'rxjs';
import { GiftService } from '../../core/services/gift.service';
import { ImageService } from '../../core/services/image.service';
import { WishlistService } from '../../core/services/wishlist.service';
import { GiftPriority } from '../../models/gift.model';
import { UserSummary } from '../../models/user.model';
import { getApiErrorMessage } from '../../shared/api-error.util';
import { IconComponent } from '../../shared/components/icon/icon.component';
import { PageHeaderComponent } from '../../shared/components/page-header/page-header.component';

@Component({
  selector: 'app-add-gift',
  standalone: true,
  imports: [ReactiveFormsModule, IconComponent, PageHeaderComponent],
  templateUrl: './add-gift.component.html',
  styleUrl: './add-gift.component.scss'
})
export class AddGiftComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly giftService = inject(GiftService);
  private readonly imageService = inject(ImageService);
  private readonly wishlistService = inject(WishlistService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  readonly form = this.fb.nonNullable.group({
    name: ['', [Validators.required, Validators.maxLength(120)]],
    description: [''],
    notes: [''],
    productUrl: [''],
    priority: ['Medium' as GiftPriority, Validators.required]
  });

  giftId: string | null = null;
  loading = false;
  uploadingPhoto = false;
  errorMessage: string | null = null;

  /** Already-uploaded photo URLs, in display order. */
  photos: string[] = [];

  /** Friends the current user has added (whose wishlists they can see) — the pool available for the visibility filter. */
  friends: UserSummary[] = [];
  /**
   * true (default) = every friend can see the gift, including friends added
   * later; false = only the friends in `selectedFriendIds`, and none selected
   * means nobody but the owner.
   */
  shareWithAll = true;
  selectedFriendIds = new Set<string>();

  /** Dropdown with the friend picker (only shown while "tutti" is off). */
  friendPickerOpen = false;
  friendSearch = '';

  get isEditMode(): boolean {
    return this.giftId !== null;
  }

  photoPreviewUrl(url: string): string | null {
    return this.imageService.resolveUrl(url);
  }

  ngOnInit(): void {
    this.wishlistService.getMyFriends().subscribe({
      next: (friends) => (this.friends = friends),
      // Non-blocking: worst case the friend list stays empty, the "tutti i miei amici" choice still works.
      error: () => {}
    });

    const idParam = this.route.snapshot.paramMap.get('id');
    if (!idParam) {
      return;
    }

    this.giftId = idParam;
    this.loading = true;
    this.giftService.getById(this.giftId).subscribe({
      next: (gift) => {
        this.form.patchValue({
          name: gift.name,
          description: gift.description ?? '',
          notes: gift.notes ?? '',
          productUrl: gift.productUrl ?? '',
          priority: gift.priority
        });
        this.photos = [...gift.photos];
        this.shareWithAll = gift.visibleToAll;
        this.selectedFriendIds = new Set(gift.visibleToAll ? [] : gift.visibleToUserIds);
        this.loading = false;
      },
      error: () => {
        this.errorMessage = 'Non è stato possibile caricare questo regalo.';
        this.loading = false;
      }
    });
  }

  selectPriority(priority: GiftPriority): void {
    this.form.controls.priority.setValue(priority);
  }

  onPhotosSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const files = input.files ? Array.from(input.files) : [];
    if (files.length === 0) {
      return;
    }

    this.uploadingPhoto = true;
    this.errorMessage = null;

    let remaining = files.length;
    files.forEach((file) => {
      this.imageService.upload(file).subscribe({
        next: (result) => this.photos.push(result.imageUrl),
        error: (error) => {
          this.errorMessage = getApiErrorMessage(error, 'Non è stato possibile caricare una foto.');
        },
        complete: () => {
          remaining -= 1;
          if (remaining === 0) {
            this.uploadingPhoto = false;
          }
        }
      });
    });

    input.value = '';
  }

  removePhoto(index: number): void {
    this.photos.splice(index, 1);
  }

  /**
   * "Tutti i miei amici": on = everyone (also future friends); off = nobody,
   * until some friends are picked from the dropdown (opened right away).
   */
  toggleShareWithAll(): void {
    this.shareWithAll = !this.shareWithAll;
    this.selectedFriendIds.clear();
    this.friendSearch = '';
    this.friendPickerOpen = !this.shareWithAll;
  }

  toggleFriendPicker(): void {
    this.friendPickerOpen = !this.friendPickerOpen;
    if (!this.friendPickerOpen) {
      this.friendSearch = '';
    }
  }

  onFriendSearch(event: Event): void {
    this.friendSearch = (event.target as HTMLInputElement).value;
  }

  toggleFriend(friendId: string): void {
    if (this.selectedFriendIds.has(friendId)) {
      this.selectedFriendIds.delete(friendId);
    } else {
      this.selectedFriendIds.add(friendId);
    }
  }

  isFriendSelected(friendId: string): boolean {
    return this.selectedFriendIds.has(friendId);
  }

  /** Friends matching the search box (case-insensitive, partial match). */
  get filteredFriends(): UserSummary[] {
    const query = this.friendSearch.trim().toLowerCase();
    return query ? this.friends.filter((f) => f.username.toLowerCase().includes(query)) : this.friends;
  }

  /** Text shown on the dropdown button: who is picked so far. */
  get selectionLabel(): string {
    const count = this.selectedFriendIds.size;
    if (count === 0) {
      return 'Seleziona gli amici';
    }
    const names = this.friends.filter((f) => this.selectedFriendIds.has(f.id)).map((f) => f.username);
    if (count <= 2 && names.length === count) {
      return names.join(', ');
    }
    return count === 1 ? '1 amico selezionato' : `${count} amici selezionati`;
  }

  /** True when nobody but the owner will see the gift (shown as a warning hint). */
  get visibleToNobody(): boolean {
    return !this.shareWithAll && this.selectedFriendIds.size === 0;
  }

  get visibilitySummary(): string {
    if (this.shareWithAll) {
      return 'Tutti i tuoi amici vedranno questo desiderio, anche quelli che aggiungerai in futuro.';
    }
    if (this.visibleToNobody) {
      return 'Nessun amico selezionato: solo tu vedrai questo desiderio.';
    }
    const count = this.selectedFriendIds.size;
    if (this.friends.length > 0 && count === this.friends.length) {
      return 'Tutti gli amici di oggi lo vedranno, ma non quelli che aggiungerai in futuro.';
    }
    return count === 1 ? 'Solo 1 amico vedrà questo desiderio.' : `Solo ${count} amici vedranno questo desiderio.`;
  }

  submit(): void {
    if (this.form.invalid || this.loading || this.uploadingPhoto) {
      this.form.markAllAsTouched();
      return;
    }

    const { name, description, notes, productUrl, priority } = this.form.getRawValue();
    const payload = {
      name,
      description,
      notes,
      productUrl,
      priority,
      imageUrls: this.photos,
      visibleToAll: this.shareWithAll,
      visibleToUserIds: this.shareWithAll ? [] : Array.from(this.selectedFriendIds)
    };

    this.loading = true;
    this.errorMessage = null;

    const request$ = this.isEditMode
      ? this.giftService.update(this.giftId!, payload)
      : this.giftService.create(payload);

    request$.pipe(finalize(() => (this.loading = false))).subscribe({
      next: () => this.router.navigateByUrl('/wishlist/me'),
      error: (error) => {
        this.errorMessage = getApiErrorMessage(error, 'Non è stato possibile salvare il regalo.');
      }
    });
  }
}
