import {
  Component,
  HostListener,
  OnInit,
  computed,
  effect,
  inject,
  signal,
} from '@angular/core';
import { RouterLink } from '@angular/router';
import {
  LetterState,
  Row,
  mapServerStateToLetterState,
} from '../../../core/models/game';
import { GameService } from '../../../core/services/game.service';
import { AuthService } from '../../../core/services/auth.service';
import { SettingsService, WordLength } from '../../../core/services/settings.service';

@Component({
  selector: 'app-board',
  standalone: true,
  imports: [RouterLink],
  styleUrl: './board.css',
  templateUrl: './board.html',
})
export class Board implements OnInit {
  private readonly MAX_ROWS = 6;

  private readonly gameService = inject(GameService);
  readonly authService = inject(AuthService);
  readonly settingsService = inject(SettingsService);

  readonly wordLength = computed(() => this.settingsService.wordLength());
  readonly availableLengths = this.settingsService.availableLengths;

  boardState: Row[] = [];
  currentRowIndex = 0;
  currentTileIndex = 0;

  readonly isLoading = signal(false);
  readonly isGameOver = signal(false);
  readonly isWon = signal(false);
  readonly toastMessage = signal<string | null>(null);
  readonly isShakingRow = signal<number | null>(null);

  readonly keyboardStates = new Map<string, LetterState>();

  private toastTimeout: any;

  constructor() {
    this.initializeBoard(this.wordLength());

    effect(() => {
      const len = this.wordLength();
      if (this.boardState.length > 0 && this.boardState[0].tiles.length !== len) {
        this.handleWordLengthChange(len);
      }
    });
  }

  ngOnInit(): void {
    if (this.authService.isAuthenticated()) {
      this.loadTodayChallenge(this.wordLength());
    }
  }

  setWordLength(length: WordLength): void {
    this.settingsService.setWordLength(length);
    this.handleWordLengthChange(length);
  }

  @HostListener('window:keydown', ['$event'])
  handleKeyboardEvent(event: KeyboardEvent): void {
    if (event.ctrlKey || event.metaKey || event.altKey) {
      return;
    }

    const target = event.target as HTMLElement | null;
    if (
      target &&
      (target.tagName === 'INPUT' ||
        target.tagName === 'TEXTAREA' ||
        target.isContentEditable)
    ) {
      return;
    }

    const key = event.key;

    if (key === 'Enter') {
      event.preventDefault();
      this.onKeyPress('ENTER');
    } else if (key === 'Backspace' || key === 'Delete') {
      event.preventDefault();
      this.onKeyPress('DEL');
    } else if (/^[a-zA-Z]$/.test(key)) {
      event.preventDefault();
      this.onKeyPress(key.toUpperCase());
    }
  }

  onKeyPress(key: string): void {
    if (this.isGameOver() || this.isLoading()) {
      return;
    }

    if (key === 'ENTER') {
      this.submitGuess();
    } else if (key === 'DEL') {
      this.deleteLetter();
    } else if (/^[A-Z]$/.test(key)) {
      this.addLetter(key);
    }
  }

  getKeyState(key: string): string {
    return this.keyboardStates.get(key) || '';
  }

  private handleWordLengthChange(length: number): void {
    this.keyboardStates.clear();
    this.currentRowIndex = 0;
    this.currentTileIndex = 0;
    this.isGameOver.set(false);
    this.isWon.set(false);
    this.toastMessage.set(null);
    this.initializeBoard(length);

    if (this.authService.isAuthenticated()) {
      this.loadTodayChallenge(length);
    }
  }

  private initializeBoard(length: number): void {
    this.boardState = Array.from({ length: this.MAX_ROWS }, () => ({
      tiles: Array.from({ length }, () => ({
        letter: '',
        state: 'empty',
      })),
    }));
  }

  private addLetter(letter: string): void {
    const maxLen = this.wordLength();
    if (
      this.currentTileIndex < maxLen &&
      this.currentRowIndex < this.MAX_ROWS
    ) {
      const tile = this.boardState[this.currentRowIndex].tiles[this.currentTileIndex];
      tile.letter = letter;
      this.currentTileIndex++;
    }
  }

  private deleteLetter(): void {
    if (this.currentTileIndex > 0) {
      this.currentTileIndex--;
      const tile = this.boardState[this.currentRowIndex].tiles[this.currentTileIndex];
      tile.letter = '';
    }
  }

