import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { environment } from '../../../../environments/environment';
import { ChangePasswordDialogComponent } from './change-password-dialog.component';

describe('ChangePasswordDialogComponent', () => {
  let fixture: ComponentFixture<ChangePasswordDialogComponent>;
  let component: ChangePasswordDialogComponent;
  let httpMock: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ChangePasswordDialogComponent],
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])]
    }).compileComponents();

    fixture = TestBed.createComponent(ChangePasswordDialogComponent);
    component = fixture.componentInstance;
    httpMock = TestBed.inject(HttpTestingController);
    fixture.componentRef.setInput('open', true);
    fixture.detectChanges();
  });

  afterEach(() => httpMock.verify());

  function fill(current: string, next: string, confirm: string): void {
    component.form.setValue({ currentPassword: current, newPassword: next, confirmNewPassword: confirm });
  }

  it('shows the popup with the three password fields', () => {
    const el = fixture.nativeElement as HTMLElement;
    expect(el.textContent).toContain('Cambia password');
    expect(el.querySelectorAll('input[type="password"]').length).toBe(3);
  });

  it('does not call the API while the form is invalid (short or mismatching new password)', () => {
    fill('password123', '123', '123');
    component.submit();
    fill('password123', 'newpassword456', 'different456');
    component.submit();

    httpMock.expectNone(`${environment.apiUrl}/auth/change-password`);
    expect(component.form.hasError('mismatch')).toBeTrue();
  });

  it('sends the change and emits "changed" on success', () => {
    const changed = jasmine.createSpy('changed');
    component.changed.subscribe(changed);
    fill('password123', 'newpassword456', 'newpassword456');

    component.submit();
    const request = httpMock.expectOne(`${environment.apiUrl}/auth/change-password`);
    expect(request.request.body).toEqual({
      currentPassword: 'password123',
      newPassword: 'newpassword456',
      confirmNewPassword: 'newpassword456'
    });
    request.flush(null, { status: 204, statusText: 'No Content' });

    expect(changed).toHaveBeenCalled();
  });

  it('shows the server message (e.g. wrong current password) and stays open', () => {
    const changed = jasmine.createSpy('changed');
    component.changed.subscribe(changed);
    fill('wrong', 'newpassword456', 'newpassword456');

    component.submit();
    httpMock
      .expectOne(`${environment.apiUrl}/auth/change-password`)
      .flush({ message: 'La password attuale non è corretta.' }, { status: 400, statusText: 'Bad Request' });
    fixture.detectChanges();

    expect(changed).not.toHaveBeenCalled();
    expect((fixture.nativeElement as HTMLElement).textContent).toContain('La password attuale non è corretta.');
  });

  it('starts from an empty form each time it is reopened', () => {
    fill('password123', 'newpassword456', 'newpassword456');
    fixture.componentRef.setInput('open', false);
    fixture.detectChanges();
    fixture.componentRef.setInput('open', true);
    fixture.detectChanges();

    expect(component.form.getRawValue()).toEqual({ currentPassword: '', newPassword: '', confirmNewPassword: '' });
  });
});
