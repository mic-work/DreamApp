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
      id: 1,
      wishlistId: 1,
      name: 'AirPods',
      priority: 'High',
      createdAt: new Date().toISOString(),
      updatedAt: new Date().toISOString()
    });

    expect(navigateSpy).toHaveBeenCalledWith('/wishlist/me');
  });

  it('does not submit when the name is missing', () => {
    expect(component.form.controls.name.invalid).toBeTrue();
    component.submit();
    httpMock.expectNone(`${environment.apiUrl}/gifts`);
  });
});