  private submitGuess(): void {
    if (this.currentRowIndex >= this.MAX_ROWS) {
      return;
    }

    const targetLength = this.wordLength();

    // 1. Client-side length validation
    if (this.currentTileIndex !== targetLength) {
      this.triggerRowShake(this.currentRowIndex);
      this.showToast('Not enough letters');
      return;
    }

    // 2. Auth check - Daily Challenge requires login
    if (!this.authService.isAuthenticated()) {
      this.triggerRowShake(this.currentRowIndex);
      this.showToast('Please log in or sign up to play the daily challenge!');
      return;
    }

    const currentWord = this.boardState[this.currentRowIndex].tiles
      .map((t) => t.letter)
      .join('');

    this.isLoading.set(true);

    this.gameService.submitGuess(currentWord, targetLength).subscribe({
      next: (result) => {
        this.isLoading.set(false);

        // Apply evaluations to current row
        result.evaluations.forEach((evalItem, index) => {
          if (index < targetLength) {
            const state = mapServerStateToLetterState(evalItem.state);
            this.boardState[this.currentRowIndex].tiles[index].state = state;
            this.updateKeyboardState(evalItem.letter, state);
          }
        });

        if (result.isWon) {
          this.isWon.set(true);
          this.isGameOver.set(true);
          this.showToast('Splendid! 🎉', 3500);
        } else if (result.isGameOver) {
          this.isGameOver.set(true);
          const answer = result.correctWord ? `The word was ${result.correctWord}` : 'Game Over!';
          this.showToast(answer, 4000);
        }

        this.currentRowIndex++;
        this.currentTileIndex = 0;
      },
      error: (err) => {
        this.isLoading.set(false);
        this.triggerRowShake(this.currentRowIndex);
        const errorMsg =
          err?.error?.message ||
          err?.error?.Message ||
          'Not in word list';
        this.showToast(errorMsg);
      },
    });
  }

  private updateKeyboardState(letter: string, newState: LetterState): void {
    const key = letter.toUpperCase();
    const currentState = this.keyboardStates.get(key);

    if (currentState === 'correct') {
      return; // Already highest priority
    }
    if (currentState === 'present' && newState === 'absent') {
      return; // Don't downgrade present to absent
    }

    this.keyboardStates.set(key, newState);
  }

  private loadTodayChallenge(length: number): void {
    this.isLoading.set(true);
    this.gameService.getTodayChallenge(length).subscribe({
      next: (status) => {
        this.isLoading.set(false);

        if (status.previousGuesses && status.previousGuesses.length > 0) {
          status.previousGuesses.forEach((guessDto, rowIndex) => {
            if (rowIndex < this.MAX_ROWS) {
              guessDto.evaluations.forEach((evalItem, tileIndex) => {
                if (tileIndex < length) {
                  const state = mapServerStateToLetterState(evalItem.state);
                  this.boardState[rowIndex].tiles[tileIndex] = {
                    letter: evalItem.letter.toUpperCase(),
                    state: state,
                  };
                  this.updateKeyboardState(evalItem.letter, state);
                }
              });
            }
          });

          this.currentRowIndex = status.previousGuesses.length;
          this.currentTileIndex = 0;
        }

        if (status.isGameOver) {
          this.isGameOver.set(true);
          this.isWon.set(status.isSolved);
          if (status.isSolved) {
            this.showToast('You already solved today’s challenge! 🏆', 3000);
          } else if (status.correctWord) {
            this.showToast(`Today’s word was ${status.correctWord}`, 3000);
          }
        }
      },
      error: () => {
        this.isLoading.set(false);
      },
    });
  }

  private triggerRowShake(rowIndex: number): void {
    this.isShakingRow.set(rowIndex);
    setTimeout(() => {
      if (this.isShakingRow() === rowIndex) {
        this.isShakingRow.set(null);
      }
    }, 600);
  }

  private showToast(message: string, durationMs = 2000): void {
    if (this.toastTimeout) {
      clearTimeout(this.toastTimeout);
    }
    this.toastMessage.set(message);
    this.toastTimeout = setTimeout(() => {
      this.toastMessage.set(null);
    }, durationMs);
  }
}
