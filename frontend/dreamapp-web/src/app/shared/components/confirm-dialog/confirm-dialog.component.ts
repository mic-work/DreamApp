import { Component, EventEmitter, Input, Output } from '@angular/core';

@Component({
  selector: 'app-confirm-dialog',
  standalone: true,
  templateUrl: './confirm-dialog.component.html',
  styleUrl: './confirm-dialog.component.scss'
})
export class ConfirmDialogComponent {
  @Input() open = false;
  @Input() title = 'Sei sicuro?';
  @Input() message = '';
  @Input() confirmLabel = 'ELIMINA';
  @Input() cancelLabel = 'ANNULLA';

  @Output() confirmed = new EventEmitter<void>();
  @Output() cancelled = new EventEmitter<void>();
}
