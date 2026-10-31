import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { of, throwError } from 'rxjs';
import { vi } from 'vitest';
import { Board } from './board';
import { AuthService } from '../../../core/services/auth.service';
import { GameService } from '../../../core/services/game.service';
import { SettingsService } from '../../../core/services/settings.service';
import { DailyChallengeResultDto } from '../../../core/models/game';

describe('Board Component', () => {
  let component: Board;
  let fixture: ComponentFixture<Board>;
  let gameService: GameService;
  let authService: AuthService;
  let settingsService: SettingsService;

  beforeEach(async () => {
    localStorage.clear();
    await TestBed.configureTestingModule({
      imports: [Board],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
      ],
    }).compileComponents();

    gameService = TestBed.inject(GameService);
    authService = TestBed.inject(AuthService);
    settingsService = TestBed.inject(SettingsService);

    fixture = TestBed.createComponent(Board);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  afterEach(() => {
    localStorage.clear();
  });

  it('should create board with 6 rows and 5 empty tiles per row by default', () => {
    expect(component).toBeTruthy();
    expect(component.boardState.length).toBe(6);
    expect(component.boardState[0].tiles.length).toBe(5);
  });

  it('should adapt board tiles count when word length is changed in settings', () => {
    component.setWordLength(6);
    expect(component.wordLength()).toBe(6);
    expect(component.boardState[0].tiles.length).toBe(6);

    component.setWordLength(3);
    expect(component.wordLength()).toBe(3);
    expect(component.boardState[0].tiles.length).toBe(3);

    component.setWordLength(8);
    expect(component.wordLength()).toBe(8);
    expect(component.boardState[0].tiles.length).toBe(8);
  });

  it('should respond to physical keyboard letter inputs and backspace', () => {
    window.dispatchEvent(new KeyboardEvent('keydown', { key: 'a' }));
    window.dispatchEvent(new KeyboardEvent('keydown', { key: 'B' }));
    window.dispatchEvent(new KeyboardEvent('keydown', { key: 'c' }));

    expect(component.boardState[0].tiles[0].letter).toBe('A');
    expect(component.boardState[0].tiles[1].letter).toBe('B');
    expect(component.boardState[0].tiles[2].letter).toBe('C');
    expect(component.currentTileIndex).toBe(3);

    // Backspace deletes last letter
    window.dispatchEvent(new KeyboardEvent('keydown', { key: 'Backspace' }));
    expect(component.boardState[0].tiles[2].letter).toBe('');
    expect(component.currentTileIndex).toBe(2);
  });

  it('should reject attempt when fewer than expected letters are entered and trigger shake', () => {
    window.dispatchEvent(new KeyboardEvent('keydown', { key: 'w' }));
    window.dispatchEvent(new KeyboardEvent('keydown', { key: 'o' }));
    window.dispatchEvent(new KeyboardEvent('keydown', { key: 'r' }));

    window.dispatchEvent(new KeyboardEvent('keydown', { key: 'Enter' }));

    expect(component.toastMessage()).toBe('Not enough letters');
    expect(component.isShakingRow()).toBe(0);
    expect(component.currentRowIndex).toBe(0); // Did not advance
  });

  it('should prompt to login if unauthenticated when submitting guess', () => {
    ['S', 'P', 'A', 'R', 'K'].forEach((l) =>
      window.dispatchEvent(new KeyboardEvent('keydown', { key: l }))
    );

    vi.spyOn(authService, 'isAuthenticated').mockReturnValue(false);

    window.dispatchEvent(new KeyboardEvent('keydown', { key: 'Enter' }));

    expect(component.toastMessage()).toContain('Please log in');
    expect(component.currentRowIndex).toBe(0);
  });

  it('should submit guess and apply evaluations on server success', () => {
    ['S', 'P', 'A', 'R', 'K'].forEach((l) =>
      window.dispatchEvent(new KeyboardEvent('keydown', { key: l }))
    );

    vi.spyOn(authService, 'isAuthenticated').mockReturnValue(true);

    const mockResult: DailyChallengeResultDto = {
      isWon: true,
      isGameOver: true,
      attemptsUsed: 1,
      evaluations: [
        { letter: 'S', state: 'Correct' },
        { letter: 'P', state: 'Correct' },
        { letter: 'A', state: 'Correct' },
        { letter: 'R', state: 'Correct' },
        { letter: 'K', state: 'Correct' },
      ],
    };

    vi.spyOn(gameService, 'submitGuess').mockReturnValue(of(mockResult));

    window.dispatchEvent(new KeyboardEvent('keydown', { key: 'Enter' }));

    expect(component.boardState[0].tiles[0].state).toBe('correct');
    expect(component.getKeyState('S')).toBe('correct');
    expect(component.isWon()).toBe(true);
    expect(component.isGameOver()).toBe(true);
    expect(component.currentRowIndex).toBe(1);
  });

  it('should shake row and show error toast when word is not in dictionary', () => {
    ['Z', 'Z', 'Z', 'Z', 'Z'].forEach((l) =>
      window.dispatchEvent(new KeyboardEvent('keydown', { key: l }))
    );

    vi.spyOn(authService, 'isAuthenticated').mockReturnValue(true);
    vi.spyOn(gameService, 'submitGuess').mockReturnValue(
      throwError(() => ({
        error: { message: 'Word does not exist in dictionary.' },
      }))
    );

    window.dispatchEvent(new KeyboardEvent('keydown', { key: 'Enter' }));

    expect(component.toastMessage()).toBe('Word does not exist in dictionary.');
    expect(component.isShakingRow()).toBe(0);
    expect(component.currentRowIndex).toBe(0);
  });
});
