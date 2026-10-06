import { Component, EventEmitter, HostListener, Input, Output } from '@angular/core';
import { IconComponent } from '../icon/icon.component';

/** Simple message popup with a single close button (Holographic look, like the other popups). */
@Component({
  selector: 'app-info-dialog',
  standalone: true,
  imports: [IconComponent],
  templateUrl: './info-dialog.component.html',
  styleUrl: './info-dialog.component.scss'
})
export class InfoDialogComponent {
  @Input() open = false;
  @Input() title = '';
  @Input() message = '';
  @Input() closeLabel = 'CHIUDI';

  @Output() closed = new EventEmitter<void>();

  @HostListener('document:keydown.escape')
  onEscape(): void {
    if (this.open) {
      this.closed.emit();
    }
  }
}
