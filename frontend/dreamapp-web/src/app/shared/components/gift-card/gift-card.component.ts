import { Component, EventEmitter, Input, Output } from '@angular/core';
import { ImageService } from '../../../core/services/image.service';
import { Gift } from '../../../models/gift.model';
import { PriorityBadgeComponent } from '../priority-badge/priority-badge.component';

@Component({
  selector: 'app-gift-card',
  standalone: true,
  imports: [PriorityBadgeComponent],
  templateUrl: './gift-card.component.html',
  styleUrl: './gift-card.component.scss'
})
export class GiftCardComponent {
  @Input({ required: true }) gift!: Gift;
  /** When true, hides the edit/delete actions (used for a friend's read-only wishlist). */
  @Input() readonly = false;

  @Output() edit = new EventEmitter<Gift>();
  @Output() remove = new EventEmitter<Gift>();

  constructor(private readonly imageService: ImageService) {}

  get imageUrl(): string | null {
    return this.imageService.resolveUrl(this.gift.imageUrl);
  }
}
