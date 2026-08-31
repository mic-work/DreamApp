import { DatePipe } from '@angular/common';
import { Component } from '@angular/core';
import { AuthService } from '../../core/services/auth.service';

@Component({
  selector: 'app-profile',
  standalone: true,
  imports: [DatePipe],
  templateUrl: './profile.component.html',
  styleUrl: './profile.component.scss'
})
export class ProfileComponent {
  constructor(readonly authService: AuthService) {}

  logout(): void {
    this.authService.logout();
  }
}
