import { Component, Input } from '@angular/core';
import { GiftPriority } from '../../../models/gift.model';

@Component({
  selector: 'app-priority-badge',
  standalone: true,
  templateUrl: './priority-badge.component.html',
  styleUrl: './priority-badge.component.scss'
})
export class PriorityBadgeComponent {
  @Input({ required: true }) priority!: GiftPriority;

  get stars(): string {
    switch (this.priority) {
      case 'High':
        return '⭐⭐⭐';
      case 'Medium':
        return '⭐⭐';
      default:
        return '⭐';
    }
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
