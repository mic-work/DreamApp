import { Component } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { finalize } from 'rxjs';
import { UserService } from '../../core/services/user.service';
import { WishlistService } from '../../core/services/wishlist.service';
import { UserPublic } from '../../models/user.model';
import { getApiErrorMessage } from '../../shared/api-error.util';

@Component({
  selector: 'app-add-friend',
  standalone: true,
  imports: [FormsModule],
  templateUrl: './add-friend.component.html',
  styleUrl: './add-friend.component.scss'
})
export class AddFriendComponent {
  username = '';
  searching = false;
  adding = false;
  searched = false;
  foundUser: UserPublic | null = null;
  errorMessage: string | null = null;
  successMessage: string | null = null;
  notFoundMessage = 'Utente non trovato.';

  constructor(
    private readonly userService: UserService,
    private readonly wishlistService: WishlistService,
    private readonly router: Router
  ) {}

  search(): void {
    const trimmed = this.username.trim();
    if (!trimmed || this.searching) {
      return;
    }

    this.searching = true;
    this.searched = false;
    this.foundUser = null;
    this.errorMessage = null;
    this.successMessage = null;
    this.notFoundMessage = 'Utente non trovato.';

    this.userService
      .findByUsername(trimmed)
      .pipe(finalize(() => (this.searching = false)))
      .subscribe({
        next: (user) => {
          this.foundUser = user;
          this.searched = true;
        },
        error: (error) => {
          // A 400 here means "you searched your own username" — show that
          // specific message instead of a generic "not found".
          if (error?.status === 400) {
            this.notFoundMessage = getApiErrorMessage(error, this.notFoundMessage);
          }
          this.searched = true;
        }
      });
  }

  addList(): void {
    if (!this.foundUser || this.adding) {
      return;
    }

    this.adding = true;
    this.wishlistService
      .grantAccessToSelf(this.foundUser.wishlistId)
      .pipe(finalize(() => (this.adding = false)))
      .subscribe({
        next: () => this.router.navigateByUrl('/home'),
        error: (error) => (this.errorMessage = getApiErrorMessage(error, 'Non è stato possibile aggiungere questa lista.'))
      });
  }
}
