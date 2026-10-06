import { Component, ElementRef, EventEmitter, HostListener, Input, OnDestroy, OnInit, Output, ViewChild } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Subject, Subscription, catchError, debounceTime, filter, map, of, switchMap, tap } from 'rxjs';
import { UserService } from '../../../core/services/user.service';
import { WishlistService } from '../../../core/services/wishlist.service';
import { UserPublic } from '../../../models/user.model';
import { getApiErrorMessage } from '../../api-error.util';
import { IconComponent } from '../icon/icon.component';

/**
 * "Aggiungi amico" popup: type part of a username ("mic", "ele"…) and pick
 * the user from the list of matches, then add their list. Lives on the Home
 * page instead of a separate route, so adding a friend never takes the user
 * away from their list of friends.
 */
@Component({
  selector: 'app-add-friend-dialog',
  standalone: true,
  imports: [FormsModule, IconComponent],
  templateUrl: './add-friend-dialog.component.html',
  styleUrl: './add-friend-dialog.component.scss'
})
export class AddFriendDialogComponent implements OnInit, OnDestroy {
  private isOpen = false;

  /** Opening the popup always starts from a clean search. */
  @Input()
  get open(): boolean {
    return this.isOpen;
  }
  set open(value: boolean) {
    if (value && !this.isOpen) {
      this.reset();
      // Wait for the @if to render the input before focusing it.
      setTimeout(() => this.usernameInput?.nativeElement.focus());
    }
    this.isOpen = value;
  }

  /** Usernames the caller has already added, shown as "Già aggiunto" instead of a button. */
  @Input() set existingUsernames(value: string[]) {
    this.existing = new Set((value ?? []).map((name) => name.toLowerCase()));
  }

  /** Emitted when the user dismisses the popup without adding anyone. */
  @Output() closed = new EventEmitter<void>();
  /** Emitted after a friend's list has been added successfully. */
  @Output() added = new EventEmitter<void>();

  @ViewChild('usernameInput') usernameInput?: ElementRef<HTMLInputElement>;

  username = '';
  searching = false;
  searched = false;
  results: UserPublic[] = [];
  addingId: string | null = null;
  errorMessage: string | null = null;

  private existing = new Set<string>();
  private readonly query$ = new Subject<string>();
  private subscription?: Subscription;

  constructor(
    private readonly userService: UserService,
    private readonly wishlistService: WishlistService
  ) {}

  ngOnInit(): void {
    // Search while typing, but only once the user pauses; a newer query cancels the one still in flight.
    this.subscription = this.query$
      .pipe(
        map((query) => query.trim()),
        debounceTime(300),
        tap((query) => {
          this.errorMessage = null;
          if (query) {
            this.searching = true;
          } else {
            this.results = [];
            this.searched = false;
            this.searching = false;
          }
        }),
        filter((query) => query.length > 0),
        switchMap((query) =>
          this.userService.searchUsers(query).pipe(
            catchError(() => {
              this.errorMessage = 'Non è stato possibile completare la ricerca.';
              return of([] as UserPublic[]);
            })
          )
        )
      )
      .subscribe((users) => {
        this.results = users;
        this.searched = true;
        this.searching = false;
      });
  }

  ngOnDestroy(): void {
    this.subscription?.unsubscribe();
  }

  @HostListener('document:keydown.escape')
  onEscape(): void {
    if (this.isOpen) {
      this.closed.emit();
    }
  }

  onQueryChange(value: string): void {
    this.username = value;
    this.query$.next(value);
  }

  isAlreadyAdded(user: UserPublic): boolean {
    return this.existing.has(user.username.toLowerCase());
  }

  addList(user: UserPublic): void {
    if (this.addingId || this.isAlreadyAdded(user)) {
      return;
    }

    this.addingId = user.id;
    this.errorMessage = null;
    this.wishlistService.grantAccessToSelf(user.wishlistId).subscribe({
      next: () => {
        this.addingId = null;
        this.added.emit();
      },
      error: (error) => {
        this.addingId = null;
        this.errorMessage = getApiErrorMessage(error, 'Non è stato possibile aggiungere questa lista.');
      }
    });
  }

  private reset(): void {
    this.username = '';
    this.searching = false;
    this.searched = false;
    this.results = [];
    this.addingId = null;
    this.errorMessage = null;
  }
}
