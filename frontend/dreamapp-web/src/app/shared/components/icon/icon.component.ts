import { Component, Input } from '@angular/core';

export type IconName =
  | 'home'
  | 'gift'
  | 'user'
  | 'heart'
  | 'chevron-left'
  | 'chevron-right'
  | 'plus'
  | 'trash'
  | 'pencil'
  | 'star'
  | 'star-filled'
  | 'link'
  | 'camera'
  | 'dots-vertical'
  | 'check'
  | 'x'
  | 'logout'
  | 'search'
  | 'note';

/**
 * Small inline-SVG icon set (outline style, 24x24, currentColor) used
 * across the app instead of emoji for anything that conveys an action or
 * meaning (navigation, buttons, badges). Emoji are still fine as
 * decorative accents in copy/headings.
 */
@Component({
  selector: 'app-icon',
  standalone: true,
  templateUrl: './icon.component.html',
  styleUrl: './icon.component.scss'
})
export class IconComponent {
  @Input({ required: true }) name!: IconName;
  @Input() size = 22;
}
