import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { environment } from '../../../environments/environment';
import { WishlistDetail } from '../../models/wishlist.model';
import { WishlistDetailComponent } from './wishlist-detail.component';

const FRIEND_WISHLIST: WishlistDetail = {
  id: '2',
  ownerId: '2',
  ownerUsername: 'tatina',
  name: 'La mia lista',
  createdAt: new Date().toISOString(),
  isOwner: false,
  gifts: [
    {
      id: '20',
      wishlistId: '2',
      name: 'Profumo',
      description: 'Quello di Dior',
      notes: null,
      imageUrl: null,
      productUrl: null,
      priority: 'High',
      createdAt: new Date().toISOString(),
      updatedAt: new Date().toISOString(),
      isOwner: false,
      photos: [],
      visibleToUserIds: [],
      receivedByMe: false,
      myClaimPending: false,
      claimedByUserIds: []
    },
    {
      id: '21',
      wishlistId: '2',
      name: 'Sciarpa',
      description: null,
      notes: null,
      imageUrl: null,
      productUrl: null,
      priority: 'Low',
      createdAt: new Date().toISOString(),
      updatedAt: new Date().toISOString(),
      isOwner: false,
      photos: [],
      visibleToUserIds: [],
      receivedByMe: true,
      receivedFromUserId: '5',
      receivedFromUsername: 'tatina-friend',
      receivedAt: '2024-06-02T10:00:00.000Z',
      myClaimPending: false,
      claimedByUserIds: []
    },
    {
      id: '23',
      wishlistId: '2',
      name: 'Cappello',
      description: null,
      notes: null,
      imageUrl: null,
      productUrl: null,
      priority: 'Medium',
      createdAt: new Date().toISOString(),
      updatedAt: new Date().toISOString(),
      isOwner: false,
      photos: [],
      visibleToUserIds: [],
      receivedByMe: true,
      receivedFromUserId: '5',
      receivedFromUsername: 'tatina-friend',
      receivedAt: '2024-01-01T10:00:00.000Z',
      myClaimPending: false,
      claimedByUserIds: []
    },
    {
      id: '22',
      wishlistId: '2',
      name: 'Guanti',
      description: null,
      notes: null,
      imageUrl: null,
      productUrl: null,
      priority: 'Low',
      createdAt: new Date().toISOString(),
      updatedAt: new Date().toISOString(),
      isOwner: false,
      photos: [],
      visibleToUserIds: [],
      receivedByMe: false,
      myClaimPending: true,
      claimedByUserIds: []
    }
  ]
};

describe('WishlistDetailComponent (friend, read-only)', () => {
  let fixture: ComponentFixture<WishlistDetailComponent>;
  let httpMock: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [WishlistDetailComponent],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        {
          provide: ActivatedRoute,
          useValue: { paramMap: of(convertToParamMap({ username: 'tatina' })) }
        }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(WishlistDetailComponent);
    httpMock = TestBed.inject(HttpTestingController);
    fixture.detectChanges();

    httpMock.expectOne(`${environment.apiUrl}/wishlists/tatina`).flush(FRIEND_WISHLIST);
    fixture.detectChanges();
  });

  afterEach(() => httpMock.verify());

  it('shows the friend gifts', () => {
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).toContain('Profumo');
    expect(compiled.textContent).toContain('tatina');
  });

  it('never shows edit/delete actions for a gift that is not the caller\'s own', () => {
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).not.toContain('MODIFICA');
    expect(compiled.textContent).not.toContain('ELIMINA');
  });

  it('shows a back button to return to Home (removing a friend now lives there, in the 3-dot menu)', () => {
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('.back-btn')).not.toBeNull();
  });

  it('splits gifts into two tabs based on receivedByMe, defaulting to "Da regalare"', () => {
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).toContain('Da regalare');
    expect(compiled.textContent).toContain('Già regalati');
    expect(compiled.textContent).toContain('Profumo');
    expect(compiled.textContent).not.toContain('Sciarpa');

    const givenTab = Array.from(compiled.querySelectorAll<HTMLButtonElement>('.tab-btn')).find((btn) =>
      btn.textContent?.includes('Già regalati')
    );
    givenTab?.click();
    fixture.detectChanges();

    expect(compiled.textContent).toContain('Sciarpa');
  });

  it('keeps a gift the caller has only claimed (not yet confirmed by the owner) under "Da regalare", with a pending hint', () => {
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).toContain('Guanti');
    expect(compiled.textContent).toContain('In attesa di conferma');
  });

  it('orders "Già regalati" by confirmation date, most recent first', () => {
    const compiled = fixture.nativeElement as HTMLElement;
    const givenTab = Array.from(compiled.querySelectorAll<HTMLButtonElement>('.tab-btn')).find((btn) =>
      btn.textContent?.includes('Già regalati')
    );
    givenTab?.click();
    fixture.detectChanges();

    const names = Array.from(compiled.querySelectorAll('a.gift-row h3')).map((el) => el.textContent?.trim());
    // Sciarpa was confirmed on 2024-06-02, Cappello on 2024-01-01: Sciarpa must come first.
    expect(names.indexOf('Sciarpa')).toBeLessThan(names.indexOf('Cappello'));
  });
});
