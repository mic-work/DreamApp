import { Component, Input } from '@angular/core';
import { GiftPriority } from '../../../models/gift.model';
import { IconComponent } from '../icon/icon.component';

@Component({
  selector: 'app-priority-badge',
  standalone: true,
  imports: [IconComponent],
  templateUrl: './priority-badge.component.html',
  styleUrl: './priority-badge.component.scss'
})
export class PriorityBadgeComponent {
  @Input({ required: true }) priority!: GiftPriority;

  /** Text-only "Priorità alta" style used by the Holographic list rows. */
  @Input() holo = false;

  get starCount(): number {
    switch (this.priority) {
      case 'High':
        return 3;
      case 'Medium':
        return 2;
      default:
        return 1;
    }
  }

  get stars(): number[] {
    return Array.from({ length: this.starCount });
  }

  get label(): string {
    switch (this.priority) {
      case 'High':
        return 'Alta';
      case 'Medium':
        return 'Media';
      default:
        return 'Bassa';
    }
  }
}
