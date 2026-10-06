import { ComponentFixture, TestBed } from '@angular/core/testing';
import { InfoDialogComponent } from './info-dialog.component';

describe('InfoDialogComponent', () => {
  let fixture: ComponentFixture<InfoDialogComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({ imports: [InfoDialogComponent] }).compileComponents();
    fixture = TestBed.createComponent(InfoDialogComponent);
    fixture.componentRef.setInput('title', 'Password dimenticata?');
    fixture.componentRef.setInput('message', 'Contatta Trolese Michele.');
  });

  it('renders nothing while closed', () => {
    fixture.componentRef.setInput('open', false);
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('.dialog')).toBeNull();
  });

  it('shows title and message when open, and emits "closed" from the button, the X and the backdrop', () => {
    const closed = jasmine.createSpy('closed');
    fixture.componentInstance.closed.subscribe(closed);
    fixture.componentRef.setInput('open', true);
    fixture.detectChanges();

    const el = fixture.nativeElement as HTMLElement;
    expect(el.textContent).toContain('Password dimenticata?');
    expect(el.textContent).toContain('Contatta Trolese Michele.');

    (el.querySelector('.btn') as HTMLButtonElement).click();
    (el.querySelector('.close-btn') as HTMLButtonElement).click();
    (el.querySelector('.overlay') as HTMLElement).click();
    expect(closed).toHaveBeenCalledTimes(3);
  });
});
