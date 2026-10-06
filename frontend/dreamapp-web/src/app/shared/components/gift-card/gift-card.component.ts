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

  /**
   * The wishlist owner's username, for building a readable
   * /wishlist/{username}/gifts/{id} link. Only needed when showing a
   * FRIEND's gift (gift.isOwner is false) — for the caller's own gifts the
   * link always uses the literal 'me' segment, same as the rest of the app
   * (/wishlist/me), so callers rendering only own gifts (e.g. MyWishlistComponent)
   * can omit it.
   */
  @Input() username?: string;

  /**
   * 'holo' = the "Holographic" look used by the Home / My wishlist pages
   * (gradient border + round gift icon + readable priority line). Every other
   * caller keeps the default look, so a friend's wishlist is unchanged.
   */
  @Input() variant: 'default' | 'holo' = 'default';

  get giftRoute(): string[] {
    return ['/wishlist', this.gift.isOwner ? 'me' : this.username ?? 'me', 'gifts', this.gift.id];
  }
}
