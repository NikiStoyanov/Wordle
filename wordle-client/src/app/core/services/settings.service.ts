import { Injectable, signal } from '@angular/core';

export const AVAILABLE_WORD_LENGTHS = [3, 4, 5, 6, 7, 8] as const;
export type WordLength = (typeof AVAILABLE_WORD_LENGTHS)[number];
export const DEFAULT_WORD_LENGTH: WordLength = 5;

const STORAGE_KEY = 'wordle_word_length';

@Injectable({
  providedIn: 'root',
})
export class SettingsService {
  readonly availableLengths = AVAILABLE_WORD_LENGTHS;
  readonly wordLength = signal<WordLength>(this.loadInitialWordLength());
  readonly isSettingsModalOpen = signal<boolean>(false);

  setWordLength(length: number): void {
    if (this.isValidWordLength(length)) {
      this.wordLength.set(length);
      try {
        localStorage.setItem(STORAGE_KEY, length.toString());
      } catch {
        // Handle environments where localStorage is restricted
      }
    }
  }

  openSettings(): void {
    this.isSettingsModalOpen.set(true);
  }

  closeSettings(): void {
    this.isSettingsModalOpen.set(false);
  }

  toggleSettings(): void {
    this.isSettingsModalOpen.update((open) => !open);
  }

  private loadInitialWordLength(): WordLength {
    try {
      const stored = localStorage.getItem(STORAGE_KEY);
      if (stored) {
        const parsed = parseInt(stored, 10);
        if (this.isValidWordLength(parsed)) {
          return parsed;
        }
      }
    } catch {
      // Fallback
    }
    return DEFAULT_WORD_LENGTH;
  }

  private isValidWordLength(length: number): length is WordLength {
    return (AVAILABLE_WORD_LENGTHS as readonly number[]).includes(length);
  }
}
