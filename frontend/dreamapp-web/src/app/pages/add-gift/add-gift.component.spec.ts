import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router, convertToParamMap, provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AddGiftComponent } from './add-gift.component';

describe('AddGiftComponent (create mode)', () => {
  let fixture: ComponentFixture<AddGiftComponent>;
  let component: AddGiftComponent;
  let httpMock: HttpTestingController;
  let router: Router;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AddGiftComponent],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        { provide: ActivatedRoute, useValue: { snapshot: { paramMap: convertToParamMap({}) }, paramMap: of(convertToParamMap({})) } }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(AddGiftComponent);
    component = fixture.componentInstance;
    httpMock = TestBed.inject(HttpTestingController);
    router = TestBed.inject(Router);
    fixture.detectChanges();

    // ngOnInit loads the friends the gift can be shared with.
    httpMock.expectOne(`${environment.apiUrl}/wishlists/me/friends`).flush([]);
  });

  afterEach(() => httpMock.verify());

  it('starts in create mode (no gift id)', () => {
    expect(component.isEditMode).toBeFalse();
  });

  it('creates the gift and redirects to /wishlist/me', () => {
    component.form.patchValue({ name: 'AirPods', priority: 'High' });
    const navigateSpy = spyOn(router, 'navigateByUrl');

    component.submit();

    const req = httpMock.expectOne(`${environment.apiUrl}/gifts`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body.name).toBe('AirPods');
    req.flush({
      id: 'gift-1',
      wishlistId: 'wishlist-1',
      name: 'AirPods',
      priority: 'High',
      createdAt: new Date().toISOString(),
      updatedAt: new Date().toISOString()
    });

    expect(navigateSpy).toHaveBeenCalledWith('/wishlist/me');
  });

  it('accepts a product link without https:// and sends it as a full address', () => {
    component.form.patchValue({ name: 'Zaino', productUrl: 'www.negozio.it/zaino' });
    expect(component.form.controls.productUrl.valid).toBeTrue();

    component.submit();

    const req = httpMock.expectOne(`${environment.apiUrl}/gifts`);
    expect(req.request.body.productUrl).toBe('https://www.negozio.it/zaino');
    req.flush({});
  });

  it('blocks the submit when the product link is not a web address', () => {
    component.form.patchValue({ name: 'Zaino', productUrl: 'non è un link' });

    component.submit();

    httpMock.expectNone(`${environment.apiUrl}/gifts`);
    expect(component.form.controls.productUrl.invalid).toBeTrue();
  });

  it('limits description and notes to what the server accepts', () => {
    component.form.patchValue({ name: 'Zaino', description: 'a'.repeat(501), notes: 'b'.repeat(1001) });
    expect(component.form.controls.description.invalid).toBeTrue();
    expect(component.form.controls.notes.invalid).toBeTrue();

    component.form.patchValue({ description: 'a'.repeat(500), notes: 'b'.repeat(1000) });
    expect(component.form.valid).toBeTrue();
  });

  it('lets the user save again after a photo upload fails', () => {
    const file = new File(['x'], 'foto.txt', { type: 'text/plain' });
    const input = document.createElement('input');
    Object.defineProperty(input, 'files', { value: [file] });

    component.onPhotosSelected({ target: input } as unknown as Event);
    expect(component.uploadingPhoto).toBeTrue();

    httpMock
      .expectOne(`${environment.apiUrl}/images`)
      .flush({ message: 'Formato immagine non supportato.' }, { status: 400, statusText: 'Bad Request' });

    expect(component.uploadingPhoto).toBeFalse();
    expect(component.errorMessage).toBe('Formato immagine non supportato.');
  });

  it('shares the gift with every friend by default', () => {
    component.form.patchValue({ name: 'AirPods' });

    component.submit();

    const req = httpMock.expectOne(`${environment.apiUrl}/gifts`);
    expect(req.request.body.visibleToAll).toBeTrue();
    expect(req.request.body.visibleToUserIds).toEqual([]);
    req.flush({});
  });

  it('sends visibleToAll = false and an empty list when "tutti" is switched off and nobody is picked', () => {
    component.form.patchValue({ name: 'Segreto' });

    component.toggleShareWithAll();
    expect(component.visibleToNobody).toBeTrue();
    component.submit();

    const req = httpMock.expectOne(`${environment.apiUrl}/gifts`);
    expect(req.request.body.visibleToAll).toBeFalse();
    expect(req.request.body.visibleToUserIds).toEqual([]);
    req.flush({});
  });

  it('sends only the picked friends', () => {
    component.friends = [
      { id: 'f1', username: 'bob' },
      { id: 'f2', username: 'carol' }
    ];
    component.form.patchValue({ name: 'Libro' });

    component.toggleShareWithAll(); // off
    component.toggleFriend('f1');
    expect(component.visibleToNobody).toBeFalse();
    component.submit();

    const req = httpMock.expectOne(`${environment.apiUrl}/gifts`);
    expect(req.request.body.visibleToAll).toBeFalse();
    expect(req.request.body.visibleToUserIds).toEqual(['f1']);
    req.flush({});
  });

  it('opens the friend dropdown when "tutti" is switched off and closes it when switched back on', () => {
    expect(component.friendPickerOpen).toBeFalse();

    component.toggleShareWithAll();
    expect(component.friendPickerOpen).toBeTrue();

    component.toggleShareWithAll();
    expect(component.shareWithAll).toBeTrue();
    expect(component.friendPickerOpen).toBeFalse();
  });

  it('keeps an explicit list even when every friend is ticked (future friends are not included)', () => {
    component.friends = [
      { id: 'f1', username: 'bob' },
      { id: 'f2', username: 'carol' }
    ];
    component.toggleShareWithAll(); // off

    component.toggleFriend('f1');
    component.toggleFriend('f2');

    expect(component.shareWithAll).toBeFalse();
    expect(component.visibilitySummary).toContain('non quelli che aggiungerai in futuro');
  });

  it('filters the friends with a partial, case-insensitive search', () => {
    component.friends = [
      { id: 'f1', username: 'Michele' },
      { id: 'f2', username: 'carol' },
      { id: 'f3', username: 'ele' }
    ];

    component.friendSearch = 'ELE';

    expect(component.filteredFriends.map((f) => f.id)).toEqual(['f1', 'f3']);
  });

  it('summarises the picked friends on the dropdown button', () => {
    component.friends = [
      { id: 'f1', username: 'bob' },
      { id: 'f2', username: 'carol' },
      { id: 'f3', username: 'dave' }
    ];
    component.toggleShareWithAll(); // off
    expect(component.selectionLabel).toBe('Seleziona gli amici');

    component.toggleFriend('f1');
    expect(component.selectionLabel).toBe('bob');
    component.toggleFriend('f2');
    expect(component.selectionLabel).toBe('bob, carol');
    component.toggleFriend('f3');
    expect(component.selectionLabel).toBe('3 amici selezionati');
  });

  it('does not submit when the name is missing', () => {
    expect(component.form.controls.name.invalid).toBeTrue();
    component.submit();
    httpMock.expectNone(`${environment.apiUrl}/gifts`);
  });
});
