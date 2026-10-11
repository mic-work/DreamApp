import { Component, Input } from '@angular/core';
import { IconComponent, IconName } from '../icon/icon.component';

/**
 * Friendly "nothing here yet" block: a floating iridescent badge with an icon,
 * a title and a one-line hint. Projected content (usually a button) goes
 * underneath. Replaces bare grey sentences so empty lists feel intentional.
 */
@Component({
  selector: 'app-empty-state',
  standalone: true,
  imports: [IconComponent],
  templateUrl: './empty-state.component.html',
  styleUrl: './empty-state.component.scss'
})
export class EmptyStateComponent {
  @Input() icon: IconName = 'gift';
  @Input({ required: true }) title!: string;
  @Input() text?: string;
  /** Smaller variant for tab panels inside a page that already has a header. */
  @Input() compact = false;
}
