import { Component, OnInit, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { finalize } from 'rxjs';
import { GiftService } from '../../core/services/gift.service';
import { ImageService } from '../../core/services/image.service';
import { GiftPriority } from '../../models/gift.model';
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
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  readonly form = this.fb.nonNullable.group({
    name: ['', [Validators.required, Validators.maxLength(120)]],
    description: [''],
    notes: [''],
    imageUrl: [''],
    productUrl: [''],
    priority: ['Medium' as GiftPriority, Validators.required]
  });

  giftId: number | null = null;
  loading = false;
  uploadingPhoto = false;
  errorMessage: string | null = null;

  get isEditMode(): boolean {
    return this.giftId !== null;
  }

  get previewUrl(): string | null {
    return this.imageService.resolveUrl(this.form.controls.imageUrl.value);
  }

  ngOnInit(): void {
    const idParam = this.route.snapshot.paramMap.get('id');
    if (!idParam) {
      return;
    }

    this.giftId = Number(idParam);
    this.loading = true;
    this.giftService.getById(this.giftId).subscribe({
      next: (gift) => {
        this.form.patchValue({
          name: gift.name,
          description: gift.description ?? '',
          notes: gift.notes ?? '',
          imageUrl: gift.imageUrl ?? '',
          productUrl: gift.productUrl ?? '',
          priority: gift.priority
        });
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

  onPhotoSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (!file) {
      return;
    }

    this.uploadingPhoto = true;
    this.imageService.upload(file).subscribe({
      next: (result) => {
        this.form.controls.imageUrl.setValue(result.imageUrl);
        this.uploadingPhoto = false;
      },
      error: (error) => {
        this.errorMessage = getApiErrorMessage(error, "Non è stato possibile caricare la foto.");
        this.uploadingPhoto = false;
      }
    });
  }

  submit(): void {
    if (this.form.invalid || this.loading) {
      this.form.markAllAsTouched();
      return;
    }

    const payload = this.form.getRawValue();
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
