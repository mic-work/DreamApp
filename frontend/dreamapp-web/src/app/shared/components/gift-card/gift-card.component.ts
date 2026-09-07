import { Component, Input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { Gift } from '../../../models/gift.model';
import { IconComponent } from '../icon/icon.component';
import { PriorityBadgeComponent } from '../priority-badge/priority-badge.component';

/**
 * Compact, text-only row used in list views (my wishlist, a friend's
 * wishlist). Deliberately shows no photo — "at a glance" the list is just
 * names + priority; tapping a row opens GiftDetailComponent, which is where
 * the photo, description, notes and link live.
 */
@Component({
  selector: 'app-gift-card',
  standalone: true,
  imports: [RouterLink, IconComponent, PriorityBadgeComponent],
  templateUrl: './gift-card.component.html',
  styleUrl: './gift-card.component.scss'
})
export class GiftCardComponent {
  @Input({ required: true }) gift!: Gift;
}
