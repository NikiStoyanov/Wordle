import { Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { AuthService } from '../../services/auth.service';
import { SettingsService, WordLength } from '../../services/settings.service';

@Component({
  selector: 'app-header',
  standalone: true,
  imports: [RouterLink],
  styleUrl: './header.css',
  templateUrl: './header.html',
})
export class Header {
  private readonly authService = inject(AuthService);
  readonly settingsService = inject(SettingsService);

  readonly isAuthenticated = this.authService.isAuthenticated;
  readonly currentUser = this.authService.currentUser;
  readonly currentWordLength = this.settingsService.wordLength;
  readonly isSettingsOpen = this.settingsService.isSettingsModalOpen;
  readonly availableLengths = this.settingsService.availableLengths;

  logout(): void {
    this.authService.logout();
  }

  toggleSettings(): void {
    this.settingsService.toggleSettings();
  }

  closeSettings(): void {
    this.settingsService.closeSettings();
  }

  selectWordLength(length: WordLength): void {
    this.settingsService.setWordLength(length);
  }
}
