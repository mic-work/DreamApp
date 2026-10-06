import { DatePipe } from '@angular/common';
import { Component, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { finalize } from 'rxjs';
import { AuthService } from '../../core/services/auth.service';
import { UserService } from '../../core/services/user.service';
import { getApiErrorMessage } from '../../shared/api-error.util';
import { ChangePasswordDialogComponent } from '../../shared/components/change-password-dialog/change-password-dialog.component';
import { IconComponent } from '../../shared/components/icon/icon.component';

@Component({
  selector: 'app-profile',
  standalone: true,
  imports: [DatePipe, ReactiveFormsModule, IconComponent, ChangePasswordDialogComponent],
  templateUrl: './profile.component.html',
  styleUrl: './profile.component.scss'
})
export class ProfileComponent {
  private readonly fb = inject(FormBuilder);
  private readonly userService = inject(UserService);
  readonly authService = inject(AuthService);

  readonly form = this.fb.nonNullable.group({
    username: ['', [Validators.required, Validators.minLength(3), Validators.maxLength(32)]],
    email: ['', [Validators.required, Validators.email]]
  });

  editing = false;
  changePasswordOpen = false;
  saving = false;
  errorMessage: string | null = null;
  successMessage: string | null = null;

  startEditing(): void {
    const user = this.authService.currentUser();
    if (!user) {
      return;
    }

    this.form.setValue({ username: user.username, email: user.email });
    this.errorMessage = null;
    this.successMessage = null;
    this.editing = true;
  }

  cancelEditing(): void {
    this.editing = false;
    this.errorMessage = null;
  }

  save(): void {
    if (this.form.invalid || this.saving) {
      this.form.markAllAsTouched();
      return;
    }

    this.saving = true;
    this.errorMessage = null;

    this.userService
      .updateMe(this.form.getRawValue())
      .pipe(finalize(() => (this.saving = false)))
      .subscribe({
        next: (user) => {
          this.authService.updateStoredUser(user);
          this.editing = false;
          this.successMessage = 'Dati aggiornati con successo.';
        },
        error: (error) => {
          this.errorMessage = getApiErrorMessage(error, 'Non è stato possibile salvare le modifiche.');
        }
      });
  }

  openChangePassword(): void {
    this.successMessage = null;
    this.errorMessage = null;
    this.changePasswordOpen = true;
  }

  closeChangePassword(): void {
    this.changePasswordOpen = false;
  }

  onPasswordChanged(): void {
    this.changePasswordOpen = false;
    this.successMessage = 'Password aggiornata con successo.';
  }

  logout(): void {
    this.authService.logout();
  }
}
