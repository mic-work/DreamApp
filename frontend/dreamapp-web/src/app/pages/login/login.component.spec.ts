import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { environment } from '../../../environments/environment';
import { LoginComponent } from './login.component';

describe('LoginComponent', () => {
  let fixture: ComponentFixture<LoginComponent>;
  let component: LoginComponent;
  let httpMock: HttpTestingController;
  let router: Router;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [LoginComponent],
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([{ path: 'home', children: [] }])]
    }).compileComponents();

    fixture = TestBed.createComponent(LoginComponent);
    component = fixture.componentInstance;
    httpMock = TestBed.inject(HttpTestingController);
    router = TestBed.inject(Router);
    fixture.detectChanges();
  });

  afterEach(() => httpMock.verify());

  it('does not call the API when the form is invalid', () => {
    expect(component.form.invalid).toBeTrue();
    component.submit();
    httpMock.expectNone(`${environment.apiUrl}/auth/login`);
  });

  it('logs in and navigates to /home on success', async () => {
    component.form.setValue({ username: 'michele', password: 'password123' });
    const navigateSpy = spyOn(router, 'navigateByUrl');

    component.submit();

    const req = httpMock.expectOne(`${environment.apiUrl}/auth/login`);
    req.flush({
      token: 'abc',
      expiresAt: new Date().toISOString(),
      user: { id: 1, username: 'michele', email: 'a@b.it', createdAt: '' }
    });

    expect(navigateSpy).toHaveBeenCalledWith('/home');
  });

  it('shows an error message when the credentials are rejected', () => {
    component.form.setValue({ username: 'michele', password: 'wrong' });

    component.submit();

    const req = httpMock.expectOne(`${environment.apiUrl}/auth/login`);
    req.flush({ message: 'Username o password non corretti.' }, { status: 401, statusText: 'Unauthorized' });

    expect(component.errorMessage).toBe('Username o password non corretti.');
  });
});
