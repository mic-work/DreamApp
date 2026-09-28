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
  /** false = visible to every friend (default); true = only to the ones selected below. */
  restrictVisibility = false;
  selectedFriendIds = new Set<string>();

  get isEditMode(): boolean {
    return this.giftId !== null;
  }

  photoPreviewUrl(url: string): string | null {
    return this.imageService.resolveUrl(url);
  }

  ngOnInit(): void {
    this.wishlistService.getMyFriends().subscribe({
      next: (friends) => (this.friends = friends),
      // Non-blocking: worst case the visibility filter section stays hidden, the gift still saves as "visible to everyone".
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
        this.restrictVisibility = gift.visibleToUserIds.length > 0;
        this.selectedFriendIds = new Set(gift.visibleToUserIds);
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

  setRestrictVisibility(restrict: boolean): void {
    this.restrictVisibility = restrict;
    if (!restrict) {
      this.selectedFriendIds.clear();
    }
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
      visibleToUserIds: this.restrictVisibility ? Array.from(this.selectedFriendIds) : []
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
