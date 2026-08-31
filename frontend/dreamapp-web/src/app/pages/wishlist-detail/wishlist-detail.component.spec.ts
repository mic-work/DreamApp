import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { environment } from '../../../environments/environment';
import { WishlistDetail } from '../../models/wishlist.model';
import { WishlistDetailComponent } from './wishlist-detail.component';

const FRIEND_WISHLIST: WishlistDetail = {
  id: 2,
  ownerId: 2,
  ownerUsername: 'tatina',
  name: 'La mia lista',
  createdAt: new Date().toISOString(),
  isOwner: false,
  gifts: [
    {
      id: 20,
      wishlistId: 2,
      name: 'Profumo',
      description: 'Quello di Dior',
      notes: null,
      imageUrl: null,
      productUrl: null,
      priority: 'High',
      createdAt: new Date().toISOString(),
      updatedAt: new Date().toISOString()
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

  it('offers a "remove friend" action since this is not the caller\'s own wishlist', () => {
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('.remove-friend-btn')).not.toBeNull();
  });
});
