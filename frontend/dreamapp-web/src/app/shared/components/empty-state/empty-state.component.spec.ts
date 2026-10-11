import { ComponentFixture, TestBed } from '@angular/core/testing';
import { EmptyStateComponent } from './empty-state.component';

describe('EmptyStateComponent', () => {
  let fixture: ComponentFixture<EmptyStateComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({ imports: [EmptyStateComponent] }).compileComponents();
    fixture = TestBed.createComponent(EmptyStateComponent);
  });

  it('shows the title and the hint', () => {
    fixture.componentRef.setInput('title', 'Nessun desiderio');
    fixture.componentRef.setInput('text', 'Inizia subito!');
    fixture.detectChanges();

    const element: HTMLElement = fixture.nativeElement;
    expect(element.querySelector('h3')?.textContent).toContain('Nessun desiderio');
    expect(element.querySelector('p')?.textContent).toContain('Inizia subito!');
  });

  it('omits the hint paragraph when no text is given', () => {
    fixture.componentRef.setInput('title', 'Vuoto');
    fixture.detectChanges();

    expect((fixture.nativeElement as HTMLElement).querySelector('p')).toBeNull();
  });
});
