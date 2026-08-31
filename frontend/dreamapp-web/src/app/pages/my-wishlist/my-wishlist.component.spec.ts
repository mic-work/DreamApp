import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { environment } from '../../../environments/environment';
import { WishlistDetail } from '../../models/wishlist.model';
import { MyWishlistComponent } from './my-wishlist.component';

const SAMPLE_WISHLIST: WishlistDetail = {
  id: 1,
  ownerId: 1,
  ownerUsername: 'michele',
  name: 'La mia lista',
  createdAt: new Date().toISOString(),
  isOwner: true,
  gifts: [
    {
      id: 10,
      wishlistId: 1,
      name: 'AirPods',
      description: 'Colore nero',
      notes: null,
      imageUrl: null,
      productUrl: null,
      priority: 'High',
      createdAt: new Date().toISOString(),
      updatedAt: new Date().toISOString()
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

  it('renders the gifts returned by the API, with edit/delete actions', () => {
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).toContain('AirPods');
    expect(compiled.querySelector('app-gift-card')).not.toBeNull();
  });

  it('asks for confirmation before deleting a gift', () => {
    const component = fixture.componentInstance;
    component.askDelete(SAMPLE_WISHLIST.gifts[0]);
    fixture.detectChanges();

    expect(component.giftPendingDeletion?.id).toBe(10);
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).toContain('Sei sicuro di voler eliminare questo desiderio?');
  });
});
