import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed, fakeAsync, flush, tick } from '@angular/core/testing';
import { environment } from '../../../../environments/environment';
import { AddFriendDialogComponent } from './add-friend-dialog.component';

const MICHELE = { id: '1', username: 'michele', wishlistId: '10' };
const GABRIELE = { id: '2', username: 'Gabriele', wishlistId: '20' };

describe('AddFriendDialogComponent', () => {
  let fixture: ComponentFixture<AddFriendDialogComponent>;
  let httpMock: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AddFriendDialogComponent],
      providers: [provideHttpClient(), provideHttpClientTesting()]
    }).compileComponents();

    fixture = TestBed.createComponent(AddFriendDialogComponent);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  function setOpen(value: boolean): void {
    fixture.componentRef.setInput('open', value);
    fixture.detectChanges();
  }

  /** Types into the search box and lets the debounce elapse, answering the request with `users`. */
  function search(text: string, users: unknown[]): void {
    fixture.componentInstance.onQueryChange(text);
    tick(300);
    httpMock.expectOne(`${environment.apiUrl}/users/search?q=${text}`).flush(users);
    fixture.detectChanges();
  }

  it('renders nothing while closed and the popup once opened', fakeAsync(() => {
    setOpen(false);
    expect(fixture.nativeElement.querySelector('.dialog')).toBeNull();

    setOpen(true);
    flush();
    expect(fixture.nativeElement.querySelector('.dialog')).not.toBeNull();
    expect(fixture.nativeElement.textContent).toContain('Aggiungi amico');
  }));

  it('lists every user whose name contains the typed text', fakeAsync(() => {
    setOpen(true);
    flush();

    search('ele', [GABRIELE, MICHELE]);

    const names = Array.from(fixture.nativeElement.querySelectorAll('.result-name')).map((el) =>
      (el as HTMLElement).textContent?.trim()
    );
    expect(names).toEqual(['Gabriele', 'michele']);
  }));

  it('waits for the user to pause typing and only sends the latest text', fakeAsync(() => {
    setOpen(true);
    flush();

    fixture.componentInstance.onQueryChange('m');
    tick(100);
    fixture.componentInstance.onQueryChange('mi');
    tick(100);
    fixture.componentInstance.onQueryChange('mic');
    tick(300);

    httpMock.expectOne(`${environment.apiUrl}/users/search?q=mic`).flush([MICHELE]);
    httpMock.expectNone(`${environment.apiUrl}/users/search?q=m`);
  }));

  it('says so when nothing matches', fakeAsync(() => {
    setOpen(true);
    flush();

    search('zzz', []);

    expect(fixture.nativeElement.textContent).toContain('Nessun utente trovato.');
  }));

  it('adds the chosen user\'s list and emits "added"', fakeAsync(() => {
    const added = jasmine.createSpy('added');
    fixture.componentInstance.added.subscribe(added);
    setOpen(true);
    flush();
    search('mic', [MICHELE]);

    (fixture.nativeElement.querySelector('.add-btn') as HTMLButtonElement).click();
    httpMock.expectOne(`${environment.apiUrl}/wishlists/10/access`).flush(null);

    expect(added).toHaveBeenCalled();
  }));

  it('marks users that were already added instead of offering the button again', fakeAsync(() => {
    fixture.componentRef.setInput('existingUsernames', ['Michele']);
    setOpen(true);
    flush();

    search('ele', [GABRIELE, MICHELE]);

    expect(fixture.nativeElement.querySelectorAll('.add-btn').length).toBe(1);
    expect(fixture.nativeElement.querySelector('.already-added')?.textContent).toContain('Già aggiunto');
  }));

  it('emits "closed" when the backdrop or the close button is clicked', fakeAsync(() => {
    const closed = jasmine.createSpy('closed');
    fixture.componentInstance.closed.subscribe(closed);
    setOpen(true);
    flush();

    (fixture.nativeElement.querySelector('.close-btn') as HTMLButtonElement).click();
    (fixture.nativeElement.querySelector('.overlay') as HTMLElement).click();
    expect(closed).toHaveBeenCalledTimes(2);
  }));

  it('starts from a clean search each time it is reopened', fakeAsync(() => {
    setOpen(true);
    flush();
    search('mic', [MICHELE]);

    setOpen(false);
    setOpen(true);
    flush();

    expect(fixture.componentInstance.username).toBe('');
    expect(fixture.componentInstance.results).toEqual([]);
  }));
});
