import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { environment } from '../../../environments/environment';
import { AuthService } from './auth.service';

describe('AuthService', () => {
  let service: AuthService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])]
    });
    service = TestBed.inject(AuthService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
    localStorage.clear();
  });

  it('starts unauthenticated when localStorage is empty', () => {
    expect(service.isAuthenticated()).toBeFalse();
    expect(service.currentUser()).toBeNull();
  });

  it('login() stores the token/user and flips isAuthenticated to true', () => {
    service.login({ username: 'michele', password: 'password123' }).subscribe();

    const req = httpMock.expectOne(`${environment.apiUrl}/auth/login`);
    expect(req.request.method).toBe('POST');
    req.flush({
      token: 'abc.def.ghi',
      expiresAt: new Date().toISOString(),
      user: { id: 'user-1', username: 'michele', email: 'michele@example.com', createdAt: new Date().toISOString() }
    });

    expect(service.isAuthenticated()).toBeTrue();
    expect(service.currentUser()?.username).toBe('michele');
    expect(service.getToken()).toBe('abc.def.ghi');
  });

  it('logout() clears the session', () => {
    service.login({ username: 'michele', password: 'password123' }).subscribe();
    httpMock
      .expectOne(`${environment.apiUrl}/auth/login`)
      .flush({ token: 't', expiresAt: new Date().toISOString(), user: { id: 'user-1', username: 'michele', email: 'a@b.it', createdAt: '' } });

    service.logout();

    expect(service.isAuthenticated()).toBeFalse();
    expect(service.getToken()).toBeNull();
  });
});
