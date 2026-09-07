import { Location } from '@angular/common';
import { Component, Input } from '@angular/core';
import { Router } from '@angular/router';
import { IconComponent } from '../icon/icon.component';

/**
 * Back chevron + title, used at the top of every "drill-in" screen (a
 * friend's wishlist, add/edit gift, gift detail, add friend) that isn't one
 * of the primary bottom-tab destinations — so there's always a visible,
 * predictable way back, regardless of how the page was reached.
 *
 * Pass `backTo` when the parent screen is fixed/known (e.g. add-gift always
 * comes from "my wishlist"). Omit it to fall back to the browser history
 * (used by gift-detail, which can be reached from either "my wishlist" or a
 * friend's wishlist and should return to whichever one it came from).
 */
@Component({
  selector: 'app-page-header',
  standalone: true,
  imports: [IconComponent],
  templateUrl: './page-header.component.html',
  styleUrl: './page-header.component.scss'
})
export class PageHeaderComponent {
  @Input({ required: true }) title!: string;
  @Input() backTo?: string;

  constructor(
    private readonly router: Router,
    private readonly location: Location
  ) {}

  goBack(): void {
    if (this.backTo) {
      this.router.navigateByUrl(this.backTo);
    } else {
      this.location.back();
    }
  }
}
