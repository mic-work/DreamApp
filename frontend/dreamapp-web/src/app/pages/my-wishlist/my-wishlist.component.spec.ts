import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { environment } from '../../../environments/environment';
import { WishlistDetail } from '../../models/wishlist.model';
import { MyWishlistComponent } from './my-wishlist.component';

const SAMPLE_WISHLIST: WishlistDetail = {
  id: '1',
  ownerId: '1',
  ownerUsername: 'michele',
  name: 'La mia lista',
  createdAt: new Date().toISOString(),
  isOwner: true,
  gifts: [
    {
      id: '10',
      wishlistId: '1',
      name: 'AirPods',
      description: 'Colore nero',
      notes: null,
      imageUrl: null,
      productUrl: null,
      priority: 'High',
      createdAt: new Date().toISOString(),
      updatedAt: new Date().toISOString(),
      isOwner: true,
      photos: [],
      visibleToUserIds: [],
      receivedByMe: false,
      myClaimPending: false,
      claimedByUserIds: []
    },
    {
      id: '11',
      wishlistId: '1',
      name: 'Collana',
      description: null,
      notes: null,
      imageUrl: null,
      productUrl: null,
      priority: 'Medium',
      createdAt: new Date().toISOString(),
      updatedAt: new Date().toISOString(),
      isOwner: true,
      photos: [],
      visibleToUserIds: [],
      receivedByMe: true,
      receivedFromUserId: '3',
      receivedFromUsername: 'tatina',
      receivedAt: '2024-06-02T10:00:00.000Z',
      myClaimPending: false,
      claimedByUserIds: []
    },
    {
      id: '13',
      wishlistId: '1',
      name: 'Guanti',
      description: null,
      notes: null,
      imageUrl: null,
      productUrl: null,
      priority: 'High',
      createdAt: new Date().toISOString(),
      updatedAt: new Date().toISOString(),
      isOwner: true,
      photos: [],
      visibleToUserIds: [],
      receivedByMe: true,
      receivedFromUserId: '3',
      receivedFromUsername: 'tatina',
      receivedAt: '2024-01-01T10:00:00.000Z',
      myClaimPending: false,
      claimedByUserIds: []
    },
    {
      id: '12',
      wishlistId: '1',
      name: 'Sciarpa',
      description: null,
      notes: null,
      imageUrl: null,
      productUrl: null,
      priority: 'Low',
      createdAt: new Date().toISOString(),
      updatedAt: new Date().toISOString(),
      isOwner: true,
      photos: [],
      visibleToUserIds: [],
      receivedByMe: false,
      myClaimPending: false,
      claimedByUserIds: ['7']
    }
  ]
};

describe('MyWishlistComponent', () => {
  let fixture: ComponentFixture<MyWishlistComponent>;
  let httpMock: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [MyWishlistComponent],
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])]
    }).compileComponents();

    fixture = TestBed.createComponent(MyWishlistComponent);
    httpMock = TestBed.inject(HttpTestingController);
    fixture.detectChanges();

    httpMock.expectOne(`${environment.apiUrl}/wishlists/me`).flush(SAMPLE_WISHLIST);
    fixture.detectChanges();
  });

  afterEach(() => httpMock.verify());

  it('renders the gift names returned by the API as a compact list (no photo at a glance)', () => {
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).toContain('AirPods');
    expect(compiled.querySelector('app-gift-card')).not.toBeNull();
    expect(compiled.querySelector('img')).toBeNull();
  });

  it('links each gift row to its detail page, where the photo/description/actions live', () => {
    const compiled = fixture.nativeElement as HTMLElement;
    const link = compiled.querySelector('a.gift-row') as HTMLAnchorElement | null;
    expect(link?.getAttribute('href')).toBe('/wishlist/me/gifts/10');
  });

  it('splits gifts into two tabs based on receivedByMe, defaulting to "Da ricevere"', () => {
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).toContain('Da ricevere');
    expect(compiled.textContent).toContain('Ricevuti');
    expect(compiled.textContent).toContain('AirPods');
    expect(compiled.textContent).not.toContain('Collana');

    const receivedTab = Array.from(compiled.querySelectorAll<HTMLButtonElement>('.tab-btn')).find((btn) =>
      btn.textContent?.includes('Ricevuti')
    );
    receivedTab?.click();
    fixture.detectChanges();

    expect(compiled.textContent).toContain('Collana');
  });

  it('orders the "Ricevuti" tab by confirmation date, most recent first', () => {
    const compiled = fixture.nativeElement as HTMLElement;
    const receivedTab = Array.from(compiled.querySelectorAll<HTMLButtonElement>('.tab-btn')).find((btn) =>
      btn.textContent?.includes('Ricevuti')
    );
    receivedTab?.click();
    fixture.detectChanges();

    const names = Array.from(compiled.querySelectorAll('a.gift-row h3')).map((el) => el.textContent?.trim());
    // Collana was confirmed on 2024-06-02, Guanti on 2024-01-01: Collana must come first.
    expect(names.indexOf('Collana')).toBeLessThan(names.indexOf('Guanti'));
  });

  it('flags, on the owner\'s own list, a gift a friend has already claimed but not yet confirmed', () => {
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).toContain('Sciarpa');
    expect(compiled.textContent).toContain('Un amico ha segnalato questo regalo');

    // AirPods has no claim at all yet, so it must not get the badge.
    const airpodsRow = Array.from(compiled.querySelectorAll('a.gift-row')).find((row) =>
      row.textContent?.includes('AirPods')
    );
    expect(airpodsRow?.textContent).not.toContain('Un amico ha segnalato questo regalo');
  });
});
