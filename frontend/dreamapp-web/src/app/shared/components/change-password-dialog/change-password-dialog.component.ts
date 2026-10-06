import { Component, EventEmitter, HostListener, Input, Output, inject } from '@angular/core';
import { AbstractControl, FormBuilder, ReactiveFormsModule, ValidationErrors, Validators } from '@angular/forms';
import { finalize } from 'rxjs';
import { AuthService } from '../../../core/services/auth.service';
import { getApiErrorMessage } from '../../api-error.util';
import { IconComponent } from '../icon/icon.component';

function passwordsMatch(group: AbstractControl): ValidationErrors | null {
  const next = group.get('newPassword')?.value;
  const confirm = group.get('confirmNewPassword')?.value;
  return next === confirm ? null : { mismatch: true };
}

/**
 * "Cambia password" popup of the profile page: current password + new
 * password (twice). The backend re-checks the current password, so a wrong
 * one is reported here instead of logging the user out.
 */
@Component({
  selector: 'app-change-password-dialog',
  standalone: true,
  imports: [ReactiveFormsModule, IconComponent],
  templateUrl: './change-password-dialog.component.html',
  styleUrl: './change-password-dialog.component.scss'
})
export class ChangePasswordDialogComponent {
  private readonly fb = inject(FormBuilder);
  private readonly authService = inject(AuthService);
  private isOpen = false;

  /** Opening the popup always starts from an empty form. */
  @Input()
  get open(): boolean {
    return this.isOpen;
  }
  set open(value: boolean) {
    if (value && !this.isOpen) {
      this.form.reset();
      this.errorMessage = null;
      this.saving = false;
    }
    this.isOpen = value;
  }

  @Output() closed = new EventEmitter<void>();
  /** Emitted once the password has been changed. */
  @Output() changed = new EventEmitter<void>();

  readonly form = this.fb.nonNullable.group(
    {
      currentPassword: ['', Validators.required],
      newPassword: ['', [Validators.required, Validators.minLength(6), Validators.maxLength(128)]],
      confirmNewPassword: ['', Validators.required]
    },
    { validators: passwordsMatch }
  );

  saving = false;
  errorMessage: string | null = null;

  @HostListener('document:keydown.escape')
  onEscape(): void {
    if (this.isOpen && !this.saving) {
      this.closed.emit();
    }
  }

  submit(): void {
    if (this.form.invalid || this.saving) {
      this.form.markAllAsTouched();
      return;
    }

    this.saving = true;
    this.errorMessage = null;

    this.authService
      .changePassword(this.form.getRawValue())
      .pipe(finalize(() => (this.saving = false)))
      .subscribe({
        next: () => this.changed.emit(),
        error: (error) => {
          this.errorMessage = getApiErrorMessage(error, 'Non è stato possibile cambiare la password.');
        }
      });
  }
}
